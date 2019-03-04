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
            throw new NotImplementedException();
        }

        public List<List<BinaryOption>> GenerateUpToNFast(VariabilityModel vm, int n)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public List<BinaryOption> GenerateConfigWithoutOption(BinaryOption optionToBeRemoved,
            List<BinaryOption> originalConfig, out List<BinaryOption> removedElements,
            VariabilityModel vm)
        {
            throw new NotImplementedException();
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
