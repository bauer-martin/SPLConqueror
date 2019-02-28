using System;
using System.Diagnostics;
using System.IO;

namespace MachineLearning.Solver
{
    internal class JavaAdapter
    {
        private Process _javaProcess;
        private readonly string _pathToJar;
        private StreamReader _javaOutput;
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
                    RedirectStandardInput = true
                }
            };
            _javaProcess.Start();
            _javaOutput = _javaProcess.StandardOutput;
            _javaInput = _javaProcess.StandardInput;
        }

        internal string Execute(String command)
        {
            _javaInput.WriteLine(command);
            return _javaOutput.ReadLine();
        }

        internal void Terminate()
        {
            _javaInput.WriteLine("exit");
            _javaProcess.WaitForExit();
        }
    }
}
