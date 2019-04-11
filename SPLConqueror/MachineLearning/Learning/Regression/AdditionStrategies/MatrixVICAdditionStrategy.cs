using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MachineLearning.Sampling;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression.AdditionStrategies
{
    /// <summary>
    /// Select configurations containing options that are selected in configurations with a high error rate and
    /// deselected in configurations with a low error rate.
    /// 'VIC' stands for 'Very Important Configurations'
    /// </summary>
    class MatrixVICAdditionStrategy : SamplingBasedAdditionStrategy
    {
        public MatrixVICAdditionStrategy(ML_Settings mlSettings, ConfigurationBuilder configBuilder,
            string sampleTask) : base(mlSettings, configBuilder, sampleTask)
        {
        }

        protected override List<Configuration> FindNewConfigurationsImpl(List<Configuration> learningSet,
            List<Configuration> validationSet, List<Feature> currentModel)
        {
            // exclude configurations from learning set and validation set
            List<Configuration> existingConfigurations = new List<Configuration>(learningSet);
            foreach (Configuration config in validationSet)
            {
                if (!existingConfigurations.Contains(config))
                {
                    existingConfigurations.Add(config);
                }
            }
            configBuilder.existingConfigurations = existingConfigurations;

            // find configurations with high and low error rates
            int maxNumberOfConfigs = (int) Math.Round(0.1 * validationSet.Count);
            List<Configuration> sortedConfigs = SortedConfigsByError(validationSet, currentModel).ToList();
            List<Configuration> badConfigs = sortedConfigs.Take(maxNumberOfConfigs).ToList();
            sortedConfigs.Reverse();
            List<Configuration> goodConfigs = sortedConfigs.Take(maxNumberOfConfigs).ToList();

            // extract relevant influences
            OptionOccurrence optionOccurrence = new OptionOccurrence(badConfigs);
            optionOccurrence.Subtract(goodConfigs);
            List<List<BinaryOption>> influences = optionOccurrence.GetMostCommonInfluences();
            influences = FilterForSmallDegreeAndNotInModel(influences, currentModel);

            // for every influence: find n configurations which includes the very influence
            List<Configuration> result = new List<Configuration>();
            foreach (List<BinaryOption> influence in influences)
            {
                for (int i = 0; i < 5; i++)
                {
                    List<BinaryOption> desiredOptions = new List<BinaryOption>(influence);
                    List<Configuration> newConfigs = configBuilder.buildConfigs(GlobalState.varModel, desiredOptions);
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

        /// <summary>
        /// Filters the given influences such that only those influences with the fewest options are return.
        /// Influences that are already known by the given model are excluded as well.
        /// </summary>
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

    /// <summary>
    /// Example for the configurations [option1,option2], [option1,option3]
    ///
    /// option1 | option2 | option3 | option1,option2 | option1,option3
    /// ---------------------------------------------------------------
    ///    1    |    1    |    0    |        1        |        0
    ///    +    |    +    |    +    |        +        |        +
    ///    1    |    0    |    1    |        0        |        1
    /// ---------------------------------------------------------------
    ///    2    |    1    |    1    |        1        |        1
    /// </summary>
    public class OptionOccurrence
    {
        private readonly Dictionary<UnorderedSet<BinaryOption>, int> data =
            new Dictionary<UnorderedSet<BinaryOption>, int>();

        public OptionOccurrence(List<Configuration> configs)
        {
            foreach (Configuration config in configs)
            {
                List<BinaryOption> selectedOptions = config.getBinaryOptions(BinaryOption.BinaryValue.Selected);
                List<UnorderedSet<BinaryOption>> combinations = GetCombinations(config);
                foreach (UnorderedSet<BinaryOption> combination in combinations)
                {
                    if (combination.All(o => selectedOptions.Contains(o)))
                    {
                        if (data.ContainsKey(combination))
                        {
                            data[combination] += 1;
                        }
                        else
                        {
                            data[combination] = 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Return the power set (up to degree 4) of all options selected in the given configurations.
        /// </summary>
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
                    if (data.ContainsKey(combination))
                    {
                        data[combination] -= 1;
                    }
                    else
                    {
                        data[combination] = -1;
                    }
                }
            }
        }

        public List<List<BinaryOption>> GetMostCommonInfluences()
        {
            List<List<BinaryOption>> maximalOptions = new List<List<BinaryOption>>();
            int maxOccurrence = Int32.MinValue;
            foreach (KeyValuePair<UnorderedSet<BinaryOption>, int> keyValuePair in data)
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
