using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class ChocoCheckConfigSAT : ICheckConfigSAT
    {
        private readonly JavaSolverAdapter _adapter;

        public ChocoCheckConfigSAT(JavaSolverAdapter adapter) { this._adapter = adapter; }

        public bool checkConfigurationSAT(List<BinaryOption> config, VariabilityModel vm, bool partialConfiguration)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(SolverType.CHOCO);
            string optionsString = String.Join(",", config.Select(o => o.Name));
            string partialString = partialConfiguration ? "partial" : "complete";
            string response = _adapter.Execute($"check-sat {partialString} {optionsString}");
            return response.Equals("true");
        }

        public bool checkConfigurationSAT(Configuration c, VariabilityModel vm, bool partialConfiguration)
        {
            List<BinaryOption> selectionOptions = c.getBinaryOptions(BinaryOption.BinaryValue.Selected);
            return checkConfigurationSAT(selectionOptions, vm, partialConfiguration);
        }
    }
}
