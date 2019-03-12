using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class JavaBasedCheckConfigSAT : ICheckConfigSAT
    {
        private readonly JavaSolverAdapter _adapter;
        private readonly SolverType _solverType;

        public JavaBasedCheckConfigSAT(JavaSolverAdapter adapter, SolverType solverType)
        {
            _adapter = adapter;
            _solverType = solverType;
        }

        public bool checkConfigurationSAT(List<BinaryOption> config, VariabilityModel vm, bool partialConfiguration)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(_solverType);
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
