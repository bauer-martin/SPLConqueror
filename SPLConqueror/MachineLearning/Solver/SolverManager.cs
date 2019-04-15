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

        public static SolverType SetupSolver(string str)
        {
            string solverName;
            Dictionary<string, string> parameters;
            InterpretCommand(str, out solverName, out parameters);
            if (!_solverTypesByName.ContainsKey(solverName))
                throw new ArgumentOutOfRangeException($"The solver '{solverName}' was not found. "
                    + $"Please specify one of the following: {String.Join(", ", _solverTypesByName.Keys)}");
            SolverType solverType = _solverTypesByName[solverName];
            SetupSolver(solverType, parameters);
            return solverType;
        }

        private static void InterpretCommand(string str, out string solverName,
            out Dictionary<string, string> parameters)
        {
            if (str.Length == 0)
            {
                throw new InvalidOperationException("solver name is missing");
            }
            string[] tokens = str.Split(new[] {' '}, 2);
            solverName = tokens[0];
            parameters = new Dictionary<string, string>();
            if (tokens.Length > 1)
            {
                string parameterString = tokens[1];
                List<string> parameterTokens = Tokenize(parameterString, new HashSet<char> {' ', ':'});
                if (parameterTokens.Count % 2 != 0)
                {
                    throw new InvalidOperationException("all solver parameters have to be named (key:value)");
                }
                for (int i = 0; i < parameterTokens.Count; i += 2)
                {
                    parameters[parameterTokens[i]] = parameterTokens[i + 1];
                }
            }
        }

        private static List<string> Tokenize(string str, ICollection<char> delimiters)
        {
            int startIndex = 0;
            List<string> tokens = new List<string>();
            bool isQuoted = false;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (delimiters.Contains(c) && !isQuoted)
                {
                    // remove quotation marks if necessary
                    string token = str[startIndex] == '"'
                        ? str.Substring(startIndex + 1, i - startIndex - 2)
                        : str.Substring(startIndex, i - startIndex);
                    tokens.Add(token);
                    startIndex = i + 1;
                }
                else if (c == '"')
                {
                    isQuoted = !isQuoted;
                }
            }
            tokens.Add(str.Substring(startIndex, str.Length - startIndex));
            return tokens;
        }

        public static void SetupSolver(SolverType solverType, Dictionary<string, string> parameters = null)
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
                    if (parameters == null || !parameters.ContainsKey(SolverParameterKeys.EXECUTABLE_PATH))
                    {
                        throw new ArgumentException("path to external solver executable must be specified");
                    }
                    string executablePath = parameters[SolverParameterKeys.EXECUTABLE_PATH];
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
            facade.SetParameters(parameters);
            _solverFacades[solverType] = facade;
        }

        public static void SetDefaultSolver(SolverType solverType, Dictionary<string, string> parameters = null)
        {
            if (!_solverFacades.ContainsKey(solverType)) SetupSolver(solverType, parameters);
            _selectedSolverType = solverType;
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

        public static IVariantGenerator DefaultVariantGenerator { get { return DefaultSolverFacade.VariantGenerator; } }
    }
}
