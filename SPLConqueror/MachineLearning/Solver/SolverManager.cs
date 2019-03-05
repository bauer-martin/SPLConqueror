using System;
using System.Collections.Generic;

namespace MachineLearning.Solver
{
    public enum SolverType
    {
        MICROSOFT_SOLVER_FOUNDATION = 1,
        Z3 = 2,
        CHOCO = 3
    }

    public static class SolverTypeMethods
    {
        public static String GetName(this SolverType solverType)
        {
            switch (solverType)
            {
                case SolverType.MICROSOFT_SOLVER_FOUNDATION:
                    return "msf";
                case SolverType.Z3:
                    return "z3";
                case SolverType.CHOCO:
                    return "choco";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static class SolverManager
    {
        private static readonly Dictionary<string, SolverType> _solverTypesByName;

        private static SolverType _selectedSolverType;
        private static readonly Dictionary<SolverType, ISolverFactory> _solverFactories;

        // java-based solvers need access to JVM
        private static JavaSolverAdapter _javaSolverAdapter;

        static SolverManager()
        {
            _solverTypesByName = new Dictionary<string, SolverType>();
            foreach (SolverType solverType in Enum.GetValues(typeof(SolverType)))
            {
                _solverTypesByName[solverType.GetName()] = solverType;
            }
            _solverTypesByName["smt"] = SolverType.Z3;
            _solverTypesByName["csp"] = SolverType.MICROSOFT_SOLVER_FOUNDATION;
            _solverTypesByName["microsoft solver foundation"] = SolverType.MICROSOFT_SOLVER_FOUNDATION;
            _solverFactories = new Dictionary<SolverType, ISolverFactory>();
        }

        public static void SetSelectedSolver(string name)
        {
            if (!_solverTypesByName.ContainsKey(name))
                throw new ArgumentOutOfRangeException($"The solver '{name}' was not found. "
                    + $"Please specify one of the following: {String.Join(", ", _solverTypesByName.Keys)}");
            _selectedSolverType = _solverTypesByName[name];
        }

        public static ISolverFactory DefaultSolverFactory
        {
            get
            {
                if (_selectedSolverType == 0)
                    throw new InvalidOperationException("solver type has not been set");
                return GetSolverFactory(_selectedSolverType);
            }
        }

        public static ISolverFactory GetSolverFactory(SolverType solverType)
        {
            if (!_solverFactories.ContainsKey(solverType))
            {
                ISolverFactory factory;
                switch (solverType)
                {
                    case SolverType.MICROSOFT_SOLVER_FOUNDATION:
                        factory = new MSFSolverFactory();
                        break;
                    case SolverType.Z3:
                        factory = new Z3SolverFactory();
                        break;
                    case SolverType.CHOCO:
                        if (_javaSolverAdapter == null)
                        {
                            _javaSolverAdapter = new JavaSolverAdapter();
                        }
                        factory = new ChocoSolverFactory(_javaSolverAdapter);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _solverFactories[solverType] = factory;
            }
            return _solverFactories[solverType];
        }

        public static ICheckConfigSAT DefaultSatisfiabilityChecker
        {
            get { return DefaultSolverFactory.CreateSatisfiabilityChecker(); }
        }

        public static IVariantGenerator DefaultVariantGenerator
        {
            get { return DefaultSolverFactory.CreateVariantGenerator(); }
        }
    }
}
