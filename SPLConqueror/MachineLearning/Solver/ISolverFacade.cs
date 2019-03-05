namespace MachineLearning.Solver
{
    public interface ISolverFacade
    {
        ICheckConfigSAT SatisfiabilityChecker { get; }
        IVariantGenerator VariantGenerator { get; }
    }
}
