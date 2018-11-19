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
        private const string ADD_NEW_CONFIGS_COMMAND = "addNewConfigs";
        private const string EXCHANGE_CONFIGS_COMMAND = "exchangeConfigs";

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
        private string addNewConfigsSamplingTask;

        /// <summary>
        /// If set to true, new configurations are added at the beginning of every active learning round.
        /// </summary>
        private bool shouldAddNewConfigurations = false;

        /// <summary>
        /// If set to true, configurations are exchanged between the learning set and the validation set.
        /// Also performs one additional learning in an active learning round.
        /// </summary>
        private bool shouldExchangeConfigurations = false;

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
                    case ADD_NEW_CONFIGS_COMMAND:
                        addNewConfigsSamplingTask = string.Join(" ", taskParameters);
                        shouldAddNewConfigurations = true;
                        break;
                    case EXCHANGE_CONFIGS_COMMAND:
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
                            shouldExchangeConfigurations = true;
                        }
                        else
                        {
                            GlobalState.logError.logLine("Invalid exchange strategy: " + tokens[1]);
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

        private static void printConfigs(List<Configuration> configs)
        {
            foreach (string s in configs.Select(config => config.ToString()).OrderBy(config => config))
            {
                Console.WriteLine(s);
            }
        }

        /// <summary>
        /// Learns a model using multiple rounds of <see cref="Learning"/>.
        /// </summary>
        /// <param name="parameters">The parameters for the 'active-learn-splconqueror' command</param>
        public void learn(string[] parameters)
        {
            if (!PrepareActiveLearning(parameters, out List<Configuration> learningSet,
                out List<Configuration> validationSet)) return;
            Console.WriteLine("initial learning set");
            printConfigs(learningSet);

            // learn initial model
            currentRound = 1;
            List<Feature> currentModel = null;
            LearnNewModel(learningSet, validationSet, ref currentModel);

            while (!shouldAbortActiveLearning())
            {
                currentRound++;
                if (shouldAddNewConfigurations)
                {
                    if (currentRound == 2)
                    {
                        configBuilder.clear();
                        configBuilder.performOneCommand(addNewConfigsSamplingTask);
                    }
                    else
                    {
                        configBuilder.binaryParams.updateSeeds();
                    }
                    List<Configuration> configsForNextRun = GetConfigsForNextRun(learningSet, validationSet, currentModel);
                    learningSet.AddRange(configsForNextRun);
                }
                if (shouldAddNewConfigurations && shouldExchangeConfigurations)
                {
                    LearnNewModel(learningSet, validationSet, ref currentModel);
                    if (shouldAbortActiveLearning()) break;
                }
                if (shouldExchangeConfigurations)
                {
                    exchangeStrategy.exchangeConfigurations(learningSet, validationSet, currentModel);
                }
                LearnNewModel(learningSet, validationSet, ref currentModel);
            }
        }

        private List<Configuration> GetConfigsForNextRun(List<Configuration> learningSet, List<Configuration> validationSet, List<Feature> currentModel)
        {
            List<Configuration> existingConfigurations = new List<Configuration>(learningSet);
            foreach (Configuration config in validationSet)
            {
                if (!existingConfigurations.Contains(config))
                {
                    existingConfigurations.Add(config);
                }
            }
            configBuilder.existingConfigurations = existingConfigurations;
            int maxNumberOfConfigs = (int) Math.Round(0.1 * learningSet.Count);
            List<Configuration> badConfigs =
                SortedConfigsByError(learningSet, currentModel).Take(maxNumberOfConfigs).ToList();
            if (badConfigs.Count == 0)
            {
                throw new Exception("learning set is to small");
            }
            Dictionary<BinaryOption, List<int>> matrix = new Dictionary<BinaryOption, List<int>>();
            foreach (Configuration badConfig in badConfigs)
            {
                foreach (BinaryOption binaryOption in GlobalState.varModel.BinaryOptions)
                {
                    if (!binaryOption.Optional && !binaryOption.hasAlternatives()) continue;
                    int entry = badConfig.BinaryOptions.ContainsKey(binaryOption)
                        && badConfig.BinaryOptions[binaryOption] == BinaryOption.BinaryValue.Selected
                            ? 1
                            : 0;
                    if (matrix.ContainsKey(binaryOption))
                    {
                        matrix[binaryOption].Add(entry);
                    }
                    else
                    {
                        matrix[binaryOption] = new List<int> {entry};
                    }
                }
            }
            List<Tuple<BinaryOption, int>> optionsSortedByOccurrence =
                matrix.Select(pair => new Tuple<BinaryOption, int>(pair.Key, pair.Value.Sum()))
                    .OrderByDescending(tuple => tuple.Item2)
                    .ToList();
            Tuple<BinaryOption, int> first = optionsSortedByOccurrence.First();
            List<BinaryOption> maximalOptions = optionsSortedByOccurrence.TakeWhile(tuple => tuple.Item2 == first.Item2).Select(tuple => tuple.Item1).ToList();
            Console.WriteLine("matrix");
            foreach (KeyValuePair<BinaryOption, List<int>> pair in matrix.OrderBy(pair => pair.Key.Name))
            {
                if (pair.Value.Sum() == first.Item2)
                {
                    Console.WriteLine(pair.Key.Name.PadLeft(20) + ": " + string.Join(" ", pair.Value) + " <-");
                }
                else
                {
                    Console.WriteLine(pair.Key.Name.PadLeft(20) + ": " + string.Join(" ", pair.Value));
                }
            }

            List<Configuration> result = new List<Configuration>();
            Console.WriteLine("new configs");
            foreach (BinaryOption maximalOption in maximalOptions)
            {
                List<BinaryOption> whiteList = new List<BinaryOption> {maximalOption};
                // TODO List<BinaryOption> blackList = maximalOptions.Where(c => !c.Equals(maximalOption)).ToList();
                List<BinaryOption> blackList = new List<BinaryOption>();
                List<Configuration> newConfigs = configBuilder.buildConfigs(GlobalState.varModel, whiteList, blackList);
                configBuilder.existingConfigurations.AddRange(newConfigs);
                result.AddRange(newConfigs);
                Console.WriteLine("for " + maximalOption);
                printConfigs(newConfigs);
            }
            return result;
        }

        private IEnumerable<Configuration> SortedConfigsByError(List<Configuration> configs, List<Feature> model)
        {
            List<Tuple<Configuration, double>> list = new List<Tuple<Configuration, double>>();
            foreach (Configuration c in configs)
            {
                double estimatedValue = FeatureSubsetSelection.estimate(model, c);
                double realValue = c.GetNFPValue(GlobalState.currentNFP);
                double error = 0;
                switch (mlSettings.lossFunction)
                {
                    case ML_Settings.LossFunction.RELATIVE:
                        error = Math.Abs((estimatedValue - realValue) / realValue) * 100;
                        break;
                    case ML_Settings.LossFunction.LEASTSQUARES:
                        error = Math.Pow(realValue - estimatedValue, 2);
                        break;
                    case ML_Settings.LossFunction.ABSOLUTE:
                        error = Math.Abs(realValue - estimatedValue);
                        break;
                }
                list.Add(new Tuple<Configuration, double>(c, error));
            }
            Console.WriteLine("predictions");
            foreach (Tuple<Configuration, double> tuple in list.OrderBy(tuple => tuple.Item1.ToString()))
            {
                Console.WriteLine(tuple.Item1 + " -> " + tuple.Item2);
            }
            return list.OrderByDescending(tuple => tuple.Item2).Select(tuple => tuple.Item1);
        }

        private bool PrepareActiveLearning(string[] parameters, out List<Configuration> learningSet,
            out List<Configuration> validationSet)
        {
            bool shouldProceed = parseActiveLearningParameters(parameters);
            if (!shouldProceed)
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
            GlobalState.logInfo.logLine("Learning set: " + learningSet.Count + ", Validation set: "
                + validationSet.Count);
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
            previousRelativeError = currentRelativeError;
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
            if (improvement < mlSettings.minImprovementPerActiveLearningRound)
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
