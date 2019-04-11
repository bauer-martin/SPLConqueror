using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    abstract class AbstractOptionCoding : IOptionCoding
    {
        public abstract string GetName();

        public abstract string EncodeOption<T>(T option, VariabilityModel vm) where T : ConfigurationOption;

        public string EncodeOptions<T>(List<T> options, VariabilityModel vm) where T : ConfigurationOption
        {
            return String.Join(",", options.Select(o => EncodeOption(o, vm)));
        }

        public abstract List<BinaryOption> DecodeBinaryOptions(string str, VariabilityModel vm);

        public List<List<BinaryOption>> DecodeBinaryOptionsList(string str, VariabilityModel vm)
        {
            return str.Split(';').Select(s => DecodeBinaryOptions(s, vm)).ToList();
        }
    }
}