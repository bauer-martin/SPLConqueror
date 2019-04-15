using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public interface ICheckConfigSAT
    {
        /// <summary>
        /// Checks whether the boolean selection is valid w.r.t. the variability model. Does not check for numeric options' correctness.
        /// </summary>
        /// <param name="config">The list of binary options that are SELECTED (only selected options must occur in the list).</param>
        /// <param name="partialConfiguration">Whether the given list of options represents only a partial configuration. This means that options not in config might be additionally select to obtain a valid configuration.</param>
        /// <returns>True if it is a valid selection w.r.t. the VM, false otherwise</returns>
        bool checkConfigurationSAT(List<BinaryOption> config, bool partialConfiguration);

        /// <summary>
        /// Checks whether the boolean selection of a configuration is valid w.r.t. the variability model. Does not check for numeric options' correctness.
        /// </summary>
        /// <param name="c">The configuration that needs to be checked.</param>
        /// <returns>True if it is a valid selection w.r.t. the VM, false otherwise</returns>
        bool checkConfigurationSAT(Configuration c, bool partialConfiguration = false);

        //Not important
        //List<ConfigurationOption> determineSetOfInvalidFeatures(int nbOfFeatures, VariabilityModel vm, bool withDerivatives, List<ConfigurationOption> forbiddenFeatures, RuntimeProperty rp, NFPConstraint constraint);
    }
}
