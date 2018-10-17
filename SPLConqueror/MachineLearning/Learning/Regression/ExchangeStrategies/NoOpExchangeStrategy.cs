using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.ExchangeStrategies
{
    /// <inheritdoc />
    /// <summary>
    /// This strategy does not exchange any configurations at all.
    /// </summary>
    public class NoOpExchangeStrategy : ConfigurationExchangeStrategy
    {
        public void exchangeConfigurations(List<Configuration> learningSet, List<Configuration> validationSet) { }
    }
}
