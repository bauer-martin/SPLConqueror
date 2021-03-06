﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPLConqueror_Core
{


    /// <summary>
    /// This class represents a non boolean constraint constraining the configuration space of the variability model. 
    /// </summary>
    public class NonBooleanConstraint
    {
        private InfluenceFunction leftHandSide = null;
        private InfluenceFunction rightHandSide = null;
        private String comparator = "";

        /// <summary>
        /// Creates a new NonBooleanConstraint for a expression. The expression have to consist binary and numeric options and operators such as +, *, &lt;=, &lt;, &gt;=, &gt;, and = only. 
        /// Where all binary and numeric options have to be defined in the variability model. 
        /// </summary>
        /// <param name="unparsedExpression"></param>
        /// <param name="varModel"></param>
        public NonBooleanConstraint(String unparsedExpression, VariabilityModel varModel)
        {
            if (unparsedExpression.Contains(">="))
            {
                comparator = ">=";
            }
            else if (unparsedExpression.Contains("<="))
            {
                comparator = "<=";
            }
            else if (unparsedExpression.Contains("="))
            {
                comparator = "=";
            }
            else if (unparsedExpression.Contains(">"))
            {
                comparator = ">";
            }
            else if (unparsedExpression.Contains("<"))
            {
                comparator = "<";
            }

            String[] parts = unparsedExpression.Split(comparator.ToCharArray());
            leftHandSide = new InfluenceFunction(parts[0], varModel);
            rightHandSide = new InfluenceFunction(parts[parts.Length - 1], varModel);


        }

        /// <summary>
        /// Tests whether a configuration holds for the given non-boolean constraint.
        /// </summary>
        /// <param name="config">The configuration of interest.</param>
        /// <returns>True is the configuration holds for the constraint.</returns>
        public virtual bool configIsValid(Configuration config)
        {
            if (!configHasOptionsOfConstraint(config))
                return true;


            double left = leftHandSide.eval(config);
            double right = rightHandSide.eval(config);

            switch (comparator)
            {
                case ">=":
                    {
                        if (left >= right)
                            return true;
                        break;
                    }
                case "<=":
                    {
                        if (left <= right)
                            return true;
                        break;
                    }
                case "=":
                    {
                        if (left == right)
                            return true;
                        break;
                    }
                case ">":
                    {
                        if (left > right)
                            return true;
                        break;
                    }
                case "<":
                    {
                        if (left < right)
                            return true;
                        break;
                    }
            }

            return false;
        }

        /// <summary>
        /// Tests whether the given partial configuration (consisting only of the numeric-configuration options and their selected value) holds
        /// for the given non-functional constraint.
        /// </summary>
        /// <param name="config">A partial configuration consisting of the numeric-configurations options and their selected values.</param>
        /// <returns>True if the partial configuration holds for the non-functional property.</returns>
        public bool configIsValid(Dictionary<NumericOption, double> config)
        {
            if (!configHasOptionsOfConstraint(config))
                return true;


            double left = leftHandSide.eval(config);
            double right = rightHandSide.eval(config);

            switch (comparator)
            {
                case ">=":
                    {
                        if (left >= right)
                            return true;
                        break;
                    }
                case "<=":
                    {
                        if (left <= right)
                            return true;
                        break;
                    }
                case "=":
                    {
                        if (left == right)
                            return true;
                        break;
                    }
                case ">":
                    {
                        if (left > right)
                            return true;
                        break;
                    }
                case "<":
                    {
                        if (left < right)
                            return true;
                        break;
                    }
            }

            return false;
        }


        protected bool configHasOptionsOfConstraint(Configuration config)
        {
            foreach (BinaryOption bo in leftHandSide.participatingBoolOptions.Union(rightHandSide.participatingBoolOptions))
            {
                if (!config.BinaryOptions.ContainsKey(bo))
                    return false;
            }

            foreach (NumericOption no in leftHandSide.participatingNumOptions.Union(rightHandSide.participatingNumOptions))
            {
                if (!config.NumericOptions.ContainsKey(no))
                    return false;
            }

            return true;
        }

        private bool configHasOptionsOfConstraint(Dictionary<NumericOption, double> config)
        {
            foreach (NumericOption no in leftHandSide.participatingNumOptions.Union(rightHandSide.participatingNumOptions))
            {
                if (!config.ContainsKey(no))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// All configuration options that participate in the constraint.
        /// </summary>
        public List<ConfigurationOption> ParticipatingOptions()
        {
            List<ConfigurationOption> result = new List<ConfigurationOption>();
            result.AddRange(leftHandSide.participatingBoolOptions);
            result.AddRange(leftHandSide.participatingNumOptions);
            result.AddRange(rightHandSide.participatingBoolOptions);
            result.AddRange(rightHandSide.participatingNumOptions);
            return result.Distinct().ToList();
        }


        /// <summary>
        /// Returns the string representation of the constraint consisting of the left hand side, the comparator and the right hand side of the 
        /// constraint.
        /// </summary>
        /// <returns>The string representation of the constraint.</returns>
        public override string ToString()
        {
            return leftHandSide + " " + comparator + " " + rightHandSide;
        }
    }
}
