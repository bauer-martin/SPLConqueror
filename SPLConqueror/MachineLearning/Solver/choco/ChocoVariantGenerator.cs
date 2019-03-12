using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class ChocoVariantGenerator : IVariantGenerator
    {
        private readonly JavaSolverAdapter _adapter;

        public ChocoVariantGenerator(JavaSolverAdapter adapter) { _adapter = adapter; }

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

        private static List<List<BinaryOption>> ParseBinaryConfigs(string str, VariabilityModel vm)
        {
            return str.Split(';').Select(s => ParseBinaryOptions(s, vm)).ToList();
        }

        public List<Configuration> GenerateAllVariants(VariabilityModel vm, List<ConfigurationOption> optionsToConsider)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(SolverType.CHOCO);
            string optionsString = String.Join(",", optionsToConsider.Select(o => o.Name));
            string response = _adapter.Execute($"generate-all-variants {optionsString}");
            List<List<BinaryOption>> allVariants = ParseBinaryConfigs(response, vm);
            return allVariants.Select(binarySelection => new Configuration(binarySelection)).ToList();
        }

        public List<List<BinaryOption>> GenerateAllVariantsFast(VariabilityModel vm)
        {
            return GenerateUpToNFast(vm, -1);
        }

        public List<List<BinaryOption>> GenerateUpToNFast(VariabilityModel vm, int n)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(SolverType.CHOCO);
            string response = _adapter.Execute($"generate-up-to {n}");
            List<List<BinaryOption>> optimalConfigs = ParseBinaryConfigs(response, vm);
            return optimalConfigs;
        }

        public List<BinaryOption> FindMinimizedConfig(List<BinaryOption> config, VariabilityModel vm,
            List<BinaryOption> unWantedOptions)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(SolverType.CHOCO);
            string optionsString = String.Join(",", config.Select(o => o.Name));
            string command;
            if (unWantedOptions == null)
            {
                command = $"find-optimal-config {optionsString}";
            }
            else
            {
                string unwantedOptionsString = String.Join(",", unWantedOptions.Select(o => o.Name));
                command = $"find-optimal-config {optionsString} {unwantedOptionsString}";
            }
            string response = _adapter.Execute(command);
            List<BinaryOption> optimalConfig = ParseBinaryOptions(response, vm);
            return optimalConfig;
        }

        public List<List<BinaryOption>> FindAllMaximizedConfigs(List<BinaryOption> config, VariabilityModel vm,
            List<BinaryOption> unwantedOptions)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(SolverType.CHOCO);
            string command;
            if (config == null)
            {
                command = $"find-all-optimal-configs";
            }
            else
            {
                string optionsString = String.Join(",", config.Select(o => o.Name));
                if (unwantedOptions == null)
                {
                    command = $"find-all-optimal-configs {optionsString}";
                }
                else
                {
                    string unwantedOptionsString = String.Join(",", unwantedOptions.Select(o => o.Name));
                    command = $"find-all-optimal-configs {optionsString} {unwantedOptionsString}";
                }
            }
            string response = _adapter.Execute(command);
            List<List<BinaryOption>> optimalConfigs = ParseBinaryConfigs(response, vm);
            return optimalConfigs;
        }

        public List<BinaryOption> GenerateConfigWithoutOption(BinaryOption optionToBeRemoved,
            List<BinaryOption> originalConfig, out List<BinaryOption> removedElements,
            VariabilityModel vm)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(SolverType.CHOCO);
            string optionsString = String.Join(",", originalConfig.Select(o => o.Name));
            string response = _adapter.Execute(
                $"generate-config-without-option {optionsString} {optionToBeRemoved.Name}");
            string[] tokens = response.Split(' ');
            List<BinaryOption> optimalConfig = ParseBinaryOptions(tokens[0], vm);
            removedElements = ParseBinaryOptions(tokens[1], vm);
            return optimalConfig;
        }

        public List<BinaryOption> GenerateConfigurationFromBucket(VariabilityModel vm, int numberSelectedFeatures,
            Dictionary<List<BinaryOption>, int> featureWeight,
            Configuration lastSampledConfiguration)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(SolverType.CHOCO);
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
            List<BinaryOption> config = ParseBinaryOptions(tokens[0], vm);
            return config;
        }

        public void ClearCache()
        {
            _adapter.SetSolver(SolverType.CHOCO);
            string response = _adapter.Execute("clear-bucket-cache");
            _adapter.ThrowExceptionIfError(response);
        }
    }
}
