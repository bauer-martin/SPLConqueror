using System;
using System.Collections.Generic;
using System.Linq;

namespace MachineLearning.Solver
{
    class ExternalSolverFacade : ISolverFacade
    {
        private readonly ExternalSolverAdapter _externalSolverAdapter;
        private ExternalCheckConfigSAT _satisfiabilityChecker;
        private ExternalVariantGenerator _variantGenerator;
        private readonly SolverType _solverType;
        private readonly IOptionCoding _optionCoding;

        internal ExternalSolverFacade(ExternalSolverAdapter externalSolverAdapter, SolverType solverType)
        {
            _externalSolverAdapter = externalSolverAdapter;
            _solverType = solverType;
            _optionCoding = new VariabilityModelIndexOptionCoding();
        }

        public ICheckConfigSAT SatisfiabilityChecker
        {
            get
            {
                return _satisfiabilityChecker
                    ?? (_satisfiabilityChecker =
                        new ExternalCheckConfigSAT(_externalSolverAdapter, _solverType, _optionCoding));
            }
        }

        public IVariantGenerator VariantGenerator
        {
            get
            {
                return _variantGenerator
                    ?? (_variantGenerator =
                        new ExternalVariantGenerator(_externalSolverAdapter, _solverType, _optionCoding));
            }
        }

        public void SetParameters(Dictionary<string, string> parameters)
        {
            if (parameters == null) return;
            List<string> tokens = parameters.Where(pair => !pair.Key.Equals(SolverParameterKeys.EXECUTABLE_PATH))
                .Select(pair => $"{pair.Key}={pair.Value}")
                .ToList();
            string response = _externalSolverAdapter.Execute($"set-solver-parameters {String.Join(";", tokens)}");
            _externalSolverAdapter.ThrowExceptionIfError(response);
        }
    }
}