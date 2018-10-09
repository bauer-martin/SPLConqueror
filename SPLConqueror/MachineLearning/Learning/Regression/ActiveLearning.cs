using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using MachineLearning.Sampling;
using SPLConqueror_Core;

namespace MachineLearning.Learning.Regression
{
    public class ActiveLearning
    {
        private ML_Settings mlSettings = null;
        private InfluenceModel influenceModel;
        private ConfigurationBuilder configBuilder;

        public ActiveLearning(ML_Settings mlSettings, InfluenceModel influenceModel, ConfigurationBuilder configBuilder)
        {
            this.mlSettings = mlSettings;
            this.influenceModel = influenceModel;
            this.configBuilder = configBuilder;
        }

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
            Learning exp = new Learning(configurationsLearning, configurationsValidation)
            {
                metaModel = this.influenceModel,
                mlSettings = this.mlSettings
            };
            exp.learn();

            // set up abort criteria
            if (exp.models.Count != 1)
            {
                GlobalState.logError.logLine("There should be exactly one learned model! Aborting active learning!");
                return;
            }
            int round = 1;
            double relativeError = exp.models[0].finalError;
            if (abortActiveLearning(round, relativeError)) return;

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
                List<Configuration> configsForNextRun = configBuilder.buildSet(mlSettings);
                configurationsLearning.AddRange(configsForNextRun);
                exp = new Learning(configurationsLearning, configurationsValidation)
                {
                    metaModel = influenceModel,
                    mlSettings = this.mlSettings
                };
                exp.learn();
                relativeError = exp.models[0].finalError;
            } while (!abortActiveLearning(round, relativeError));
        }

        private bool abortActiveLearning(int round, double relativeError)
        {
            if (round >= mlSettings.maxNumberOfActiveLearningRounds)
            {
                GlobalState.logInfo.logLine("Aborting active learning because maximum number of rounds reached");
                return true;
            }

            if (relativeError < mlSettings.minImprovementPerActiveLearningRound)
            {
                GlobalState.logInfo.logLine("Aborting active learning because model did not achieve great improvement anymore");
                return true;
            }

            if (relativeError < mlSettings.abortError)
            {
                GlobalState.logInfo.logLine("Aborting active learning because model is already good enough");
                return true;
            }

            return false;
        }
    }
    
}