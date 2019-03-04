namespace MachineLearning.Solver
{
    public class MSFSolverFactory : ISolverFactory
    {
        public ICheckConfigSAT CreateSatisfiabilityChecker() { return new MSFCheckConfigSAT(); }

        public IVariantGenerator CreateVariantGenerator() { return new MSFVariantGenerator(); }
    }
}
