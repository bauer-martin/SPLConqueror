using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;
using static MachineLearning.Learning.Regression.ExchangeStrategies.PairwiseDistanceExchangeStrategy;
using static MachineLearning.Learning.Regression.ExchangeStrategies.PerformanceValueExchangeStrategy;

namespace MachineLearning.Learning.Regression.ExchangeStrategies
{
    /// <summary>
    /// Combination of PerformanceValueExchangeStrategy and PairwiseDistanceExchangeStrategy.
    /// Select configurations that have the same performance value as many other configurations (ordering in buckets).
    /// Randomly select one configuration from the pair of options with the smallest distance to each other within
    /// the bucket with the largest number of configurations.
    /// </summary>
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
