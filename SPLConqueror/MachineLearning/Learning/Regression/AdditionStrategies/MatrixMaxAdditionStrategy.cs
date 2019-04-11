using System;
using System.Collections.Generic;
using System.Linq;
using MachineLearning.Sampling;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.AdditionStrategies
{
    /// <summary>
    /// Select configurations with options leading to a high error rate.
    /// </summary>
    class MatrixMaxAdditionStrategy : SamplingBasedAdditionStrategy
    {
        public MatrixMaxAdditionStrategy(ML_Settings mlSettings, ConfigurationBuilder configBuilder,
            string sampleTask) : base(mlSettings, configBuilder, sampleTask)
        {
        }

        protected override List<Configuration> FindNewConfigurationsImpl(List<Configuration> learningSet,
            List<Configuration> validationSet, List<Feature> currentModel)
        {
            // exclude configurations from learning set and validation set
            List<Configuration> existingConfigurations = new List<Configuration>(learningSet);
            foreach (Configuration config in validationSet)
            {
                if (!existingConfigurations.Contains(config))
                {
                    existingConfigurations.Add(config);
                }
            }
            configBuilder.existingConfigurations = existingConfigurations;

            // find configurations with high error rates
            int maxNumberOfConfigs = (int) Math.Round(0.1 * validationSet.Count);
            List<Configuration> badConfigs = SortedConfigsByError(validationSet, currentModel)
                .Take(maxNumberOfConfigs).ToList();

            // arrange configurations in a table
            Dictionary<BinaryOption, List<int>> matrix = CreateMatrix(badConfigs);
            List<Configuration> result = new List<Configuration>();
            do
            {
                // the most common options selected in the matrix
                List<BinaryOption> mostCommonOptions = GetMostCommonOptions(matrix);
                foreach (BinaryOption mostCommonOption in mostCommonOptions)
                {
                    List<BinaryOption> desiredOptions = new List<BinaryOption> {mostCommonOption};
                    List<Configuration> newConfigs = configBuilder.buildConfigs(GlobalState.varModel, desiredOptions);
                    if (newConfigs.Count > 0)
                    {
                        configBuilder.existingConfigurations.AddRange(newConfigs);
                        result.AddRange(newConfigs);
                    }
                    else
                    {
                        matrix.Remove(mostCommonOption);
                    }
                }
            } while (result.Count == 0 && matrix.Count > 0);
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
                        error = Math.Abs((estimatedValue - realValue) / realValue);
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
            return list.OrderByDescending(tuple => tuple.Item2).Select(tuple => tuple.Item1);
        }

        /// <summary>
        /// Creates a table from the given configurations.
        /// The header (keys of the dictionary) contains all occurring options.
        /// The columns are stored as values.
        /// A cell contains 1 iff the appropriate configuration contains the option; otherwise 0.
        ///
        /// Example for the configurations [option1], [option1,option2], [option2,option3]
        ///
        /// option1 | option2 | option3
        /// ---------------------------
        ///    1    |    0    |    0
        ///    1    |    1    |    0
        ///    0    |    1    |    1
        /// </summary>
        private static Dictionary<BinaryOption, List<int>> CreateMatrix(List<Configuration> badConfigs)
        {
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
            return matrix;
        }

        private static List<BinaryOption> GetMostCommonOptions(Dictionary<BinaryOption, List<int>> matrix)
        {
            List<BinaryOption> maximalOptions = new List<BinaryOption>();
            int maxOccurrence = Int32.MinValue;
            foreach (KeyValuePair<BinaryOption, List<int>> keyValuePair in matrix)
            {
                int sum = keyValuePair.Value.Sum();
                if (sum > maxOccurrence)
                {
                    maxOccurrence = sum;
                    maximalOptions = new List<BinaryOption> {keyValuePair.Key};
                }
                else if (sum == maxOccurrence)
                {
                    maximalOptions.Add(keyValuePair.Key);
                }
            }
            return maximalOptions;
        }
    }
}
