using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.AdditionStrategies
{
    class NoOpAdditionStrategy : ConfigurationAdditionStrategy
    {
        public List<Configuration> FindNewConfigurations(List<Configuration> learningSet,
            List<Configuration> validationSet, List<Feature> model)
        {
            return new List<Configuration>();
        }
    }
}
