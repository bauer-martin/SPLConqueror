using System;
using System.Collections.Generic;
using System.Linq;
using MachineLearning.Learning.Regression.AdditionStrategies;
using MachineLearning.Learning.Regression.ExchangeStrategies;
using MachineLearning.Sampling;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression
{
    public enum ConfigurationAdditionStrategies
    {
        NONE,
        SIMPLE,
        MATRIX_MAX,
        MATRIX_VIC
    }

    public class ActiveLearning
    {
        private const string ADD_NEW_CONFIGS_COMMAND = "addNewConfigs";
        private const string EXCHANGE_CONFIGS_COMMAND = "exchangeConfigs";

        private static readonly Dictionary<string, ConfigurationAdditionStrategies> additionStrategiesByName =
            new Dictionary<string, ConfigurationAdditionStrategies>
            {
                {"none", ConfigurationAdditionStrategies.NONE},
                {"simple", ConfigurationAdditionStrategies.SIMPLE},
                {"matrixMax", ConfigurationAdditionStrategies.MATRIX_MAX},
                {"matrixVIC", ConfigurationAdditionStrategies.MATRIX_VIC}
            };

        private readonly ML_Settings mlSettings = null;
        private readonly InfluenceModel influenceModel;
        private readonly ConfigurationBuilder configBuilder;

        /// <summary>
        /// The number of active learning rounds.
        /// </summary>
        private int currentRound = -1;

        /// <summary>
        /// The validation error of the active learning round.
        /// </summary>
        private double previousValidationError = Double.MaxValue;
        private double currentValidationError = Double.MaxValue;

        /// <summary>
        /// The global error of the current active learning round.
        /// </summary>
        private double currentGlobalError = Double.MaxValue;

        private List<Configuration> previousValidationSet = null;
        private List<Configuration> currentValidationSet = null;

        private List<Configuration> previousLearningSet = null;
        private List<Configuration> currentLearningSet = null;

        private List<Feature> previousModel;
        private List<Feature> currentModel;

        /// <summary>
        /// The strategy to add new configurations after an active learning round.
        /// </summary>
        private ConfigurationAdditionStrategy additionStrategy = new NoOpAdditionStrategy();

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
                        string additionStrategyName = taskParameters[0];
                        if (additionStrategiesByName.ContainsKey(additionStrategyName))
                        {
                            switch (additionStrategiesByName[additionStrategyName])
                            {
                                case ConfigurationAdditionStrategies.NONE:
                                    additionStrategy = new NoOpAdditionStrategy();
                                    break;
                                case ConfigurationAdditionStrategies.SIMPLE:
                                    additionStrategy = new SimpleDistributionBasedAdditionStrategy(mlSettings,
                                        configBuilder, string.Join(" ", taskParameters, 1, taskParameters.Length - 1));
                                    break;
                                case ConfigurationAdditionStrategies.MATRIX_MAX:
                                    additionStrategy = new MatrixMaxAdditionStrategy(mlSettings,
                                        configBuilder, string.Join(" ", taskParameters, 1, taskParameters.Length - 1));
                                    break;
                                case ConfigurationAdditionStrategies.MATRIX_VIC:
                                    additionStrategy = new MatrixVICAdditionStrategy(mlSettings,
                                        configBuilder, string.Join(" ", taskParameters, 1, taskParameters.Length - 1));
                                    break;
                                default:
                                    return false;
                            }
                        }
                        else
                        {
                            GlobalState.logError.logLine("Invalid addition strategy: " + tokens[1]);
                            return false;
                        }
                        break;
                    case EXCHANGE_CONFIGS_COMMAND:
                        GlobalState.logError.logLine("Exchanging configurations is not supported at the moment!");
                        return false;
                    default:
                        GlobalState.logError.logLine("Invalid parameter for active learning: " + parameter);
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Learns a model using multiple rounds of <see cref="Learning"/>.
        /// </summary>
        /// <param name="parameters">The parameters for the 'active-learn-splconqueror' command</param>
        public void learn(string[] parameters)
        {
            if (!PrepareActiveLearning(parameters)) return;
            Console.WriteLine("initial learning set");
            foreach (string s in currentLearningSet.Select(config => config.ToString()).OrderBy(config => config))
            {
                Console.WriteLine(s);
            }

            // learn initial model
            currentRound = 1;
            LearnInitialModel();

            bool shouldAddNewConfigurations = !(additionStrategy is NoOpAdditionStrategy);

            while (!shouldAbortActiveLearning())
            {
                currentRound++;
                previousLearningSet = new List<Configuration>(currentLearningSet);
                previousValidationSet = new List<Configuration>(currentValidationSet);
                if (shouldAddNewConfigurations)
                {
                    List<Configuration> configsForNextRun = additionStrategy.FindNewConfigurations(currentLearningSet,
                        currentValidationSet, currentModel);
                    if (configsForNextRun.Count == 0)
                    {
                        GlobalState.logInfo.logLine(
                            "Aborting active learning because no new configurations can be found");
                        return;
                    }
                    currentLearningSet.AddRange(configsForNextRun);
                }
                LearnNewModel();
            }
        }

        private bool PrepareActiveLearning(string[] parameters)
        {
            bool shouldProceed = parseActiveLearningParameters(parameters);
            if (!shouldProceed)
            {
                currentLearningSet = null;
                currentValidationSet = null;
                return false;
            }
            Tuple<List<Configuration>, List<Configuration>> learnAndValidation =
                configBuilder.buildSetsEfficient(mlSettings);
            currentLearningSet = learnAndValidation.Item1;
            currentValidationSet = learnAndValidation.Item2;
            if (currentLearningSet.Count == 0 && currentValidationSet.Count == 0)
            {
                GlobalState.logInfo.logLine("The learning set is empty! Cannot start learning!");
                return false;
            }
            if (currentLearningSet.Count == 0)
            {
                currentLearningSet = currentValidationSet;
            }
            else if (currentValidationSet.Count == 0)
            {
                currentValidationSet = currentLearningSet;
            }
            return true;
        }

        private void LearnInitialModel()
        {
            GlobalState.logInfo.logLine("Learning set: " + currentLearningSet.Count + ", Validation set: "
                + currentValidationSet.Count);
            Learning exp = new Learning(currentLearningSet, currentValidationSet)
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
            FeatureSubsetSelection fss = exp.models[0];
            currentModel = fss.LearningHistory.Last().FeatureSet;
            currentValidationError = fss.finalError;
            currentGlobalError = fss.computeError(currentModel, GlobalState.allMeasurements.Configurations, false);
            GlobalState.logInfo.logLine("globalError = " + currentGlobalError);
        }

        private void LearnNewModel()
        {
            previousModel = new List<Feature>(currentModel);
            GlobalState.logInfo.logLine("Learning set: " + currentLearningSet.Count + ", Validation set: "
                + currentLearningSet.Count);
            Learning exp = new Learning(currentLearningSet, currentValidationSet)
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
            FeatureSubsetSelection fss = exp.models[0];
            currentModel = fss.LearningHistory.Last().FeatureSet;
            previousValidationError = currentValidationError;
            currentValidationError = fss.finalError;
            currentGlobalError = fss.computeError(currentModel, GlobalState.allMeasurements.Configurations, false);
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

            double improvement = previousValidationError - currentValidationError;
            if (improvement < mlSettings.minImprovementPerActiveLearningRound)
            {
                GlobalState.logInfo.logLine(
                    "Aborting active learning because model did not achieve great improvement anymore");
                return true;
            }

            if (currentValidationError < mlSettings.abortError)
            {
                GlobalState.logInfo.logLine("Aborting active learning because model is already good enough");
                return true;
            }

            return false;
        }
    }
}
