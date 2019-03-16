using System;

namespace MachineLearning.Solver
{
    public class ExternalSolverException : Exception
    {
        public ExternalSolverException()
        {
        }

        public ExternalSolverException(string message) : base(message)
        {
        }
    }
}
