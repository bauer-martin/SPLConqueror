﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPLConqueror_Core;
using Microsoft.Z3;

namespace MachineLearning.Solver
{
    /// <summary>
    /// This class represents the variant generator for the z3 solver and is responsible for
    /// using the z3 solver to generate different kinds of solutions.
    /// </summary>
    public class Z3VariantGenerator : IVariantGenerator
    {
        private Dictionary<int, Z3Cache> _z3Cache;

        private uint z3RandomSeed = 1;
        private const string RANDOM_SEED = ":random-seed";

        public bool henard = false;

        /// <summary>
        /// This method sets the random seed for the z3 solver.
        /// </summary>
        /// <param name="seed">The random seed for the z3 solver.</param>
        public void setSeed(uint seed)
        {
            this.z3RandomSeed = seed;
        }

        /// <summary>
        /// Generates all valid combinations of all configuration options in the given model.
        /// </summary>
        /// <param name="vm">the variability model containing the binary options and their constraints</param>
        /// <param name="optionsToConsider">the options that should be considered. All other options are ignored</param>
        /// <returns>Returns a list of <see cref="Configuration"/></returns>
        public List<Configuration> GenerateAllVariants(VariabilityModel vm, List<ConfigurationOption> optionsToConsider)
        {
            List<Configuration> allConfigurations = new List<Configuration>();
            List<Expr> variables;
            Dictionary<Expr, ConfigurationOption> termToOption;
            Dictionary<ConfigurationOption, Expr> optionToTerm;
            Tuple<Context, BoolExpr> z3Tuple = Z3Solver.GetInitializedSolverSystem(out variables, out optionToTerm, out termToOption, vm);
            Context z3Context = z3Tuple.Item1;
            BoolExpr z3Constraints = z3Tuple.Item2;

            Microsoft.Z3.Solver solver = z3Context.MkSolver();

            solver.Set (RANDOM_SEED, z3RandomSeed);

            solver.Assert(z3Constraints);

            while (solver.Check() == Status.SATISFIABLE)
            {
                Model model = solver.Model;

                Tuple<List<BinaryOption>, Dictionary<NumericOption, double>> confOpts = RetrieveConfiguration(variables, model, termToOption, optionsToConsider);

                Configuration c = new Configuration(confOpts.Item1, confOpts.Item2);
                // Check if the non-boolean constraints are satisfied
                bool configIsValid = vm.configurationIsValid(c);
                bool isInConfigurationFile = !VariantGeneratorUtilities.IsInConfigurationFile(c, allConfigurations);
                bool fulfillsMixedConstraintrs = VariantGeneratorUtilities.FulfillsMixedConstraints(c, vm);
                if (configIsValid && isInConfigurationFile && fulfillsMixedConstraintrs)
                {
                    allConfigurations.Add(c);
                }
                solver.Push();
                solver.Assert(Z3Solver.NegateExpr(z3Context, Z3Solver.ConvertConfiguration(z3Context, confOpts.Item1, optionToTerm, vm, numericValues: confOpts.Item2)));
            }

            solver.Push();
            solver.Pop(Convert.ToUInt32(allConfigurations.Count() + 1));
            return allConfigurations;
        }

        /// <summary>
        /// Generates up to n solutions of the given variability model. 
        /// Note that this method could also generate less than n solutions if the variability model does not contain sufficient solutions.
        /// Moreover, in the case that <code>n &lt; 0</code>, all solutions are generated.
        /// </summary>
        /// <param name="vm">The <see cref="VariabilityModel"/> to obtain solutions for.</param>
        /// <param name="n">The number of solutions to obtain.</param>
        /// <returns>A list of configurations, in which a configuration is a list of SELECTED binary options.</returns>
        public List<List<BinaryOption>> GenerateUpToN(VariabilityModel vm, int n)
        {
            // Use the random seed to produce new random seeds
            Random random = new Random(Convert.ToInt32(z3RandomSeed));

            List<BoolExpr> variables;
            Dictionary<BoolExpr, BinaryOption> termToOption;
            Dictionary<BinaryOption, BoolExpr> optionToTerm;
            Tuple<Context, BoolExpr> z3Tuple = Z3Solver.GetInitializedBooleanSolverSystem(out variables, out optionToTerm, out termToOption, vm, this.henard, random.Next());
            Context z3Context = z3Tuple.Item1;
            BoolExpr z3Constraints = z3Tuple.Item2;
            List<BoolExpr> excludedConfigurations = new List<BoolExpr>();
            List<BoolExpr> constraints = Z3Solver.lastConstraints;

            List<List<BinaryOption>> configurations = new List<List<BinaryOption>>();

            Microsoft.Z3.Solver s = z3Context.MkSolver();

            if (henard)
            {
                s.Set (RANDOM_SEED, NextUInt(random));
            }
            else
            {
                s.Set (RANDOM_SEED, z3RandomSeed);
            }

            s.Assert(z3Constraints);
            s.Push();

            Model model = null;
            while (s.Check() == Status.SATISFIABLE && (configurations.Count < n || n < 0))
            {
                model = s.Model;

                List<BinaryOption> config = RetrieveConfiguration(variables, model, termToOption);

                configurations.Add(config);

                if (henard)
                {
                    BoolExpr newConstraint = Z3Solver.NegateExpr(z3Context, Z3Solver.ConvertConfiguration(z3Context, config, optionToTerm, vm));

                    excludedConfigurations.Add(newConstraint);

                    Dictionary<BoolExpr, BinaryOption> oldTermToOption = termToOption;

                    // Now, initialize a new one for the next configuration
                    z3Tuple = Z3Solver.GetInitializedBooleanSolverSystem(out variables, out optionToTerm, out termToOption, vm, this.henard, random.Next());
                    z3Context = z3Tuple.Item1;
                    z3Constraints = z3Tuple.Item2;

                    s = z3Context.MkSolver();

                    s.Set (RANDOM_SEED, NextUInt (random));

                    constraints = Z3Solver.lastConstraints;

                    excludedConfigurations = Z3Solver.ConvertConstraintsToNewContext(oldTermToOption, optionToTerm, excludedConfigurations, z3Context);

                    constraints.AddRange(excludedConfigurations);

                    s.Assert(z3Context.MkAnd(Z3Solver.Shuffle(constraints, new Random(random.Next()))));

                    s.Push();
                }
                else
                {
                    s.Add(Z3Solver.NegateExpr(z3Context, Z3Solver.ConvertConfiguration(z3Context, config, optionToTerm, vm)));
                }
            }

            return configurations;
        }

        private static uint NextUInt(Random random)
        {
            int randomNumber = random.Next();
            uint result = 0;
            bool found = false;
            while (!found)
            {
                try
                {
                    result = Convert.ToUInt32(randomNumber);
                    found = true;
                }
                catch
                {
                    // Do nothing in this case
                }
            }
            return result;
        }
        
        /// <summary>
        /// Parses a z3 solution into a configuration.
        /// This method also supports numeric variables.
        /// </summary>
        /// <param name="variables">List of all variables in the z3 context.</param>
        /// <param name="model">Solution of the context.</param>
        /// <param name="termToOption">Map from variables to binary options.</param>
        /// <param name="optionsToConsider">The options that are considered for the solution.</param>
        /// <returns>Configuration parsed from the solution.</returns>
        public static Tuple<List<BinaryOption>, Dictionary<NumericOption, double>> RetrieveConfiguration(
            List<Expr> variables, Model model, Dictionary<Expr, ConfigurationOption> termToOption, 
            List<ConfigurationOption> optionsToConsider = null)
        {
            List<BinaryOption> binOpts = new List<BinaryOption>();
            Dictionary<NumericOption, double> config = new Dictionary<NumericOption, double>();
            foreach (Expr variable in variables)
            {
                if (optionsToConsider != null && !optionsToConsider.Contains(termToOption[variable]))
                {
                    continue;
                }

                Expr allocation = model.Eval(variable, completion: true);
                if (allocation.GetType() == typeof(BoolExpr))
                {
                    BoolExpr boolExpr = (BoolExpr)allocation;
                    if (boolExpr.IsTrue)
                    {
                        binOpts.Add((BinaryOption) termToOption[variable]);
                    }
                }
                else
                {
                    // In this case, we have a numeric variable
                    FPNum fpNum = (FPNum) allocation;

                    config.Add((NumericOption) termToOption[variable], Z3Solver.lookUpNumericValue(fpNum.ToString()));

                }
                
            }
            return new Tuple<List<BinaryOption>, Dictionary<NumericOption, double>>(binOpts, config);
        }

        /// <summary>
        /// Parses a z3 solution into a configuration.
        /// </summary>
        /// <param name="variables">List of all variables in the z3 context.</param>
        /// <param name="model">Solution of the context.</param>
        /// <param name="termToOption">Map from variables to binary options.</param>
        /// <param name="optionsToConsider">The options that are considered for the solution.</param>
        /// <returns>Configuration parsed from the solution.</returns>
        public static List<BinaryOption> RetrieveConfiguration(List<BoolExpr> variables, Model model, Dictionary<BoolExpr, BinaryOption> termToOption, List<ConfigurationOption> optionsToConsider = null)
        {
            List<BinaryOption> config = new List<BinaryOption>();
            foreach (BoolExpr variable in variables)
            {
                if (optionsToConsider != null && !optionsToConsider.Contains(termToOption[variable]))
                {
                    continue;
                }

                Expr allocation = model.Evaluate(variable);
                BoolExpr boolExpr = (BoolExpr)allocation;
                if (boolExpr.IsTrue)
                {
                    config.Add(termToOption[variable]);
                }
            }
            return config;
        }

        /// <summary>
        /// The method aims at finding a configuration which is similar to the given configuration, but does not contain the optionToBeRemoved. If further options need to be removed from the given configuration, they are outputed in removedElements.
        /// </summary>
        /// <param name="optionToBeRemoved">The binary configuration option that must not be part of the new configuration.</param>
        /// <param name="originalConfig">The configuration for which we want to find a similar one.</param>
        /// <param name="removedElements">If further options need to be removed from the given configuration to build a valid configuration, they are outputed in this list.</param>
        /// <param name="vm">The variability model containing all options and their constraints.</param>
        /// <returns>A configuration that is valid, similar to the original configuration and does not contain the optionToBeRemoved.</returns>
        public List<BinaryOption> GenerateConfigWithoutOption(BinaryOption optionToBeRemoved, List<BinaryOption> originalConfig, out List<BinaryOption> removedElements, VariabilityModel vm)
        {
            removedElements = new List<BinaryOption>();
            var originalConfigWithoutRemoved = originalConfig.Where(x => !x.Equals(optionToBeRemoved));

            List<BoolExpr> variables;
            Dictionary<BoolExpr, BinaryOption> termToOption;
            Dictionary<BinaryOption, BoolExpr> optionToTerm;
            Tuple<Context, BoolExpr> z3Tuple = Z3Solver.GetInitializedBooleanSolverSystem(out variables, out optionToTerm, out termToOption, vm, this.henard);
            Context z3Context = z3Tuple.Item1;
            BoolExpr z3Constraints = z3Tuple.Item2;
            List<BoolExpr> constraints = new List<BoolExpr>();
            constraints.Add(z3Constraints);

            constraints.Add(z3Context.MkNot(optionToTerm[optionToBeRemoved]));

            ArithExpr[] minGoals = new ArithExpr[variables.Count];


            for (int r = 0; r < variables.Count; r++)
            {
                BinaryOption currOption = termToOption[variables[r]];
                ArithExpr numericVariable = z3Context.MkIntConst(currOption.Name);

                int weight = -1000;

                if (!originalConfigWithoutRemoved.Contains(currOption))
                {
                    weight = 1000;
                }
                else if (currOption.Equals(optionToBeRemoved))
                {
                    weight = 100000;
                }

                constraints.Add(z3Context.MkEq(numericVariable, z3Context.MkITE(variables[r], z3Context.MkInt(weight), z3Context.MkInt(0))));
                minGoals[r] = numericVariable;

            }

            Optimize optimizer = z3Context.MkOptimize();
            optimizer.Assert(constraints.ToArray());
            optimizer.MkMinimize(z3Context.MkAdd(minGoals));

            if (optimizer.Check() != Status.SATISFIABLE)
            {
                return null;
            }
            else
            {
                Model model = optimizer.Model;
                List<BinaryOption> similarConfig = RetrieveConfiguration(variables, model, termToOption);
                removedElements = originalConfigWithoutRemoved.Except(similarConfig).ToList();
                return similarConfig;
            }

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
            List<List<BinaryOption>> optimalConfigurations = new List<List<BinaryOption>>();

            List<BoolExpr> variables;
            Dictionary<BoolExpr, BinaryOption> termToOption;
            Dictionary<BinaryOption, BoolExpr> optionToTerm;
            Tuple<Context, BoolExpr> z3Tuple = Z3Solver.GetInitializedBooleanSolverSystem(out variables, out optionToTerm, out termToOption, vm, this.henard);
            Context z3Context = z3Tuple.Item1;
            BoolExpr z3Constraints = z3Tuple.Item2;

            List<BoolExpr> constraints = new List<BoolExpr>();
            constraints.Add(z3Constraints);
            List<BoolExpr> requireConfigs = new List<BoolExpr>();

            if (config != null)
            {
                foreach (BinaryOption option in config)
                {
                    requireConfigs.Add(optionToTerm[option]);
                }
                constraints.Add(z3Context.MkAnd(requireConfigs.ToArray()));
            }

            ArithExpr[] optimizationGoals = new ArithExpr[variables.Count];

            for (int r = 0; r < variables.Count; r++)
            {
                BinaryOption currOption = termToOption[variables[r]];
                ArithExpr numericVariable = z3Context.MkIntConst(currOption.Name);

                int weight = -1;

                if (unwantedOptions != null && (unwantedOptions.Contains(termToOption[variables[r]]) && !config.Contains(termToOption[variables[r]])))
                {
                    weight = 10000;
                }

                constraints.Add(z3Context.MkEq(numericVariable, z3Context.MkITE(variables[r], z3Context.MkInt(weight), z3Context.MkInt(0))));

                optimizationGoals[r] = numericVariable;
            }

            Optimize optimizer = z3Context.MkOptimize();
            optimizer.Assert(constraints.ToArray());
            optimizer.MkMinimize(z3Context.MkAdd(optimizationGoals));
            int bestSize = 0;
            int currentSize = 0;
            while (optimizer.Check() == Status.SATISFIABLE && currentSize >= bestSize)
            {
                Model model = optimizer.Model;
                List<BinaryOption> solution = RetrieveConfiguration(variables, model, termToOption);
                currentSize = solution.Count;
                if (currentSize >= bestSize)
                {
                    optimalConfigurations.Add(solution);
                }
                if (bestSize == 0)
                    bestSize = solution.Count;
                currentSize = solution.Count;
                optimizer.Assert(z3Context.MkNot(Z3Solver.ConvertConfiguration(z3Context, solution, optionToTerm, vm)));
            }

            return optimalConfigurations;
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
            List<BoolExpr> variables;
            Dictionary<BoolExpr, BinaryOption> termToOption;
            Dictionary<BinaryOption, BoolExpr> optionToTerm;
            Tuple<Context, BoolExpr> z3Tuple = Z3Solver.GetInitializedBooleanSolverSystem(out variables, out optionToTerm, out termToOption, vm, this.henard);
            Context z3Context = z3Tuple.Item1;
            BoolExpr z3Constraints = z3Tuple.Item2;

            List<BoolExpr> constraints = new List<BoolExpr>();
            constraints.Add(z3Constraints);

            //Feature Selection
            foreach (BinaryOption binOpt in config)
            {
                BoolExpr term = optionToTerm[binOpt];
                constraints.Add(term);
            }

            Model model = null;

            //Defining Goals
            ArithExpr[] optimizationGoals = new ArithExpr[variables.Count];

            for (int r = 0; r < variables.Count; r++)
            {
                BinaryOption currOption = termToOption[variables[r]];
                ArithExpr numericVariable = z3Context.MkIntConst(currOption.Name);

                int weight = 1;
                if (unWantedOptions != null && (unWantedOptions.Contains(termToOption[variables[r]]) && !config.Contains(termToOption[variables[r]])))
                {
                    weight = 1000;
                }

                constraints.Add(z3Context.MkEq(numericVariable, z3Context.MkITE(variables[r], z3Context.MkInt(weight), z3Context.MkInt(0))));

                optimizationGoals[r] = numericVariable;

            }
            // For minimization, we need the class 'Optimize'
            Optimize optimizer = z3Context.MkOptimize();
            optimizer.Assert(constraints.ToArray());
            optimizer.MkMinimize(z3Context.MkAdd(optimizationGoals));

            if (optimizer.Check() != Status.SATISFIABLE)
            {
                return new List<BinaryOption>();
            }
            else
            {
                model = optimizer.Model;
            }

            List<BinaryOption> result = RetrieveConfiguration(variables, model, termToOption);

            return result;
        }

        public IBucketSession CreateBucketSession(VariabilityModel vm)
        {
            return new Z3BucketSession(vm, z3RandomSeed, henard);
        }
    }
}
