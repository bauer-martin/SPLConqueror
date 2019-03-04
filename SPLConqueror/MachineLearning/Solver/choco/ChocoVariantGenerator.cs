using System;
using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class ChocoVariantGenerator : IVariantGenerator
    {
        private readonly JavaSolverAdapter _adapter;

        public ChocoVariantGenerator(JavaSolverAdapter adapter) { _adapter = adapter; }

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

        public List<BinaryOption> MinimizeConfig(List<BinaryOption> config, VariabilityModel vm, bool minimize,
            List<BinaryOption> unWantedOptions)
        {
            throw new NotImplementedException();
        }

        public List<List<BinaryOption>> MaximizeConfig(List<BinaryOption> config, VariabilityModel vm, bool minimize,
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
