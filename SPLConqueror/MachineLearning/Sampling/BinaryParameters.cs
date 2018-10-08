using System;
using System.Collections.Generic;

namespace MachineLearning.Sampling
{
    public class BinaryParameters
    {
        public List<Dictionary<string, string>> tWiseParameters { get; set; }
        public List<Dictionary<string, string>> randomBinaryParameters { get; set; }
        public List<Dictionary<string, string>> satParameters { get; set; }
        public List<Dictionary<string, string>> distanceMaxParameters { get; set; }

        public BinaryParameters()
        {
            tWiseParameters = new List<Dictionary<string, string>>();
            randomBinaryParameters = new List<Dictionary<string, string>>();
            satParameters = new List<Dictionary<string, string>>();
            distanceMaxParameters = new List<Dictionary<string, string>>();
        }

        public void updateSeeds()
        {
            updateSeeds(tWiseParameters);
            updateSeeds(randomBinaryParameters);
            updateSeeds(satParameters);
            updateSeeds(distanceMaxParameters);
        }

        private static void updateSeeds(List<Dictionary<string, string>> binaryParameters)
        {
            foreach (Dictionary<string, string> parameters in binaryParameters)
            {
                if (parameters.ContainsKey("seed"))
                {
                    uint seed = UInt32.Parse(parameters["seed"]);
                    parameters["seed"] = (seed + 1).ToString();
                }
            }
        }
    }
}
