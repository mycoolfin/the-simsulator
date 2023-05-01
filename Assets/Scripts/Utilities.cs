using System;
using System.Text.RegularExpressions;
using UnityEngine;

public static class Utilities
{
    public static float RandomGaussian()
    {
        float u, v, S;

        do
        {
            u = 2.0f * UnityEngine.Random.value - 1.0f;
            v = 2.0f * UnityEngine.Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        float fac = Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
        return u * fac;
    }

    public static T RandomEnumValue<T>() where T : Enum
    {
        Array enumValues = Enum.GetValues(typeof(T));
        return (T)enumValues.GetValue(UnityEngine.Random.Range(0, enumValues.Length));
    }

    public static string PascalToSentenceCase(this string str)
    {
        return Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {m.Value[1]}");
    }

    public static string SentenceToPascalCase(this string str)
    {
        return str.Replace(" ", string.Empty);
    }
}
