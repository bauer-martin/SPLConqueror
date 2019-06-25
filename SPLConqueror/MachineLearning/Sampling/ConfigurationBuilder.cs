﻿using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;
using MachineLearning.Solver;
using MachineLearning.Sampling.Heuristics;
using MachineLearning.Sampling.ExperimentalDesigns;
using MachineLearning.Sampling.Hybrid;

namespace MachineLearning.Sampling
{
    public class ConfigurationBuilder
    {
        public static int binaryThreshold = 0;
        public static int binaryModulu = 0;
        public static Dictionary<SamplingStrategies, List<BinaryOption>> optionsToConsider = new Dictionary<SamplingStrategies, List<BinaryOption>>();
        public static BinaryParameters binaryParams = new BinaryParameters();

        private static List<String> blacklisted;

        public static void setBlacklisted(List<String> blacklist)
        {
            ConfigurationBuilder.blacklisted = blacklist;
        }

        public static void clear()
        {
            binaryModulu = 0;
            binaryThreshold = 0;
            optionsToConsider = new Dictionary<SamplingStrategies, List<BinaryOption>>();
            binaryParams = new BinaryParameters();
        }

        public static List<Configuration> buildConfigs(List<SamplingStrategies> binaryStrategies,
            List<ExperimentalDesign> experimentalDesigns, List<HybridStrategy> hybridStrategies)
        {
            List<Configuration> result = new List<Configuration>();
            IVariantGenerator vg = SolverManager.DefaultVariantGenerator;

            List<List<BinaryOption>> binaryConfigs = new List<List<BinaryOption>>();
            List<Dictionary<NumericOption, Double>> numericConfigs = new List<Dictionary<NumericOption, double>>();
            foreach (SamplingStrategies strat in binaryStrategies)
            {
                switch (strat)
                {
                    //Binary sampling heuristics
                    case SamplingStrategies.ALLBINARY:
                        binaryConfigs.AddRange(vg.GenerateUpToN(-1));
                        break;
                    case SamplingStrategies.SAT:
                        int numberSamples = 2;
                        foreach (Dictionary<string, string> parameters in binaryParams.satParameters)
                        {
                            if (parameters.ContainsKey("numConfigs"))
                            {
                                try
                                {
                                    numberSamples = Int32.Parse(parameters["numConfigs"]);
                                }
                                catch (FormatException)
                                {
                                    TWise tw = new TWise();
                                    numberSamples = tw.generateT_WiseVariants_new(Int32.Parse(parameters["numConfigs"].Remove(0, 4))).Count;
                                }
                            }

                            binaryConfigs.AddRange(vg.GenerateUpToN(numberSamples));
                            numberSamples = 2;
                        }
                        break;
                    case SamplingStrategies.BINARY_RANDOM:
                        RandomBinary rb = new RandomBinary();
                        foreach (Dictionary<string, string> expDesignParamSet in binaryParams.randomBinaryParameters)
                        {
                            binaryConfigs.AddRange(rb.getRandomConfigs(expDesignParamSet));
                        }

                        break;
                    case SamplingStrategies.OPTIONWISE:
                        {
                            FeatureWise fw = new FeatureWise();
                            binaryConfigs.AddRange(fw.generateFeatureWiseConfigurations());
                        }
                        break;

                    //case SamplingStrategies.MINMAX:
                    //    {
                    //        MinMax mm = new MinMax();
                    //        binaryConfigs.AddRange(mm.generateMinMaxConfigurations(GlobalState.varModel));

                    //    }
                    //    break;

                    case SamplingStrategies.PAIRWISE:
                        {
                            PairWise pw = new PairWise();
                            binaryConfigs.AddRange(pw.generatePairWiseVariants());
                        }
                        break;
                    case SamplingStrategies.NEGATIVE_OPTIONWISE:
                        {
                            NegFeatureWise neg = new NegFeatureWise();//2nd option: neg.generateNegativeFWAllCombinations(GlobalState.varModel));
                            binaryConfigs.AddRange(neg.generateNegativeFW());
                        }
                        break;

                    case SamplingStrategies.T_WISE:
                        foreach (Dictionary<string, string> ParamSet in binaryParams.tWiseParameters)
                        {
                            TWise tw = new TWise();
                            int t = 3;

                            foreach (KeyValuePair<String, String> param in ParamSet)
                            {
                                if (param.Key.Equals(TWise.PARAMETER_T_NAME))
                                {
                                    t = Convert.ToInt16(param.Value);
                                }

                                binaryConfigs.AddRange(tw.generateT_WiseVariants_new(t));
                            }
                        }
                        break;
                }
            }

            //Experimental designs for numeric options
            if (experimentalDesigns.Count != 0)
            {
                handleDesigns(experimentalDesigns, numericConfigs);
            }


            foreach (List<BinaryOption> binConfig in binaryConfigs)
            {
                if (numericConfigs.Count == 0)
                {
                    Configuration c = new Configuration(binConfig);
                    result.Add(c);
                }
                foreach (Dictionary<NumericOption, double> numConf in numericConfigs)
                {
                    Configuration c = new Configuration(binConfig, numConf);
                    result.Add(c);
                }
            }

            // Filter configurations based on the NonBooleanConstratins
            List<Configuration> filtered = new List<Configuration>();
            foreach (Configuration conf in result)
            {
                bool isValid = true;
                foreach (NonBooleanConstraint nbc in GlobalState.varModel.NonBooleanConstraints)
                {
                    if (!nbc.configIsValid(conf))
                    {
                        isValid = false;
                        continue;
                    }
                }

                if (isValid)
                    filtered.Add(conf);
            }
            result = filtered;


            // Hybrid designs
            if (hybridStrategies.Count != 0)
            {
                List<Configuration> configurations = ExecuteHybridStrategy(hybridStrategies);

                if (experimentalDesigns.Count == 0 && binaryStrategies.Count == 0)
                {
                    result = configurations;
                }
                else
                {
                    // Prepare the previous sample sets
                    if (result.Count == 0 && binaryConfigs.Count == 0)
                    {
                        foreach (Dictionary<NumericOption, double> numConf in numericConfigs)
                        {
                            Configuration c = new Configuration(new Dictionary<BinaryOption, BinaryOption.BinaryValue>(), numConf);
                            result.Add(c);
                        }
                    }


                    // Build the cartesian product
                    List<Configuration> newResult = new List<Configuration>();
                    foreach (Configuration config in result)
                    {
                        foreach (Configuration hybridConfiguration in configurations)
                        {
                            Dictionary<BinaryOption, BinaryOption.BinaryValue> binOpts = new Dictionary<BinaryOption, BinaryOption.BinaryValue>(config.BinaryOptions);
                            Dictionary<NumericOption, double> numOpts = new Dictionary<NumericOption, double>(config.NumericOptions);

                            Dictionary<BinaryOption, BinaryOption.BinaryValue> hybridBinOpts = hybridConfiguration.BinaryOptions;
                            foreach (BinaryOption binOpt in hybridConfiguration.BinaryOptions.Keys)
                            {
                                binOpts.Add(binOpt, hybridBinOpts[binOpt]);
                            }

                            Dictionary<NumericOption, double> hybridNumOpts = hybridConfiguration.NumericOptions;
                            foreach (NumericOption numOpt in hybridConfiguration.NumericOptions.Keys)
                            {
                                numOpts.Add(numOpt, hybridNumOpts[numOpt]);
                            }

                            newResult.Add(new Configuration(binOpts, numOpts));
                        }
                    }
                    result = newResult;
                }
            }

            if (GlobalState.varModel.MixedConstraints.Count == 0)
            {
                if (binaryStrategies.Count == 1 && binaryStrategies.Last().Equals(SamplingStrategies.ALLBINARY) && experimentalDesigns.Count == 1 && experimentalDesigns.Last() is FullFactorialDesign)
                {
                    return replaceReference(result.ToList());
                }
                else
                {
                    return replaceReference(result.Distinct().ToList());
                }
            }
            else
            {
                List<Configuration> unfilteredList = result.Distinct().ToList();
                List<Configuration> filteredConfiguration = new List<Configuration>();
                foreach (Configuration toTest in unfilteredList)
                {
                    bool isValid = true;
                    foreach (MixedConstraint constr in GlobalState.varModel.MixedConstraints)
                    {
                        if (!constr.requirementsFulfilled(toTest))
                        {
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        filteredConfiguration.Add(toTest);
                    }
                }
                return replaceReference(filteredConfiguration);
            }
        }

        private static List<Configuration> replaceReference(List<Configuration> sampled)
        {
            // Replaces the reference of the sampled configuration with the corresponding measured configurstion if it exists

            var measured = GlobalState.allMeasurements.Configurations.Intersect(sampled);
            var notMeasured = sampled.Except(measured);
            return measured.Concat(notMeasured).ToList();
        }

        private static List<Configuration> ExecuteHybridStrategy(List<HybridStrategy> hybridStrategies)
        {
            List<Configuration> allSampledConfigurations = new List<Configuration>();
            foreach (HybridStrategy hybrid in hybridStrategies)
            {
                hybrid.ComputeSamplingStrategy();
                allSampledConfigurations.AddRange(hybrid.selectedConfigurations);
            }
            return allSampledConfigurations;
        }

        private static void handleDesigns(List<ExperimentalDesign> samplingDesigns, List<Dictionary<NumericOption, Double>> numericOptions)
        {
            foreach (ExperimentalDesign samplingDesign in samplingDesigns)
            {
                if (samplingDesign.getSamplingDomain() == null ||
                    samplingDesign.getSamplingDomain().Count == 0)
                {
                    samplingDesign.setSamplingDomain(GlobalState.varModel.getNonBlacklistedNumericOptions(blacklisted));
                }
                else
                {
                    samplingDesign.setSamplingDomain(GlobalState.varModel.getNonBlacklistedNumericOptions(blacklisted)
                        .Intersect(samplingDesign.getSamplingDomain()).ToList());
                }

                samplingDesign.computeDesign();
                numericOptions.AddRange(samplingDesign.SelectedConfigurations);
            }
        }

        public static void printSelectetedConfigurations_expDesign(List<Dictionary<NumericOption, double>> configurations)
        {
            GlobalState.varModel.NumericOptions.ForEach(x => GlobalState.logInfo.log(x.Name + " | "));
            GlobalState.logInfo.log("\n");
            foreach (Dictionary<NumericOption, double> configuration in configurations)
            {
                GlobalState.varModel.NumericOptions.ForEach(x =>
                {
                    if (configuration.ContainsKey(x))
                        GlobalState.logInfo.log(configuration[x] + " | ");
                    else
                        GlobalState.logInfo.log("\t | ");
                });
                GlobalState.logInfo.log("\n");
            }
        }
    }
}
