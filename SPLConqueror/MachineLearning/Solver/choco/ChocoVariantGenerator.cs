using System;
using System.Collections.Generic;
using System.Linq;
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

        public List<List<BinaryOption>> DistanceMaximization(VariabilityModel vm,
            List<BinaryOption> minimalConfiguration, int numberToSample, int optionWeight)
        {
            throw new NotImplementedException();
        }

        public List<Configuration> GenerateAllVariants(VariabilityModel vm, List<ConfigurationOption> optionsToConsider)
        {
            throw new NotImplementedException();
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

        public List<BinaryOption> FindConfig(List<BinaryOption> config, VariabilityModel vm, bool minimize,
            List<BinaryOption> unWantedOptions)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(SolverType.CHOCO);
            string optionsString = String.Join(",", config.Select(o => o.Name));
            string optimizationString = minimize ? "minimize" : "maximize";
            string response;
            if (unWantedOptions == null)
            {
                response = _adapter.Execute($"find-optimal-config {optimizationString} {optionsString}");
            }
            else
            {
                string unwantedOptionsString = String.Join(",", unWantedOptions.Select(o => o.Name));
                response = _adapter.Execute(
                    $"find-optimal-config {optimizationString} {optionsString} {unwantedOptionsString}");
            }
            List<BinaryOption> optimalConfig = ParseBinaryOptions(response, vm);
            return optimalConfig;
        }

        public List<List<BinaryOption>> FindAllConfigs(List<BinaryOption> config, VariabilityModel vm, bool minimize,
            List<BinaryOption> unwantedOptions)
        {
            _adapter.LoadVm(vm);
            _adapter.SetSolver(SolverType.CHOCO);
            string optionsString = String.Join(",", config.Select(o => o.Name));
            string optimizationString = minimize ? "minimize" : "maximize";
            string response;
            if (unwantedOptions == null)
            {
                response = _adapter.Execute($"find-all-optimal-configs {optimizationString} {optionsString}");
            }
            else
            {
                string unwantedOptionsString = String.Join(",", unwantedOptions.Select(o => o.Name));
                response = _adapter.Execute(
                    $"find-all-optimal-configs {optimizationString} {optionsString} {unwantedOptionsString}");
            }
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
            throw new NotImplementedException();
        }

        public void ClearCache() { throw new NotImplementedException(); }
    }
}
