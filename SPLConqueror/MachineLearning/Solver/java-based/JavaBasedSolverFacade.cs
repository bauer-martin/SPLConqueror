namespace MachineLearning.Solver
{
    public class JavaBasedSolverFacade : ISolverFacade
    {
        private readonly JavaSolverAdapter _javaSolverAdapter;
        private JavaBasedCheckConfigSAT _satisfiabilityChecker;
        private JavaBasedVariantGenerator _variantGenerator;
        private readonly SolverType _solverType;

        public JavaBasedSolverFacade(JavaSolverAdapter javaSolverAdapter, SolverType solverType)
        {
            _javaSolverAdapter = javaSolverAdapter;
            _solverType = solverType;
        }

        public ICheckConfigSAT SatisfiabilityChecker
        {
            get
            {
                return _satisfiabilityChecker
                    ?? (_satisfiabilityChecker = new JavaBasedCheckConfigSAT(_javaSolverAdapter, _solverType));
            }
        }

        public IVariantGenerator VariantGenerator
        {
            get
            {
                return _variantGenerator
                    ?? (_variantGenerator = new JavaBasedVariantGenerator(_javaSolverAdapter, _solverType));
            }
        }
    }
}
