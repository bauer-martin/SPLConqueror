﻿using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using MachineLearning.Solver;

namespace MachineLearning.Sampling.Heuristics
{
    public class DistanceBased
    {
        public const string NUMBER_SAMPLES = "numConfigs";

        public const string OPTION_WEIGHT = "optionWeight";

        private VariabilityModel vm;

        public DistanceBased(VariabilityModel vm)
        {
            this.vm = vm;
        }

        public List<List<BinaryOption>> getSample(Dictionary<string, string> parameter)
        {
            int numberConfigs;

            int optionWeight = 1;

            string numConfigsValue = null;

            if (parameter.ContainsKey(NUMBER_SAMPLES))
            {
                numConfigsValue = parameter[NUMBER_SAMPLES];
            }
            else
            {
                numConfigsValue = "asOW";
            }

            if (numConfigsValue.Contains("asOW"))
            {
                FeatureWise fw = new FeatureWise();
                numberConfigs = fw.generateFeatureWiseConfigsCSP(vm).Count;
            }
            else if (numConfigsValue.Contains("asTW"))
            {
                numConfigsValue.Replace("asTW", "");
                numberConfigs = Int32.Parse(numConfigsValue);
                TWise tw = new TWise();
                numberConfigs = tw.generateT_WiseVariants_new(vm, numberConfigs).Count;
            }
            else
            {
                numberConfigs = Int32.Parse(numConfigsValue);
            }

            if (parameter.ContainsKey(OPTION_WEIGHT))
            {
                optionWeight = Int32.Parse(parameter[OPTION_WEIGHT]);
            }

            IVariantGenerator vg = SolverManager.VariantGenerator;
            List<BinaryOption> minimalConfiguration = vg.FindConfig(new List<BinaryOption>(), vm, true, null);
            return vg.DistanceMaximization(vm, minimalConfiguration, numberConfigs, optionWeight);
        }
    }
}
