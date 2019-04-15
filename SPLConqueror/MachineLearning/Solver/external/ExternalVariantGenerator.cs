using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    class ExternalVariantGenerator : IVariantGenerator
    {
        private readonly ExternalSolverAdapter _adapter;
        private readonly SolverType _solverType;
        private readonly IOptionCoding _optionCoding;
        private readonly VariabilityModel _vm;

        internal ExternalVariantGenerator(ExternalSolverAdapter adapter, SolverType solverType,
            IOptionCoding optionCoding, VariabilityModel vm)
        {
            _adapter = adapter;
            _solverType = solverType;
            _optionCoding = optionCoding;
            _vm = vm;
        }

        public List<Configuration> GenerateAllVariants(List<ConfigurationOption> optionsToConsider)
        {
            _adapter.LoadVm(_vm);
            _adapter.SetSolver(_solverType);
            _adapter.SetOptionCoding(_optionCoding);
            string optionsString = _optionCoding.EncodeOptions(optionsToConsider, _vm);
            string response = _adapter.Execute($"generate-all-variants {optionsString}");
            List<List<BinaryOption>> allVariants = _optionCoding.DecodeBinaryOptionsList(response, _vm);
            return allVariants.Select(binarySelection => new Configuration(binarySelection)).ToList();
        }

        public List<List<BinaryOption>> GenerateUpToN(int n)
        {
            _adapter.LoadVm(_vm);
            _adapter.SetSolver(_solverType);
            _adapter.SetOptionCoding(_optionCoding);
            string response = _adapter.Execute($"generate-up-to {n}");
            List<List<BinaryOption>> optimalConfigs = _optionCoding.DecodeBinaryOptionsList(response, _vm);
            return optimalConfigs;
        }

        public List<BinaryOption> FindMinimizedConfig(List<BinaryOption> config, List<BinaryOption> unWantedOptions)
        {
            _adapter.LoadVm(_vm);
            _adapter.SetSolver(_solverType);
            _adapter.SetOptionCoding(_optionCoding);
            string optionsString = _optionCoding.EncodeOptions(config, _vm);
            string command;
            if (unWantedOptions == null)
            {
                command = $"find-minimized-config {optionsString}";
            }
            else
            {
                string unwantedOptionsString = _optionCoding.EncodeOptions(unWantedOptions, _vm);
                command = $"find-minimized-config {optionsString} {unwantedOptionsString}";
            }
            string response = _adapter.Execute(command);
            List<BinaryOption> optimalConfig = _optionCoding.DecodeBinaryOptions(response, _vm);
            return optimalConfig;
        }

        public List<List<BinaryOption>> FindAllMaximizedConfigs(List<BinaryOption> config,
            List<BinaryOption> unwantedOptions)
        {
            _adapter.LoadVm(_vm);
            _adapter.SetSolver(_solverType);
            _adapter.SetOptionCoding(_optionCoding);
            string command;
            if (config == null)
            {
                command = $"find-all-maximized-configs";
            }
            else
            {
                string optionsString = _optionCoding.EncodeOptions(config, _vm);
                if (unwantedOptions == null)
                {
                    command = $"find-all-maximized-configs {optionsString}";
                }
                else
                {
                    string unwantedOptionsString = _optionCoding.EncodeOptions(unwantedOptions, _vm);
                    command = $"find-all-maximized-configs {optionsString} {unwantedOptionsString}";
                }
            }
            string response = _adapter.Execute(command);
            List<List<BinaryOption>> optimalConfigs = _optionCoding.DecodeBinaryOptionsList(response, _vm);
            return optimalConfigs;
        }

        public List<BinaryOption> GenerateConfigWithoutOption(BinaryOption optionToBeRemoved,
            List<BinaryOption> originalConfig, out List<BinaryOption> removedElements)
        {
            _adapter.LoadVm(_vm);
            _adapter.SetSolver(_solverType);
            _adapter.SetOptionCoding(_optionCoding);
            string optionsString = _optionCoding.EncodeOptions(originalConfig, _vm);
            string encodedOption = _optionCoding.EncodeOption(optionToBeRemoved, _vm);
            string response = _adapter.Execute(
                $"generate-config-without-option {optionsString} {encodedOption}");
            string[] tokens = response.Split(' ');
            List<BinaryOption> optimalConfig = _optionCoding.DecodeBinaryOptions(tokens[0], _vm);
            removedElements = _optionCoding.DecodeBinaryOptions(tokens[1], _vm);
            return optimalConfig;
        }

        public IBucketSession CreateBucketSession()
        {
            return new ExternalBucketSession(_vm, _adapter, _solverType, _optionCoding);
        }
    }
}
