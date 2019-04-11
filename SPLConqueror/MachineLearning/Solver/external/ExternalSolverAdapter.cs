using System;
using System.Diagnostics;
using System.IO;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    class ExternalSolverAdapter
    {
        private const string ERROR_PREFIX = "error: ";
        private Process _process;
        private StreamReader _processOutput;
        private StreamReader _processError;
        private StreamWriter _processInput;
        private string _loadedVmName;
        private SolverType _selectedSolver;
        private string _selectedOptionCodingName;
        private readonly string _pathToExecutable;

        internal ExternalSolverAdapter(string pathToExecutable)
        {
            if (!File.Exists(pathToExecutable))
            {
                throw new InvalidOperationException("'" + pathToExecutable + "' does not exist");
            }
            _pathToExecutable = pathToExecutable;
        }

        private void Setup()
        {
            _process = new Process
            {
                StartInfo =
                {
                    FileName = _pathToExecutable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                }
            };
            _process.Start();
            _processOutput = _process.StandardOutput;
            _processError = _process.StandardError;
            _processInput = _process.StandardInput;
        }

        internal string Execute(String command)
        {
            if (_process == null) Setup();
            _processInput.WriteLine(command);
            string response = _processOutput.ReadLine();
            if (response == null)
            {
                string errorMessage = _processError.ReadToEnd();
                GlobalState.logError.logLine(errorMessage);
                throw new ExternalSolverException("external solver execution terminated -> see error log");
            }
            ThrowExceptionIfError(response);
            return response;
        }

        public void TerminateProcess()
        {
            if (_process == null || _process.HasExited) return;
            _processInput.WriteLine("exit");
            _process.WaitForExit();
            _process = null;
            _processOutput = null;
            _processError = null;
            _processInput = null;
            _loadedVmName = null;
            _selectedSolver = 0;
        }

        public void LoadVm(VariabilityModel vm)
        {
            if (_loadedVmName != null)
            {
                if (_loadedVmName.Equals(vm.Name)) return;
                throw new InvalidOperationException("switching variability model is not supported");
            }
            string vmPath = vm.Path;
            string response = Execute($"load-vm {vmPath}");
            ThrowExceptionIfError(response);
            _loadedVmName = vm.Name;
        }

        public void SetSolver(SolverType solverType)
        {
            if (solverType == _selectedSolver) return;
            string response = Execute($"select-solver {solverType.GetName()}");
            ThrowExceptionIfError(response);
            _selectedSolver = solverType;
        }

        public void SetOptionCoding(IOptionCoding optionCoding)
        {
            if (optionCoding.GetName() == _selectedOptionCodingName) return;
            string response = Execute($"select-option-coding {optionCoding.GetName()}");
            ThrowExceptionIfError(response);
            _selectedOptionCodingName = optionCoding.GetName();
        }

        public void ThrowExceptionIfError(String response)
        {
            if (response.StartsWith(ERROR_PREFIX))
                throw new ExternalSolverException(response);
        }
    }
}
