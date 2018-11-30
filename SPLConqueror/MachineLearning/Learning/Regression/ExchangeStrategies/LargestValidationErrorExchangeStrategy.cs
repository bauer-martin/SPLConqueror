using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.ExchangeStrategies
{
    public abstract class LargestValidationErrorExchangeStrategy: ConfigurationExchangeStrategy
    {
        protected readonly ML_Settings mlSettings;
        private readonly double percentageToExchange;

        protected LargestValidationErrorExchangeStrategy(ML_Settings mlSettings, double percentageToExchange)
        {
            this.mlSettings = mlSettings;
            this.percentageToExchange = percentageToExchange;
        }

        public void exchangeConfigurations(List<Configuration> learningSet, List<Configuration> validationSet,
            List<Feature> model)
        {
            // determine how many configurations need to be exchanged
            int numberOfConfigsToExchange = (int) (validationSet.Count * percentageToExchange);

            // select configurations for exchange
            IEnumerable<Configuration> learningConfigsToExchange =
                SelectConfigsFromLearningSet(learningSet, numberOfConfigsToExchange);
            IEnumerable<Configuration> validationConfigsToExchange =
                SelectConfigsFromValidationSet(validationSet, numberOfConfigsToExchange, model);

            // exchange configurations
            learningSet.RemoveAll(config => learningConfigsToExchange.Contains(config));
            validationSet.RemoveAll(config => validationConfigsToExchange.Contains(config));
            learningSet.AddRange(validationConfigsToExchange);
            validationSet.AddRange(learningConfigsToExchange);
        }

        protected abstract IEnumerable<Configuration> SelectConfigsFromLearningSet(List<Configuration> learningSet,
            int count);

        private IEnumerable<Configuration> SelectConfigsFromValidationSet(List<Configuration> validationSet, int count,
            List<Feature> model)
        {
            IEnumerable<Configuration> configsSortedByError = SortedConfigsByError(validationSet, model);
            return configsSortedByError.Take(count);
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
