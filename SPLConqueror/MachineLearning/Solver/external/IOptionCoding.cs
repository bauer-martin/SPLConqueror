using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public interface IOptionCoding
    {
        string GetName();

        string EncodeOptions<T>(List<T> options, VariabilityModel vm) where T : ConfigurationOption;
        List<BinaryOption> DecodeBinaryOptions(string str, VariabilityModel vm);
        List<List<BinaryOption>> DecodeBinaryOptionsList(string str, VariabilityModel vm);
    }
}
