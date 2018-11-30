using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.AdditionStrategies
{
    public interface ConfigurationAdditionStrategy
    {
        List<Configuration> FindNewConfigurations(List<Configuration> learningSet, List<Configuration> validationSet,
            List<Feature> model);
    }
}
