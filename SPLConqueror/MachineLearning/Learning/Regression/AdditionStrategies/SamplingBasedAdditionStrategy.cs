using System.Collections.Generic;
using MachineLearning.Sampling;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.AdditionStrategies
{
    /// <summary>
    /// Base class for strategies that rely on sampling.
    /// </summary>
    public abstract class SamplingBasedAdditionStrategy : ConfigurationAdditionStrategy
    {
        protected readonly ML_Settings mlSettings;
        protected readonly ConfigurationBuilder configBuilder;
        private readonly string sampleTask;
        private bool didPerformSampleCommand = false;

        protected SamplingBasedAdditionStrategy(ML_Settings mlSettings, ConfigurationBuilder configBuilder,
            string sampleTask)
        {
            this.configBuilder = configBuilder;
            this.mlSettings = mlSettings;
            this.sampleTask = sampleTask;
        }

        public List<Configuration> FindNewConfigurations(List<Configuration> learningSet,
            List<Configuration> validationSet, List<Feature> model)
        {
            if (!didPerformSampleCommand)
            {
                // reset configuration builder such that another sampling can be performed
                configBuilder.clear();
                configBuilder.performOneCommand(sampleTask);
                didPerformSampleCommand = true;
            }
            return FindNewConfigurationsImpl(learningSet, validationSet, model);
        }

        protected abstract List<Configuration> FindNewConfigurationsImpl(List<Configuration> learningSet,
            List<Configuration> validationSet, List<Feature> model);
    }
}
