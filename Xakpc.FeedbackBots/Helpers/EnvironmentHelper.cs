using System;

namespace Xakpc.FeedbackBots.Helpers
{
    public static class EnvironmentHelper
    {
        public static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
