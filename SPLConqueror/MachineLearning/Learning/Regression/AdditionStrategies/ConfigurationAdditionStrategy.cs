using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.AdditionStrategies
{
    /// <summary>
    /// This interface provides the ability to find new configurations to be added to the learning set for the next
    /// active learning round.
    /// </summary>
    public interface ConfigurationAdditionStrategy
    {
        List<Configuration> FindNewConfigurations(List<Configuration> learningSet, List<Configuration> validationSet,
            List<Feature> model);
    }
}
