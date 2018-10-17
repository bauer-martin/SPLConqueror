using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using MachineLearning.Sampling;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression
{
    public class ActiveLearning
    {
        private ML_Settings mlSettings = null;
        private InfluenceModel influenceModel;
        private ConfigurationBuilder configBuilder;

        /// <summary>
        /// The number of active learning rounds.
        /// </summary>
        private int round = -1;
        /// <summary>
        /// The relative error of the last active learning round.
        /// </summary>
        private double lastRelativeError = -1;

        public ActiveLearning(ML_Settings mlSettings, InfluenceModel influenceModel, ConfigurationBuilder configBuilder)
        {
            this.mlSettings = mlSettings;
            this.influenceModel = influenceModel;
            this.configBuilder = configBuilder;
        }

        /// <summary>
        /// Learns a model using multiple rounds of <see cref="Learning"/>.
        /// </summary>
        /// <param name="samplingTask">The parameter string for the 'active-learn-splconqueror' command</param>
        public void learn(string samplingTask)
        {
            Tuple<List<Configuration>, List<Configuration>> learnAndValidation = configBuilder.buildSetsEfficient(mlSettings);
            List<Configuration> configurationsLearning = learnAndValidation.Item1;
            List<Configuration> configurationsValidation = learnAndValidation.Item2;
            if (configurationsLearning.Count == 0 && configurationsValidation.Count == 0)
            {
                GlobalState.logInfo.logLine("The learning set is empty! Cannot start learning!");
                return;
            }
            if (configurationsLearning.Count == 0)
            {
                configurationsLearning = configurationsValidation;
            }
            else if (configurationsValidation.Count == 0)
            {
                configurationsValidation = configurationsLearning;
            }

            GlobalState.logInfo.logLine("Learning: NumberOfConfigurationsLearning:" + configurationsLearning.Count 
                + " NumberOfConfigurationsValidation:" + configurationsValidation.Count);

            // learn initial model
            round = 1;
            Learning exp = new Learning(configurationsLearning, configurationsValidation)
            {
                metaModel = this.influenceModel,
                mlSettings = this.mlSettings
            };
            exp.learn();

            if (exp.models.Count != 1)
            {
                GlobalState.logError.logLine("There should be exactly one learned model! Aborting active learning!");
                return;
            }
            lastRelativeError = exp.models[0].finalError;
            if (abortActiveLearning()) return;
            if (exp.models[0].LearningHistory.Count < 1)
            {
                GlobalState.logError.logLine("There should be at least one learning round! Aborting active learning!");
                return;
            }
            List<Feature> featureSet = exp.models[0].LearningHistory.Last().FeatureSet;

            // continue learning
            do
            {
                if (round == 1)
                {
                    // switch sampling strategy to select new configurations
                    configBuilder.clear();
                    configBuilder.performOneCommand(samplingTask);
                }
                else
                {
                    // keep sampling strategy but use different seed
                    configBuilder.binaryParams.updateSeeds();
                }
                round++;
                configBuilder.existingConfigurations = configurationsLearning;
                List<Configuration> configsForNextRun = configBuilder.buildSet(mlSettings);
                configurationsLearning.AddRange(configsForNextRun);
                exp = new Learning(configurationsLearning, configurationsValidation)
                {
                    metaModel = influenceModel,
                    mlSettings = this.mlSettings
                };
                exp.learn(featureSet);
                if (exp.models.Count != 1)
                {
                    GlobalState.logError.logLine("There should be exactly one learned model! Aborting active learning!");
                    return;
                }
                lastRelativeError = exp.models[0].finalError;
                featureSet = exp.models[0].LearningHistory.Last().FeatureSet;
            } while (!abortActiveLearning());
        }

        private bool abortActiveLearning()
        {
            if (round >= mlSettings.maxNumberOfActiveLearningRounds)
            {
                GlobalState.logInfo.logLine("Aborting active learning because maximum number of rounds has been reached");
                return true;
            }

            if (lastRelativeError < mlSettings.minImprovementPerActiveLearningRound)
            {
                GlobalState.logInfo.logLine("Aborting active learning because model did not achieve great improvement anymore");
                return true;
            }

            if (lastRelativeError < mlSettings.abortError)
            {
                GlobalState.logInfo.logLine("Aborting active learning because model is already good enough");
                return true;
            }

            return false;
        }
    }
    
}