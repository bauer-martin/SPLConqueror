using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    class OptionNameOptionCoding : AbstractOptionCoding
    {
        public override string GetName() { return "option-name"; }

        public override string EncodeOption<T>(T option, VariabilityModel vm)
        {
            return option.Name;
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
