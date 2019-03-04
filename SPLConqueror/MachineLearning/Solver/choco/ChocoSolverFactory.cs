namespace MachineLearning.Solver
{
    public class ChocoSolverFactory : ISolverFactory
    {
        private readonly JavaSolverAdapter _javaSolverAdapter;

        public ChocoSolverFactory(JavaSolverAdapter javaSolverAdapter) { _javaSolverAdapter = javaSolverAdapter; }

        public ICheckConfigSAT CreateSatisfiabilityChecker() { return new ChocoCheckConfigSAT(_javaSolverAdapter); }

        public IVariantGenerator CreateVariantGenerator() { return new ChocoVariantGenerator(_javaSolverAdapter); }
    }
}
