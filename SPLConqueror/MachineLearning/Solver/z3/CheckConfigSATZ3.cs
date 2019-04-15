using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Z3;
using SPLConqueror_Core;

namespace MachineLearning.Solver
{
    class CheckConfigSATZ3 : ICheckConfigSAT
    {
        private readonly VariabilityModel _vm;

        public CheckConfigSATZ3(VariabilityModel vm) { _vm = vm; }

        public bool checkConfigurationSAT(List<BinaryOption> config, bool partialConfiguration)
        {
            List<BoolExpr> variables;
            Dictionary<BoolExpr, BinaryOption> termToOption;
            Dictionary<BinaryOption, BoolExpr> optionToTerm;
            Tuple<Context, BoolExpr> z3Tuple = Z3Solver.GetInitializedBooleanSolverSystem(out variables, out optionToTerm, out termToOption, _vm, false);
            Context z3Context = z3Tuple.Item1;
            BoolExpr z3Constraints = z3Tuple.Item2;

            List<BoolExpr> constraints = new List<BoolExpr>();
            Microsoft.Z3.Solver solver = z3Context.MkSolver();
            solver.Assert(z3Constraints);

            solver.Assert(Z3Solver.ConvertConfiguration(z3Context, config, optionToTerm, _vm, partialConfiguration));

            if (solver.Check() == Status.SATISFIABLE)
            {
                return true;
            }
            return false;
        }

        public bool checkConfigurationSAT(Configuration c, bool partialConfiguration = false)
        {
            List<Expr> variables;
            Dictionary<Expr, ConfigurationOption> termToOption;
            Dictionary<ConfigurationOption, Expr> optionToTerm;
            Tuple<Context, BoolExpr> z3Tuple = Z3Solver.GetInitializedSolverSystem(out variables, out optionToTerm, out termToOption, _vm);
            Context z3Context = z3Tuple.Item1;
            BoolExpr z3Constraints = z3Tuple.Item2;

            List<Expr> constraints = new List<Expr>();
            Microsoft.Z3.Solver solver = z3Context.MkSolver();
            solver.Assert(z3Constraints);

            solver.Assert(Z3Solver.ConvertConfiguration(z3Context, c.getBinaryOptions(BinaryOption.BinaryValue.Selected), optionToTerm, _vm, partialConfiguration, c.NumericOptions));

            if (solver.Check() == Status.SATISFIABLE)
            {
                return true;
            }
            return false;
        }
    }
}
