namespace MachineLearning.Solver
{
    public class Z3SolverFactory : ISolverFactory
    {
        public ICheckConfigSAT CreateSatisfiabilityChecker() { return new CheckConfigSATZ3(); }

        public IVariantGenerator CreateVariantGenerator() { return new Z3VariantGenerator(); }
    }
}
