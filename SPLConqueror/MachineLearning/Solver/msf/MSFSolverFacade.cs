using System.Collections.Generic;

namespace MachineLearning.Solver
{
    public class MSFSolverFacade : ISolverFacade
    {
        private MSFCheckConfigSAT _satisfiabilityChecker;
        private MSFVariantGenerator _variantGenerator;

        public ICheckConfigSAT SatisfiabilityChecker
        {
            get { return _satisfiabilityChecker ?? (_satisfiabilityChecker = new MSFCheckConfigSAT()); }
        }

        public IVariantGenerator VariantGenerator
        {
            get { return _variantGenerator ?? (_variantGenerator = new MSFVariantGenerator()); }
        }

        public void SetParameters(Dictionary<string, string> parameters) { }
    }
}