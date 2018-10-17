using System;
using System.Collections.Generic;
using System.Linq;
using MachineLearning.Learning;
using MachineLearning.Sampling.ExperimentalDesigns;
using MachineLearning.Sampling.Heuristics;
using MachineLearning.Sampling.Hybrid;
using MachineLearning.Sampling.Hybrid.Distributive;
using MachineLearning.Solver;
using SPLConqueror_Core;

namespace MachineLearning.Sampling
{
    public class ConfigurationBuilder
    {
        public const string COMMAND_BINARY_SAMPLING = "binary";
        public const string COMMAND_SAMPLE_ALLBINARY = "allbinary";
        public const string COMMAND_SAMPLE_FEATUREWISE = "featurewise";
        public const string COMMAND_SAMPLE_OPTIONWISE = "optionwise";
        public const string COMMAND_SAMPLE_PAIRWISE = "pairwise";
        public const string COMMAND_SAMPLE_NEGATIVE_OPTIONWISE = "negfw";
        public const string COMMAND_SAMPLE_BINARY_RANDOM = "random";
        public const string COMMAND_SAMPLE_BINARY_TWISE = "twise";
        public const string COMMAND_SAMPLE_BINARY_SAT = "satoutput";
        public const string COMMAND_SAMPLE_BINARY_DISTANCE = "distance-based";

        #region tag for numeric sampling
        public const string COMMAND_NUMERIC_SAMPLING = "numeric";
        // deprecated
        public const string COMMAND_EXPERIMENTALDESIGN = "expdesign";
        #endregion
        public const string COMMAND_EXPDESIGN_BOXBEHNKEN = "boxbehnken";
        public const string COMMAND_EXPDESIGN_CENTRALCOMPOSITE = "centralcomposite";
        public const string COMMAND_EXPDESIGN_FULLFACTORIAL = "fullfactorial";
        public const string COMMAND_EXPDESIGN_FACTORIAL = "factorial";
        public const string COMMAND_EXPDESIGN_HYPERSAMPLING = "hypersampling";
        public const string COMMAND_EXPDESIGN_ONEFACTORATATIME = "onefactoratatime";
        public const string COMMAND_EXPDESIGN_KEXCHANGE = "kexchange";
        public const string COMMAND_EXPDESIGN_PLACKETTBURMAN = "plackettburman";
        public const string COMMAND_EXPDESIGN_RANDOM = "random";

        public const string COMMAND_HYBRID = "hybrid";
        public const string COMMAND_HYBRID_DISTRIBUTION_AWARE = "distribution-aware";
        public const string COMMAND_HYBRID_DISTRIBUTION_PRESERVING = "distribution-preserving";

        public const string COMMAND_VALIDATION = "validation";

        public List<SamplingStrategies> binaryStrategies = new List<SamplingStrategies>();
        public List<SamplingStrategies> binaryStrategiesValidation = new List<SamplingStrategies>();
        public List<HybridStrategy> hybridStrategies = new List<HybridStrategy>();
        public List<HybridStrategy> hybridStrategiesValidation = new List<HybridStrategy>();
        public List<ExperimentalDesign> numericStrategies = new List<ExperimentalDesign>();
        public List<ExperimentalDesign> numericStrategiesValidation = new List<ExperimentalDesign>();

        public Dictionary<SamplingStrategies, List<BinaryOption>> optionsToConsider = new Dictionary<SamplingStrategies, List<BinaryOption>>();
        public BinaryParameters binaryParams = new BinaryParameters();

        // The default variant generator is the one using the CSP solver of the Microsoft solver foundation
        public static IVariantGenerator vg = new VariantGenerator();

        public List<String> blacklisted;
        public List<Configuration> existingConfigurations = new List<Configuration>();

        public void clear()
        {
            binaryStrategies.Clear();
            binaryStrategiesValidation.Clear();
            numericStrategies.Clear();
            numericStrategiesValidation.Clear();
            hybridStrategies.Clear();
            hybridStrategiesValidation.Clear();
            optionsToConsider = new Dictionary<SamplingStrategies, List<BinaryOption>>();
            binaryParams = new BinaryParameters();
            existingConfigurations.Clear();
            vg.ClearCache();
        }

        public bool performOneCommand(string line)
        {
            // remove comment part of the line (the comment starts with an #)
            line = line.Split(new[] { '#' }, 2)[0];
            if (line.Length == 0)
                return true;

            // split line in command and parameters of the command
            string[] components = line.Split(new[] { ' ' }, 2);

            string command = components[0];
            string task = components.Length > 1 ? components[1] : "";

            switch (command.ToLower())
            {
                case COMMAND_BINARY_SAMPLING:
                    performOneCommand_Binary(task);
                    break;
                case COMMAND_NUMERIC_SAMPLING:
                    performOneCommand_ExpDesign(task);
                    break;
                case COMMAND_HYBRID:
                    performOneCommand_Hybrid(task);
                    break;
                default:
                    // Try to perform it as deprecated command.
#pragma warning disable 618
                    return performOneCommand_Depr(line);
#pragma warning restore 618
            }
            return true;
        }

        #region execution of deprecated commands
        [Obsolete("Warning: You are using deprecated commands. These commands might" +
            " be removed in the future. Please update your scripts or use the converter.", false)]
        public bool performOneCommand_Depr(string line)
        {
            string command;
            line = line.Split(new[] { '#' }, 2)[0];

            if (line.Length == 0)
                return true;

            string[] components = line.Split(new[] { ' ' }, 2);
            string task = "";
            if (components.Length > 1)
                task = components[1];
            string[] taskAsParameter = task.Split(' ');
            command = components[0];
            switch (command.ToLower())
            {
                case COMMAND_SAMPLE_PAIRWISE:
                    addBinSamplingNoParams(SamplingStrategies.PAIRWISE,
                        "PW", taskAsParameter.Contains(COMMAND_VALIDATION));
                    break;

                case COMMAND_SAMPLE_BINARY_TWISE:
                    {
                        string[] para = task.Split(' ');

                        Dictionary<String, String> parameters = new Dictionary<string, string>();
                        //parseParametersToLinearAndQuadraticBinarySampling(para);

                        for (int i = 0; i < para.Length; i++)
                        {
                            if (para[i].Contains(":"))
                            {
                                parameters.Add(para[i].Split(':')[0], para[i].Split(':')[1]);
                            }
                        }
                        addBinSamplingParams(SamplingStrategies.T_WISE, "TW", parameters,
                            para.Contains(COMMAND_VALIDATION));
                    }
                    break;

                case COMMAND_EXPERIMENTALDESIGN:
                    performOneCommand_ExpDesign(task);
                    break;

                case COMMAND_SAMPLE_FEATUREWISE:
                case COMMAND_SAMPLE_OPTIONWISE:
                    addBinSamplingNoParams(SamplingStrategies.OPTIONWISE,
                        "OW", taskAsParameter.Contains(COMMAND_VALIDATION));
                    break;

                case COMMAND_SAMPLE_ALLBINARY:
                    addBinSamplingNoParams(SamplingStrategies.ALLBINARY, "ALLB",
                        taskAsParameter.Contains(COMMAND_VALIDATION));
                    break;

                case COMMAND_SAMPLE_BINARY_RANDOM:
                    {
                        Dictionary<String, String> parameter = new Dictionary<String, String>();
                        string[] para = task.Split(' ');
                        for (int i = 0; i < para.Length; i++)
                        {
                            String key = para[i].Split(':')[0];
                            String value = para[i].Split(':')[1];
                            parameter.Add(key, value);
                        }
                        addBinSamplingParams(SamplingStrategies.BINARY_RANDOM, "RANDB",
                            parameter, taskAsParameter.Contains(COMMAND_VALIDATION));

                        break;
                    }

                case COMMAND_SAMPLE_NEGATIVE_OPTIONWISE:
                    // TODO there are two different variants in generating NegFW configurations.
                    addBinSamplingNoParams(SamplingStrategies.NEGATIVE_OPTIONWISE,
                        "NEGOW", taskAsParameter.Contains(COMMAND_VALIDATION));
                    break;

                default:
                    GlobalState.logInfo.logLine("Invalid deprecated command: " + command);
                    return false;
            }
            return true;
        }
        #endregion

        private static void getParametersAndSamplingDomain(string taskLine, out Dictionary<string, string> parameters,
            out List<ConfigurationOption> samplingDomain)
        {
            parameters = new Dictionary<string, string>();
            samplingDomain = new List<ConfigurationOption>();

            string[] args = taskLine.Split(new string[] { " " }, StringSplitOptions.None);

            if (args.Length > 1)
            {
                foreach (string param in args)
                {
                    if (param.Contains("["))
                    {
                        string[] options = param.Substring(1, param.Length - 2).Split(',');
                        foreach (string option in options)
                        {
                            samplingDomain.Add(GlobalState.varModel.getOption(option));
                        }
                    }
                    else if (param.Contains(":"))
                    {
                        string[] keyAndValue = param.Split(new string[] { ":" }, StringSplitOptions.None);
                        parameters.Add(keyAndValue[0], keyAndValue[1]);
                    }
                }
            }
        }

        public void performOneCommand_Binary(string task)
        {
            string strategyName = task.Split(new[] { " " }, StringSplitOptions.None)[0];
            Dictionary<string, string> parameterKeyAndValue;
            List<BinaryOption> optionsToConsider;
            List<ConfigurationOption> temp = new List<ConfigurationOption>();
            getParametersAndSamplingDomain(task, out parameterKeyAndValue, out temp);
            optionsToConsider = temp.OfType<BinaryOption>().ToList();
            bool isValidation = task.Contains(COMMAND_VALIDATION);

            switch (strategyName.ToLower())
            {
                case COMMAND_SAMPLE_ALLBINARY:
                    addBinarySamplingDomain(SamplingStrategies.ALLBINARY, optionsToConsider);
                    addBinSamplingNoParams(SamplingStrategies.ALLBINARY, "ALLB", isValidation);
                    break;
                case COMMAND_SAMPLE_FEATUREWISE:
                case COMMAND_SAMPLE_OPTIONWISE:
                    addBinarySamplingDomain(SamplingStrategies.OPTIONWISE, optionsToConsider);
                    addBinSamplingNoParams(SamplingStrategies.OPTIONWISE, "OW", isValidation);
                    break;
                case COMMAND_SAMPLE_PAIRWISE:
                    addBinarySamplingDomain(SamplingStrategies.PAIRWISE, optionsToConsider);
                    addBinSamplingNoParams(SamplingStrategies.PAIRWISE, "PW", isValidation);
                    break;
                case COMMAND_SAMPLE_NEGATIVE_OPTIONWISE:
                    addBinarySamplingDomain(SamplingStrategies.NEGATIVE_OPTIONWISE, optionsToConsider);
                    addBinSamplingNoParams(SamplingStrategies.NEGATIVE_OPTIONWISE, "NEGOW", isValidation);
                    break;
                case COMMAND_SAMPLE_BINARY_RANDOM:
                    addBinarySamplingDomain(SamplingStrategies.BINARY_RANDOM, optionsToConsider);
                    addBinSamplingParams(SamplingStrategies.BINARY_RANDOM, "RANDB", parameterKeyAndValue,
                        isValidation);
                    break;
                case COMMAND_SAMPLE_BINARY_TWISE:
                    addBinarySamplingDomain(SamplingStrategies.T_WISE, optionsToConsider);
                    addBinSamplingParams(SamplingStrategies.T_WISE, "TW", parameterKeyAndValue, isValidation);
                    break;
                case COMMAND_SAMPLE_BINARY_SAT:
                    addBinarySamplingDomain(SamplingStrategies.SAT, optionsToConsider);
                    addBinSamplingParams(SamplingStrategies.SAT, "SAT", parameterKeyAndValue, isValidation);
                    break;
                case COMMAND_SAMPLE_BINARY_DISTANCE:
                    addBinarySamplingDomain(SamplingStrategies.DISTANCE_BASED, optionsToConsider);
                    addBinSamplingParams(SamplingStrategies.DISTANCE_BASED, "DIST_BASE", parameterKeyAndValue, isValidation);
                    break;
                //TODO:hybrid as bin/num
                //case COMMAND_HYBRID_DISTRIBUTION_AWARE:
                //    addHybridAsBin(new DistributionAware(), task.Contains(COMMAND_VALIDATION), parameterKeyAndValue);
                //    break;
                //case COMMAND_HYBRID_DISTRIBUTION_PRESERVING:
                //    addHybridDesign(new DistributionPreserving(), task.Contains(COMMAND_VALIDATION),
                //        parameterKeyAndValue);
                //    break;
                default:
                    GlobalState.logError.logLine("Invalid binary strategy: " + strategyName);
                    break;
            }
        }

        private void addBinarySamplingDomain(SamplingStrategies strat, List<BinaryOption> optionsToConsider)
        {
            if (optionsToConsider.Count > 0)
            {
                this.optionsToConsider.Add(strat, optionsToConsider);
            }
        }

        private void addBinSamplingNoParams(SamplingStrategies strategy, string name, bool isValidation)
        {
            if (isValidation)
            {
                this.binaryStrategiesValidation.Add(strategy);
            }
            else
            {
                this.binaryStrategies.Add(strategy);
            }
        }

        private void addBinSamplingParams(SamplingStrategies strategy, string name,
            Dictionary<string, string> parameter, bool isValidation)
        {
            addBinSamplingNoParams(strategy, name, isValidation);
            switch (strategy)
            {
                case SamplingStrategies.BINARY_RANDOM:
                    binaryParams.randomBinaryParameters.Add(parameter);
                    break;
                case SamplingStrategies.T_WISE:
                    binaryParams.tWiseParameters.Add(parameter);
                    break;
                case SamplingStrategies.SAT:
                    binaryParams.satParameters.Add(parameter);
                    break;
                case SamplingStrategies.DISTANCE_BASED:
                    binaryParams.distanceMaxParameters.Add(parameter);
                    break;
            }
        }

        /// <summary>
        ///
        /// Note: An experimental design might have parameters and also consider only a specific set of numeric options.
        ///         [option1,option3,...,optionN] param1:value param2:value
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public string performOneCommand_ExpDesign(string task)
        {
            // splits the task in design and parameters of the design
            string[] designAndParams = task.Split(new[] { ' ' }, 2);
            string designName = designAndParams[0]; ;



            // parsing of the parameters
            List<NumericOption> optionsToConsider;
            Dictionary<string, string> parameter = new Dictionary<string, string>();
            List<ConfigurationOption> temp = new List<ConfigurationOption>();
            getParametersAndSamplingDomain(task, out parameter, out temp);
            optionsToConsider = temp.OfType<NumericOption>().ToList();

            if (optionsToConsider.Count == 0)
                optionsToConsider = GlobalState.varModel.NumericOptions;

            ExperimentalDesign expDesign;

            switch (designName.ToLower())
            {
                case COMMAND_EXPDESIGN_BOXBEHNKEN:
                    expDesign = new BoxBehnkenDesign();
                    break;
                case COMMAND_EXPDESIGN_CENTRALCOMPOSITE:
                    expDesign = new CentralCompositeInscribedDesign();
                    break;
                case COMMAND_EXPDESIGN_FULLFACTORIAL:
                    expDesign = new FullFactorialDesign();
                    break;

                case COMMAND_EXPDESIGN_FACTORIAL:
                    expDesign = new FactorialDesign();
                    break;

                case COMMAND_EXPDESIGN_HYPERSAMPLING:
                    expDesign = new HyperSampling();
                    break;

                case COMMAND_EXPDESIGN_ONEFACTORATATIME:
                    expDesign = new OneFactorAtATime();
                    break;

                case COMMAND_EXPDESIGN_KEXCHANGE:
                    expDesign = new KExchangeAlgorithm();
                    break;

                case COMMAND_EXPDESIGN_PLACKETTBURMAN:
                    expDesign = new PlackettBurmanDesign();
                    break;

                case COMMAND_EXPDESIGN_RANDOM:
                    expDesign = new RandomSampling();
                    break;

                //TODO:hybrids as bin/num
                //case COMMAND_HYBRID_DISTRIBUTION_AWARE:
                //    addHybridAsNumeric(new DistributionAware(), parameter.ContainsKey("validation"), parameter);
                //    return "";

                //case COMMAND_HYBRID_DISTRIBUTION_PRESERVING:
                //    addHybridAsNumeric(new DistributionPreserving(), parameter.ContainsKey("validation"), parameter);
                //    return "";

                default:
                    return task;
            }

            if (optionsToConsider.Count > 0)
            {
                expDesign.setSamplingDomain(optionsToConsider);
            }

            if ((expDesign is KExchangeAlgorithm || expDesign is RandomSampling)
                && parameter.ContainsKey("sampleSize") && GlobalState.varModel != null)
            {
                int maximumNumberNumVariants = computeNumberOfPossibleNumericVariants(GlobalState.varModel);
                String numberOfSamples;
                parameter.TryGetValue("sampleSize", out numberOfSamples);
                if (Double.Parse(numberOfSamples) > maximumNumberNumVariants)
                {
                    GlobalState.logInfo.logLine("The number of stated numeric variants exceeds the maximum number "
                        + "of possible variants. Only " + maximumNumberNumVariants
                        + " variants are possible. Switching to fullfactorial design.");
                    expDesign = new FullFactorialDesign();
                }
            }

            expDesign.setSamplingParameters(parameter);
            if (parameter.ContainsKey("validation"))
            {
                this.numericStrategiesValidation.Add(expDesign);
            }
            else
            {
                this.numericStrategies.Add(expDesign);
            }

            return "";
        }

        /// <summary>
        /// Calculate the number of possible configurations for numeric options in a vm.
        /// </summary>
        /// <param name="vm">The variability model used.</param>
        /// <returns>Number of possible configurations.</returns>
        private static int computeNumberOfPossibleNumericVariants(VariabilityModel vm)
        {
            List<int> numberOfSteps = new List<int>();

            foreach (NumericOption numOpt in vm.NumericOptions)
            {
                if (numOpt.Values != null)
                    numberOfSteps.Add(numOpt.Values.Count());
                else
                    numberOfSteps.Add((int)numOpt.getNumberOfSteps());
            }

            if (numberOfSteps.Count == 0)
            {
                return numberOfSteps.Count;
            }
            else
            {
                int numberOfNumVariants = 1;
                numberOfSteps.ForEach(x => numberOfNumVariants *= x);
                return numberOfNumVariants;
            }
        }

        /// <summary>
        /// This method sets the according variables to perform the hybrid sampling strategy.
        /// Note: A hybrid sampling strategy might have parameters and also consider only a specific set of numeric options.
        ///         [option1,option3,...,optionN] param1:value param2:value
        /// </summary>
        /// <param name="task">the task containing the name of the sampling strategy and the parameters</param>
        /// <returns>the name of the sampling strategy if it is not found; empty string otherwise</returns>
        public string performOneCommand_Hybrid(string task)
        {
            // splits the task in design and parameters of the design
            string[] designAndParams = task.Split(new[] { ' ' }, 2);
            string designName = designAndParams[0];

            // parsing of the parameters
            List<ConfigurationOption> optionsToConsider;
            Dictionary<string, string> parameter;
            List<ConfigurationOption> temp = new List<ConfigurationOption>();
            getParametersAndSamplingDomain(task, out parameter, out optionsToConsider);


            if (optionsToConsider.Count == 0)
            {
                optionsToConsider.AddRange(GlobalState.varModel.NumericOptions);
                optionsToConsider.AddRange(GlobalState.varModel.BinaryOptions);
            }

            HybridStrategy hybridDesign = null;

            switch (designName.ToLower())
            {
                case COMMAND_HYBRID_DISTRIBUTION_AWARE:
                    parameter.Remove(designName);
                    hybridDesign = new DistributionAware();
                    hybridDesign.SetSamplingDomain(optionsToConsider);
                    break;
                case COMMAND_HYBRID_DISTRIBUTION_PRESERVING:
                    parameter.Remove(designName);
                    hybridDesign = new DistributionPreserving();
                    hybridDesign.SetSamplingDomain(optionsToConsider);
                    break;
                default:
                    return task;
            }

            addHybridDesign(hybridDesign, parameter.ContainsKey("validation"), parameter);

            return "";
        }

        private void addHybridDesign(HybridStrategy hybrid, bool isValidation, Dictionary<string, string> parameters)
        {
            hybrid.SetSamplingParameters(parameters);
            if (isValidation)
            {
                this.hybridStrategiesValidation.Add(hybrid);
            }
            else
            {
                this.hybridStrategies.Add(hybrid);
            }

        }

        private bool isAllMeasurementsToSample()
        {
            return binaryStrategies.Contains(SamplingStrategies.ALLBINARY) 
                && numericStrategies.Any(strategy => strategy is FullFactorialDesign);
        }

        private bool isAllMeasurementsValidation()
        {
            return binaryStrategiesValidation.Contains(SamplingStrategies.ALLBINARY) 
                && numericStrategiesValidation.Any(strategy => strategy is FullFactorialDesign);
        }

        private bool allMeasurementsValid()
        {
            foreach (Configuration conf in GlobalState.allMeasurements.Configurations)
            {
                if (!conf.nfpValues.ContainsKey(GlobalState.currentNFP))
                    return false;
            }
            return true;
        }

        public Tuple<List<Configuration>, List<Configuration>> buildSetsEfficient(ML_Settings mlSettings)
        {
            bool measurementsValid = false;
            List<Configuration> configurationsLearning;
            List<Configuration> configurationsValidation;

            if (isAllMeasurementsToSample() && allMeasurementsValid() && (mlSettings.blacklisted == null || mlSettings.blacklisted.Count == 0))
            {
                measurementsValid = true;
                configurationsLearning = GlobalState.allMeasurements.Configurations;
            }
            else
            {
                configurationsLearning = buildSet(mlSettings, binaryStrategies, numericStrategies, hybridStrategies);
            }

            if (isAllMeasurementsValidation() && (measurementsValid || allMeasurementsValid()) && (mlSettings.blacklisted == null || mlSettings.blacklisted.Count == 0))
            {
                configurationsValidation = GlobalState.allMeasurements.Configurations;
            }
            else
            {
                configurationsValidation = buildSet(mlSettings, binaryStrategiesValidation, numericStrategiesValidation, hybridStrategiesValidation);
            }
            return Tuple.Create(configurationsLearning, configurationsValidation);
        }

        public List<Configuration> buildSet(ML_Settings mlSettings)
        {
            return buildSet(mlSettings, binaryStrategies, numericStrategies, hybridStrategies);
        }

        public List<Configuration> buildSetForValidation(ML_Settings mlSettings)
        {
            return buildSet(mlSettings, binaryStrategiesValidation, numericStrategiesValidation, hybridStrategiesValidation);
        }

        private List<Configuration> buildSet(ML_Settings mlSettings, List<SamplingStrategies> binaryStrats, List<ExperimentalDesign> numericStrats, List<HybridStrategy> hybridStrats)
        {
            blacklisted = mlSettings.blacklisted;
            List<Configuration> configurationsTest = buildConfigs(GlobalState.varModel, binaryStrats, numericStrats, hybridStrats);
            configurationsTest = GlobalState.getMeasuredConfigs(configurationsTest);
            return configurationsTest;
        }

        public List<Configuration> buildConfigs(VariabilityModel vm)
        {
            return buildConfigs(vm, binaryStrategies, numericStrategies, hybridStrategies);
        }

        public List<Configuration> buildConfigsForValidation(VariabilityModel vm)
        {
            return buildConfigs(vm, binaryStrategiesValidation, numericStrategiesValidation, hybridStrategiesValidation);
        }

        private List<Configuration> buildConfigs(VariabilityModel vm, List<SamplingStrategies> binaryStrats, List<ExperimentalDesign> numericStrats, List<HybridStrategy> hybridStrats)
        {
            List<Configuration> result = new List<Configuration>();

            List<List<BinaryOption>> binaryConfigs = new List<List<BinaryOption>>();
            List<Dictionary<NumericOption, Double>> numericConfigs = new List<Dictionary<NumericOption, double>>();
            foreach (SamplingStrategies strat in binaryStrats)
            {
                switch (strat)
                {
                    //Binary sampling heuristics
                    case SamplingStrategies.ALLBINARY:
                        if (optionsToConsider.ContainsKey(SamplingStrategies.ALLBINARY))
                        {
                            List<List<BinaryOption>> variants =
                                vg.GenerateAllVariantsFast(vm.reduce(optionsToConsider[SamplingStrategies.ALLBINARY]));
                            binaryConfigs.AddRange(changeModel(vm, variants));
                        }
                        else
                        {
                            binaryConfigs.AddRange(vg.GenerateAllVariantsFast(vm));
                        }
                        break;
                    case SamplingStrategies.SAT:
                        int numberSamples = 2;
                        foreach (Dictionary<string, string> parameters in binaryParams.satParameters)
                        {
                            if (parameters.ContainsKey("henard"))
                            {
                                try
                                {
                                    bool b = Boolean.Parse(parameters["henard"]);
                                    ((Z3VariantGenerator)vg).henard = b;
                                }
                                catch (FormatException e)
                                {
                                    Console.Error.WriteLine(e);
                                }
                            }
                            if (parameters.ContainsKey("numConfigs"))
                            {
                                try
                                {
                                    numberSamples = Int32.Parse(parameters["numConfigs"]);
                                }
                                catch (FormatException)
                                {
                                    TWise tw = new TWise();
                                    numberSamples = tw.generateT_WiseVariants_new(GlobalState.varModel, Int32.Parse(parameters["numConfigs"].Remove(0, 4))).Count;
                                }
                            }

                            if (parameters.ContainsKey("seed") && vg is Z3VariantGenerator)
                            {
                                uint seed = 0;
                                seed = UInt32.Parse(parameters["seed"]);
                                ((Z3VariantGenerator)vg).setSeed(seed);
                            }
                            if (optionsToConsider.ContainsKey(SamplingStrategies.SAT))
                            {
                                List<List<BinaryOption>> variants =
                                    vg.GenerateUpToNFast(vm.reduce(optionsToConsider[SamplingStrategies.SAT]), numberSamples);
                                binaryConfigs.AddRange(changeModel(vm, variants));
                            }
                            else
                            {
                                binaryConfigs.AddRange(vg.GenerateUpToNFast(vm, numberSamples));
                            }
                            numberSamples = 2;
                        }
                        break;
                    case SamplingStrategies.BINARY_RANDOM:
                        RandomBinary rb;
                        if (optionsToConsider.ContainsKey(SamplingStrategies.BINARY_RANDOM))
                        {
                            rb = new RandomBinary(vm.reduce(optionsToConsider[SamplingStrategies.BINARY_RANDOM]));
                        }
                        else
                        {
                            rb = new RandomBinary(vm);
                        }
                        foreach (Dictionary<string, string> expDesignParamSet in binaryParams.randomBinaryParameters)
                        {
                            binaryConfigs.AddRange(changeModel(vm, rb.getRandomConfigs(expDesignParamSet)));
                        }

                        break;
                    case SamplingStrategies.OPTIONWISE:
                        {
                            FeatureWise fw = new FeatureWise();
                            if (optionsToConsider.ContainsKey(SamplingStrategies.OPTIONWISE))
                            {
                                List<List<BinaryOption>> variants = fw.generateFeatureWiseConfigurations(GlobalState.varModel
                                    .reduce(optionsToConsider[SamplingStrategies.OPTIONWISE]));
                                binaryConfigs.AddRange(changeModel(vm, variants));
                            }
                            else
                            {
                                binaryConfigs.AddRange(fw.generateFeatureWiseConfigurations(GlobalState.varModel));
                            }
                        }
                        break;
                    case SamplingStrategies.DISTANCE_BASED:
                        foreach (Dictionary<string, string> parameters in binaryParams.distanceMaxParameters)
                        {
                            DistanceBased distSampling = new DistanceBased(vm);
                            binaryConfigs.AddRange(distSampling.getSample(parameters));
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
                            if (optionsToConsider.ContainsKey(SamplingStrategies.PAIRWISE))
                            {
                                List<List<BinaryOption>> variants = pw.generatePairWiseVariants(GlobalState.varModel
                                    .reduce(optionsToConsider[SamplingStrategies.PAIRWISE]));
                                binaryConfigs.AddRange(changeModel(vm, variants));
                            }
                            else
                            {
                                binaryConfigs.AddRange(pw.generatePairWiseVariants(GlobalState.varModel));
                            }
                        }
                        break;
                    case SamplingStrategies.NEGATIVE_OPTIONWISE:
                        {
                            NegFeatureWise neg = new NegFeatureWise();//2nd option: neg.generateNegativeFWAllCombinations(GlobalState.varModel));
                            if (optionsToConsider.ContainsKey(SamplingStrategies.NEGATIVE_OPTIONWISE))
                            {
                                List<List<BinaryOption>> variants = neg.generateNegativeFW(GlobalState.varModel
                                    .reduce(optionsToConsider[SamplingStrategies.NEGATIVE_OPTIONWISE]));
                                binaryConfigs.AddRange(changeModel(vm, variants));
                            }
                            else
                            {
                                binaryConfigs.AddRange(neg.generateNegativeFW(GlobalState.varModel));
                            }
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

                                if (optionsToConsider.ContainsKey(SamplingStrategies.T_WISE))
                                {
                                    List<List<BinaryOption>> variants = tw.generateT_WiseVariants_new(
                                        vm.reduce(optionsToConsider[SamplingStrategies.T_WISE]), t);
                                    binaryConfigs.AddRange(changeModel(vm, variants));
                                }
                                else
                                {
                                    binaryConfigs.AddRange(tw.generateT_WiseVariants_new(vm, t));
                                }
                            }
                        }
                        break;
                }
            }

            //Experimental designs for numeric options
            if (numericStrats.Count != 0)
            {
                handleDesigns(numericStrats, numericConfigs, vm);
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

            // Filter configurations based on the NonBooleanConstraints
            List<Configuration> filtered = new List<Configuration>();
            foreach (Configuration conf in result)
            {
                bool isValid = true;
                foreach (NonBooleanConstraint nbc in vm.NonBooleanConstraints)
                {
                    if (!nbc.configIsValid(conf))
                    {
                        isValid = false;
                    }
                }

                if (isValid)
                    filtered.Add(conf);
            }
            result = filtered;


            // Hybrid designs
            if (hybridStrats.Count != 0)
            {
                List<Configuration> configurations = ExecuteHybridStrategy(hybridStrats, vm);

                if (numericStrats.Count == 0 && binaryStrats.Count == 0)
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

            if (vm.MixedConstraints.Count == 0)
            {
                if (binaryStrats.Count == 1 && binaryStrats.Last().Equals(SamplingStrategies.ALLBINARY) && numericStrats.Count == 1 && numericStrats.Last() is FullFactorialDesign)
                {
                    return replaceReference(result.ToList());
                }
                return replaceReference(result.Distinct().ToList());
            }
            List<Configuration> unfilteredList = result.Distinct().ToList();
            List<Configuration> filteredConfiguration = new List<Configuration>();
            foreach (Configuration toTest in unfilteredList)
            {
                bool isValid = true;
                foreach (MixedConstraint constr in vm.MixedConstraints)
                {
                    if (!constr.requirementsFulfilled(toTest))
                    {
                        isValid = false;
                    }
                }

                if (isValid)
                {
                    filteredConfiguration.Add(toTest);
                }
            }
            return replaceReference(filteredConfiguration);
        }

        private static List<Configuration> replaceReference(List<Configuration> sampled)
        {
            // Replaces the reference of the sampled configuration with the corresponding measured configuration if it exists

            var measured = GlobalState.allMeasurements.Configurations.Intersect(sampled);
            var notMeasured = sampled.Except(measured);
            return measured.Concat(notMeasured).ToList();
        }

        private List<Configuration> ExecuteHybridStrategy(List<HybridStrategy> hybridStrategies, VariabilityModel vm)
        {
            List<Configuration> allSampledConfigurations = new List<Configuration>();
            foreach (HybridStrategy hybrid in hybridStrategies)
            {
                hybrid.SetExistingConfigurations(existingConfigurations);
                hybrid.ComputeSamplingStrategy();
                allSampledConfigurations.AddRange(hybrid.selectedConfigurations);
            }
            return allSampledConfigurations;
        }

        private static List<List<BinaryOption>> changeModel(VariabilityModel vm, List<List<BinaryOption>> variants)
        {
            List<List<BinaryOption>> toReturn = new List<List<BinaryOption>>();

            foreach (List<BinaryOption> variant in variants)
            {
                List<BinaryOption> variantInRightModel = new List<BinaryOption>();

                foreach (BinaryOption opt in variant)
                {
                    variantInRightModel.Add(vm.getBinaryOption(opt.Name));
                }

                toReturn.Add(variantInRightModel);
            }

            return toReturn;
        }

        private void handleDesigns(List<ExperimentalDesign> samplingDesigns, List<Dictionary<NumericOption, Double>> numericOptions,
            VariabilityModel vm)
        {
            foreach (ExperimentalDesign samplingDesign in samplingDesigns)
            {
                if (samplingDesign.getSamplingDomain() == null ||
                    samplingDesign.getSamplingDomain().Count == 0)
                {
                    samplingDesign.setSamplingDomain(vm.getNonBlacklistedNumericOptions(blacklisted));
                }
                else
                {
                    samplingDesign.setSamplingDomain(vm.getNonBlacklistedNumericOptions(blacklisted)
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
