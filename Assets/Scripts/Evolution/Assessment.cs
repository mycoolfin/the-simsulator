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
                phenotype.SetVisible(phenotype.fitness > 0.1f);
                phenotype.SetRGB(bestFitness == 0 ? 1f : 1f - (phenotype.fitness / bestFitness), bestFitness == 0 ? 0f : phenotype.fitness / bestFitness, 0f);
            }
        }

        returnValueCallback(phenotype.fitness);
    }

    public static IEnumerator WaterDistance(Phenotype phenotype, float bestFitness, Action<float> returnValueCallback)
    {
        int runtimeSeconds = 20;
        int fitnessUpdateIntervals = 100;
        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;

        Vector3 startPosition = phenotype.GetBounds().center;

        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);

            Bounds bounds = phenotype.GetBounds();
            Vector3 currentPosition = bounds.center;

            float displacement = Vector3.Distance(startPosition, currentPosition);
            float phenotypeLength = Mathf.Max(bounds.extents.x * 2, bounds.extents.y * 2, bounds.extents.z * 2);
            float lengthsTravelled = displacement / phenotypeLength;
            phenotype.fitness = displacement;

            float runtimeProgress = 1 - (i / fitnessUpdateIntervals);
            float fitnessRatio = bestFitness == 0f || runtimeProgress == 0f ? 0f : phenotype.fitness / (bestFitness * runtimeProgress);
            phenotype.SetVisible(fitnessRatio > 0.5f);
            phenotype.SetRGB(1f - fitnessRatio, fitnessRatio, 0f);
        }

        phenotype.fitness = phenotype.fitness > 100f ? 0f : phenotype.fitness; // Filter out glitches (improve this).

        returnValueCallback(phenotype.fitness);
    }
}
