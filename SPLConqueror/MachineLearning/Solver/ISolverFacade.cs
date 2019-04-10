using System.Collections.Generic;

namespace MachineLearning.Solver
{
    public interface ISolverFacade
    {
        ICheckConfigSAT SatisfiabilityChecker { get; }
        IVariantGenerator VariantGenerator { get; }
        void SetParameters(Dictionary<string, string> parameters);
    }
}
