using System;
using System.Collections.Generic;

namespace MachineLearning.Solver
{
    public class Z3SolverFacade : ISolverFacade
    {
        private CheckConfigSATZ3 _satisfiabilityChecker;
        private Z3VariantGenerator _variantGenerator;

        public ICheckConfigSAT SatisfiabilityChecker
        {
            get { return _satisfiabilityChecker ?? (_satisfiabilityChecker = new CheckConfigSATZ3()); }
        }

        public IVariantGenerator VariantGenerator
        {
            get { return _variantGenerator ?? (_variantGenerator = new Z3VariantGenerator()); }
        }

        public void SetParameters(Dictionary<string, string> parameters) { }
    }
}
