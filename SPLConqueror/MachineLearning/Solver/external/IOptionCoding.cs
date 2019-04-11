using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    interface IOptionCoding
    {
        string GetName();

        string EncodeOption<T>(T option, VariabilityModel vm) where T : ConfigurationOption;
        string EncodeOptions<T>(List<T> options, VariabilityModel vm) where T : ConfigurationOption;
        List<BinaryOption> DecodeBinaryOptions(string str, VariabilityModel vm);
        List<List<BinaryOption>> DecodeBinaryOptionsList(string str, VariabilityModel vm);
    }
}
