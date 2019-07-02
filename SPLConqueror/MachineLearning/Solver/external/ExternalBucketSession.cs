using System.Collections.Generic;
using System.Text;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    class ExternalBucketSession : IBucketSession
    {
        private readonly VariabilityModel _vm;
        private readonly ExternalSolverAdapter _adapter;
        private readonly SolverType _solverType;
        private readonly IOptionCoding _optionCoding;

        internal ExternalBucketSession(VariabilityModel vm, ExternalSolverAdapter adapter, SolverType solverType,
            IOptionCoding optionCoding)
        {
            _vm = vm;
            _adapter = adapter;
            _solverType = solverType;
            _optionCoding = optionCoding;
        }

        ~ExternalBucketSession() { Reset(); }

        public List<BinaryOption> GenerateConfiguration(int numberSelectedFeatures,
            Dictionary<List<BinaryOption>, int> featureWeight)
        {
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
                    featureWeightString.Append(_optionCoding.EncodeOptions(pair.Key, _vm));
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
            List<BinaryOption> config = _optionCoding.DecodeBinaryOptions(tokens[0], _vm);
            return config;
        }

        private void Reset()
        {
            string response = _adapter.Execute("clear-bucket-cache");
            _adapter.ThrowExceptionIfError(response);
        }
    }
}
