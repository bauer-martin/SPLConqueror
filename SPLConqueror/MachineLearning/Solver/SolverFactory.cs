using System;
using System.Collections.Generic;
using MachineLearning.Solver;

namespace MachineLearning.Solver
{
    public enum SolverType
    {
        MICROSOFT_SOLVER_FOUNDATION,
        Z3
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static class SolverFactory
    {
        private static readonly Dictionary<string, SolverType> _solverTypesByName;

        private static SolverType _selectedSolverType;
        private static ICheckConfigSAT _satisfiabilityChecker;

        static SolverFactory()
        {
            _solverTypesByName = new Dictionary<string, SolverType>();
            foreach (SolverType solverType in Enum.GetValues(typeof(SolverType)))
            {
                _solverTypesByName[solverType.GetName()] = solverType;
            }
        }

        public static void SetSelectedSolver(string name)
        {
            if (!_solverTypesByName.ContainsKey(name))
                throw new ArgumentOutOfRangeException($"The solver '{name}' was not found. "
                    + $"Please specify one of the following: {String.Join(", ", _solverTypesByName.Keys)}");
            _selectedSolverType = _solverTypesByName[name];
        }

        public static ICheckConfigSAT GetSatisfiabilityChecker()
        {
            if (_satisfiabilityChecker == null)
            {
                switch (_selectedSolverType)
                {
                    case SolverType.MICROSOFT_SOLVER_FOUNDATION:
                        _satisfiabilityChecker = new MSFCheckConfigSAT();
                        break;
                    case SolverType.Z3:
                        _satisfiabilityChecker = new CheckConfigSATZ3();
                        break;
                    default:
                        throw new InvalidOperationException(_selectedSolverType.GetName()
                            + " does not support satisfiability checking");
                }
            }
            return _satisfiabilityChecker;
        }
    }
}
