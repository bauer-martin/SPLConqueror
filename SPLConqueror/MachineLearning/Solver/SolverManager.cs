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
        ORTOOLS = 5,
        OPTIMATHSAT = 6
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
                case SolverType.ORTOOLS:
                    return "ortools";
                case SolverType.OPTIMATHSAT:
                    return "optimathsat";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static class SolverManager
    {
        private static readonly Dictionary<string, SolverType> _solverTypesByName;

        private static SolverType _selectedSolverType;
        private static readonly Dictionary<SolverType, ISolverFacade> _solverFacades;
        private static readonly Dictionary<string, ExternalSolverAdapter> _externalSolverAdapters;

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
            _solverFacades = new Dictionary<SolverType, ISolverFacade>();
            _externalSolverAdapters = new Dictionary<string, ExternalSolverAdapter>();
        }

        public static void SetSelectedSolver(string str)
        {
            string[] tokens = str.Split(new[] {' '}, 2);
            string name = tokens[0];
            if (!_solverTypesByName.ContainsKey(name))
                throw new ArgumentOutOfRangeException($"The solver '{name}' was not found. "
                    + $"Please specify one of the following: {String.Join(", ", _solverTypesByName.Keys)}");
            _selectedSolverType = _solverTypesByName[name];
            SetupSolverFacade(_selectedSolverType, tokens);
        }

        private static void SetupSolverFacade(SolverType solverType, string[] tokens)
        {
            if (_solverFacades.ContainsKey(solverType)) return;
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
                {
                    if (tokens.Length < 2)
                    {
                        throw new ArgumentException("path to external solver executable must be specified");
                    }
                    string executablePath = tokens[1];
                    ExternalSolverAdapter adapter;
                    if (_externalSolverAdapters.ContainsKey(executablePath))
                    {
                        adapter = _externalSolverAdapters[executablePath];
                    }
                    else
                    {
                        adapter = new ExternalSolverAdapter(executablePath);
                        _externalSolverAdapters[executablePath] = adapter;
                    }
                    facade = new ExternalSolverFacade(adapter, solverType);
                    break;
                }
                case SolverType.JACOP: goto case SolverType.CHOCO;
                case SolverType.ORTOOLS: goto case SolverType.CHOCO;
                case SolverType.OPTIMATHSAT: goto case SolverType.CHOCO;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _solverFacades[solverType] = facade;
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

        public static ISolverFacade GetSolverFacade(SolverType solverType) { return _solverFacades[solverType]; }

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
