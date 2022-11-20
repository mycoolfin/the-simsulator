using System;
using System.Collections;
using UnityEngine;

public delegate IEnumerator AssessmentFunction(Phenotype phenotype, float bestFitness, Action<float> returnValueCallback);

public static class Assessment
{
    public static IEnumerator GroundDistance(Phenotype phenotype, float bestFitness, Action<float> returnValueCallback)
    {
        int settleSeconds = 10;
        int runtimeSeconds = 10;
        int fitnessUpdateIntervals = 100;
        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;

        phenotype.transform.position = new Vector3(0, -phenotype.GetBounds().center.y + phenotype.GetBounds().extents.y, 0);

        yield return new WaitForSeconds(settleSeconds);

        Vector3 startPosition = phenotype.GetBounds().center;

        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);

            Bounds bounds = phenotype.GetBounds();
            Vector3 currentPosition = bounds.center;

            if (currentPosition.y < 0) // Phenotype fell below ground somehow.
            {
                phenotype.fitness = 0f;
                phenotype.SetVisible(false);
                break;
            }
            else
            {
                float planarDisplacement = Vector3.Magnitude(new Vector3(currentPosition.x - startPosition.x, 0, currentPosition.z - startPosition.z));
                float phenotypeLength = Mathf.Max(bounds.extents.x * 2, bounds.extents.y * 2, bounds.extents.z * 2);
                float lengthsTravelled = planarDisplacement / phenotypeLength;
                phenotype.fitness = lengthsTravelled;
                phenotype.SetVisible(phenotype.fitness > 0.01f);
                phenotype.SetRGB(bestFitness == 0 ? 1f : 1f - (phenotype.fitness / bestFitness), bestFitness == 0 ? 0f : phenotype.fitness / bestFitness, 0f);
            }
        }

        returnValueCallback(phenotype.fitness);
    }

    public static IEnumerator WaterDistance(Phenotype phenotype, float bestFitness, Action<float> returnValueCallback)
    {
        int runtimeSeconds = 10;
        int fitnessUpdateIntervals = 100;
        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;

        Vector3 startPosition = phenotype.GetBounds().center;

        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);

            Bounds bounds = phenotype.GetBounds();
            Vector3 currentPosition = bounds.center;

            if (phenotype.GetComponentInChildren<Rigidbody>().velocity.magnitude > 100f)
            {
                phenotype.fitness = 0f;
                break;
            }

            float displacement = Vector3.Distance(startPosition, currentPosition);
            float phenotypeLength = Mathf.Max(bounds.extents.x * 2, bounds.extents.y * 2, bounds.extents.z * 2);
            float lengthsTravelled = displacement / phenotypeLength;
            phenotype.fitness = lengthsTravelled;
            phenotype.SetVisible(phenotype.fitness > 0.01f);
            phenotype.SetRGB(bestFitness == 0 ? 1f : 1f - (phenotype.fitness / bestFitness), bestFitness == 0 ? 0f : phenotype.fitness / bestFitness, 0f);
        }

        returnValueCallback(phenotype.fitness);
    }
}
