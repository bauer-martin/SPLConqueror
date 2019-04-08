namespace MachineLearning.Solver
{
    public class ExternalSolverFacade : ISolverFacade
    {
        private readonly ExternalSolverAdapter _externalSolverAdapter;
        private ExternalCheckConfigSAT _satisfiabilityChecker;
        private JavaBasedVariantGenerator _variantGenerator;
        private readonly SolverType _solverType;
        private readonly IOptionCoding _optionCoding;

        public ExternalSolverFacade(ExternalSolverAdapter externalSolverAdapter, SolverType solverType)
        {
            _externalSolverAdapter = externalSolverAdapter;
            _solverType = solverType;
            _optionCoding = new OptionNameOptionCoding();
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
                        new JavaBasedVariantGenerator(_externalSolverAdapter, _solverType, _optionCoding));
            }
        }
    }
}
