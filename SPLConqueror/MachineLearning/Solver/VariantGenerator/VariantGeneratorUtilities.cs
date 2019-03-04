using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class VariantGeneratorUtilities
    {
        /// <summary>
        /// Returns <code>true</code> if the mixed constraints are satisfied by the given configuration and
        /// <code>false</code> if not.
        /// </summary>
        /// <param name="c">the configuration to check</param>
        /// <param name="vm">the variability model</param>
        /// <returns><code>true</code> if the mixed constraints are satisfied by the given configuration and
        /// <code>false</code> if not.</returns>
        public static bool FulfillsMixedConstraints(Configuration c, VariabilityModel vm)
        {
            List<MixedConstraint> mixedConstraints = vm.MixedConstraints;
            foreach (MixedConstraint constraint in mixedConstraints)
            {
                if (!constraint.requirementsFulfilled(c))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns <code>true</code> if the configuration is already included;<code>false</code> otherwise.
        /// </summary>
        /// <param name="c">the configuration to search for</param>
        /// <param name="configurations">a list containing all configurations</param>
        /// <returns><code>true</code> if the configuration is already included;<code>false</code> otherwise</returns>
        public static bool IsInConfigurationFile(Configuration c, List<Configuration> configurations)
        {
            foreach (Configuration conf in configurations)
            {
                if (conf.ToString().Equals(c.ToString()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
