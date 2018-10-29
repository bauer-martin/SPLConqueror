using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.ExchangeStrategies
{
    public class PerformanceValueExchangeStrategy : LargestValidationErrorExchangeStrategy
    {
        private class Distribution
        {
            private readonly List<Bucket> buckets;
            private readonly double bucketSize;
            private readonly double domainMin;

            private Distribution(double bucketSize, double domainMin)
            {
                this.bucketSize = bucketSize;
                this.domainMin = domainMin;
                this.buckets = new List<Bucket>();
            }

            internal static Distribution CreateDefault(ICollection<Configuration> configs)
            {
                double domainMin = configs.Min(config => config.nfpValues[GlobalState.currentNFP]);
                double domainMax = configs.Max(config => config.nfpValues[GlobalState.currentNFP]);
                double bucketSize = (domainMax - domainMin) / configs.Count * 2;
                Distribution dist = new Distribution(bucketSize, domainMin);
                foreach (Configuration config in configs)
                {
                    double nfpValue = config.nfpValues[GlobalState.currentNFP];
                    Bucket bucket = dist.GetBucketContaining(nfpValue);
                    bucket.Add(config);
                }
                return dist;
            }

            internal static Distribution CreateShifted(ICollection<Configuration> configs)
            {
                double domainMin = configs.Min(config => config.nfpValues[GlobalState.currentNFP]);
                double domainMax = configs.Max(config => config.nfpValues[GlobalState.currentNFP]);
                double bucketSize = (domainMax - domainMin) / configs.Count * 2;
                Distribution dist = new Distribution(bucketSize, domainMin - bucketSize / 2);
                foreach (Configuration config in configs)
                {
                    double nfpValue = config.nfpValues[GlobalState.currentNFP];
                    Bucket bucket = dist.GetBucketContaining(nfpValue);
                    bucket.Add(config);
                }
                return dist;
            }

            private Bucket GetBucketContaining(double value)
            {
                Bucket bucket = buckets.FirstOrDefault(b => value >= b.lowerBound && value < b.upperBound);
                if (bucket == null)
                {
                    double lowerBound = (int) ((value - domainMin) / bucketSize) * bucketSize + domainMin;
                    bucket = new Bucket(lowerBound, lowerBound + bucketSize);
                    buckets.Add(bucket);
                }
                return bucket;
            }

            internal ICollection<Bucket> GetMaximalBuckets()
            {
                List<Bucket> maximalCandidates = new List<Bucket>();
                int maximalCount = Int32.MinValue;
                foreach (Bucket bucket in buckets)
                {
                    if (bucket.configCount > maximalCount)
                    {
                        maximalCount = bucket.configCount;
                        maximalCandidates.Clear();
                        maximalCandidates.Add(bucket);
                    }
                    else if (bucket.configCount == maximalCount)
                    {
                        maximalCandidates.Add(bucket);
                    }
                }
                return maximalCandidates;
            }

            public class Bucket
            {
                internal readonly double lowerBound;
                internal readonly double upperBound;
                internal IEnumerable<Configuration> configs { get { return backingConfigurations; } }
                internal int configCount { get { return backingConfigurations.Count; } }
                private readonly ICollection<Configuration> backingConfigurations = new List<Configuration>();

                public Bucket(double lowerBound, double upperBound)
                {
                    this.lowerBound = lowerBound;
                    this.upperBound = upperBound;
                }

                internal void Add(Configuration config) { backingConfigurations.Add(config); }

                public override string ToString()
                {
                    return $"{lowerBound:F}-{upperBound:F}: {backingConfigurations.Count}";
                }
            }
        }

        public PerformanceValueExchangeStrategy(ML_Settings mlSettings) : base(mlSettings, 0.1)
        {
        }

        protected override IEnumerable<Configuration> SelectConfigsFromLearningSet(List<Configuration> learningSet,
            int count)
        {
            List<Configuration> mutableConfigsList = new List<Configuration>(learningSet);
            List<Configuration> selectedConfigs = new List<Configuration>();
            do
            {
                Distribution layer1 = Distribution.CreateDefault(mutableConfigsList);
                Distribution layer2 = Distribution.CreateShifted(mutableConfigsList);
                ICollection<Distribution.Bucket> maximalBucketsLayer1 = layer1.GetMaximalBuckets();
                ICollection<Distribution.Bucket> maximalBucketsLayer2 = layer2.GetMaximalBuckets();
                ICollection<Distribution.Bucket> maximalBuckets =
                    maximalBucketsLayer1.First().configCount >= maximalBucketsLayer2.First().configCount
                        ? maximalBucketsLayer1
                        : maximalBucketsLayer2;
                foreach (Distribution.Bucket bucket in maximalBuckets)
                {
                    Configuration configuration = bucket.configs.First();
                    selectedConfigs.Add(configuration);
                    mutableConfigsList.Remove(configuration);
                }
            } while (selectedConfigs.Count < count && mutableConfigsList.Count > 0);
            return selectedConfigs.Take(count).ToList();
        }
    }
}
