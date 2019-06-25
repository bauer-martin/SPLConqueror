﻿using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public interface IVariantGenerator
    {
        /// <summary>
        /// Generates all valid combinations of all configuration options in the given model.
        /// </summary>
        /// <param name="vm">the variability model containing the binary options and their constraints</param>
        /// <param name="optionsToConsider">the options that should be considered. All other options are ignored</param>
        /// <returns>Returns a list of <see cref="Configuration"/></returns>
        List<Configuration> GenerateAllVariants(VariabilityModel vm, List<ConfigurationOption> optionsToConsider);

        /// <summary>
        /// Generates all valid binary combinations of all binary configurations options in the given model
        /// </summary>
        /// <param name="vm">The variability model containing the binary options and their constraints.</param>
        /// <returns>Returns a list of configurations, in which a configuration is a list of SELECTED binary options (deselected options are not present)</returns>
        List<List<BinaryOption>> GenerateAllVariantsFast(VariabilityModel vm);

        /// <summary>
        /// Generates up to n solutions of the given variability model. 
        /// Note that this method could also generate less than n solutions if the variability model does not contain sufficient solutions.
        /// Moreover, in the case that <code>n &lt; 0</code>, all solutions are generated.
        /// </summary>
        /// <param name="vm">The <see cref="VariabilityModel"/> to obtain solutions for.</param>
        /// <param name="n">The number of solutions to obtain.</param>
        /// <returns>A list of configurations, in which a configuration is a list of SELECTED binary options.</returns>
        List<List<BinaryOption>> GenerateUpToNFast(VariabilityModel vm, int n);

        /// <summary>
        /// Based on a given (partial) configuration and variability model, we search for the smallest (in terms of selected options) valid configuration.
        /// </summary>
        /// <param name="config">The (partial) configuration which needs to be expanded to be valid.</param>
        /// <param name="vm">Variability model containing all options and their constraints.</param>
        /// <param name="unWantedOptions">Binary options that we do not want to become part of the configuration. Might be part if there is no other valid configuration without them.</param>
        /// <returns>The valid configuration (or null if there is none) that satisfies the VM and the goal.</returns>
        List<BinaryOption> FindConfig(List<BinaryOption> config, VariabilityModel vm, List<BinaryOption> unWantedOptions);

        /// <summary>
        /// Based on a given (partial) configuration and variability model, we search for all smallest (in terms of selected binary options) valid configurations.
        /// </summary>
        /// <param name="config">The (partial) configuration which needs to be expanded to be valid.</param>
        /// <param name="vm">Variability model containing all options and their constraints.</param>
        /// <param name="unwantedOptions">Binary options that we do not want to become part of the configuration. Might be part if there is no other valid configuration without them</param>
        /// <returns>A list of configurations that satisfies the VM and the goal (or null if there is none).</returns>
        List<List<BinaryOption>> FindAllConfigs(List<BinaryOption> config, VariabilityModel vm, List<BinaryOption> unwantedOptions);

        /// <summary>
        /// The method aims at finding a configuration which is similar to the given configuration, but does not contain the optionToBeRemoved. If further options need to be removed from the given configuration, they are output in removedElements.
        /// </summary>
        /// <param name="optionToBeRemoved">The binary configuration option that must not be part of the new configuration.</param>
        /// <param name="originalConfig">The configuration for which we want to find a similar one.</param>
        /// <param name="removedElements">If further options need to be removed from the given configuration to build a valid configuration, they are output in this list.</param>
        /// <param name="vm">The variability model containing all options and their constraints.</param>
        /// <returns>A configuration that is valid, similar to the original configuration and does not contain the optionToBeRemoved.</returns>
        List<BinaryOption> GenerateConfigWithoutOption(BinaryOption optionToBeRemoved, List<BinaryOption> originalConfig, out List<BinaryOption> removedElements, VariabilityModel vm);

        /// <summary>
        /// This method returns a configuration with the given number of selected features.
        /// </summary>
        /// <param name="vm">The variability model containing all options and their constraints.</param>
        /// <param name="numberSelectedFeatures">The number of features that should be selected.</param>
        /// <param name="featureWeight">The weight of certain feature combinations.</param>
        /// <param name="lastSampledConfiguration">The last included sampled configuration.</param>
        /// <returns>A list of <see cref="BinaryOption"/>, which should be selected.</returns>
        List<BinaryOption> GenerateConfigurationFromBucket(VariabilityModel vm, int numberSelectedFeatures, Dictionary<List<BinaryOption>, int> featureWeight, Configuration lastSampledConfiguration);

        /// <summary>
        /// This method clears the cache if caches are used.
        /// </summary>
        void ClearCache();
    }
}
