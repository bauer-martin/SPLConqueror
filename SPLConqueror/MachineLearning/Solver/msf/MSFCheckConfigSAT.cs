﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPLConqueror_Core;
using MachineLearning.Learning.LinearProgramming;
using System.Reflection;
using Microsoft.SolverFoundation.Solvers;
using MicrosoftSolverFoundation;

namespace MachineLearning.Solver
{
    class MSFCheckConfigSAT : ICheckConfigSAT
    {
        private readonly VariabilityModel _vm;

        public MSFCheckConfigSAT(VariabilityModel vm) { _vm = vm; }

        /// <summary>
        /// Checks whether the boolean selection is valid w.r.t. the variability model. Does not check for numeric options' correctness.
        /// </summary>
        /// <param name="config">The list of binary options that are SELECTED (only selected options must occur in the list).</param>
        /// <param name="partialConfiguration">Whether the given list of options represents only a partial configuration. This means that options not in config might be additionally select to obtain a valid configuration.</param>
        /// <returns>True if it is a valid selection w.r.t. the VM, false otherwise</returns>
        public bool checkConfigurationSAT(List<BinaryOption> config, bool partialConfiguration)
        {
            List<CspTerm> variables = new List<CspTerm>();
            Dictionary<BinaryOption, CspTerm> elemToTerm = new Dictionary<BinaryOption, CspTerm>();
            Dictionary<CspTerm, BinaryOption> termToElem = new Dictionary<CspTerm, BinaryOption>();
            ConstraintSystem S = CSPsolver.getConstraintSystem(out variables, out elemToTerm, out termToElem, _vm);

            //Feature Selection
            foreach (BinaryOption binayOpt in elemToTerm.Keys)
            {
                CspTerm term = elemToTerm[binayOpt];
                if (config.Contains(binayOpt))
                {
                    S.AddConstraints(S.Implies(S.True, term));
                }
                else if (!partialConfiguration)
                {
                    S.AddConstraints(S.Implies(S.True, S.Not(term)));
                }
            }

            ConstraintSolverSolution sol = S.Solve();
            if (sol.HasFoundSolution)
            {
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Checks whether the boolean selection of a configuration is valid w.r.t. the variability model. Does not check for numeric options' correctness.
        /// </summary>
        /// <param name="c">The configuration that needs to be checked.</param>
        /// <returns>True if it is a valid selection w.r.t. the VM, false otherwise</returns>
        public bool checkConfigurationSAT(Configuration c, bool partialConfiguration = false)
        {
            List<CspTerm> variables = new List<CspTerm>();
            Dictionary<BinaryOption, CspTerm> elemToTerm = new Dictionary<BinaryOption, CspTerm>();
            Dictionary<CspTerm, BinaryOption> termToElem = new Dictionary<CspTerm, BinaryOption>();
            ConstraintSystem S = CSPsolver.getConstraintSystem(out variables, out elemToTerm, out termToElem, _vm);

            //Feature Selection
            foreach (BinaryOption binayOpt in elemToTerm.Keys)
            {
                CspTerm term = elemToTerm[binayOpt];
                if (c.getBinaryOptions(BinaryOption.BinaryValue.Selected).Contains(binayOpt))
                {
                    S.AddConstraints(S.Implies(S.True, term));
                }
                else
                {
                    S.AddConstraints(S.Implies(S.True, S.Not(term)));
                }
            }

            ConstraintSolverSolution sol = S.Solve();
            if (sol.HasFoundSolution)
            {
                int count = 0;
                foreach (CspTerm cT in variables)
                {
                    if (sol.GetIntegerValue(cT) == 1)
                        count++;
                }
                //-1??? Needs testing TODO
                if (count - 1 != c.getBinaryOptions(BinaryOption.BinaryValue.Selected).Count)
                {
                    return false;
                }
                return true;
            }
            else
                return false;
        }
    }
}
