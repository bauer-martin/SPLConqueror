﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MachineLearning.Learning.LinearProgramming;
using SPLConqueror_Core;
using System.Reflection;
using Microsoft.SolverFoundation.Solvers;
using MicrosoftSolverFoundation;

namespace MachineLearning.Solver
{
    /// <summary>
    /// This class provides methods for generating all configurations of a given variability model.
    /// </summary>
    public class MSFVariantGenerator : IVariantGenerator
    {

        /// <summary>
        /// Generates all valid combinations of all configuration options in the given model.
        /// </summary>
        /// <param name="vm">the variability model containing the binary options and their constraints</param>
        /// <param name="optionsToConsider">the options that should be considered. All other options are ignored</param>
        /// <returns>Returns a list of <see cref="Configuration"/></returns>
        public List<Configuration> GenerateAllVariants(VariabilityModel vm, List<ConfigurationOption> optionsToConsider)
        {
            List<Configuration> allConfigurations = new List<Configuration>();
            Dictionary<CspTerm, bool> variables;
            Dictionary<ConfigurationOption, CspTerm> optionToTerm;
            Dictionary<CspTerm, ConfigurationOption> termToOption;
            ConstraintSystem S = CSPsolver.GetGeneralConstraintSystem(out variables, out optionToTerm, out termToOption, vm);

            ConstraintSolverSolution soln = S.Solve();

            while (soln.HasFoundSolution)
            {
                Dictionary<BinaryOption, BinaryOption.BinaryValue> binOpts = new Dictionary<BinaryOption, BinaryOption.BinaryValue>();
                Dictionary<NumericOption, double> numOpts = new Dictionary<NumericOption, double>();

                foreach (CspTerm ct in variables.Keys)
                {
                    // Ignore all options that should not be considered.
                    if (!optionsToConsider.Contains(termToOption[ct]))
                    {
                        continue;
                    }

                    // If it is a binary option
                    if (variables[ct])
                    {
                        BinaryOption.BinaryValue isSelected = soln.GetIntegerValue(ct) == 1 ? BinaryOption.BinaryValue.Selected : BinaryOption.BinaryValue.Deselected;
                        if (isSelected == BinaryOption.BinaryValue.Selected)
                        {
                            binOpts.Add((BinaryOption)termToOption[ct], isSelected);
                        }
                    }
                    else
                    {
                        numOpts.Add((NumericOption)termToOption[ct], soln.GetIntegerValue(ct));
                    }
                }

                Configuration c = new Configuration(binOpts, numOpts);

                // Check if the non-boolean constraints are satisfied
                if (vm.configurationIsValid(c)
                    && !VariantGeneratorUtilities.IsInConfigurationFile(c, allConfigurations)
                    && VariantGeneratorUtilities.FulfillsMixedConstraints(c, vm))
                {
                    allConfigurations.Add(c);
                }
                soln.GetNext();
            }

            return allConfigurations;
        }

        /// <summary>
        /// Generates up to n valid binary combinations of all binary configuration options in the given model.
        /// In case n < 0 all valid binary combinations will be generated. 
        /// </summary>
        /// <param name="m">The variability model containing the binary options and their constraints.</param>
        /// <param name="n">The maximum number of samples that will be generated.</param>
        /// <returns>Returns a list of configurations, in which a configuration is a list of SELECTED binary options (deselected options are not present)</returns>
        public List<List<BinaryOption>> GenerateUpToN(VariabilityModel m, int n)
        {
            List<List<BinaryOption>> configurations = new List<List<BinaryOption>>();
            List<CspTerm> variables = new List<CspTerm>();
            Dictionary<BinaryOption, CspTerm> elemToTerm = new Dictionary<BinaryOption, CspTerm>();
            Dictionary<CspTerm, BinaryOption> termToElem = new Dictionary<CspTerm, BinaryOption>();
            ConstraintSystem S = CSPsolver.getConstraintSystem(out variables, out elemToTerm, out termToElem, m);

            ConstraintSolverSolution soln = S.Solve();

            // TODO: Better solution than magic number?
            while (soln.HasFoundSolution && (configurations.Count < n || n < 0))
            {
                List<BinaryOption> config = new List<BinaryOption>();
                foreach (CspTerm cT in variables)
                {
                    if (soln.GetIntegerValue(cT) == 1)
                        config.Add(termToElem[cT]);
                }
                //THese should always be new configurations
                //  if(!Configuration.containsBinaryConfiguration(configurations, config))
                configurations.Add(config);

                soln.GetNext();
            }
            return configurations;
        }

        /// <summary>
        /// Simulates a simple method to get valid configurations of binary options of a variability model. The randomness is simulated by the modulu value.
        /// We take only the modulu'th configuration into the result set based on the CSP solvers output. If modulu is larger than the number of valid variants, the result set is empty. 
        /// </summary>
        /// <param name="vm">The variability model containing the binary options and their constraints.</param>
        /// <param name="treshold">Maximum number of configurations</param>
        /// <param name="modulu">Each configuration that is % modulu == 0 is taken to the result set</param>
        /// <returns>Returns a list of configurations, in which a configuration is a list of SELECTED binary options (deselected options are not present</returns>
        public List<List<BinaryOption>> GenerateRandomVariants(VariabilityModel vm, int treshold, int modulu)
        {
            List<CspTerm> variables = new List<CspTerm>();
            Dictionary<BinaryOption, CspTerm> elemToTerm = new Dictionary<BinaryOption, CspTerm>();
            Dictionary<CspTerm, BinaryOption> termToElem = new Dictionary<CspTerm, BinaryOption>();
            ConstraintSystem S = CSPsolver.getConstraintSystem(out variables, out elemToTerm, out termToElem, vm);
            List<List<BinaryOption>> erglist = new List<List<BinaryOption>>();
            ConstraintSolverSolution soln = S.Solve();

            List<List<BinaryOption>> allConfigs = new List<List<BinaryOption>>();

            while (soln.HasFoundSolution)
            {
                soln.GetNext();
                List<BinaryOption> tempConfig = new List<BinaryOption>();
                foreach (CspTerm cT in variables)
                {
                    if (soln.GetIntegerValue(cT) == 1)
                        tempConfig.Add(termToElem[cT]);
                }
                if (tempConfig.Contains(null))
                    tempConfig.Remove(null);
                allConfigs.Add(tempConfig);
            }

            Random r = new Random(modulu);
            for (int i = 0; i < treshold; i++)
            {
                erglist.Add(allConfigs[r.Next(allConfigs.Count)]);
            }
            return erglist;
        }

        /// <summary>
        /// This method searches for a corresponding methods in the dynamically loaded assemblies and calls it if found. It prefers due to performance reasons the Microsoft Solver Foundation implementation.
        /// </summary>
        /// <param name="config">The (partial) configuration which needs to be expaned to be valid.</param>
        /// <param name="vm">Variability model containing all options and their constraints.</param>
        /// <param name="unWantedOptions">Binary options that we do not want to become part of the configuration. Might be part if there is no other valid configuration without them.</param>
        /// <returns>The valid configuration (or null if there is none) that satisfies the VM and the goal.</returns>
        public List<BinaryOption> FindMinimizedConfig(List<BinaryOption> config, VariabilityModel vm, List<BinaryOption> unWantedOptions)
        {
            List<CspTerm> variables = new List<CspTerm>();
            Dictionary<BinaryOption, CspTerm> elemToTerm = new Dictionary<BinaryOption, CspTerm>();
            Dictionary<CspTerm, BinaryOption> termToElem = new Dictionary<CspTerm, BinaryOption>();
            ConstraintSystem S = CSPsolver.getConstraintSystem(out variables, out elemToTerm, out termToElem, vm);


            //Feature Selection
            foreach (BinaryOption binOpt in config)
            {
                CspTerm term = elemToTerm[binOpt];
                S.AddConstraints(S.Implies(S.True, term));
            }

            //Defining Goals
            CspTerm[] finalGoals = new CspTerm[variables.Count];
            for (int r = 0; r < variables.Count; r++)
            {
                if (unWantedOptions != null && (unWantedOptions.Contains(termToElem[variables[r]]) && !config.Contains(termToElem[variables[r]])))
                    finalGoals[r] = variables[r] * 100;
                else
                    finalGoals[r] = variables[r] * 1;
            }

            S.TryAddMinimizationGoals(S.Sum(finalGoals));

            ConstraintSolverSolution soln = S.Solve();
            List<string> erg2 = new List<string>();
            List<BinaryOption> tempConfig = new List<BinaryOption>();
            if (soln.HasFoundSolution)
            {
                tempConfig.Clear();
                foreach (CspTerm cT in variables)
                {
                    if (soln.GetIntegerValue(cT) == 1)
                        tempConfig.Add(termToElem[cT]);
                }
            }
            return tempConfig;
        }

        /// <summary>
        /// Based on a given (partial) configuration and a variability, we aim at finding all optimally maximal or minimal (in terms of selected binary options) configurations.
        /// </summary>
        /// <param name="config">The (partial) configuration which needs to be expaned to be valid.</param>
        /// <param name="vm">Variability model containing all options and their constraints.</param>
        /// <param name="unwantedOptions">Binary options that we do not want to become part of the configuration. Might be part if there is no other valid configuration without them</param>
        /// <returns>A list of configurations that satisfies the VM and the goal (or null if there is none).</returns>
        public List<List<BinaryOption>> FindAllMaximizedConfigs(List<BinaryOption> config, VariabilityModel vm, List<BinaryOption> unwantedOptions)
        {
            List<CspTerm> variables = new List<CspTerm>();
            Dictionary<BinaryOption, CspTerm> elemToTerm = new Dictionary<BinaryOption, CspTerm>();
            Dictionary<CspTerm, BinaryOption> termToElem = new Dictionary<CspTerm, BinaryOption>();
            ConstraintSystem S = CSPsolver.getConstraintSystem(out variables, out elemToTerm, out termToElem, vm);
            //Feature Selection
            if (config != null)
            {
                foreach (BinaryOption binOpt in config)
                {
                    CspTerm term = elemToTerm[binOpt];
                    S.AddConstraints(S.Implies(S.True, term));
                }
            }
            //Defining Goals
            CspTerm[] finalGoals = new CspTerm[variables.Count];
            for (int r = 0; r < variables.Count; r++)
            {
                if (unwantedOptions != null && (unwantedOptions.Contains(termToElem[variables[r]]) && !config.Contains(termToElem[variables[r]])))
                    finalGoals[r] = variables[r] * 10000;
                else
                    finalGoals[r] = variables[r] * 1;
            }
            S.TryAddMinimizationGoals(S.Sum(finalGoals));

            ConstraintSolverSolution soln = S.Solve();
            List<string> erg2 = new List<string>();
            List<BinaryOption> tempConfig = new List<BinaryOption>();
            List<List<BinaryOption>> resultConfigs = new List<List<BinaryOption>>();
            while (soln.HasFoundSolution && soln.Quality == ConstraintSolverSolution.SolutionQuality.Optimal)
            {
                tempConfig.Clear();
                foreach (CspTerm cT in variables)
                {
                    if (soln.GetIntegerValue(cT) == 1)
                        tempConfig.Add(termToElem[cT]);
                }
                if (!Configuration.containsBinaryConfiguration(resultConfigs, tempConfig))
                    resultConfigs.Add(tempConfig);
                soln.GetNext();
            }
            return resultConfigs;
        }

        /// <summary>
        ///  The method aims at finding a configuration which is similar to the given configuration, but does not contain the optionToBeRemoved. If further options need to be removed from the given configuration, they are outputed in removedElements.
        /// Idea: Encode this as a CSP problem. We aim at finding a configuration that maximizes a goal. Each option of the given configuration gets a large value assigned. All other options of the variability model gets a negative value assigned.
        /// We will further create a boolean constraint that forbids selecting the optionToBeRemoved. Now, we find an optimal valid configuration.
        /// </summary>
        /// <param name="optionToBeRemoved">The binary configuration option that must not be part of the new configuration.</param>
        /// <param name="originalConfig">The configuration for which we want to find a similar one.</param>
        /// <param name="removedElements">If further options need to be removed from the given configuration to build a valid configuration, they are outputed in this list.</param>
        /// <param name="vm">The variability model containing all options and their constraints.</param>
        /// <returns>A configuration that is valid, similar to the original configuration and does not contain the optionToBeRemoved.</returns>
        public List<BinaryOption> GenerateConfigWithoutOption(BinaryOption optionToBeRemoved, List<BinaryOption> originalConfig, out List<BinaryOption> removedElements, VariabilityModel vm)
        {
            List<CspTerm> variables = new List<CspTerm>();
            Dictionary<BinaryOption, CspTerm> elemToTerm = new Dictionary<BinaryOption, CspTerm>();
            Dictionary<CspTerm, BinaryOption> termToElem = new Dictionary<CspTerm, BinaryOption>();
            ConstraintSystem S = CSPsolver.getConstraintSystem(out variables, out elemToTerm, out termToElem, vm);

            removedElements = new List<BinaryOption>();

            //Forbid the selection of this configuration option
            CspTerm optionToRemove = elemToTerm[optionToBeRemoved];
            S.AddConstraints(S.Implies(S.True, S.Not(optionToRemove)));

            //Defining Goals
            CspTerm[] finalGoals = new CspTerm[variables.Count];
            int r = 0;
            foreach (var term in variables)
            {
                if (originalConfig.Contains(termToElem[term]))
                    finalGoals[r] = term * -1000; //Since we minimize, we put a large negative value of an option that is within the original configuration to increase chances that the option gets selected again
                else
                    finalGoals[r] = variables[r] * 10000;//Positive number will lead to a small chance that an option gets selected when it is not in the original configuration
                r++;
            }

            S.TryAddMinimizationGoals(S.Sum(finalGoals));

            ConstraintSolverSolution soln = S.Solve();
            List<BinaryOption> tempConfig = new List<BinaryOption>();
            if (soln.HasFoundSolution && soln.Quality == ConstraintSolverSolution.SolutionQuality.Optimal)
            {
                tempConfig.Clear();
                foreach (CspTerm cT in variables)
                {
                    if (soln.GetIntegerValue(cT) == 1)
                        tempConfig.Add(termToElem[cT]);
                }
                //Adding the options that have been removed from the original configuration
                foreach (var opt in originalConfig)
                {
                    if (!tempConfig.Contains(opt))
                        removedElements.Add(opt);
                }
                return tempConfig;
            }

            return null;
        }

        public IBucketSession CreateBucketSession() { return new MSFBucketSession(); }

        //public List<List<BinaryOption>> generateTilSize(int i1, int size, int timeout, VariabilityModel vm)
        //{
        //    foreach (Lazy<IVariantGenerator, ISolverType> solver in solvers)
        //    {
        //        if (solver.Metadata.SolverType.Equals("MSSolverFoundation")) return solver.Value.generateTilSize(i1, size, timeout, vm);
        //    }

        //    //If not MS Solver, take any solver. Should be changed when supporting more than 2 solvers here
        //    foreach (Lazy<IVariantGenerator, ISolverType> solver in solvers)
        //    {
        //        return solver.Value.generateTilSize(i1, size, timeout, vm);
        //    }
        //    return new List<List<BinaryOption>>();
        //}

    }
}
