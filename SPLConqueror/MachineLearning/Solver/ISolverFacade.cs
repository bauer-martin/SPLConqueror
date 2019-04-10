using System.Collections.Generic;

namespace MachineLearning.Solver
{
    public static class SolverParameterKeys
    {
        public const string RANDOM_SEED = "seed";
        public const string HENARD = "henard";
    }

    public interface ISolverFacade
    {
        ICheckConfigSAT SatisfiabilityChecker { get; }
        IVariantGenerator VariantGenerator { get; }
        void SetParameters(Dictionary<string, string> parameters);
    }
}
