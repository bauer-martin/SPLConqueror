namespace MachineLearning.Solver
{
    public class ExternalSolverFacade : ISolverFacade
    {
        private readonly ExternalSolverAdapter _externalSolverAdapter;
        private ExternalCheckConfigSAT _satisfiabilityChecker;
        private JavaBasedVariantGenerator _variantGenerator;
        private readonly SolverType _solverType;

        public ExternalSolverFacade(ExternalSolverAdapter externalSolverAdapter, SolverType solverType)
        {
            _externalSolverAdapter = externalSolverAdapter;
            _solverType = solverType;
        }

        public ICheckConfigSAT SatisfiabilityChecker
        {
            get
            {
                return _satisfiabilityChecker
                    ?? (_satisfiabilityChecker = new ExternalCheckConfigSAT(_externalSolverAdapter, _solverType));
            }
        }

        public IVariantGenerator VariantGenerator
        {
            get
            {
                return _variantGenerator
                    ?? (_variantGenerator = new JavaBasedVariantGenerator(_externalSolverAdapter, _solverType));
            }
        }
    }
}
