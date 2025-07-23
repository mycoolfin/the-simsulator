using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.UnityIntegration.Utilities
{
    public static class CommandLineArgs
    {
        private static readonly Dictionary<string, string> args;

        static CommandLineArgs()
        {
            CommandLineArgs.args = new Dictionary<string, string>();
            string[] args = System.Environment.GetCommandLineArgs();

            foreach (string arg in args)
            {
                if (arg.StartsWith("--"))
                {
                    var split = arg[2..].Split('=');
                    if (split.Length == 2)
                        CommandLineArgs.args[split[0]] = split[1];
                    else
                        CommandLineArgs.args[split[0]] = "true";
                }
            }
        }

        public static bool TryGet(string key, out string value) => args.TryGetValue(key, out value);
        public static string Get(string key, string fallback = null) => args.TryGetValue(key, out var val) ? val : fallback;
        public static bool GetBool(string key) => args.ContainsKey(key) && args[key] != "false";
    }
}
