using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class ExternalBucketSession : IBucketSession
    {
        private readonly VariabilityModel _vm;
        private readonly ExternalSolverAdapter _adapter;
        private readonly SolverType _solverType;

        public ExternalBucketSession(VariabilityModel vm, ExternalSolverAdapter adapter, SolverType solverType)
        {
            _vm = vm;
            _adapter = adapter;
            _solverType = solverType;
        }

        ~ExternalBucketSession() => Reset();

        private static List<BinaryOption> ParseBinaryOptions(string str, VariabilityModel vm)
        {
            List<BinaryOption> result;
            if (str.Equals("none"))
            {
                result = null;
            }
            else
            {
                string[] tokens = str.Split(',');
                result = tokens.Select(vm.getBinaryOption).ToList();
            }
            return result;
        }

        public List<BinaryOption> GenerateConfiguration(int numberSelectedFeatures,
            Dictionary<List<BinaryOption>, int> featureWeight)
        {
            _adapter.LoadVm(_vm);
            _adapter.SetSolver(_solverType);
            string command;
            if (featureWeight == null)
            {
                command = $"generate-config-from-bucket {numberSelectedFeatures}";
            }
            else
            {
                StringBuilder featureWeightString = new StringBuilder();
                foreach (KeyValuePair<List<BinaryOption>, int> pair in featureWeight)
                {
                    featureWeightString.Append(String.Join(",", pair.Key.Select(o => o.Name)));
                    featureWeightString.Append("=");
                    featureWeightString.Append(pair.Value);
                    featureWeightString.Append(";");
                }
                if (featureWeightString.Length > 0)
                {
                    featureWeightString.Remove(featureWeightString.Length - 1, 1);
                }
                command = $"generate-config-from-bucket {numberSelectedFeatures} {featureWeightString}";
            }
            string response = _adapter.Execute(command);
            string[] tokens = response.Split(' ');
            List<BinaryOption> config = ParseBinaryOptions(tokens[0], _vm);
            return config;
        }

        private void Reset()
        {
            _adapter.SetSolver(_solverType);
            string response = _adapter.Execute("clear-bucket-cache");
            _adapter.ThrowExceptionIfError(response);
        }
    }
}
