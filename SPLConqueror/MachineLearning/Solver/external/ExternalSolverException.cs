using System;

namespace MachineLearning.Solver
{
    class ExternalSolverException : Exception
    {
        internal ExternalSolverException()
        {
        }

        internal ExternalSolverException(string message) : base(message)
        {
        }
    }
}
