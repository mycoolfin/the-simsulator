using System.Text.RegularExpressions;

namespace mycoolfin.TheSimsulator.UnityIntegration.Utilities
{
    public static class StringUtils
    {
        public static string PascalToSentenceCase(this string str)
        {
            return Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {m.Value[1]}");
        }

        public static string SentenceToPascalCase(this string str)
        {
            return str.Replace(" ", string.Empty);
        }
    }
}
