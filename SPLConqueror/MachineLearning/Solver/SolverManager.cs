using System;
using System.Collections.Generic;

namespace MachineLearning.Solver
{
    public enum SolverType
    {
        MICROSOFT_SOLVER_FOUNDATION = 1,
        Z3 = 2,
        CHOCO = 3,
        JACOP = 4,
        OR_TOOLS = 5
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
                case SolverType.JACOP:
                    return "jacop";
                case SolverType.OR_TOOLS:
                    return "or-tools";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static class SolverManager
    {
        private static readonly Dictionary<string, SolverType> _solverTypesByName;

        private static SolverType _selectedSolverType;
        private static readonly Dictionary<SolverType, ISolverFacade> _solverFacade;

        private static string _pathToExternalSolverExecutable;
        private static ExternalSolverAdapter _externalSolverAdapter;

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
            _solverFacade = new Dictionary<SolverType, ISolverFacade>();
        }

        public static void SetSelectedSolver(string str)
        {
            string[] tokens = str.Split(new[] {' '}, 2);
            string name = tokens[0];
            if (!_solverTypesByName.ContainsKey(name))
                throw new ArgumentOutOfRangeException($"The solver '{name}' was not found. "
                    + $"Please specify one of the following: {String.Join(", ", _solverTypesByName.Keys)}");
            _selectedSolverType = _solverTypesByName[name];

            // parse additional arguments
            switch (_selectedSolverType)
            {
                case SolverType.MICROSOFT_SOLVER_FOUNDATION:
                    break;
                case SolverType.Z3:
                    break;
                case SolverType.CHOCO:
                    SetExternalSolverExecutablePath(tokens);
                    break;
                case SolverType.JACOP:
                    SetExternalSolverExecutablePath(tokens);
                    break;
                case SolverType.OR_TOOLS:
                    SetExternalSolverExecutablePath(tokens);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void SetExternalSolverExecutablePath(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("path to external solver executable must be specified");
            }
            _pathToExternalSolverExecutable = args[1];
        }

        public static ISolverFacade DefaultSolverFacade
        {
            get
            {
                if (_selectedSolverType == 0)
                    throw new InvalidOperationException("solver type has not been set");
                return GetSolverFacade(_selectedSolverType);
            }
        }

        public static ISolverFacade GetSolverFacade(SolverType solverType)
        {
            if (!_solverFacade.ContainsKey(solverType))
            {
                ISolverFacade facade;
                switch (solverType)
                {
                    case SolverType.MICROSOFT_SOLVER_FOUNDATION:
                        facade = new MSFSolverFacade();
                        break;
                    case SolverType.Z3:
                        facade = new Z3SolverFacade();
                        break;
                    case SolverType.CHOCO:
                        facade = CreateExternalSolverFacade(solverType);
                        break;
                    case SolverType.JACOP:
                        facade = CreateExternalSolverFacade(solverType);
                        break;
                    case SolverType.OR_TOOLS:
                        facade = CreateExternalSolverFacade(solverType);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _solverFacade[solverType] = facade;
            }
            return _solverFacade[solverType];
        }

        private static ExternalSolverFacade CreateExternalSolverFacade(SolverType solverType)
        {
            if (_externalSolverAdapter == null)
            {
                _externalSolverAdapter = new ExternalSolverAdapter(_pathToExternalSolverExecutable);
            }
            return new ExternalSolverFacade(_externalSolverAdapter, solverType);
        }

        public static ICheckConfigSAT DefaultSatisfiabilityChecker
        {
            get { return DefaultSolverFacade.SatisfiabilityChecker; }
        }

        public static IVariantGenerator DefaultVariantGenerator
        {
            get { return DefaultSolverFacade.VariantGenerator; }
        }
    }
}
