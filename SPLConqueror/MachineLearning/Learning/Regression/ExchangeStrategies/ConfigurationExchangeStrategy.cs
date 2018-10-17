using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.ExchangeStrategies
{
    /// <summary>
    /// This interface provides the ability to modify the learning set and validation set after an active learning round.
    /// </summary>
    public interface ConfigurationExchangeStrategy
    {
        void exchangeConfigurations(List<Configuration> learningSet, List<Configuration> validationSet);
    }
}
