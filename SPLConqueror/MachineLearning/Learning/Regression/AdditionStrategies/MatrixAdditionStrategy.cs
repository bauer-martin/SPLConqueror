using System;
using System.Collections.Generic;
using System.Linq;
using MachineLearning.Sampling;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.AdditionStrategies
{
    class MatrixDistributionBasedAdditionStrategy : DistributionBasedAdditionStrategy
    {
        public MatrixDistributionBasedAdditionStrategy(ML_Settings mlSettings, ConfigurationBuilder configBuilder,
            string sampleTask) : base(mlSettings, configBuilder, sampleTask)
        {
        }

        protected override List<Configuration> FindNewConfigurationsImpl(List<Configuration> learningSet,
            List<Configuration> validationSet, List<Feature> currentModel)
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
            List<BinaryOption> maximalOptions = optionsSortedByOccurrence.TakeWhile(tuple => tuple.Item2 == first.Item2)
                .Select(tuple => tuple.Item1).ToList();

            List<Configuration> result = new List<Configuration>();
            foreach (BinaryOption maximalOption in maximalOptions)
            {
                List<BinaryOption> whiteList = new List<BinaryOption> {maximalOption};
                List<Configuration> newConfigs = configBuilder.buildConfigs(GlobalState.varModel, whiteList);
                configBuilder.existingConfigurations.AddRange(newConfigs);
                result.AddRange(newConfigs);
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
    }
}
