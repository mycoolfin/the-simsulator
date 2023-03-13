using System.Collections;
using UnityEngine;

public delegate IEnumerator AssessmentFunction(Individual individual);

public static class Assessment
{
    public static IEnumerator GroundDistance(Individual individual)
    {
        DisablePhenotypeCollisions(individual.phenotype);

        int settleSeconds = 10;
        int runtimeSeconds = 10;
        int fitnessUpdateIntervals = 100;
        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;

        individual.phenotype.transform.position = new Vector3(0, -individual.phenotype.GetBounds().center.y + individual.phenotype.GetBounds().extents.y, 0);

        yield return new WaitForSeconds(settleSeconds);

        Vector3 startPosition = individual.phenotype.GetBounds().center;

        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);

            Bounds bounds = individual.phenotype.GetBounds();
            Vector3 currentPosition = bounds.center;

            if (currentPosition.y < 0) // Phenotype fell below ground somehow.
            {
                individual.fitness = 0f;
                break;
            }
            else
            {
                float planarDisplacement = Vector3.Magnitude(new Vector3(currentPosition.x - startPosition.x, 0, currentPosition.z - startPosition.z));
                float phenotypeLength = Mathf.Max(bounds.extents.x * 2, bounds.extents.y * 2, bounds.extents.z * 2);
                float lengthsTravelled = planarDisplacement / phenotypeLength;
                individual.fitness = lengthsTravelled;
            }
        }
    }

    public static IEnumerator WaterDistance(Individual individual)
    {
        DisablePhenotypeCollisions(individual.phenotype);

        int runtimeSeconds = 20;
        int fitnessUpdateIntervals = 100;
        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;

        Vector3 startPosition = individual.phenotype.GetBounds().center;

        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);

            Bounds bounds = individual.phenotype.GetBounds();
            Vector3 currentPosition = bounds.center;

            float displacement = Vector3.Distance(startPosition, currentPosition);
            float phenotypeLength = Mathf.Max(bounds.extents.x * 2, bounds.extents.y * 2, bounds.extents.z * 2);
            float lengthsTravelled = displacement / phenotypeLength;
            individual.fitness = lengthsTravelled;
        }
    }

    private static void DisablePhenotypeCollisions(Phenotype phenotype)
    {
        foreach (Collider col in phenotype.GetComponentsInChildren<Collider>())
            foreach (Phenotype otherPhenotype in UnityEngine.Object.FindObjectsOfType<Phenotype>())
                if (otherPhenotype != phenotype)
                    foreach (Collider otherCol in otherPhenotype.GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(col, otherCol);
    }
}
