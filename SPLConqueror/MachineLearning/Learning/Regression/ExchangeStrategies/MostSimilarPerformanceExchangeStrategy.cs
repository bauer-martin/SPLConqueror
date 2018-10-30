using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;
using static MachineLearning.Learning.Regression.ExchangeStrategies.PairwiseDistanceExchangeStrategy;
using static MachineLearning.Learning.Regression.ExchangeStrategies.PerformanceValueExchangeStrategy;

namespace MachineLearning.Learning.Regression.ExchangeStrategies
{
    public class MostSimilarPerformanceExchangeStrategy : LargestValidationErrorExchangeStrategy
    {
        public MostSimilarPerformanceExchangeStrategy(ML_Settings mlSettings) : base(mlSettings, 0.1) { }

        protected override IEnumerable<Configuration> SelectConfigsFromLearningSet(List<Configuration> learningSet,
            int count)
        {
            List<Configuration> result = new List<Configuration>();
            List<Configuration> mutableConfigsList = new List<Configuration>(learningSet);
            do
            {
                ICollection<Distribution.Bucket> maximalBuckets = FindMaximalBuckets(mutableConfigsList);
                foreach (Distribution.Bucket maximalBucket in maximalBuckets)
                {
                    Configuration configuration;
                    if (maximalBucket.configCount > 1)
                    {
                        List<Triple<Configuration, Configuration, double>> pairsSortedByMinDistance
                            = SortedByMinDistance(maximalBucket.configs.ToList()).ToList();
                        configuration = pairsSortedByMinDistance.First().first;
                    }
                    else
                    {
                        configuration = maximalBucket.configs.First();
                    }
                    result.Add(configuration);
                    mutableConfigsList.Remove(configuration);
                }
            } while (result.Count < count && mutableConfigsList.Count > 0);
            return result.Take(count);
        }
    }
}
