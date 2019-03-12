using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public interface IBucketSession
    {
        /// <summary>
        /// This method returns a configuration with the given number of selected features.
        /// Configurations that have been returned previously will not be returned again.
        /// </summary>
        /// <param name="numberSelectedFeatures">The number of features that should be selected.</param>
        /// <param name="featureWeight">The weight of certain feature combinations.</param>
        /// <returns>A list of <see cref="BinaryOption"/>, which should be selected.</returns>
        List<BinaryOption> GenerateConfiguration(int numberSelectedFeatures,
            Dictionary<List<BinaryOption>, int> featureWeight);
    }
}
