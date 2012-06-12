using System;

namespace CoLiW
{
    public class InvalidCommand : Exception
    {
        public InvalidCommand(string wrongNumberOfParameters) : base(wrongNumberOfParameters)
        {
            
        }
    }
}