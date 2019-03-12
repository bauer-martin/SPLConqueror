using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Z3;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    public class Z3BucketSession : IBucketSession
    {
        private const string RANDOM_SEED = ":random-seed";
        private readonly uint _z3RandomSeed;
        private readonly bool _henard;
        private Dictionary<int, Z3Cache> _z3Cache;

        public Z3BucketSession(uint z3RandomSeed, bool henard)
        {
            _z3RandomSeed = z3RandomSeed;
            _henard = henard;
        }

        public List<BinaryOption> GenerateConfigurationFromBucket(VariabilityModel vm, int numberSelectedFeatures, Dictionary<List<BinaryOption>, int> featureWeight, Configuration lastSampledConfiguration)
        {
            if (_z3Cache == null)
            {
                _z3Cache = new Dictionary<int, Z3Cache>();
            }

            List<KeyValuePair<List<BinaryOption>, int>> featureRanking;
            if (featureWeight != null)
            {
                featureRanking = featureWeight.ToList();
                featureRanking.Sort((first, second) => first.Value.CompareTo(second.Value));
            }
            else
            {
                featureRanking = new List<KeyValuePair<List<BinaryOption>, int>>();
            }

            List<BoolExpr> variables = null;
            Dictionary<BoolExpr, BinaryOption> termToOption = null;
            Dictionary<BinaryOption, BoolExpr> optionToTerm = null;
            Tuple<Context, BoolExpr> z3Tuple;
            Context z3Context;
            Microsoft.Z3.Solver solver;

            // Reuse the solver if it is already in the cache
            if (this._z3Cache.Keys.Contains(numberSelectedFeatures))
            {
                Z3Cache cache = this._z3Cache[numberSelectedFeatures];
                z3Context = cache.GetContext();
                solver = cache.GetSolver();
                variables = cache.GetVariables();
                termToOption = cache.GetTermToOptionMapping();
                optionToTerm = cache.GetOptionToTermMapping();

                if (lastSampledConfiguration != null)
                {
                    // Add the previous configurations as constraints
                    solver.Assert(Z3Solver.NegateExpr(z3Context, Z3Solver.ConvertConfiguration(z3Context, lastSampledConfiguration.getBinaryOptions(BinaryOption.BinaryValue.Selected), optionToTerm, vm)));

                    // Create a new backtracking point for the next run
                    solver.Push();
                }

            }
            else
            {
                z3Tuple = Z3Solver.GetInitializedBooleanSolverSystem(out variables, out optionToTerm, out termToOption, vm, _henard);
                z3Context = z3Tuple.Item1;
                BoolExpr z3Constraints = z3Tuple.Item2;
                solver = z3Context.MkSolver();

                solver.Set (RANDOM_SEED, _z3RandomSeed);

                solver.Assert(z3Constraints);

                if (lastSampledConfiguration != null)
                {
                    // Add the previous configurations as constraints
                    solver.Assert(Z3Solver.NegateExpr(z3Context, Z3Solver.ConvertConfiguration(z3Context, lastSampledConfiguration.getBinaryOptions(BinaryOption.BinaryValue.Selected), optionToTerm, vm)));
                }

                // The goal of this method is, to have an exact number of features selected

                // Therefore, initialize an integer array with the value '1' for the pseudo-boolean equal function
                int[] neutralWeights = new int[variables.Count];
                for (int i = 0; i < variables.Count; i++)
                {
                    neutralWeights[i] = 1;
                }
                solver.Assert(z3Context.MkPBEq(neutralWeights, variables.ToArray(), numberSelectedFeatures));

                // Create a backtracking point before adding the optimization goal
                solver.Push();

                this._z3Cache[numberSelectedFeatures] = new Z3Cache(z3Context, solver, variables, optionToTerm, termToOption);
            }

            // Check if there is still a solution available by finding the first satisfiable configuration
            if (solver.Check() == Status.SATISFIABLE)
            {
                Model model = solver.Model;
                List<BinaryOption> possibleSolution = Z3VariantGenerator.RetrieveConfiguration(variables, model, termToOption);

                // Disable finding a configuration where the least frequent feature/feature combinations are selected
                // if no featureWeight is given.
                List<BinaryOption> approximateOptimal = null;
                if (featureRanking.Count != 0)
                {
                    approximateOptimal = WeightMinimizer
                    .getSmallWeightConfig(featureRanking, this._z3Cache[numberSelectedFeatures], vm);
                }

                if (approximateOptimal == null)
                {
                    return possibleSolution;
                }
                else
                {
                    return approximateOptimal;
                }

            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Clears the cache needed for an optimization.
        /// </summary>
        public void ClearBucketCache()
        {
            this._z3Cache = null;
        }
    }
}
