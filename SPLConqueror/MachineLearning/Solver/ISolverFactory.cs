namespace MachineLearning.Solver
{
    public interface ISolverFactory
    {
        ICheckConfigSAT CreateSatisfiabilityChecker();
        IVariantGenerator CreateVariantGenerator();
    }
}
