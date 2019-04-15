using System;
using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    class Z3SolverFacade : ISolverFacade
    {
        private uint _seed = 1;
        private bool _henard = false;
        private readonly VariabilityModel _vm;
        private CheckConfigSATZ3 _satisfiabilityChecker;
        private Z3VariantGenerator _variantGenerator;

        public Z3SolverFacade(VariabilityModel vm) { _vm = vm; }

        public ICheckConfigSAT SatisfiabilityChecker
        {
            get { return _satisfiabilityChecker ?? (_satisfiabilityChecker = new CheckConfigSATZ3(_vm)); }
        }

        public IVariantGenerator VariantGenerator
        {
            get
            {
                if (_variantGenerator == null)
                {
                    _variantGenerator = new Z3VariantGenerator(_vm);
                    ApplyParameters();
                }
                return _variantGenerator;
            }
        }

        public void SetParameters(Dictionary<string, string> parameters)
        {
            if (parameters == null) return;
            if (parameters.ContainsKey(SolverParameterKeys.RANDOM_SEED))
            {
                _seed = UInt32.Parse(parameters[SolverParameterKeys.RANDOM_SEED]);
            }
            if (parameters.ContainsKey(SolverParameterKeys.HENARD))
            {
                _henard = Boolean.Parse(parameters[SolverParameterKeys.HENARD]);
            }
            if (_variantGenerator != null) ApplyParameters();
        }

        private void ApplyParameters()
        {
            _variantGenerator.setSeed(_seed);
            _variantGenerator.henard = _henard;
        }
    }
}
