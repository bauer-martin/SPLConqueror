using System;
using System.Diagnostics;
using System.IO;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class JavaSolverAdapter
    {
        private const string ERROR_PREFIX = "error: ";
        private Process _javaProcess;
        private StreamReader _javaOutput;
        private StreamReader _javaError;
        private StreamWriter _javaInput;
        private string _loadedVmName;
        private SolverType _selectedSolver;
        private readonly string _pathToJar;

        public JavaSolverAdapter(string pathToJar)
        {
            _pathToJar = pathToJar;
        }

        private void Setup()
        {
            _javaProcess = new Process
            {
                StartInfo =
                {
                    FileName = "/usr/bin/java",
                    Arguments = $"-jar {_pathToJar}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                }
            };
            _javaProcess.Start();
            _javaOutput = _javaProcess.StandardOutput;
            _javaError = _javaProcess.StandardError;
            _javaInput = _javaProcess.StandardInput;
        }

        internal string Execute(String command)
        {
            if (_javaProcess == null) Setup();
            _javaInput.WriteLine(command);
            string response = _javaOutput.ReadLine();
            if (response == null)
            {
                string errorMessage = _javaError.ReadToEnd();
                GlobalState.logError.logLine(errorMessage);
                throw new JavaException("Jar execution terminated; see error log.");
            }
            ThrowExceptionIfError(response);
            return response;
        }

        public void TerminateJavaProcess()
        {
            if (_javaProcess == null || _javaProcess.HasExited) return;
            _javaInput.WriteLine("exit");
            _javaProcess.WaitForExit();
            _javaProcess = null;
            _javaOutput = null;
            _javaError = null;
            _javaInput = null;
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

        public void ThrowExceptionIfError(String response)
        {
            if (response.StartsWith(ERROR_PREFIX))
                throw new JavaException(response);
        }
    }
}
