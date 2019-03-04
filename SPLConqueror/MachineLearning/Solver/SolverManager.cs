using System;
using System.Collections.Generic;

namespace MachineLearning.Solver
{
    public enum SolverType
    {
        MICROSOFT_SOLVER_FOUNDATION,
        Z3,
        CHOCO
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
        private static ISolverFactory _solverFactory;

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
        }

        public static void SetSelectedSolver(string name)
        {
            if (!_solverTypesByName.ContainsKey(name))
                throw new ArgumentOutOfRangeException($"The solver '{name}' was not found. "
                    + $"Please specify one of the following: {String.Join(", ", _solverTypesByName.Keys)}");
            _selectedSolverType = _solverTypesByName[name];
            switch (_selectedSolverType)
            {
                case SolverType.MICROSOFT_SOLVER_FOUNDATION:
                    _solverFactory = new MSFSolverFactory();
                    break;
                case SolverType.Z3:
                    _solverFactory = new Z3SolverFactory();
                    break;
                case SolverType.CHOCO:
                    if (_javaSolverAdapter == null)
                    {
                        _javaSolverAdapter = new JavaSolverAdapter();
                    }
                    _solverFactory = new ChocoSolverFactory(_javaSolverAdapter);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static ICheckConfigSAT SatisfiabilityChecker
        {
            get
            {
                if (_solverFactory == null) throw new InvalidOperationException("solver has not been set");
                return _solverFactory.CreateSatisfiabilityChecker();
            }
        }

        public static IVariantGenerator VariantGenerator
        {
            get
            {
                if (_solverFactory == null) throw new InvalidOperationException("solver has not been set");
                return _solverFactory.CreateVariantGenerator();
            }
        }
    }
}
