using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPLConqueror_Core
{
    /// <summary>
    /// This object contains a mixed constraint.
    /// </summary>
    public class MixedConstraint : NonBooleanConstraint
    {
        private const string REQUIRE_ALL = "all";

        private const string REQUIRE_NONE = "none";

        private const string NEGATIVE = "neg";

        private const string POSITIVE = "pos";

        private string requirement;

        private string negativeOrPositiveExpr;

        /// <summary>
        /// Creates a new mixed constraint between boolean and numeric options and literals.
        /// </summary>
        /// <param name="unparsedExpr">The expression of the constraint as string.</param>
        /// <param name="varMod">The variability model the constraint applies to.</param>
        /// <param name="requirement">String indicating if the constraints evaluates to
        ///                           to false if not all options are present.</param>
        /// <param name="exprKind">Value indicating if the the expression will be negated.</param>
        public MixedConstraint(String unparsedExpr, VariabilityModel varMod, string requirement, string exprKind = "pos")
            : base(unparsedExpr, varMod)
        {
            if (requirement.Trim().ToLower().Equals(REQUIRE_ALL))
            {
                this.requirement = REQUIRE_ALL;
            }
            else if (requirement.Trim().ToLower().Equals(REQUIRE_NONE))
            {
                this.requirement = REQUIRE_NONE;
            }
            else
            {
                throw new ArgumentException(String.Format("The tag {0} for mixed requirements is not valid.", requirement));
            }

            if (exprKind.Trim().ToLower().Equals(NEGATIVE))
            {
                this.negativeOrPositiveExpr = NEGATIVE;
            }
            else if (exprKind.Trim().ToLower().Equals(POSITIVE))
            {
                this.negativeOrPositiveExpr = POSITIVE;
            }
            else
            {
                throw new ArgumentException(String.Format("The expression kind {0} is not valid. Expression can either be neg or pos.", exprKind));
            }
        }

        /// <summary>
        /// Tests whether a configuration holds for the given non-boolean constraint.
        /// </summary>
        /// <param name="config">The configuration of interest.</param>
        /// <returns>True is the configuration holds for the constraint.</returns>
        public override bool configIsValid(Configuration config)
        {
            if (negativeOrPositiveExpr.Equals(POSITIVE))
            {
                return evaluatePos(config);
            }
            else if (negativeOrPositiveExpr.Equals(NEGATIVE))
            {
                return evaluateNeg(config);
            }
            else
            {
                throw new ArgumentException("Illegal expression kind");
            }
        }

        private bool evaluateNeg(Configuration conf)
        {
            if (requirement.Equals(REQUIRE_ALL))
            {
                if (!configHasOptionsOfConstraint(conf))
                {
                    return true;
                }
                else
                {
                    return !base.configIsValid(conf);
                }
            }
            else if (requirement.Equals(REQUIRE_NONE))
            {
                foreach (BinaryOption binOpt in GlobalState.varModel.BinaryOptions)
                {
                    if (!conf.BinaryOptions.ContainsKey(binOpt))
                    {
                        conf.BinaryOptions.Add(binOpt, BinaryOption.BinaryValue.Deselected);
                    }
                }

                return !base.configIsValid(conf);
            }
            else
            {
                throw new ArgumentException("Illegal Requirement for mixed constraints");
            }
        }

        private bool evaluatePos(Configuration conf)
        {
            if (requirement.Equals(REQUIRE_ALL))
            {
                return base.configIsValid(conf);
            }
            else if (requirement.Equals(REQUIRE_NONE))
            {
                foreach (BinaryOption binOpt in GlobalState.varModel.BinaryOptions)
                {
                    if (!conf.BinaryOptions.ContainsKey(binOpt))
                    {
                        conf.BinaryOptions.Add(binOpt, BinaryOption.BinaryValue.Deselected);
                    }
                }

                return base.configIsValid(conf);
            }
            else
            {
                throw new ArgumentException("Illegal Requirement for mixed constraints");
            }
        }

        /// <summary>
        /// Returns the string-representation of the <code>MixedConstraint</code>-object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            if (negativeOrPositiveExpr == NEGATIVE)
                return "!:" + requirement + ": " + base.ToString();
            else
                return requirement + ": " + base.ToString();
        }
    }
}
