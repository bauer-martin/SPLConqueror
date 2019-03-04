using System;
using System.Diagnostics;
using System.IO;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    internal class JavaAdapter
    {
        private Process _javaProcess;
        private readonly string _pathToJar;
        private StreamReader _javaOutput;
        private StreamReader _javaError;
        private StreamWriter _javaInput;

        internal JavaAdapter(string pathToJar) { this._pathToJar = pathToJar; }

        internal void Start()
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
            _javaInput.WriteLine(command);
            string response = _javaOutput.ReadLine();
            if (response == null)
            {
                string errorMessage = _javaError.ReadToEnd();
                GlobalState.logError.logLine(errorMessage);
                throw new JavaException("Jar execution terminated; see error log.");
            }
            return response;
        }

        internal void Terminate()
        {
            _javaInput.WriteLine("exit");
            _javaProcess.WaitForExit();
        }
    }
}
