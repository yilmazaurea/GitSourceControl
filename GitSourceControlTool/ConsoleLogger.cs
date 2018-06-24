using System;
using GitSourceControl.Interfaces;

namespace GitSourceControlTool
{
    internal class ConsoleLogger : ILogger
    {
        public void LogError(string message)
        {
            Console.WriteLine("ERROR: {0}", message);
        }

        public void LogInfo(string message)
        {
            Console.WriteLine("INFO: {0}", message);
        }
    }
}