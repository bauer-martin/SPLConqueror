using System;
using System.Collections.Generic;

namespace MachineLearning.Solver
{
    public class Z3SolverFacade : ISolverFacade
    {
        private CheckConfigSATZ3 _satisfiabilityChecker;
        private Z3VariantGenerator _variantGenerator;
        private uint _seed = 1;

        public ICheckConfigSAT SatisfiabilityChecker
        {
            get { return _satisfiabilityChecker ?? (_satisfiabilityChecker = new CheckConfigSATZ3()); }
        }

        public IVariantGenerator VariantGenerator
        {
            get
            {
                if (_variantGenerator == null)
                {
                    _variantGenerator = new Z3VariantGenerator();
                    ApplyParameters();
                }
                return _variantGenerator;
            }
        }

        public void SetParameters(Dictionary<string, string> parameters)
        {
            if (parameters.ContainsKey(SolverParameterKeys.RANDOM_SEED))
            {
                _seed = UInt32.Parse(parameters[SolverParameterKeys.RANDOM_SEED]);
            }
            if (_variantGenerator != null) ApplyParameters();
        }

        private void ApplyParameters()
        {
            _variantGenerator.setSeed(_seed);
        }
    }
}
