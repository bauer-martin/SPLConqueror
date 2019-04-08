using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class JavaBasedVariantGenerator : IVariantGenerator
    {
        private readonly ExternalSolverAdapter _adapter;
        private readonly SolverType _solverType;
        private readonly IOptionCoding _optionCoding;

        public JavaBasedVariantGenerator(ExternalSolverAdapter adapter, SolverType solverType,
            IOptionCoding optionCoding)
        {
            _adapter = adapter;
            _solverType = solverType;
            _optionCoding = optionCoding;
        }

        public List<Configuration> GenerateAllVariants(VariabilityModel vm, List<ConfigurationOption> optionsToConsider)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(_solverType);
            _adapter.SetOptionCoding(_optionCoding);
            string optionsString = _optionCoding.EncodeOptions(optionsToConsider, vm);
            string response = _adapter.Execute($"generate-all-variants {optionsString}");
            List<List<BinaryOption>> allVariants = _optionCoding.DecodeBinaryOptionsList(response, vm);
            return allVariants.Select(binarySelection => new Configuration(binarySelection)).ToList();
        }

        public List<List<BinaryOption>> GenerateUpToN(VariabilityModel vm, int n)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(_solverType);
            _adapter.SetOptionCoding(_optionCoding);
            string response = _adapter.Execute($"generate-up-to {n}");
            List<List<BinaryOption>> optimalConfigs = _optionCoding.DecodeBinaryOptionsList(response, vm);
            return optimalConfigs;
        }

        public List<BinaryOption> FindMinimizedConfig(List<BinaryOption> config, VariabilityModel vm,
            List<BinaryOption> unWantedOptions)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(_solverType);
            _adapter.SetOptionCoding(_optionCoding);
            string optionsString = _optionCoding.EncodeOptions(config, vm);
            string command;
            if (unWantedOptions == null)
            {
                command = $"find-minimized-config {optionsString}";
            }
            else
            {
                string unwantedOptionsString = _optionCoding.EncodeOptions(unWantedOptions, vm);
                command = $"find-minimized-config {optionsString} {unwantedOptionsString}";
            }
            string response = _adapter.Execute(command);
            List<BinaryOption> optimalConfig = _optionCoding.DecodeBinaryOptions(response, vm);
            return optimalConfig;
        }

        public List<List<BinaryOption>> FindAllMaximizedConfigs(List<BinaryOption> config, VariabilityModel vm,
            List<BinaryOption> unwantedOptions)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(_solverType);
            _adapter.SetOptionCoding(_optionCoding);
            string command;
            if (config == null)
            {
                command = $"find-all-maximized-configs";
            }
            else
            {
                string optionsString = _optionCoding.EncodeOptions(config, vm);
                if (unwantedOptions == null)
                {
                    command = $"find-all-maximized-configs {optionsString}";
                }
                else
                {
                    string unwantedOptionsString = _optionCoding.EncodeOptions(unwantedOptions, vm);
                    command = $"find-all-maximized-configs {optionsString} {unwantedOptionsString}";
                }
            }
            string response = _adapter.Execute(command);
            List<List<BinaryOption>> optimalConfigs = _optionCoding.DecodeBinaryOptionsList(response, vm);
            return optimalConfigs;
        }

        public List<BinaryOption> GenerateConfigWithoutOption(BinaryOption optionToBeRemoved,
            List<BinaryOption> originalConfig, out List<BinaryOption> removedElements,
            VariabilityModel vm)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(_solverType);
            _adapter.SetOptionCoding(_optionCoding);
            string optionsString = _optionCoding.EncodeOptions(originalConfig, vm);
            string response = _adapter.Execute(
                $"generate-config-without-option {optionsString} {optionToBeRemoved.Name}");
            string[] tokens = response.Split(' ');
            List<BinaryOption> optimalConfig = _optionCoding.DecodeBinaryOptions(tokens[0], vm);
            removedElements = _optionCoding.DecodeBinaryOptions(tokens[1], vm);
            return optimalConfig;
        }

        public IBucketSession CreateBucketSession(VariabilityModel vm)
        {
            return new ExternalBucketSession(vm, _adapter, _solverType, _optionCoding);
        }
    }
}