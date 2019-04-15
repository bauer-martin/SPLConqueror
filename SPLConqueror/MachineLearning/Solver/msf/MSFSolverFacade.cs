using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    class MSFSolverFacade : ISolverFacade
    {
        private readonly VariabilityModel _vm;
        private MSFCheckConfigSAT _satisfiabilityChecker;
        private MSFVariantGenerator _variantGenerator;

        public MSFSolverFacade(VariabilityModel vm) { _vm = vm; }

        public ICheckConfigSAT SatisfiabilityChecker
        {
            get { return _satisfiabilityChecker ?? (_satisfiabilityChecker = new MSFCheckConfigSAT(_vm)); }
        }

        public IVariantGenerator VariantGenerator
        {
            get { return _variantGenerator ?? (_variantGenerator = new MSFVariantGenerator(_vm)); }
        }

        public void SetParameters(Dictionary<string, string> parameters) { }
    }
}
