using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;
using Microsoft.SolverFoundation.Solvers;
using MicrosoftSolverFoundation;

namespace MachineLearning.Solver
{
    public class MSFBucketSession: IBucketSession
    {
        private Dictionary<int, ConstraintSystemCache> _constraintSystemCache;
        private Dictionary<int, List<BinaryOption>> _lastSampledConfigs = new Dictionary<int, List<BinaryOption>>();

        /// <summary>
        /// This method has the objective to sample a configuration where n features are selected
        /// </summary>
        /// <returns>The first fitting configuration.</returns>
        /// <param name="vm">The variability model.</param>
        /// <param name="numberSelectedFeatures">The number of features that should be selected.</param>
        /// <param name="featureWeight">The weight of the features to minimize.</param>
        public List<BinaryOption> GenerateConfigurationFromBucket(VariabilityModel vm, int numberSelectedFeatures,
            Dictionary<List<BinaryOption>, int> featureWeight)
        {
            List<BinaryOption> lastSampledConfiguration;
            _lastSampledConfigs.TryGetValue(numberSelectedFeatures, out lastSampledConfiguration);
            if (this._constraintSystemCache == null)
            {
                this._constraintSystemCache = new Dictionary<int, ConstraintSystemCache>();
            }

            List<CspTerm> variables;
            Dictionary<BinaryOption, CspTerm> elemToTerm;
            Dictionary<CspTerm, BinaryOption> termToElem;
            ConstraintSystem S;

            if (this._constraintSystemCache.Keys.Contains(numberSelectedFeatures))
            {
                variables = _constraintSystemCache[numberSelectedFeatures].GetVariables();
                elemToTerm = _constraintSystemCache[numberSelectedFeatures].GetElemToTermMapping();
                termToElem = _constraintSystemCache[numberSelectedFeatures].GetTermToElemMapping();
                S = _constraintSystemCache[numberSelectedFeatures].GetConstraintSystem();

                S.ResetSolver();
                S.RemoveAllMinimizationGoals();

                // Add the missing configurations
                AddBinaryConfigurationsToConstraintSystem(vm, S, lastSampledConfiguration, elemToTerm);

            }
            else
            {
                variables = new List<CspTerm>();
                elemToTerm = new Dictionary<BinaryOption, CspTerm>();
                termToElem = new Dictionary<CspTerm, BinaryOption>();

                // Build the constraint system
                S = CSPsolver.getConstraintSystem(out variables, out elemToTerm, out termToElem, vm);

                // The first goal of this method is, to have an exact number of features selected
                S.AddConstraints(S.ExactlyMofN(numberSelectedFeatures, variables.ToArray()));

                if (lastSampledConfiguration != null)
                {
                    // Add the previous configurations as constraints
                    AddBinaryConfigurationsToConstraintSystem(vm, S, lastSampledConfiguration, elemToTerm);
                }

                this._constraintSystemCache.Add(numberSelectedFeatures, new ConstraintSystemCache(S, variables, elemToTerm, termToElem));

            }

            // Next, solve the constraint system
            ConstraintSolverSolution soln = S.Solve();

            List<BinaryOption> tempConfig = new List<BinaryOption>();

            if (soln.HasFoundSolution)
            {
                tempConfig.Clear();
                foreach (CspTerm cT in variables)
                {
                    if (soln.GetIntegerValue(cT) == 1)
                        tempConfig.Add(termToElem[cT]);
                }
                _lastSampledConfigs[numberSelectedFeatures] = tempConfig;

            }
            else
            {
                _lastSampledConfigs[numberSelectedFeatures] = null;
                return null;
            }

            return tempConfig;
        }

        /// <summary>
        /// Clears the cache-object needed for an optimization.
        /// </summary>
        public void ClearBucketCache()
        {
            this._constraintSystemCache = null;
        }

        private void AddBinaryConfigurationsToConstraintSystem(VariabilityModel vm, ConstraintSystem s, List<BinaryOption> configurationToExclude, Dictionary<BinaryOption, CspTerm> elemToTerm)
        {
            List<BinaryOption> allBinaryOptions = vm.BinaryOptions;

            List<CspTerm> positiveTerms = new List<CspTerm>();
            List<CspTerm> negativeTerms = new List<CspTerm>();
            foreach (BinaryOption binOpt in allBinaryOptions)
            {
                if (configurationToExclude.Contains(binOpt))
                {
                    positiveTerms.Add(elemToTerm[binOpt]);
                }
                else
                {
                    negativeTerms.Add(elemToTerm[binOpt]);
                }
            }

            if (negativeTerms.Count > 0)
            {
                positiveTerms.Add(s.Not(s.And(negativeTerms.ToArray())));
            }

            s.AddConstraints(s.Not(s.And(positiveTerms.ToArray())));
        }
    }
}
