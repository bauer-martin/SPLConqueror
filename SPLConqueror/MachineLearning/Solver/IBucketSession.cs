using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public interface IBucketSession
    {
        /// <summary>
        /// This method returns a configuration with the given number of selected features.
        /// </summary>
        /// <param name="vm">The variability model containing all options and their constraints.</param>
        /// <param name="numberSelectedFeatures">The number of features that should be selected.</param>
        /// <param name="featureWeight">The weight of certain feature combinations.</param>
        /// <returns>A list of <see cref="BinaryOption"/>, which should be selected.</returns>
        List<BinaryOption> GenerateConfiguration(VariabilityModel vm, int numberSelectedFeatures,
            Dictionary<List<BinaryOption>, int> featureWeight);

        /// <summary>
        /// This method clears the cache if caches are used.
        /// </summary>
        void Reset();
    }
}
