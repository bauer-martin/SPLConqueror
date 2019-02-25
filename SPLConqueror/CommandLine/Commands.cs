using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MachineLearning.Learning;
using MachineLearning.Learning.Regression;
using MachineLearning.Sampling.ExperimentalDesigns;
using MachineLearning.Solver;
using SPLConqueror_Core;
using MachineLearning.Sampling;
using Persistence;
using ProcessWrapper;
using MachineLearning.Sampling.Hybrid;
using MachineLearning.Sampling.Hybrid.Distributive;
using System.Diagnostics;

namespace CommandLine
{
    public class Commands
    {
        public const string COMMAND = "command: ";
        public const string COMMAND_TRUEMODEL = "truemodel";

        public const string COMMAND_LOG = "log";
        public const string COMMAND_MEASUREMENTS_TO_CSV = "measurementstocsv";

        public const string COMMAND_CLEAR_GLOBAL = "clean-global";
        public const string COMMAND_CLEAR_SAMPLING = "clean-sampling";
        public const string COMMAND_CLEAR_LEARNING = "clean-learning";

        public const string COMMAND_LOAD_CONFIGURATIONS = "all";

        #region load ml settings
        // deprecated
        public const string COMMAND_LOAD_MLSETTINGS = "load_mlsettings";
        // for uniform format of commands
        public const string COMMAND_LOAD_MLSETTINGS_UNIFORM = "load-mlsettings";
        #endregion

        public const string RESUME_FROM_DUMP = "resume-dump";

        //resume a A script with only log files.
        public const string RESUME_FROM_LOG = "resume-log";

        //save current SPLConqueror state to a file.
        public const string COMMAND_SAVE = "save";

        public const string COMMAND_EVALUATION_SET = "evaluationset";

        #region splconqueror learn with all measurements
        // deprecated
        public const string COMMAND_START_ALLMEASUREMENTS = "learnwithallmeasurements";

        public const string COMMAND_SELECT_ALL_MEASUREMENTS = "select-all-measurements";
        #endregion

        #region splconqueror predict all configurations
        public const string COMMAND_PREDICT_ALL_CONFIGURATIONS_SPLC = "predict-all-configs-splconqueror";
        // deprecated
        public const string COMMAND_PREDICT_ALL_CONFIGURATIONS = "predictall";
        #endregion

        public const string COMMAND_EVALUATE_MODEL = "evaluate-model";
        public const string COMMAND_ANALYZE_LEARNING = "analyze-learning";

        #region splconqueror predict configurations
        public const string COMMAND_PREDICT_CONFIGURATIONS_SPLC = "predict-configs-splconqueror";
        // deprecated
        public const string COMMAND_PREDICT_CONFIGURATIONS = "predict-configurations";
        #endregion

        // using this option, a partial or full option order can be defined. The order is used in printconfigs. To define an order, the names of the options have to be defined separated with whitespace. If an option is not defined in the order its name and the value is printed at the end of the configuration.
        public const string COMMAND_SAMPLING_OPTIONORDER = "optionorder";
        public const string COMMAND_PRINT_CONFIGURATIONS = "printconfigs";
        public const string COMMAND_PRINT_MLSETTINGS = "printsettings";

        public const string COMMAND_VARIABILITYMODEL = "vm";
        public const string COMMAND_SET_NFP = "nfp";
        public const string COMMAND_SET_MLSETTING = "mlsettings";
        public const string COMMAND_SET_SOLVER = "solver";

        #region splconqueror learn with sampling
        public const string COMMAND_START_LEARNING_SPL_CONQUEROR = "learn-splconqueror";
        // deprecated
        public const string COMMAND_START_LEARNING = "start";
        #endregion

        #region splconqueror active learning
        public const string COMMAND_START_ACTIVE_LEARNING_SPL_CONQUEROR = "active-learn-splconqueror";
        #endregion

        #region Splconqueror parameter opt
        public const string COMMAND_OPTIMIZE_PARAMETER_SPLCONQUEROR = "learn-splconqueror-opt";
        // deprecated
        public const string COMMAND_OPTIMIZE_PARAMETER = "optimize-parameter";
        #endregion

        public const string COMMAND_SUBSCRIPT = "script";

        public const string DEFINE_PYTHON_PATH = "define-python-path";
        public const string COMMAND_PYTHON_LEARN = "learn-python";
        public const string COMMAND_PYTHON_LEARN_OPT = "learn-python-opt";

        #region Conversion Commands
        public const string COMMAND_CONVERT_MEASUREMENTS = "convert-measurements";
        public const string COMMAND_CONVERT_VM = "convert-vm";
        #endregion

        #region Intern commands
        // shouldn't be used by user.
        public const string ROLLBACK_FLAG = "rollback";
        #endregion

        private bool allMeasurementsSelected = false;

        ML_Settings mlSettings = new ML_Settings();
        InfluenceFunction trueModel = null;

        private CommandHistory currentHistory = new CommandHistory();
        private bool hasLearnData = false;

        public ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        public Learning exp = new MachineLearning.Learning.Regression.Learning();

        public static string targetPath = "";

        public static string pyResult = "";

        /// <summary>
        /// Performs the functionality of one command. If no functionality is found for the command, the command is returned by this method.
        /// </summary>
        /// <param name="line">One command with its parameters.</param>
        /// <returns>Returns an empty string if the command could be performed by the method. If the command could not be performed by the method, the original command is returned.</returns>
        public string performOneCommand(string line)
        {
            string command;

            // remove comment part of the line (the comment starts with an #)
            line = line.Split(new Char[] { '#' }, 2)[0];
            if (line.Length == 0)
                return "";

            currentHistory.addCommand(line);

            // split line in command and parameters of the command
            string[] components = line.Split(new Char[] { ' ' }, 2);

            // ReSharper disable once SuggestVarOrType_BuiltInTypes
            string task = "";
            if (components.Length > 1)
                task = components[1];

            string[] taskAsParameter = task.Split(new Char[] { ' ' });
            if (!GlobalState.rollback)
            {
                GlobalState.logInfo.logLine(COMMAND + line);

                command = components[0];
            }
            else
            {
                command = components[0];
                if (!command.Equals(COMMAND_SUBSCRIPT))
                {
                    command = ROLLBACK_FLAG;
                }
            }

            switch (command.ToLower())
            {

                case COMMAND_SELECT_ALL_MEASUREMENTS:
                    if (task == null)
                    {
                        GlobalState.logInfo.logLine("The command needs either true or false as argument");
                    }
                    else if (task.Trim().ToLower().Equals("true"))
                    {
                        this.allMeasurementsSelected = true;
                    }
                    else if (task.Trim().ToLower().Equals("false"))
                    {
                        this.allMeasurementsSelected = false;
                    }
                    else
                    {
                        GlobalState.logInfo.logLine("Invalid argument. Only true or false are allowed");
                    }
                    break;

                case RESUME_FROM_DUMP:
                    {
                        Tuple<ML_Settings, List<SamplingStrategies>, List<SamplingStrategies>> recoveredData = CommandPersistence.recoverDataFromDump(taskAsParameter);
                        if (recoveredData == null)
                        {
                            GlobalState.logError.logLine("Couldn't recover.");
                        }
                        else
                        {
                            this.mlSettings = recoveredData.Item1;
                            this.configBuilder.binaryStrategies = recoveredData.Item2;
                            this.configBuilder.binaryStrategiesValidation = recoveredData.Item3;

                            FileInfo fi = new FileInfo(taskAsParameter[1]);
                            StreamReader reader = null;
                            if (!fi.Exists)
                                throw new FileNotFoundException(@"Automation script not found. ", fi.ToString());

                            reader = fi.OpenText();
                            Commands co = new Commands();
                            if (CommandPersistence.learningHistory != null && CommandPersistence.learningHistory.Item2.Count > 0 && CommandPersistence.learningHistory.Item1)
                            {
                                //restore exp
                                hasLearnData = true;
                            }
                            co.exp = this.exp;
                            co.configBuilder.binaryStrategies = this.configBuilder.binaryStrategies;
                            co.configBuilder.binaryStrategiesValidation = this.configBuilder.binaryStrategiesValidation;
                            co.mlSettings = this.mlSettings;
                            GlobalState.rollback = true;

                            while (!reader.EndOfStream)
                            {
                                String oneLine = reader.ReadLine().Trim();
                                co.performOneCommand(oneLine);

                            }
                        }
                        break;
                    }
                case COMMAND_SAVE:
                    {
                        CommandPersistence.dump(taskAsParameter, this.mlSettings, this.configBuilder.binaryStrategies,
                            this.configBuilder.binaryStrategiesValidation, this.exp, this.currentHistory);
                        break;
                    }
                case ROLLBACK_FLAG:
                    if (currentHistory.Equals(CommandPersistence.history))
                    {
                        GlobalState.rollback = false;
                        GlobalState.logInfo.logLine("Performed rollback");
                    }
                    break;

                case RESUME_FROM_LOG:
                    Tuple<bool, Dictionary<string, string>> reachedEndAndRelevantCommands = CommandPersistence.findRelevantCommandsLogFiles(task.TrimEnd(), new Dictionary<string, string>());
                    if (reachedEndAndRelevantCommands.Item1)
                    {
                        GlobalState.logInfo.logLine("The end of the script was already reached");
                    }
                    else
                    {
                        string logBuffer = null;
                        foreach (KeyValuePair<string, string> kv in reachedEndAndRelevantCommands.Item2)
                        {
                            if (!kv.Key.Equals(COMMAND_SUBSCRIPT))
                            {
                                if (kv.Key.Equals(COMMAND_LOG))
                                {
                                    logBuffer = kv.Value.Split()[1].Trim();
                                }
                                else if (!(kv.Key.Equals(COMMAND_START_LEARNING) || kv.Key.Equals(COMMAND_START_ALLMEASUREMENTS)))
                                {
                                    performOneCommand(kv.Value);
                                }
                            }
                        }
                        GlobalState.logInfo = new InfoLogger(logBuffer, true);

                        if (CommandPersistence.learningHistory != null && CommandPersistence.learningHistory.Item2.Count > 0 && CommandPersistence.learningHistory.Item1)
                        {
                            //restore exp
                            hasLearnData = true;
                        }
                        FileInfo fi = new FileInfo(task.TrimEnd());
                        StreamReader reader = null;
                        if (!fi.Exists)
                            throw new FileNotFoundException(@"Automation script not found. ", fi.ToString());

                        reader = fi.OpenText();
                        Commands co = new Commands();
                        if (CommandPersistence.learningHistory != null && CommandPersistence.learningHistory.Item2.Count > 0 && CommandPersistence.learningHistory.Item1)
                        {
                            //restore exp
                            co.hasLearnData = true;
                        }
                        co.exp = this.exp;
                        co.configBuilder = this.configBuilder;
                        co.mlSettings = this.mlSettings;
                        GlobalState.rollback = true;

                        while (!reader.EndOfStream)
                        {
                            String oneLine = reader.ReadLine().Trim();
                            co.performOneCommand(oneLine);

                        }
                    }
                    break;

                case COMMAND_TRUEMODEL:
                    // For this option, two arguments can be provided.
                    // The first option is mandatory and represents the path of the model.
                    // The second option is optional and represents the path to the file where the predictions should be written to.
                    string[] paths = task.Trim().Split(' ');

                    StreamReader readModel = new StreamReader(paths[0]);
                    // Each line of the file contains one term of the model
                    List<string> model = new List<string>();
                    while (!readModel.EndOfStream)
                    {
                        model.Add(readModel.ReadLine());
                    }
                    readModel.Close();
                    List<Feature> trueModelFeatures = new List<Feature>();
                    foreach (string term in model)
                    {
                        trueModelFeatures.Add(new Feature(term, GlobalState.varModel));
                    }

                    InfluenceModel influenceModel = new InfluenceModel(GlobalState.varModel, GlobalState.currentNFP);
                    FeatureSubsetSelection featureSubsetSelection = new FeatureSubsetSelection(influenceModel, this.mlSettings);

                    LearningRound learningRound = featureSubsetSelection.LearnWithTrueModel(trueModelFeatures);
                    GlobalState.logInfo.logLine(learningRound.ToString());

                    // Now, predict all configurations
                    if (paths.Length == 2)
                    {
                        GlobalState.logInfo.logLine("Writing the predictions to " + paths[1] + ".");
                        predict(paths[1], null, learningRound.FeatureSet);
                    }
                    else
                    {
                        GlobalState.logInfo.logLine("As no path is given, no predictions are written into a file.");
                    }

                    break;

                case COMMAND_EVALUATE_MODEL:
                    // For this option, two arguments can be provided.
                    // The first option is mandatory and represents the path of the model.
                    // The second option is optional and represents the path to the file where the predictions should be written to.
                    string[] filePaths = task.Trim().Split(' ');
                    if (!File.Exists(filePaths[0].Trim()))
                    {
                        GlobalState.logError.logLine("The given file '" + filePaths[0].Trim() + "' does not exist.");
                    }
                    // A file may contain multiple models
                    StreamReader modelReader = new StreamReader(filePaths[0].Trim());
                    List<string> models = new List<string>();
                    while (!modelReader.EndOfStream)
                    {
                        models.Add(modelReader.ReadLine());
                    }
                    modelReader.Close();

                    // Initialization
                    InfluenceModel infModel = new InfluenceModel(GlobalState.varModel, GlobalState.currentNFP);
                    FeatureSubsetSelection fSS = new FeatureSubsetSelection(infModel, this.mlSettings);

                    // Evaluate each model in the input file
                    for (int i = 0; i < models.Count; i++)
                    {
                        string m = models[i];
                        string[] terms = m.Split('+');
                        List<Feature> currentModel = new List<Feature>();

                        foreach (string term in terms)
                        {
                            string[] elements = term.Split('*');
                            string coefficient = elements[0];
                            string variables = string.Join(" * ", elements.Skip(1).ToArray());
                            Feature f = new Feature(variables, GlobalState.varModel);
                            f.Constant = double.Parse(coefficient);

                            currentModel.Add(f);
                        }

                        // Now, predict all configurations
                        if (filePaths.Length == 2)
                        {
                            string currentPath = filePaths[1];
                            currentPath = Path.GetDirectoryName(currentPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(currentPath) + "_" + i + Path.GetExtension(currentPath);
                            GlobalState.logInfo.logLine("Writing the predictions to " + currentPath + ".");
                            predict(currentPath, null, currentModel);
                        }
                        else
                        {
                            GlobalState.logInfo.logLine("As no path is given, no predictions are written into a file.");
                        }
                    }

                    break;

                case COMMAND_SUBSCRIPT:
                    {
                        FileInfo fi = new FileInfo(task.TrimEnd());
                        StreamReader reader = null;
                        if (!fi.Exists)
                            GlobalState.logError.logLine(@"Automation script not found. " + fi.ToString());
                        else
                        {
                            reader = fi.OpenText();
                            Commands co = new Commands();
                            co.exp = this.exp;

                            // Set the root directory to the location of the referenced file
                            String previousRootDirectory = Directory.GetCurrentDirectory();
                            String filePath = fi.DirectoryName;
                            Directory.SetCurrentDirectory(filePath);

                            co.currentHistory = this.currentHistory;
                            if (GlobalState.rollback)
                            {
                                co.configBuilder = this.configBuilder;
                                co.mlSettings = this.mlSettings;
                            }

                            co.hasLearnData = this.hasLearnData;
                            co.exp = this.exp;

                            this.hasLearnData = co.hasLearnData;

                            while (!reader.EndOfStream)
                            {
                                String oneLine = reader.ReadLine().Trim();
                                co.performOneCommand(oneLine);

                            }

                            // Reset the root directory after the execution of the sub-script
                            Directory.SetCurrentDirectory(previousRootDirectory);
                        }
                        break;
                    }

                case COMMAND_EVALUATION_SET:
                    {
                        GlobalState.evaluationSet.Configurations = ConfigurationReader.readConfigurations(task, GlobalState.varModel);
                        GlobalState.logInfo.logLine("Evaluation set loaded.");
                    }
                    break;
                case COMMAND_CLEAR_GLOBAL:
                    cleanGlobal();
                    break;
                case COMMAND_CLEAR_SAMPLING:
                    cleanSampling();
                    break;
                case COMMAND_CLEAR_LEARNING:
                    cleanLearning();
                    break;
                case COMMAND_LOAD_CONFIGURATIONS:
                    GlobalState.allMeasurements.setBlackList(mlSettings.blacklisted);

                    try
                    {
                        GlobalState.allMeasurements.Configurations = ConfigurationReader.readConfigurations(task.TrimEnd(), GlobalState.varModel);
                    }
                    catch (ArgumentNullException)
                    {
                        throw new ArgumentException("There was a problem when reading your configuration file." +
                            " Please check the error log for further information about the cause of the error." +
                            " Filename of the file that caused the error: \"" + task.TrimEnd() + "\"");
                    }

                    List<Configuration> invalid = GlobalState.allMeasurements.Configurations
                        .Where(conf => !GlobalState.varModel.isInModel(conf)).ToList();
                    CheckConfigSAT constraintSystem = new CheckConfigSAT();
                    invalid = invalid.Union(GlobalState.allMeasurements.Configurations
                        .Where(conf => !constraintSystem.checkConfigurationSAT(conf.BinaryOptions.ToList()
                        .Where(kv => kv.Value == BinaryOption.BinaryValue.Selected).ToList()
                        .Select(kv => kv.Key).ToList(), GlobalState.varModel, false))).ToList();
                    invalid.ForEach(conf => GlobalState.logError.logLine("Invalid configuration: " + conf.ToString()));

                    GlobalState.measurementSource = task.TrimEnd();
                    string attachement = "";
                    if (GlobalState.measurementDeviation > 0 && this.mlSettings != null && mlSettings.abortError == 1)
                    {
                        this.mlSettings.abortError = GlobalState.measurementDeviation;
                        attachement = " abortError set to highest deviation value: " + GlobalState.measurementDeviation + ".";
                    }
                    GlobalState.logInfo.logLine(GlobalState.allMeasurements.Configurations.Count + " configurations loaded." + attachement);
                    break;


                case COMMAND_MEASUREMENTS_TO_CSV:
                    FileStream ostrm;
                    ostrm = new FileStream(task.Trim(), FileMode.OpenOrCreate, FileAccess.Write);
                    ostrm.SetLength(0);
                    StreamWriter writer = new StreamWriter(ostrm);
                    StringBuilder header = new StringBuilder();
                    List<NFProperty> propertiesOrder = new List<NFProperty>();
                    for (int i = 0; i < GlobalState.varModel.optionToIndex.Count; i++)
                    {
                        header.Append(GlobalState.varModel.optionToIndex[i].Name + ";");
                    }
                    foreach (NFProperty prop in GlobalState.nfProperties.Values)
                    {
                        header.Append(prop.Name + ";");
                        propertiesOrder.Add(prop);
                    }
                    header.Append("\n");
                    StringBuilder configurations = new StringBuilder();
                    foreach (Configuration config in GlobalState.allMeasurements.Configurations)
                    {
                        for (int i = 0; i < GlobalState.varModel.optionToIndex.Count; i++)
                        {
                            ConfigurationOption opt = GlobalState.varModel.optionToIndex[i];
                            if (opt.GetType() == typeof(BinaryOption))
                            {
                                if (config.BinaryOptions.ContainsKey((BinaryOption)opt) && config.BinaryOptions[(BinaryOption)opt] == BinaryOption.BinaryValue.Selected)
                                    configurations.Append("1;");
                                else
                                    configurations.Append("0;");
                            }
                            else
                            {
                                configurations.Append(config.NumericOptions[(NumericOption)opt] + ";");
                            }
                        }
                        for (int i = 0; i < propertiesOrder.Count; i++)
                        {
                            if (!config.nfpValues.ContainsKey(propertiesOrder[i]))
                                configurations.Append("0;");
                            else
                                configurations.Append(config.nfpValues[propertiesOrder[i]] + ";");
                        }
                        configurations.Append("\n");
                    }

                    writer.Write(header);
                    writer.Write(configurations);
                    writer.Flush();
                    writer.Close();
                    ostrm.Close();
                    break;
                case COMMAND_SET_SOLVER:
                    // Select the solver
                    string solverToSet = task.Trim();
                    IVariantGenerator selectedVariantGenerator = VariantGeneratorFactory.GetVariantGenerator(solverToSet);
                    if (selectedVariantGenerator == null)
                    {
                        throw new ArgumentException("The solver '" + solverToSet + "' was not found. Please specify one of the following: "
                            + VariantGeneratorFactory.GetSolver());
                    }
                    else
                    {
                        ConfigurationBuilder.vg = selectedVariantGenerator;
                    }
                    break;
                case COMMAND_PREDICT_ALL_CONFIGURATIONS_SPLC:
                    {
                        printPredictedConfigurations(task, this.exp);

                        break;
                    }

                case COMMAND_PREDICT_CONFIGURATIONS_SPLC:
                    {
                        FeatureSubsetSelection learnedModel = exp.models[exp.models.Count - 1];
                        String samplingIdentifier = createSamplingIdentifier();

                        PythonPredictionWriter csvWriter = new PythonPredictionWriter(targetPath, new String[] { "SPLConqueror" }, GlobalState.varModel.Name + "_" + samplingIdentifier);
                        List<Feature> features = learnedModel.LearningHistory[learnedModel.LearningHistory.Count - 1].FeatureSet;
                        csvWriter.writePredictions("Configuration;MeasuredValue;PredictedValue\n");
                        for (int i = 0; i < GlobalState.allMeasurements.Configurations.Count; i++)
                        {

                            Double predictedValue = FeatureSubsetSelection.estimate(features, GlobalState.allMeasurements.Configurations[i]);
                            csvWriter.writePredictions(GlobalState.allMeasurements.Configurations[i].ToString().Replace(";", "_") + ";" + Math.Round(GlobalState.allMeasurements.Configurations[i].GetNFPValue(), 4) + ";" + Math.Round(predictedValue, 4) + "\n");
                        }
                        csvWriter.close();

                        break;
                    }

                case COMMAND_ANALYZE_LEARNING:
                    {
                        GlobalState.logInfo.logLine("Round, Model, LearningError, LearningErrorRel, ValidationError, ValidationErrorRel, ElapsedSeconds, ModelComplexity, BestCandidate, BestCandidateSize, BestCandidateScore, TestError");
                        GlobalState.logInfo.logLine("Models:");

                        if (this.mlSettings.bagging)
                        {

                            for (int i = 0; i < this.exp.models.Count; i++)
                            {
                                FeatureSubsetSelection learnedModel = exp.models[i];
                                if (learnedModel == null)
                                {
                                    GlobalState.logError.logLine("Error... learning was not performed!");
                                    break;
                                }
                                GlobalState.logInfo.logLine("Termination reason: " + learnedModel.LearningHistory.Last().terminationReason);
                                foreach (LearningRound lr in learnedModel.LearningHistory)
                                {
                                    double relativeError = 0;
                                    if (GlobalState.evaluationSet.Configurations.Count > 0)
                                    {
                                        // last parameter -- here, we remove the epsilon-tube around the performance-influence model to be able to compute the real error of the predictions
                                        relativeError = learnedModel.computeError(lr.FeatureSet, GlobalState.evaluationSet.Configurations, false);
                                    }
                                    else
                                    {
                                        // last parameter -- here, we remove the epsilon-tube around the performance-influence model to be able to compute the real error of the predictions
                                        relativeError = learnedModel.computeError(lr.FeatureSet, GlobalState.allMeasurements.Configurations, false);
                                    }

                                    GlobalState.logInfo.logLine(lr.ToString() + relativeError);
                                }
                            }
                        }
                        else
                        {
                            if (exp.models.Count == 0 || exp.models[0] == null)
                            {
                                GlobalState.logError.logLine("Error... learning was not performed!");
                                break;
                            }

                            FeatureSubsetSelection learnedModel = exp.models[0];

                            GlobalState.logInfo.logLine("Termination reason: " + learnedModel.LearningHistory.Last().terminationReason);
                            foreach (LearningRound lr in learnedModel.LearningHistory)
                            {
                                double relativeError = 0;
                                if (GlobalState.evaluationSet.Configurations.Count > 0)
                                {
                                    relativeError = learnedModel.computeError(lr.FeatureSet, GlobalState.evaluationSet.Configurations, false);
                                }
                                else
                                {
                                    relativeError = learnedModel.computeError(lr.FeatureSet, GlobalState.allMeasurements.Configurations, false);
                                }

                                GlobalState.logInfo.logLine(lr.ToString() + relativeError);
                            }
                        }
                        GlobalState.logInfo.logLine("Analyze finished");


                        break;
                    }
                case COMMAND_SAMPLING_OPTIONORDER:
                    parseOptionOrder(task);
                    break;

                case COMMAND_VARIABILITYMODEL:
                    String debug = Directory.GetCurrentDirectory();
                    GlobalState.vmSource = task.TrimEnd();
                    GlobalState.varModel = VariabilityModel.loadFromXML(task.Trim());
                    if (GlobalState.varModel == null)
                    {
                        GlobalState.logError.logLine("No variability model found at " + task);
                    }
                    else if (mlSettings.blacklisted.Count > 0)
                    {
                        mlSettings.checkAndCleanBlacklisted();
                    }
                    if (targetPath.Length == 0)
                        targetPath = task.Substring(0, Math.Max(task.LastIndexOf("\\"), task.LastIndexOf("/"))) + Path.DirectorySeparatorChar;
                    break;
                case COMMAND_SET_NFP:
                    GlobalState.currentNFP = GlobalState.getOrCreateProperty(task.Trim());
                    break;

                case COMMAND_LOG:

                    string location = task.Trim();
                    targetPath = location;
                    GlobalState.logInfo.close();
                    GlobalState.logInfo = new InfoLogger(location);

                    GlobalState.logError.close();
                    GlobalState.logError = new ErrorLogger(location + "_error");

                    GlobalState.logInfo.logLine("Current machine learning settings: " + this.mlSettings.ToString());
                    break;
                case COMMAND_SET_MLSETTING:
                    this.mlSettings = ML_Settings.readSettings(task);
                    GlobalState.logInfo.logLine("Current machine learning settings: " + this.mlSettings.ToString());
                    break;
                case COMMAND_LOAD_MLSETTINGS_UNIFORM:
                    this.mlSettings = ML_Settings.readSettingsFromFile(task);
                    break;

                case COMMAND_PRINT_MLSETTINGS:
                    GlobalState.logInfo.logLine(this.mlSettings.ToString());
                    break;

                case COMMAND_PRINT_CONFIGURATIONS:
                    {

                        string[] para = task.Split(new char[] { ' ' });

                        if (para.Length >= 1 && (para[0].Trim()).Length > 0)
                        {
                            ConfigurationPrinter printer = null;

                            configBuilder.blacklisted = mlSettings.blacklisted;
                            List<Configuration> configs = configBuilder.buildConfigs(GlobalState.varModel);

                            // Clear the content of the file
                            File.WriteAllText(para[0], string.Empty);

                            if (GlobalState.optionOrder.Count == 0)
                            {
                                GlobalState.optionOrder.AddRange(GlobalState.varModel.BinaryOptions);
                                GlobalState.optionOrder.AddRange(GlobalState.varModel.NumericOptions);
                            }

                            if (para.Length >= 3)
                            {
                                printer = new ConfigurationPrinter(para[0], GlobalState.optionOrder, para[1], para[2]);
                            }
                            else if (para.Length == 2)
                            {
                                printer = new ConfigurationPrinter(para[0], GlobalState.optionOrder, para[1]);
                            }
                            else
                            {
                                printer = new ConfigurationPrinter(para[0], GlobalState.optionOrder);
                            }
                            printer.print(configs);
                        }
                        else
                        {
                            GlobalState.logInfo.logLine("Couldn't print configs");
                            GlobalState.logError.logLine("Error cant print configs without at least a outputfile");
                        }

                        break;
                    }

                case DEFINE_PYTHON_PATH:
                {
                    FileAttributes attributes = File.GetAttributes(taskAsParameter[0]);
                    if (attributes.HasFlag(FileAttributes.Directory))
                    {
                        // Append a slash if it is not included
                        if (!taskAsParameter[0].EndsWith("/") && !taskAsParameter[0].EndsWith("\\"))
                        {
                            PythonWrapper.PYTHON_PATH = taskAsParameter[0] + "/";
                        }
                        else
                        {
                            PythonWrapper.PYTHON_PATH = taskAsParameter[0];
                        }
                        // Here, a differentiation of the operating system is required
                        if (Environment.OSVersion.Platform == PlatformID.Win32Windows)
                        {
                            PythonWrapper.PYTHON_PATH += "python.exe";
                        }
                        else
                        {
                            PythonWrapper.PYTHON_PATH += "python";
                        }
                    }
                    else
                    {
                        PythonWrapper.PYTHON_PATH = taskAsParameter[0];
                    }
                    break;
                }


                case COMMAND_PYTHON_LEARN:
                    {
                        Tuple<List<Configuration>, List<Configuration>> learnAndValidation = configBuilder.buildSetsEfficient(mlSettings);
                        List<Configuration> configurationsLearning;
                        List<Configuration> configurationsValidation;
                        if (allMeasurementsSelected)
                        {
                            configurationsLearning = GlobalState.allMeasurements.Configurations;
                            configurationsValidation = configurationsLearning;
                        }
                        else if (!configurationsPreparedForLearning(learnAndValidation,
                            out configurationsLearning, out configurationsValidation))
                            break;

                        if (taskAsParameter.Length == 0)
                            GlobalState.logInfo.logLine("No learning strategy defined! Aborting learning");
                        else
                        {
                            // SVR, DecisionTreeRegression, RandomForestRegressor, BaggingSVR, KNeighborsRegressor, KERNELRIDGE, DecisionTreeRegressor
                            if (ProcessWrapper.LearningSettings.isLearningStrategy(taskAsParameter[0]))
                            {
                                handlePythonTask(false, configurationsLearning, taskAsParameter);
                            }
                            else
                            {
                                GlobalState.logInfo.logLine("Invalid Learning strategy: " + taskAsParameter[0] + "! Aborting learning");
                            }
                        }
                        break;
                    }


                case COMMAND_PYTHON_LEARN_OPT:
                    {
                        InfluenceModel infMod = new InfluenceModel(GlobalState.varModel, GlobalState.currentNFP);
                        Tuple<List<Configuration>, List<Configuration>> learnAndValidation = configBuilder.buildSetsEfficient(mlSettings);
                        List<Configuration> configurationsLearning;
                        List<Configuration> configurationsValidation;
                        if (allMeasurementsSelected)
                        {
                            configurationsLearning = GlobalState.allMeasurements.Configurations;
                            configurationsValidation = configurationsLearning;
                        }
                        else if (!configurationsPreparedForLearning(learnAndValidation,
                          out configurationsLearning, out configurationsValidation))
                            break;

                        // SVR, DecisionTreeRegression, RandomForestRegressor, BaggingSVR, KNeighborsRegressor, KERNELRIDGE, DecisionTreeRegressor
                        if (ProcessWrapper.LearningSettings.isLearningStrategy(taskAsParameter[0]))
                        {
                            handlePythonTask(true, configurationsLearning, taskAsParameter);
                        }
                        else
                        {
                            GlobalState.logInfo.logLine("Invalid learning strategy " + taskAsParameter[0] + "! Aborting Learning");
                        }
                        break;
                    }

                case COMMAND_START_LEARNING_SPL_CONQUEROR:
                    if (allMeasurementsSelected)
                    {
                        learnWithAllMeasurements();
                    }
                    else
                    {
                        learnWithSampling();
                    }
                    break;

                case COMMAND_START_ACTIVE_LEARNING_SPL_CONQUEROR:
                    string[] activeLearningParameters = task.Split(',');
                    for (int i = 0; i < activeLearningParameters.Length; i++)
                    {
                        activeLearningParameters[i] = activeLearningParameters[i].Trim();
                    }
                    activeLearnWithSampling(activeLearningParameters);
                    break;

                case COMMAND_OPTIMIZE_PARAMETER_SPLCONQUEROR:
                    {
                        InfluenceModel infMod = new InfluenceModel(GlobalState.varModel, GlobalState.currentNFP);
                        Tuple<List<Configuration>, List<Configuration>> learnAndValidation = configBuilder.buildSetsEfficient(mlSettings);
                        List<Configuration> configurationsLearning;
                        List<Configuration> configurationsValidation;
                        if (!configurationsPreparedForLearning(learnAndValidation,
                            out configurationsLearning, out configurationsValidation))
                            break;

                        List<ML_Settings> parameterSettings = new List<ML_Settings>();

                        string[] cleanedParameters = taskAsParameter.Where(x => !x.ToLowerInvariant().Contains("seed")
                            && !x.ToLowerInvariant().Contains("samples")
                            && !x.ToLowerInvariant().Contains("randomized")).ToArray();

                        parameterSettings = ML_SettingsGenerator.generateSettings(cleanedParameters);

                        if (containsArgInvariant(taskAsParameter, "randomized"))
                        {
                            int seed = 0;
                            int numSamples = 10;

                            if (containsArgInvariant(taskAsParameter, "seed"))
                            {
                                seed = Int32.Parse(getArgValue(taskAsParameter, "seed"));
                            }

                            if (containsArgInvariant(taskAsParameter, "samples"))
                            {
                                numSamples = Int32.Parse(getArgValue(taskAsParameter, "samples"));
                            }

                            parameterSettings = ML_SettingsGenerator.getRandomCombinations(parameterSettings, numSamples, seed);
                        }

                        ML_Settings optimalParameters = null;
                        double minimalError = Double.MaxValue;

                        foreach (ML_Settings parameters in parameterSettings)
                        {
                            // We have to reuse the list of models because of a NotifyCollectionChangedEventHandlers that might be attached to the list of models.
                            KFoldCrossValidation kFold = new KFoldCrossValidation(parameters, configurationsLearning);
                            double error = kFold.learn();

                            if (error < minimalError)
                            {
                                optimalParameters = parameters;
                                minimalError = error;
                            }

                        }
                        GlobalState.logInfo.logLine("Error of optimal parameters: " + minimalError);
                        GlobalState.logInfo.logLine("Optimal parameters "
                            + formatOptimalParameters(optimalParameters.ToString(), cleanedParameters));
                        Learning experiment = new MachineLearning.Learning.Regression
                            .Learning(configurationsLearning, configurationsValidation);
                        experiment.mlSettings = optimalParameters;
                        experiment.learn();
                        StringBuilder taskAsString = new StringBuilder();
                        taskAsParameter.ToList().ForEach(x => taskAsString.Append(x));

                        string samplingIdentifier = "PreVal_SPLCon_" + GlobalState.varModel.Name + "_" + createSmallerSamplingIdentifier() + ".csv";

                        printPredictedConfigurations(samplingIdentifier, experiment);

                        //printPredictedConfigurations("./CrossValidationResultPrediction"
                        //    + taskAsString.ToString()
                        //    .Replace(" ", "-").Replace(":", "=").Replace("[", "").Replace("]", "")
                        //    .Replace(Environment.NewLine, "").Substring(0)
                        //    + ".csv", experiment);

                        break;
                    }

                case COMMAND_CONVERT_MEASUREMENTS:
                    List<string> convertm_jobs = new List<string>();
                    foreach (string directory in Directory.GetDirectories(@taskAsParameter[0], taskAsParameter[1]))
                    {
                        convertm_jobs.AddRange(Directory.GetFiles(directory, taskAsParameter[2]));
                    }
                    convertm_jobs.ForEach(job =>
                    {
                        string rootTargetDir = taskAsParameter[3].EndsWith(Path.DirectorySeparatorChar.ToString()) ? taskAsParameter[3] : taskAsParameter[3] + Path.DirectorySeparatorChar;
                        string jobDir = rootTargetDir + Path.GetDirectoryName(job).Split(new char[] { Path.DirectorySeparatorChar }).Last() + Path.DirectorySeparatorChar;
                        if (!Directory.Exists(jobDir))
                            Directory.CreateDirectory(jobDir);
                        if (job.EndsWith(".xml"))
                            Util.ConvertUtil.convertToBinaryXml(job, jobDir + Path.GetFileNameWithoutExtension(job) + "_bin.xml" );
                        else if (job.EndsWith(".csv"))
                            Util.ConvertUtil.convertToBinaryCSV(job, jobDir + Path.GetFileNameWithoutExtension(job) + "_bin.csv", GlobalState.varModel);
                    });
                    break;
                case COMMAND_CONVERT_VM:
                    List<string> convertv_jobs = new List<string>();
                    foreach (string directory in Directory.GetDirectories(@taskAsParameter[0], taskAsParameter[1]))
                    {
                        convertv_jobs.AddRange(Directory.GetFiles(directory, taskAsParameter[2]));
                    }
                    convertv_jobs.ForEach(job =>
                    {
                        string rootTargetDir = taskAsParameter[3].EndsWith(Path.DirectorySeparatorChar.ToString()) ? taskAsParameter[3] : taskAsParameter[3] + Path.DirectorySeparatorChar;
                        string jobDir = rootTargetDir + Path.GetDirectoryName(job).Split(new char[] { Path.DirectorySeparatorChar }).Last() + Path.DirectorySeparatorChar;
                        if (!Directory.Exists(jobDir))
                            Directory.CreateDirectory(jobDir);
                        VariabilityModel vm = Util.ConvertUtil.transformVarModelAllbinary(SPLConqueror_Core.VariabilityModel.loadFromXML(job));
                        vm.saveXML(jobDir + Path.GetFileNameWithoutExtension(job) + "_bin.xml");
                    });
                    break;

                default:
                    bool didPerformCommand = configBuilder.performOneCommand(line);
                    if (didPerformCommand)
                    {
                        return "";
                    }
                    else
                    {
                        // Try to perform it as deprecated command.
                        performOneCommand_Depr(line);
                        return command;
                    }
            }
            return "";
        }

        private string formatOptimalParameters(string optimalParameters, string[] consideredParameters)
        {
            StringBuilder sb = new StringBuilder();
            string[] parameters = optimalParameters.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string param in consideredParameters)
            {
                foreach (string opt in parameters)
                {
                    if (opt.StartsWith(param.Split(new char[] { '=' })[0]))
                        sb.Append(opt + ";");
                }
            }
            return sb.ToString();
        }

        #region execution of deprecated commands
        [System.Obsolete("Warning: You are using deprecated commands. These commands might" +
            " be removed in the future. Please update your scripts or use the converter.", false)]
        public string performOneCommand_Depr(string line)
        {
            string command;
            line = line.Split(new Char[] { '#' }, 2)[0];

            if (line.Length == 0)
                return "";

            string[] components = line.Split(new Char[] { ' ' }, 2);
            string task = "";
            if (components.Length > 1)
                task = components[1];
            string[] taskAsParameter = task.Split(new Char[] { ' ' });
            command = components[0];
            switch (command.ToLower())
            {
                case COMMAND_LOAD_MLSETTINGS:
                    this.mlSettings = ML_Settings.readSettingsFromFile(task);
                    break;

                case COMMAND_PREDICT_CONFIGURATIONS:
                    {
                        FeatureSubsetSelection learnedModel = exp.models[exp.models.Count - 1];
                        String samplingIdentifier = createSamplingIdentifier();

                        PythonPredictionWriter csvWriter = new PythonPredictionWriter(targetPath, new String[] { "SPLConqueror" }, GlobalState.varModel.Name + "_" + samplingIdentifier);
                        List<Feature> features = learnedModel.LearningHistory[learnedModel.LearningHistory.Count - 1].FeatureSet;
                        csvWriter.writePredictions("Configuration;MeasuredValue;PredictedValue\n");
                        for (int i = 0; i < GlobalState.allMeasurements.Configurations.Count; i++)
                        {

                            Double predictedValue = FeatureSubsetSelection.estimate(features, GlobalState.allMeasurements.Configurations[i]);
                            csvWriter.writePredictions(GlobalState.allMeasurements.Configurations[i].ToString().Replace(";", "_") + ";" + Math.Round(GlobalState.allMeasurements.Configurations[i].GetNFPValue(), 4) + ";" + Math.Round(predictedValue, 4) + "\n");
                        }

                        break;
                    }

                case COMMAND_PREDICT_ALL_CONFIGURATIONS:
                    printPredictedConfigurations(task, this.exp);
                    break;

                case COMMAND_START_ALLMEASUREMENTS:
                    learnWithAllMeasurements();
                    break;

                case COMMAND_START_LEARNING:
                    if (allMeasurementsSelected)
                    {
                        learnWithAllMeasurements();
                    }
                    else
                    {
                        learnWithSampling();
                    }
                    break;

                case COMMAND_OPTIMIZE_PARAMETER:
                    {
                        InfluenceModel infMod = new InfluenceModel(GlobalState.varModel, GlobalState.currentNFP);
                        Tuple<List<Configuration>, List<Configuration>> learnAndValidation = configBuilder.buildSetsEfficient(mlSettings);
                        List<Configuration> configurationsLearning;
                        List<Configuration> configurationsValidation;
                        if (!configurationsPreparedForLearning(learnAndValidation,
                            out configurationsLearning, out configurationsValidation))
                            break;

                        List<ML_Settings> parameterSettings = new List<ML_Settings>();
                        parameterSettings = ML_SettingsGenerator.generateSettings(taskAsParameter);

                        ML_Settings optimalParameters = null;
                        double minimalError = Double.MaxValue;

                        foreach (ML_Settings parameters in parameterSettings)
                        {
                            // We have to reuse the list of models because of a NotifyCollectionChangedEventHandlers that might be attached to the list of models.
                            KFoldCrossValidation kFold = new KFoldCrossValidation(parameters, configurationsLearning);
                            double error = kFold.learn();

                            if (error < minimalError)
                            {
                                optimalParameters = parameters;
                                minimalError = error;
                            }

                        }
                        GlobalState.logInfo.logLine("Error of optimal parameters: " + minimalError);
                        GlobalState.logInfo.logLine("Parameters: " + optimalParameters.ToString());
                        Learning experiment = new MachineLearning.Learning.Regression
                            .Learning(configurationsLearning, configurationsValidation);
                        experiment.mlSettings = optimalParameters;
                        experiment.learn();
                        StringBuilder taskAsString = new StringBuilder();
                        taskAsParameter.ToList().ForEach(x => taskAsString.Append(x));
                        printPredictedConfigurations("./CrossValidationResultPrediction"
                            + taskAsString.ToString()
                            .Replace(" ", "-").Replace(":", "=").Replace("[", "").Replace("]", "")
                            .Replace(Environment.NewLine, "").Substring(0)
                            + ".csv", experiment);

                        break;
                    }

                default:
                    GlobalState.logInfo.logLine("Invalid deprecated command: " + command);
                    break;
            }
            return "";
        }
        #endregion

        private bool containsArgInvariant(string[] args, string toTest)
        {
            foreach (string arg in args)
            {
                if (arg.ToLowerInvariant().Contains(toTest.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        private string getArgValue(string[] args, string name)
        {
            string value = null;

            foreach (string arg in args)
            {
                if (arg.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                {
                    if (arg.Contains(":"))
                    {
                        return arg.Split(new char[] { ':' }, 2)[1];
                    }
                    else
                    {
                        GlobalState.logError.logLine("Argument " + name + " has no value.");
                    }
                }
            }
            return value;

        }

        private void cleanGlobal()
        {
            SPLConqueror_Core.GlobalState.clear();
            cleanSampling();
        }

        private void cleanSampling()
        {
            this.allMeasurementsSelected = false;
            exp.clearSampling();
            configBuilder.clear();
            cleanLearning();
        }

        private void cleanLearning()
        {
            exp.clear();
        }

        private string createSamplingIdentifier()
        {
            StringBuilder sb = new StringBuilder();
            // add binary sampling strategy to the identifier
            foreach (SamplingStrategies sampling in configBuilder.binaryStrategies)
            {
                sb.Append(sampling.ToString().ToLower() + "_");
            }

            // add numeric sampling strategy to the identifier
            foreach (ExperimentalDesign sampling in configBuilder.numericStrategies)
            {
                sb.Append("_" + sampling.getTag() + "--" + sampling.parameterIdentifier());
            }
            return sb.ToString();
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

        private void printNFPsToFile(List<Configuration> conf, string file)
        {
            StreamWriter sr = new StreamWriter(file);
            conf.ForEach(x => sr.WriteLine(x.GetNFPValue()));
            sr.Flush();
            sr.Close();
        }

        private string createSmallerSamplingIdentifier()
        {
            string samplingIdentifier = createSamplingIdentifier();
            if (samplingIdentifier.Length > 200)
            {
                samplingIdentifier = samplingIdentifier.Substring(0, 200);
            }
            return samplingIdentifier;
        }

        private void handlePythonTask(bool isParamTuning, List<Configuration> configurationsLearning, string[] taskAsParameter)
        {
            string samplingIdentifier = createSmallerSamplingIdentifier();

            //            if (samplingIdentifier.Length > 50)
            //{
            //samplingIdentifier = samplingIdentifier.Substring(0, 50);

            //        }

            //print configurations and nfps to temp folder
            string tempPath = Path.GetTempPath();
            string configsLearnFile = tempPath + "learn_" + samplingIdentifier + Process.GetCurrentProcess().Id + ".csv";
            string configsValFile = tempPath + "validation_" + samplingIdentifier + Process.GetCurrentProcess().Id + ".csv";
            string nfpLearnFile = tempPath + "nfp_learn_" + samplingIdentifier + Process.GetCurrentProcess().Id + ".nfp";
            string nfpValFile = tempPath + "nfp_validation_" + samplingIdentifier + Process.GetCurrentProcess().Id + ".nfp";

            try
            {
                if (GlobalState.optionOrder.Count == 0)
                {
                    GlobalState.optionOrder.AddRange(GlobalState.varModel.BinaryOptions);
                    GlobalState.optionOrder.AddRange(GlobalState.varModel.NumericOptions);
                }
                ConfigurationPrinter printer = new ConfigurationPrinter(configsLearnFile, GlobalState.optionOrder);
                printer.print(configurationsLearning, new List<NFProperty>());
                printer = new ConfigurationPrinter(configsValFile, GlobalState.optionOrder);
                printer.print(GlobalState.allMeasurements.Configurations, new List<NFProperty>());
                printNFPsToFile(configurationsLearning, nfpLearnFile);
                printNFPsToFile(GlobalState.allMeasurements.Configurations, nfpValFile);
                PythonWrapper pyInterpreter = new PythonWrapper(this.getLocationPythonScript() +
                    Path.DirectorySeparatorChar + PythonWrapper.COMMUNICATION_SCRIPT, taskAsParameter);
                GlobalState.logInfo.logLine("Starting Prediction");

                if (isParamTuning)
                {
                    pyInterpreter.setupApplication(configsLearnFile, nfpLearnFile, configsValFile, nfpValFile,
                        PythonWrapper.START_PARAM_TUNING, GlobalState.varModel);
                    string path = targetPath.Substring(0, (targetPath.Length
                        - (((targetPath.Split(Path.DirectorySeparatorChar)).Last()).Length)));
                    pyResult = pyInterpreter.getOptimizationResult(GlobalState.allMeasurements.Configurations, path);
                    GlobalState.logInfo.logLine("Optimal parameters " + pyResult.Replace(",", ""));
                    File.Delete(configsLearnFile);
                    File.Delete(configsValFile);
                    File.Delete(nfpLearnFile);
                    File.Delete(nfpValFile);
                    var optimalParameters = pyResult.Replace(",", "").Split(new char[] { ';' },
                        StringSplitOptions.RemoveEmptyEntries).ToList();
                    optimalParameters.Insert(0, taskAsParameter[0]);
                    handlePythonTask(false, configurationsLearning, optimalParameters.ToArray());
                }
                else
                {
                    string treePath = " ";
                    if (mlSettings.debug)
                    {
                        treePath = (targetPath.Split(Path.DirectorySeparatorChar)).Last();
                        treePath = targetPath.Substring(0, (targetPath.Length - ((treePath).Length)));
                        treePath += samplingIdentifier + "_tree_" + taskAsParameter[0] + ".tree";
                    }

                    pyInterpreter.setupApplication(configsLearnFile, nfpLearnFile, configsValFile, nfpValFile,
                        PythonWrapper.START_LEARN, GlobalState.varModel, treePath);
                    PythonPredictionWriter csvWriter = new PythonPredictionWriter(targetPath, taskAsParameter,
                        GlobalState.varModel.Name + "_" + samplingIdentifier);
                    List<Configuration> predictedByPython;
                    double error = pyInterpreter.getLearningResult(GlobalState.allMeasurements.Configurations, csvWriter, out predictedByPython);

                    if (File.Exists(treePath))
                    {
                        GlobalState.logInfo.logLine("Elapsed learning time(seconds): " + pyInterpreter.getTimeToLearning());
                        while (pyInterpreter.isRunning())
                            System.Threading.Thread.Sleep(100);
                        StreamReader reader = new StreamReader(treePath);
                        string content = reader.ReadToEnd();
                        reader.Close();
                        for (int i = 0; i < GlobalState.optionOrder.Count; ++i)
                        {
                            if (taskAsParameter[0].ToLower() == "svr")
                                content = content.Replace("C(" + i + ")", GlobalState.optionOrder[i].Name);
                            else
                                content = content.Replace("(" + i + "<", "(" + GlobalState.optionOrder[i].Name + "<")
                                    .Replace("(" + i + ">", "(" + GlobalState.optionOrder[i].Name + ">");
                        }
                        StreamWriter writer = new StreamWriter(treePath);
                        writer.Write(content);
                        writer.Close();
                    }

                    if (mlSettings.pythonInfluenceAnalysis)
                    {
                        List<Configuration> tmp = GlobalState.allMeasurements.Configurations;
                        GlobalState.allMeasurements.Configurations = predictedByPython;
                        learnWithAllMeasurements();
                        GlobalState.allMeasurements.Configurations = tmp;
                    }

                    GlobalState.logInfo.logLine("Prediction finished, results written in " + csvWriter.getPath());
                    if (!Double.IsNaN(error))
                    {
                        GlobalState.logInfo.logLine("Error rate: " + error);
                    }
                    csvWriter.close();
                }
            }
            finally
            {
                File.Delete(configsLearnFile);
                File.Delete(configsValFile);
                File.Delete(nfpLearnFile);
                File.Delete(nfpValFile);
            }
        }


        private void parseOptionOrder(string task)
        {
            String[] optionNames = task.Split(' ');
            foreach (String option in optionNames)
            {
                if (option.Trim().Length == 0)
                    continue;
                GlobalState.optionOrder.Add(GlobalState.varModel.getOption(option.Trim()));
            }

        }


        private string performOneCommand_MlSetting(string command)
        {
            // splits the task in design and parameters of the design
            string[] commandAndParameter = command.Split(new Char[] { ' ' }, 2);
            string task = commandAndParameter[0];
            string param = "";
            if (commandAndParameter.Length > 1)
                param = commandAndParameter[1];
            string[] parameters = param.Split(' ');

            // parsing of the parameters
            List<NumericOption> optionsToConsider = new List<NumericOption>();
            Dictionary<string, string> parameter = new Dictionary<string, string>();


            foreach (string par in parameters)
            {
                string[] nameAndValue = par.Split(':');
                if (nameAndValue.Length > 1)
                    parameter.Add(nameAndValue[0], nameAndValue[1]);
                else
                    parameter.Add(nameAndValue[0], "");
            }


            return "";

        }

        private void printPredictedConfigurations(String task, Learning exp)
        {
            if (exp.models.Count == 0)
            {
                GlobalState.logInfo.logLine("Can't predict configurations. No learning was performed");
            }
            else if (exp.models.ElementAt(exp.models.Count - 1).LearningHistory.Count == 0)
            {
                GlobalState.logInfo.logLine("Can't predict configurations. No model was learned.");
            }
            else if (task.Length == 0)
            {
                GlobalState.logInfo.logLine("Target file is required to print prediction results");
            }
            else if (GlobalState.allMeasurements.Configurations.Count == 0)
            {
                GlobalState.logError.logLine("No measurements loaded.");
            }
            else
            {
                predict(task, exp);
            }
        }

        //TODO:hybrid as bin/num
        //private void addHybridAsBin(HybridStrategy hybrid, bool isValidation, Dictionary<string, string> parameters)
        //{
        //    initHybridParamsNoMixed(parameters, DistributionAware.ONLY_BINARY, DistributionAware.ONLY_NUMERIC);
        //    addHybridDesign(hybrid, isValidation, parameters);
        //}

        //private void addHybridAsNumeric(HybridStrategy hybrid, bool isValidation, Dictionary<string,string> parameters)
        //{
        //    initHybridParamsNoMixed(parameters, DistributionAware.ONLY_NUMERIC, DistributionAware.ONLY_BINARY);
        //    addHybridDesign(hybrid, isValidation, parameters);
        //}

        //private void initHybridParamsNoMixed(Dictionary<string, string> parameters, string defaultSet,
        //    string notAllowed)
        //{
        //    string setVal;
        //    if (!parameters.TryGetValue(defaultSet, out setVal)
        //        || !setVal.ToLower().Equals("true"))
        //    {
        //        parameters[defaultSet] = "true";
        //    }

        //    if (parameters.TryGetValue(notAllowed, out setVal)
        //        && setVal.ToLower().Equals("true"))
        //    {
        //        parameters[notAllowed] = "false";
        //    }
        //}

        private void learnWithAllMeasurements()
        {
            InfluenceModel infMod = new InfluenceModel(GlobalState.varModel, GlobalState.currentNFP);

            List<Configuration> configurations_Learning = new List<Configuration>();

            if (allMeasurementsValid())
            {
                configurations_Learning = GlobalState.allMeasurements.Configurations;

            }
            else
            {
                foreach (Configuration config in GlobalState.allMeasurements.Configurations)
                {
                    if (config.nfpValues.ContainsKey(GlobalState.currentNFP))
                        configurations_Learning.Add(config);
                }
            }

            if (configurations_Learning.Count == 0)
            {
                GlobalState.logInfo.logLine("The learning set is empty! Cannot start learning!");
                return;
            }

            GlobalState.logInfo.logLine("Learning: NumberOfConfigurationsLearning:" + configurations_Learning.Count);
            exp.models.Clear();
            var mod = exp.models;
            exp = new Learning(configurations_Learning, configurations_Learning);
            exp.models = mod;
            exp.metaModel = infMod;
            exp.mlSettings = this.mlSettings;
            exp.learn();
            GlobalState.logInfo.logLine("Finished");
        }

        private void learnWithSampling()
        {
            InfluenceModel infMod = new InfluenceModel(GlobalState.varModel, GlobalState.currentNFP);
            Tuple<List<Configuration>, List<Configuration>> learnAndValidation = configBuilder.buildSetsEfficient(mlSettings);
            List<Configuration> configurationsLearning;
            List<Configuration> configurationsValidation;
            if (!configurationsPreparedForLearning(learnAndValidation,
                out configurationsLearning, out configurationsValidation))
                return;

            // We have to reuse the list of models because of a NotifyCollectionChangedEventHandlers that might be attached to the list of models.
            if (!hasLearnData)
            {
                exp.models.Clear();
                var mod = exp.models;
                exp = new MachineLearning.Learning.Regression.Learning(configurationsLearning, configurationsValidation);
                exp.models = mod;

                exp.metaModel = infMod;
                exp.mlSettings = this.mlSettings;
                exp.learn();
            }
            else
            {
                GlobalState.logInfo.logLine("Continue learning");
                exp.models.Clear();
                var mod = exp.models;
                exp = new MachineLearning.Learning.Regression.Learning(configurationsLearning, configurationsValidation);
                exp.models = mod;
                exp.metaModel = infMod;
                exp.mlSettings = this.mlSettings;
                List<LearningRound> lr = new List<LearningRound>();
                foreach (string lrAsString in CommandPersistence.learningHistory.Item2)
                {
                    lr.Add(LearningRound.FromString(lrAsString, GlobalState.varModel));
                }
                exp.continueLearning(lr);
            }
            GlobalState.logInfo.logLine("average model: \n" + exp.metaModel.printModelAsFunction());
            double relativeerror = 0;
            if (GlobalState.evaluationSet.Configurations.Count > 0)
            {
                relativeerror = FeatureSubsetSelection.computeError(exp.metaModel, GlobalState.evaluationSet.Configurations, ML_Settings.LossFunction.RELATIVE, exp.mlSettings);
            }
            else
            {
                relativeerror = FeatureSubsetSelection.computeError(exp.metaModel, GlobalState.allMeasurements.Configurations, ML_Settings.LossFunction.RELATIVE, exp.mlSettings);
            }

            //    globalstate.loginfo.logline("error :" + relativeerror);

        }

        private void activeLearnWithSampling(string[] parameters)
        {
            if (hasLearnData)
            {
                throw new NotImplementedException("continue learning is not implemented in active learning");
            }
            InfluenceModel infMod = new InfluenceModel(GlobalState.varModel, GlobalState.currentNFP);
            ActiveLearning activeLearning = new ActiveLearning(mlSettings, infMod, configBuilder);
            activeLearning.learn(parameters);
        }

        private void predict(string task, Learning exp, List<Feature> model = null)
        {
            NFProperty nfpProperty = new NFProperty("Prediction");

            for (int i = 0; i < GlobalState.allMeasurements.Configurations.Count; ++i)
            {
                Configuration currentConfiguration = GlobalState.allMeasurements.Configurations.ElementAt(i);
                double realValue = GlobalState.allMeasurements.Configurations.ElementAt(i).GetNFPValue();
                double prediction;

                if (model != null)
                {
                    prediction = FeatureSubsetSelection.predict(model, currentConfiguration);
                }
                else
                {
                    prediction = FeatureSubsetSelection
                    .predict(exp.models.ElementAt(exp.models.Count - 1).LearningHistory.Last().FeatureSet, currentConfiguration);
                }

                if (currentConfiguration.nfpValues.ContainsKey(nfpProperty))
                {
                    currentConfiguration.nfpValues[nfpProperty] = prediction;
                }
                else
                {
                    currentConfiguration.nfpValues.Add(nfpProperty, prediction);
                }
            }

            // Choose the NFPs to print
            List<NFProperty> nfpPropertiesToPrint = new List<NFProperty>();
            nfpPropertiesToPrint.Add(GlobalState.currentNFP);
            nfpPropertiesToPrint.Add(nfpProperty);

            // Use the ConfigurationPrinter to print the file.
            ConfigurationPrinter configurationPrinter = new ConfigurationPrinter(task, GlobalState.optionOrder, "", "");
            configurationPrinter.print(GlobalState.allMeasurements.Configurations, nfpPropertiesToPrint);
        }

        private bool configurationsPreparedForLearning(Tuple<List<Configuration>, List<Configuration>> learnAndValidation,
            out List<Configuration> configurationsLearning, out List<Configuration> configurationsValidation)
        {
            configurationsLearning = learnAndValidation.Item1;
            configurationsValidation = learnAndValidation.Item2;

            if (configurationsLearning.Count == 0)
            {
                configurationsLearning = configurationsValidation;
            }

            if (configurationsLearning.Count == 0)
            {
                GlobalState.logInfo.logLine("The learning set is empty! Cannot start learning!");
                return false;
            }

            if (configurationsValidation.Count == 0)
            {
                configurationsValidation = configurationsLearning;
            }
            GlobalState.logInfo.logLine("Learning: NumberOfConfigurationsLearning:" + configurationsLearning.Count + " NumberOfConfigurationsValidation:" + configurationsValidation.Count);
            return true;
        }


        public String getLocationPythonScript()
        {

            return AppDomain.CurrentDomain.BaseDirectory;
        }


        public Dictionary<String, String> parseParametersToLinearAndQuadraticBinarySampling(string[] param)
        {
            Dictionary<string, string> parameter = new Dictionary<string, string>();

            if (param.Length > 0)
            {
                foreach (string par in param)
                {
                    if (par.Contains(':'))
                    {
                        string[] nameAndValue = par.Split(':');
                        parameter.Add(nameAndValue[0], nameAndValue[1]);
                    }
                    else
                    {
                        parameter.Add(par, "");
                    }
                }
            }
            return parameter;
        }
    }
}
