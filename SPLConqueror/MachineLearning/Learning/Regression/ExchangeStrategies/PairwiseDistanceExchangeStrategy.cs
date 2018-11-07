using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.ExchangeStrategies
{
    public class PairwiseDistanceExchangeStrategy : LargestValidationErrorExchangeStrategy
    {
        public struct Triple<T1, T2, T3>
        {
            internal T1 first;
            internal T2 second;
            internal T3 third;

            public Triple(T1 first, T2 second, T3 third)
            {
                this.first = first;
                this.second = second;
                this.third = third;
            }
        }

        public PairwiseDistanceExchangeStrategy(ML_Settings mlSettings) : base(mlSettings, 0.1) { }

        protected override IEnumerable<Configuration> SelectConfigsFromLearningSet(List<Configuration> learningSet,
            int count)
        {
            List<Triple<Configuration, Configuration, double>> triples = SortedByMinDistance(learningSet).ToList();
            List<Configuration> result = new List<Configuration>();
            int index = 0;
            while (result.Count < count && triples.Count > 0)
            {
                Triple<Configuration, Configuration, double> triple = triples[index];
                if (!result.Contains(triple.first)) result.Add(triple.first);
                if (!result.Contains(triple.second)) result.Add(triple.second);
                index++;
            }
            return result.Take(count);
        }

        private static Dictionary<Configuration, Dictionary<Configuration, double>> DistanceMatrix(List<Configuration> learningSet)
        {
            Dictionary<Configuration, Dictionary<Configuration, double>> pairs =
                new Dictionary<Configuration, Dictionary<Configuration, double>>();
            foreach (Configuration config1 in learningSet)
            {
                Dictionary<Configuration, double> row;
                if (pairs.ContainsKey(config1))
                {
                    row = pairs[config1];
                }
                else
                {
                    row = new Dictionary<Configuration, double>();
                    pairs[config1] = row;
                }
                foreach (Configuration config2 in learningSet)
                {
                    if (config1.Equals(config2)) continue;
                    if (pairs.ContainsKey(config2)) continue;
                    double distance = ComputeDistance(config1, config2);
                    row[config2] = distance;
                }
            }
            return pairs;
        }

        private static double ComputeDistance(Configuration config1, Configuration config2)
        {
            Dictionary<BinaryOption, int> participatingOptions = new Dictionary<BinaryOption, int>();
            foreach (KeyValuePair<BinaryOption, BinaryOption.BinaryValue> keyValuePair in config1.BinaryOptions)
            {
                if (keyValuePair.Value == BinaryOption.BinaryValue.Deselected) continue;
                participatingOptions[keyValuePair.Key] = 1;
            }
            foreach (KeyValuePair<BinaryOption, BinaryOption.BinaryValue> keyValuePair in config2.BinaryOptions)
            {
                if (keyValuePair.Value == BinaryOption.BinaryValue.Deselected) continue;
                if (participatingOptions.ContainsKey(keyValuePair.Key))
                {
                    participatingOptions[keyValuePair.Key] += 1;
                }
                else
                {
                    participatingOptions[keyValuePair.Key] = 1;
                }
            }
            int distance = 0;
            foreach (KeyValuePair<BinaryOption, int> keyValuePair in participatingOptions)
            {
                if (keyValuePair.Value % 2 != 0)
                {
                    distance++;
                }
            }
            return distance;
        }

        public static IEnumerable<Triple<Configuration, Configuration, double>> SortedByMinDistance(
            List<Configuration> learningSet)
        {
            return DistanceMatrix(learningSet)
                .Aggregate(new List<Triple<Configuration, Configuration, double>>(), ToTriple)
                .OrderBy(triple => triple.third);
        }

        private static List<Triple<Configuration, Configuration, double>> ToTriple(
            List<Triple<Configuration, Configuration, double>> acc,
            KeyValuePair<Configuration, Dictionary<Configuration, double>> outerKeyValuePair)
        {
            Configuration config1 = outerKeyValuePair.Key;
            Dictionary<Configuration, double> row = outerKeyValuePair.Value;
            List<Triple<Configuration, Configuration, double>> aggregatedRow = row.Aggregate(
                new List<Triple<Configuration, Configuration, double>>(),
                (innerAcc, innerKeyValuePair) =>
                {
                    Configuration config2 = innerKeyValuePair.Key;
                    double distance = innerKeyValuePair.Value;
                    innerAcc.Add(new Triple<Configuration, Configuration, double>(config1, config2, distance));
                    return innerAcc;
                });
            acc.AddRange(aggregatedRow);
            return acc;
        }
    }
}
