using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MachineLearning.Sampling;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.AdditionStrategies
{
    class MatrixVICAdditionStrategy : DistributionBasedAdditionStrategy
    {
        public MatrixVICAdditionStrategy(ML_Settings mlSettings, ConfigurationBuilder configBuilder,
            string sampleTask) : base(mlSettings, configBuilder, sampleTask)
        {
        }

        protected override List<Configuration> FindNewConfigurationsImpl(List<Configuration> learningSet,
            List<Configuration> validationSet, List<Feature> currentModel)
        {
            List<Configuration> existingConfigurations = new List<Configuration>(learningSet);
            foreach (Configuration config in validationSet)
            {
                if (!existingConfigurations.Contains(config))
                {
                    existingConfigurations.Add(config);
                }
            }
            configBuilder.existingConfigurations = existingConfigurations;

            // find configurations that can be predicted good/bad
            int maxNumberOfConfigs = (int) Math.Round(0.1 * validationSet.Count);
            List<Configuration> sortedConfigs = SortedConfigsByError(validationSet, currentModel).ToList();
            List<Configuration> badConfigs = sortedConfigs.Take(maxNumberOfConfigs).ToList();
            sortedConfigs.Reverse();
            List<Configuration> goodConfigs = sortedConfigs.Take(maxNumberOfConfigs).ToList();

            // extract relevant influences
            OptionOccurrenceMatrix matrix = new OptionOccurrenceMatrix(badConfigs);
            matrix.Subtract(goodConfigs);
            List<List<BinaryOption>> influences = matrix.GetMaximalInfluences();
            influences = FilterForSmallDegreeAndNotInModel(influences, currentModel);

            // for every influence: find n configurations which includes the very influence
            List<Configuration> result = new List<Configuration>();
            foreach (List<BinaryOption> influence in influences)
            {
                for (int i = 0; i < 5; i++)
                {
                    List<BinaryOption> desiredOptions = new List<BinaryOption>(influence);
                    List<Configuration> newConfigs = configBuilder.buildConfigs(GlobalState.varModel, desiredOptions);
                    Console.WriteLine("(" + i + ") wanted " + string.Join(",", desiredOptions) + " got " + string.Join(",", newConfigs));
                    configBuilder.existingConfigurations.AddRange(newConfigs);
                    result.AddRange(newConfigs);
                }
            }
            return result;
        }

        private IEnumerable<Configuration> SortedConfigsByError(List<Configuration> configs, List<Feature> model)
        {
            List<Tuple<Configuration, double>> list = new List<Tuple<Configuration, double>>();
            foreach (Configuration c in configs)
            {
                double estimatedValue = FeatureSubsetSelection.estimate(model, c);
                double realValue = c.GetNFPValue(GlobalState.currentNFP);
                double error = 0;
                switch (mlSettings.lossFunction)
                {
                    case ML_Settings.LossFunction.RELATIVE:
                        error = Math.Abs((estimatedValue - realValue) / realValue);
                        break;
                    case ML_Settings.LossFunction.LEASTSQUARES:
                        error = Math.Pow(realValue - estimatedValue, 2);
                        break;
                    case ML_Settings.LossFunction.ABSOLUTE:
                        error = Math.Abs(realValue - estimatedValue);
                        break;
                }
                list.Add(new Tuple<Configuration, double>(c, error));
            }
            return list.OrderByDescending(tuple => tuple.Item2).Select(tuple => tuple.Item1);
        }

        private List<List<BinaryOption>> FilterForSmallDegreeAndNotInModel(List<List<BinaryOption>> influences,
            List<Feature> currentModel)
        {
            List<List<BinaryOption>> result = new List<List<BinaryOption>>();
            ILookup<int, List<BinaryOption>> influencesByDegree = influences.ToLookup(list => list.Count);
            foreach (IGrouping<int, List<BinaryOption>> keyValuePair in influencesByDegree.OrderBy(pair => pair.Key))
            {
                foreach (List<BinaryOption> influence in keyValuePair)
                {
                    bool modelContainsInfluence = currentModel.Any(feature =>
                        feature.participatingBoolOptions.Count == influence.Count
                        && influence.All(feature.containsOption));
                    if (!modelContainsInfluence)
                    {
                        result.Add(influence);
                    }
                }
                if (result.Count != 0)
                {
                    break;
                }
            }
            return result;
        }
    }

    public class OptionOccurrenceMatrix
    {
        private readonly Dictionary<UnorderedSet<BinaryOption>, int> matrixData =
            new Dictionary<UnorderedSet<BinaryOption>, int>();

        public OptionOccurrenceMatrix(List<Configuration> configs)
        {
            foreach (Configuration config in configs)
            {
                List<BinaryOption> selectedOptions = config.getBinaryOptions(BinaryOption.BinaryValue.Selected);
                List<UnorderedSet<BinaryOption>> combinations = GetCombinations(config);
                foreach (UnorderedSet<BinaryOption> combination in combinations)
                {
                    if (combination.All(o => selectedOptions.Contains(o)))
                    {
                        if (matrixData.ContainsKey(combination))
                        {
                            matrixData[combination] += 1;
                        }
                        else
                        {
                            matrixData[combination] = 1;
                        }
                    }
                }
            }
        }

        private static List<UnorderedSet<BinaryOption>> GetCombinations(Configuration config)
        {
            List<BinaryOption> options = config.getBinaryOptions(BinaryOption.BinaryValue.Selected)
                .Where(option => option.Optional || option.hasAlternatives())
                .ToList();
            List<UnorderedSet<BinaryOption>> result = new List<UnorderedSet<BinaryOption>>
                {new UnorderedSet<BinaryOption>()};
            List<UnorderedSet<BinaryOption>> temp = new List<UnorderedSet<BinaryOption>>();
            foreach (BinaryOption item in options)
            {
                foreach (UnorderedSet<BinaryOption> set in result)
                {
                    if (set.Count() >= 4) continue;
                    temp.Add(new UnorderedSet<BinaryOption>(set, item));
                }
                result.AddRange(temp);
                temp.Clear();
            }
            result.RemoveAt(0);
            return result;
        }

        public void Subtract(List<Configuration> configs)
        {
            foreach (Configuration config in configs)
            {
                List<UnorderedSet<BinaryOption>> combinations = GetCombinations(config);
                foreach (UnorderedSet<BinaryOption> combination in combinations)
                {
                    if (matrixData.ContainsKey(combination))
                    {
                        matrixData[combination] -= 1;
                    }
                    else
                    {
                        matrixData[combination] = -1;
                    }
                }
            }
        }

        public List<List<BinaryOption>> GetMaximalInfluences()
        {
            List<List<BinaryOption>> maximalOptions = new List<List<BinaryOption>>();
            int maxOccurrence = Int32.MinValue;
            foreach (KeyValuePair<UnorderedSet<BinaryOption>, int> keyValuePair in matrixData)
            {
                if (keyValuePair.Value > maxOccurrence)
                {
                    maxOccurrence = keyValuePair.Value;
                    maximalOptions = new List<List<BinaryOption>> {new List<BinaryOption>(keyValuePair.Key)};
                }
                else if (keyValuePair.Value == maxOccurrence)
                {
                    maximalOptions.Add(new List<BinaryOption>(keyValuePair.Key));
                }
            }
            return maximalOptions;
        }

        private class UnorderedSet<T> : IEquatable<UnorderedSet<T>>, IEnumerable<T>
        {
            private readonly HashSet<T> elements;

            internal UnorderedSet() { elements = new HashSet<T>(); }

            internal UnorderedSet(UnorderedSet<T> set, T element) { elements = new HashSet<T>(set.elements) {element}; }

            public bool Equals(UnorderedSet<T> other)
            {
                foreach (T element in elements)
                {
                    if (!other.elements.Contains(element))
                    {
                        return false;
                    }
                }
                foreach (T element in other.elements)
                {
                    if (!elements.Contains(element))
                    {
                        return false;
                    }
                }
                return true;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((UnorderedSet<T>) obj);
            }

            public override int GetHashCode()
            {
                return elements.Aggregate(0, (acc, option) => acc + option.GetHashCode());
            }

            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

            public IEnumerator<T> GetEnumerator() { return elements.GetEnumerator(); }

            public override string ToString() { return $"{string.Join(",", elements)}"; }
        }
    }
}
