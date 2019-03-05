namespace MachineLearning.Solver
{
    public class ChocoSolverFacade : ISolverFacade
    {
        private readonly JavaSolverAdapter _javaSolverAdapter;
        private ChocoCheckConfigSAT _satisfiabilityChecker;
        private ChocoVariantGenerator _variantGenerator;

        public ChocoSolverFacade(JavaSolverAdapter javaSolverAdapter) { _javaSolverAdapter = javaSolverAdapter; }

        public ICheckConfigSAT SatisfiabilityChecker
        {
            get
            {
                return _satisfiabilityChecker ?? (_satisfiabilityChecker = new ChocoCheckConfigSAT(_javaSolverAdapter));
            }
        }

        public IVariantGenerator VariantGenerator
        {
            get { return _variantGenerator ?? (_variantGenerator = new ChocoVariantGenerator(_javaSolverAdapter)); }
        }
    }
}