using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class JavaBasedVariantGenerator : IVariantGenerator
    {
        private readonly ExternalSolverAdapter _adapter;
        private readonly SolverType _solverType;

        public JavaBasedVariantGenerator(ExternalSolverAdapter adapter, SolverType solverType)
        {
            _adapter = adapter;
            _solverType = solverType;
        }

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
            _adapter.SetSolver(_solverType);
            string optionsString = String.Join(",", optionsToConsider.Select(o => o.Name));
            string response = _adapter.Execute($"generate-all-variants {optionsString}");
            List<List<BinaryOption>> allVariants = ParseBinaryConfigs(response, vm);
            return allVariants.Select(binarySelection => new Configuration(binarySelection)).ToList();
        }

        public List<List<BinaryOption>> GenerateUpToN(VariabilityModel vm, int n)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(_solverType);
            string response = _adapter.Execute($"generate-up-to {n}");
            List<List<BinaryOption>> optimalConfigs = ParseBinaryConfigs(response, vm);
            return optimalConfigs;
        }

        public List<BinaryOption> FindMinimizedConfig(List<BinaryOption> config, VariabilityModel vm,
            List<BinaryOption> unWantedOptions)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(_solverType);
            string optionsString = String.Join(",", config.Select(o => o.Name));
            string command;
            if (unWantedOptions == null)
            {
                command = $"find-minimized-config {optionsString}";
            }
            else
            {
                string unwantedOptionsString = String.Join(",", unWantedOptions.Select(o => o.Name));
                command = $"find-minimized-config {optionsString} {unwantedOptionsString}";
            }
            string response = _adapter.Execute(command);
            List<BinaryOption> optimalConfig = ParseBinaryOptions(response, vm);
            return optimalConfig;
        }

        public List<List<BinaryOption>> FindAllMaximizedConfigs(List<BinaryOption> config, VariabilityModel vm,
            List<BinaryOption> unwantedOptions)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(_solverType);
            string command;
            if (config == null)
            {
                command = $"find-all-maximized-configs";
            }
            else
            {
                string optionsString = String.Join(",", config.Select(o => o.Name));
                if (unwantedOptions == null)
                {
                    command = $"find-all-maximized-configs {optionsString}";
                }
                else
                {
                    string unwantedOptionsString = String.Join(",", unwantedOptions.Select(o => o.Name));
                    command = $"find-all-maximized-configs {optionsString} {unwantedOptionsString}";
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
            _adapter.SetSolver(_solverType);
            string optionsString = String.Join(",", originalConfig.Select(o => o.Name));
            string response = _adapter.Execute(
                $"generate-config-without-option {optionsString} {optionToBeRemoved.Name}");
            string[] tokens = response.Split(' ');
            List<BinaryOption> optimalConfig = ParseBinaryOptions(tokens[0], vm);
            removedElements = ParseBinaryOptions(tokens[1], vm);
            return optimalConfig;
        }

        public IBucketSession CreateBucketSession(VariabilityModel vm)
        {
            return new ExternalBucketSession(vm, _adapter, _solverType);
        }
    }
}