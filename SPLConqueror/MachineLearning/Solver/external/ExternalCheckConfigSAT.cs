using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class ExternalCheckConfigSAT : ICheckConfigSAT
    {
        private readonly ExternalSolverAdapter _adapter;
        private readonly SolverType _solverType;
        private readonly IOptionCoding _optionCoding;

        public ExternalCheckConfigSAT(ExternalSolverAdapter adapter, SolverType solverType, IOptionCoding optionCoding)
        {
            _adapter = adapter;
            _solverType = solverType;
            _optionCoding = optionCoding;
        }

        public bool checkConfigurationSAT(List<BinaryOption> config, VariabilityModel vm, bool partialConfiguration)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(_solverType);
            _adapter.SetOptionCoding(_optionCoding);
            string optionsString = _optionCoding.EncodeOptions(config, vm);
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