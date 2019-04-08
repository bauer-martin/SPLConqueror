using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public abstract class AbstractOptionCoding : IOptionCoding
    {
        public abstract string GetName();

        public abstract string EncodeOptions<T>(List<T> options, VariabilityModel vm) where T : ConfigurationOption;

        public abstract List<BinaryOption> DecodeBinaryOptions(string str, VariabilityModel vm);

        public List<List<BinaryOption>> DecodeBinaryOptionsList(string str, VariabilityModel vm)
        {
            return str.Split(';').Select(s => DecodeBinaryOptions(s, vm)).ToList();
        }
    }
}