using System;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class JavaSolverAdapter
    {
        private const string ERROR_PREFIX = "error: ";
        private JavaAdapter _adapter;
        private string _loadedVmName;
        private SolverType _selectedSolver;

        private void Setup()
        {
            _adapter = new JavaAdapter(
                "/Users/martinbauer/Documents/Education/Master/Semester10/Masterarbeit/"
                + "spl-conqueror-solvers-java/build/libs/spl-conqueror-solver-java-all-1.0-SNAPSHOT.jar");
            _adapter.Start();
        }

        public string Execute(String command)
        {
            if (_adapter == null) Setup();
            string response = _adapter.Execute(command);
            ThrowExceptionIfError(response);
            return response;
        }

        public void Terminate() { _adapter?.Terminate(); }

        public void LoadVm(VariabilityModel vm)
        {
            if (_loadedVmName != null)
            {
                if (_loadedVmName.Equals(vm.Name)) return;
                throw new InvalidOperationException("switching variability model is not supported");
            }
            if (_adapter == null) Setup();
            string vmPath = vm.Path;
            string response = _adapter.Execute($"load-vm {vmPath}");
            ThrowExceptionIfError(response);
            _loadedVmName = vm.Name;
        }

        public void SetSolver(SolverType solverType)
        {
            if (solverType == _selectedSolver) return;
            if (_adapter == null) Setup();
            string response = _adapter.Execute($"select-solver {solverType.GetName()}");
            ThrowExceptionIfError(response);
            _selectedSolver = solverType;
        }

        private static void ThrowExceptionIfError(String response)
        {
            if (response.StartsWith(ERROR_PREFIX))
                throw new JavaException(response);
        }
    }
}
