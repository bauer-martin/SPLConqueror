using System;
using System.Collections.Generic;
using System.Linq;
using MachineLearning.Learning.Regression.ExchangeStrategies;
using MachineLearning.Sampling;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression
{
    public enum ConfigurationExchangeStrategies
    {
        NONE,
        PERFORMANCE,
        PAIRWISE_DISTANCE,
        MOST_SIMILAR_PERFORMANCE
    }

    public class ActiveLearning
    {
        private const string SAMPLE_COMMAND = "sample";
        private const string EXCHANGE_COMMAND = "exchange";

        private static readonly Dictionary<string, ConfigurationExchangeStrategies> strategiesByName =
            new Dictionary<string, ConfigurationExchangeStrategies>
            {
                {"none", ConfigurationExchangeStrategies.NONE},
                {"performance", ConfigurationExchangeStrategies.PERFORMANCE},
                {"pairwiseDistance", ConfigurationExchangeStrategies.PAIRWISE_DISTANCE},
                {"mostSimilarPerformance", ConfigurationExchangeStrategies.MOST_SIMILAR_PERFORMANCE}
            };

        private readonly ML_Settings mlSettings = null;
        private readonly InfluenceModel influenceModel;
        private readonly ConfigurationBuilder configBuilder;

        /// <summary>
        /// The number of active learning rounds.
        /// </summary>
        private int currentRound = -1;

        /// <summary>
        /// The relative error of the previous active learning round.
        /// </summary>
        private double previousRelativeError = Double.MaxValue;

        /// <summary>
        /// The relative error of the current active learning round.
        /// </summary>
        private double currentRelativeError = Double.MaxValue;

        /// <summary>
        /// The global error of the current active learning round.
        /// </summary>
        private double currentGlobalError = Double.MaxValue;

        /// <summary>
        /// The sampling task for switching the sampling strategy to find new configurations.
        /// </summary>
        private string samplingTask;

        /// <summary>
        /// The strategy to exchange configurations after an active learning round.
        /// </summary>
        private ConfigurationExchangeStrategy exchangeStrategy = new NoOpExchangeStrategy();

        public ActiveLearning(ML_Settings mlSettings, InfluenceModel influenceModel, ConfigurationBuilder configBuilder)
        {
            this.mlSettings = mlSettings;
            this.influenceModel = influenceModel;
            this.configBuilder = configBuilder;
        }

        private bool parseActiveLearningParameters(string[] parameters)
        {
            foreach (string parameter in parameters)
            {
                string[] tokens = parameter.Split(' ');
                if (tokens.Length < 1 || tokens[0].Length < 1) continue;
                string task = tokens[0];
                string[] taskParameters = tokens.Skip(1).ToArray();
                switch (task)
                {
                    case SAMPLE_COMMAND:
                        samplingTask = string.Join(" ", taskParameters);
                        break;
                    case EXCHANGE_COMMAND:
                        string strategyName = taskParameters[0];
                        if (strategiesByName.ContainsKey(strategyName))
                        {
                            switch (strategiesByName[strategyName])
                            {
                                case ConfigurationExchangeStrategies.NONE:
                                    exchangeStrategy = new NoOpExchangeStrategy();
                                    break;
                                case ConfigurationExchangeStrategies.PERFORMANCE:
                                    exchangeStrategy = new PerformanceValueExchangeStrategy(mlSettings);
                                    break;
                                case ConfigurationExchangeStrategies.PAIRWISE_DISTANCE:
                                    exchangeStrategy = new PairwiseDistanceExchangeStrategy(mlSettings);
                                    break;
                                case ConfigurationExchangeStrategies.MOST_SIMILAR_PERFORMANCE:
                                    exchangeStrategy = new MostSimilarPerformanceExchangeStrategy(mlSettings);
                                    break;
                                default:
                                    return false;
                            }
                        }
                        else
                        {
                            GlobalState.logError.logLine("Invalid exchange strategy: " + tokens[1]
                                + " Using none instead.");
                            return false;
                        }
                        break;
                    default:
                        GlobalState.logError.logLine("Invalid parameter for active learning: " + parameter);
                        return false;
                }
            }
            return true;
        }

        private bool allInformationAvailable()
        {
            if (samplingTask == null)
            {
                GlobalState.logError.logLine("You need to specify a sampling strategy for active learning.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Learns a model using multiple rounds of <see cref="Learning"/>.
        /// </summary>
        /// <param name="parameters">The parameters for the 'active-learn-splconqueror' command</param>
        public void learn(string[] parameters)
        {
            if (!PrepareActiveLearning(parameters, out List<Configuration> learningSet,
                out List<Configuration> validationSet)) return;
            GlobalState.logInfo.logLine("Learning set: " + learningSet.Count + " Validation set:"
                + validationSet.Count);

            // learn initial model
            currentRound = 1;
            List<Feature> currentModel = null;
            LearnNewModel(learningSet, validationSet, ref currentModel);

            while (!shouldAbortActiveLearning())
            {
                currentRound++;
                if (currentRound == 2)
                {
                    configBuilder.clear();
                    configBuilder.performOneCommand(samplingTask);
                }
                else
                {
                    configBuilder.binaryParams.updateSeeds();
                }
                previousRelativeError = currentRelativeError;
                configBuilder.existingConfigurations = learningSet;
                List<Configuration> configsForNextRun = configBuilder.buildSet(mlSettings);
                learningSet.AddRange(configsForNextRun);
                LearnNewModel(learningSet, validationSet, ref currentModel);
                if (shouldAbortActiveLearning()) break;
                exchangeStrategy.exchangeConfigurations(learningSet, validationSet, currentModel);
                LearnNewModel(learningSet, validationSet, ref currentModel);
            }
        }

        private bool PrepareActiveLearning(string[] parameters, out List<Configuration> learningSet,
            out List<Configuration> validationSet)
        {
            bool shouldProceed = parseActiveLearningParameters(parameters);
            if (!shouldProceed || !allInformationAvailable())
            {
                learningSet = null;
                validationSet = null;
                return false;
            }
            Tuple<List<Configuration>, List<Configuration>> learnAndValidation =
                configBuilder.buildSetsEfficient(mlSettings);
            learningSet = learnAndValidation.Item1;
            validationSet = learnAndValidation.Item2;
            if (learningSet.Count == 0 && validationSet.Count == 0)
            {
                GlobalState.logInfo.logLine("The learning set is empty! Cannot start learning!");
                return false;
            }
            if (learningSet.Count == 0)
            {
                learningSet = validationSet;
            }
            else if (validationSet.Count == 0)
            {
                validationSet = learningSet;
            }
            return true;
        }

        private void LearnNewModel(List<Configuration> learningSet, List<Configuration> validationSet,
            ref List<Feature> currentModel)
        {
            Learning exp = new Learning(learningSet, validationSet)
            {
                metaModel = influenceModel,
                mlSettings = this.mlSettings
            };
            exp.learn(currentModel);
            if (exp.models.Count != 1)
            {
                GlobalState.logError.logLine("There should be exactly one learned model! Aborting active learning!");
                Environment.Exit(0);
            }
            FeatureSubsetSelection model = exp.models[0];
            currentModel = model.LearningHistory.Last().FeatureSet;
            currentRelativeError = model.finalError;
            currentGlobalError = model.computeError(currentModel, GlobalState.allMeasurements.Configurations, false);
            GlobalState.logInfo.logLine("globalError = " + currentGlobalError);
        }

        private bool shouldAbortActiveLearning()
        {
            if (currentRound >= mlSettings.maxNumberOfActiveLearningRounds)
            {
                GlobalState.logInfo.logLine(
                    "Aborting active learning because maximum number of rounds has been reached");
                return true;
            }

            double improvement = previousRelativeError - currentRelativeError;
            if (improvement > 0 && improvement < mlSettings.minImprovementPerActiveLearningRound)
            {
                GlobalState.logInfo.logLine(
                    "Aborting active learning because model did not achieve great improvement anymore");
                return true;
            }

            if (currentRelativeError < mlSettings.abortError)
            {
                GlobalState.logInfo.logLine("Aborting active learning because model is already good enough");
                return true;
            }

            return false;
        }
    }
}
