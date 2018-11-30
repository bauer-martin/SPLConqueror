using System.Collections.Generic;
using MachineLearning.Sampling;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.AdditionStrategies
{
    class SimpleDistributionBasedAdditionStrategy : DistributionBasedAdditionStrategy
    {
        public SimpleDistributionBasedAdditionStrategy(ML_Settings mlSettings, ConfigurationBuilder configBuilder,
            string sampleTask) : base(mlSettings, configBuilder, sampleTask)
        {
        }

        protected override List<Configuration> FindNewConfigurationsImpl(List<Configuration> learningSet,
            List<Configuration> validationSet, List<Feature> model)
        {
            List<Configuration> existingConfigurations = new List<Configuration>(learningSet);
            existingConfigurations.AddRange(validationSet);
            configBuilder.existingConfigurations = existingConfigurations;
            List<Configuration> newConfigurations = configBuilder.buildSet(mlSettings);
            configBuilder.binaryParams.updateSeeds();
            return newConfigurations;
        }
    }
}
