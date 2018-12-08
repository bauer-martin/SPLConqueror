using System;
using System.Collections.Generic;
using System.Linq;
using MachineLearning.Sampling;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.AdditionStrategies
{
    class Matrix2DistributionBasedAdditionStrategy : DistributionBasedAdditionStrategy
    {
        public Matrix2DistributionBasedAdditionStrategy(ML_Settings mlSettings, ConfigurationBuilder configBuilder,
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
            List<BinaryOption> badOptions = FindBadOptions(validationSet, currentModel);
            List<Configuration> result = new List<Configuration>();
            foreach (BinaryOption option in badOptions)
            {
                List<BinaryOption> whiteList = new List<BinaryOption> {option};
                List<Configuration> newConfigs = configBuilder.buildConfigs(GlobalState.varModel, whiteList);
                configBuilder.existingConfigurations.AddRange(newConfigs);
                result.AddRange(newConfigs);
            }
            return result;
        }

        private List<BinaryOption> FindBadOptions(List<Configuration> validationSet, List<Feature> currentModel)
        {
            int maxNumberOfConfigs = (int) Math.Round(0.1 * validationSet.Count);
            List<Configuration> sortedConfigs = SortedConfigsByError(validationSet, currentModel).ToList();
            List<Configuration> badConfigs = sortedConfigs.Take(maxNumberOfConfigs).ToList();
            sortedConfigs.Reverse();
            List<Configuration> goodConfigs = sortedConfigs.Take(maxNumberOfConfigs).ToList();
            Dictionary<BinaryOption, int> occurrences = new Dictionary<BinaryOption, int>();
            foreach (Configuration badConfig in badConfigs)
            {
                foreach (BinaryOption option in badConfig.getBinaryOptions(BinaryOption.BinaryValue.Selected))
                {
                    if (occurrences.ContainsKey(option))
                    {
                        occurrences[option] += 1;
                    }
                    else
                    {
                        occurrences[option] = 1;
                    }
                }
            }
            foreach (Configuration badConfig in goodConfigs)
            {
                foreach (BinaryOption option in badConfig.getBinaryOptions(BinaryOption.BinaryValue.Selected))
                {
                    if (occurrences.ContainsKey(option))
                    {
                        occurrences[option] -= 1;
                    }
                }
            }
            var a = occurrences.Where(keyValuePair => keyValuePair.Key.Optional || keyValuePair.Key.hasAlternatives())
                .OrderByDescending(keyValuePair => keyValuePair.Value)
                .Take(maxNumberOfConfigs)
                .Select(keyValuePair => keyValuePair.Key)
                .ToList();

            if (a.Count == 0)
            {
                throw new Exception("validationSet set is to small");
            }
            return a;
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
