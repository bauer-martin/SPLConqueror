using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class VariabilityModelIndexOptionCoding : AbstractOptionCoding
    {
        private string _variabilityModelName;
        private Dictionary<string, string> _encodingSubstitutions;
        private Dictionary<string, string> _decodingSubstitutions;
        public override string GetName() { return "variability-model-index"; }

        private void SetupSubstitutions(VariabilityModel vm)
        {
            if (vm.Name == _variabilityModelName) return;
            _variabilityModelName = vm.Name;
            Comparer<string> comparer
                = Comparer<string>.Create((x, y) => string.Compare(x, y, StringComparison.InvariantCultureIgnoreCase));
            List<BinaryOption> binaryOptions = vm.BinaryOptions.OrderBy(option => option.Name, comparer).ToList();
            _encodingSubstitutions = new Dictionary<string, string>(binaryOptions.Count);
            _decodingSubstitutions = new Dictionary<string, string>(binaryOptions.Count);
            for (int i = 0; i < binaryOptions.Count; i++)
            {
                string value = binaryOptions[i].Name;
                string substitution = i.ToString();
                _encodingSubstitutions[value] = substitution;
                _decodingSubstitutions[substitution] = value;
            }
        }

        public override string EncodeOptions<T>(List<T> options, VariabilityModel vm)
        {
            SetupSubstitutions(vm);
            return String.Join(",", options.Select(o => o.Name).Select(s => _encodingSubstitutions[s]));
        }

        public override List<BinaryOption> DecodeBinaryOptions(string str, VariabilityModel vm)
        {
            SetupSubstitutions(vm);
            List<BinaryOption> result;
            if (str.Equals("none"))
            {
                result = null;
            }
            else
            {
                string[] tokens = str.Split(',');
                result = tokens.Select(s => _decodingSubstitutions[s]).Select(vm.getBinaryOption).ToList();
            }
            return result;
        }
    }
}