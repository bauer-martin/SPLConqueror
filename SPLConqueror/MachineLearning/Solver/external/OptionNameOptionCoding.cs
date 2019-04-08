using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class OptionNameOptionCoding : AbstractOptionCoding
    {
        public override string GetName() { return "option-name"; }

        public override string EncodeOptions<T>(List<T> options, VariabilityModel vm)
        {
            return String.Join(",", options.Select(o => o.Name));
        }

        public override List<BinaryOption> DecodeBinaryOptions(string str, VariabilityModel vm)
        {
            List<BinaryOption> result;
            if (str.Equals("none"))
            {
                result = null;
            }
            else
            {
                string[] tokens = str.Split(',');
                result = tokens.Select(vm.getBinaryOption).ToList();
            }
            return result;
        }
    }
}
