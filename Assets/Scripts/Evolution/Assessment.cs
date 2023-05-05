using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate IEnumerator AssessmentFunction(Individual individual, Population population, Transform trialOrigin);

public static class Assessment
{
    public static IEnumerator GroundDistance(Individual individual, Population population, Transform trialOrigin)
    {
        if (individual.phenotype == null) yield break;
        DisablePhenotypeCollisions(individual, population);
        individual.preProcessingComplete = true;

        int settleSeconds = 10;
        int runtimeSeconds = 10;
        int fitnessUpdateIntervals = 100;
        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;

        PlacePhenotype(individual.phenotype, trialOrigin);

        yield return new WaitForSeconds(settleSeconds);
        if (individual.phenotype == null) yield break;

        Vector3 startPosition = individual.phenotype.GetBounds().center;

        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);
            if (individual.phenotype == null) yield break;

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
                individual.fitness = planarDisplacement;

                if (individual.phenotype.lostLimbs)
                    individual.fitness = 0;
            }
        }
    }

    public static IEnumerator WaterDistance(Individual individual, Population population, Transform trialOrigin)
    {
        if (individual.phenotype == null) yield break;
        DisablePhenotypeCollisions(individual, population); ;
        individual.preProcessingComplete = true;

        int settleSeconds = 5;
        int runtimeSeconds = 15;
        int fitnessUpdateIntervals = 100;
        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;

        PlacePhenotype(individual.phenotype, trialOrigin);

        yield return new WaitForSeconds(settleSeconds);
        if (individual.phenotype == null) yield break;

        // Kill momentum.
        List<Rigidbody> rigidbodies = individual.phenotype.GetComponentsInChildren<Rigidbody>().ToList();
        rigidbodies.ForEach(x => { x.isKinematic = true; });
        yield return new WaitForFixedUpdate();
        if (individual.phenotype == null) yield break;
        rigidbodies.ForEach(x => { x.isKinematic = false; });

        Vector3 startPosition = individual.phenotype.GetBounds().center;

        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);
            if (individual.phenotype == null) yield break;

            Bounds bounds = individual.phenotype.GetBounds();
            Vector3 currentPosition = bounds.center;

            float displacement = Vector3.Distance(startPosition, currentPosition);
            individual.fitness = displacement;

            if (individual.phenotype.lostLimbs)
                individual.fitness = 0;
        }
    }

    public static IEnumerator LightCloseness(Individual individual, Population population, Transform trialOrigin)
    {
        if (individual.phenotype == null) yield break;
        DisablePhenotypeCollisions(individual, population); ;
        individual.preProcessingComplete = true;

        int settleSeconds = 10;
        int runtimeSeconds = 10;
        int fitnessUpdateIntervals = 100;
        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;

        PlacePhenotype(individual.phenotype, trialOrigin);

        yield return new WaitForSeconds(settleSeconds);
        if (individual.phenotype == null) yield break;

        Vector3 startPosition = individual.phenotype.GetBounds().center;

        float maxDistance = Vector3.Distance(startPosition, WorldManager.Instance.pointLight.transform.position);

        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);
            if (individual.phenotype == null) yield break;

            Bounds bounds = individual.phenotype.GetBounds();
            Vector3 currentPosition = bounds.center;

            float currentDistance = Vector3.Distance(currentPosition, WorldManager.Instance.pointLight.transform.position);
            individual.fitness = Mathf.Max(0, (maxDistance - currentDistance) / maxDistance);

            if (individual.phenotype.lostLimbs)
                individual.fitness = 0;
        }
    }

    private static void DisablePhenotypeCollisions(Individual self, Population population)
    {
        foreach (Collider col in self.phenotype.GetComponentsInChildren<Collider>())
            foreach (Individual other in population.individuals)
                if (other != self && !other.preProcessingComplete && other.phenotype != null)
                    foreach (Collider otherCol in other.phenotype.GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(col, otherCol);
    }

    private static void PlacePhenotype(Phenotype phenotype, Transform trialOrigin)
    {
        Vector3 offset = Vector3.zero;
        if (trialOrigin.position == Vector3.zero) // Offset so we don't intersect the ground.
            offset = new Vector3(0, -phenotype.GetBounds().center.y + phenotype.GetBounds().extents.y * 2f, 0);

        phenotype.transform.position = trialOrigin.position + offset;
        phenotype.gameObject.SetActive(true);
    }
}
