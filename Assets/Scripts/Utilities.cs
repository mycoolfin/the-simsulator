using System;
using System.Collections.Generic;
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
}
