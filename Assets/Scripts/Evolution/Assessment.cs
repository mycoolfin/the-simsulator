using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Assessment
{
    public Transform trialOrigin;
    protected int settleSeconds;
    protected int runtimeSeconds;
    protected const int fitnessUpdateIntervals = 100;

    protected Assessment(Transform trialOrigin, int settleSeconds, int runtimeSeconds)
    {
        this.trialOrigin = trialOrigin;
        this.settleSeconds = settleSeconds;
        this.runtimeSeconds = runtimeSeconds;
    }

    public virtual void BeforeSimulationStart() { }
    public virtual void BeforeIterationStart() { }
    public virtual void BeforeAssessmentStart() { }

    public virtual IEnumerator PreProcess(Individual individual, Population population)
    {
        if (individual.phenotype == null) { individual.Cull(); yield break; }
        yield return DisablePhenotypeCollisions(individual, population);
        if (individual.phenotype == null) { individual.Cull(); yield break; }
        PlacePhenotype(individual.phenotype, trialOrigin);
        individual.preProcessingComplete = true;
    }

    public virtual IEnumerator WaitUntilSettled(Individual individual)
    {
        yield return new WaitForSeconds(settleSeconds);
    }

    public abstract IEnumerator Assess(Individual individual);

    // This function is costly, so I've designed it to spread out over multiple frames.
    // There may be a better way to prevent phenotypes from colliding with each other
    // while maintaining self collisions, but I haven't found it yet.
    protected IEnumerator DisablePhenotypeCollisions(Individual self, Population population)
    {
        int maxOpsPerFrame = 100000 / population.individuals.Count;
        int opsCount = 0;
        foreach (Collider col in self.phenotype.activeColliders)
        {
            foreach (Individual other in population.individuals)
                if (other != self && !other.preProcessingComplete && other.phenotype != null)
                    foreach (Collider otherCol in other.phenotype.activeColliders)
                    {
                        Physics.IgnoreCollision(col, otherCol);
                        opsCount += 1;
                        if (opsCount >= maxOpsPerFrame)
                        {
                            yield return null;
                            opsCount = 0;
                        }
                    }
        }
    }

    protected void PlacePhenotype(Phenotype phenotype, Transform trialOrigin)
    {
        Vector3 offset = Vector3.zero;
        if (trialOrigin.position == Vector3.zero) // Offset so we don't intersect the ground.
            offset = new Vector3(0, -phenotype.GetBounds().center.y + phenotype.GetBounds().extents.y * 2f, 0);

        phenotype.transform.position = trialOrigin.position + offset;
        phenotype.gameObject.SetActive(true);
    }
}

public class GroundDistanceAssessment : Assessment
{
    public GroundDistanceAssessment() : base(WorldManager.Instance.groundOrigin.transform, 10, 10) { }

    public override void BeforeSimulationStart()
    {
        WorldManager.Instance.simulateFluid = false;
        WorldManager.Instance.gravity = true;
        WorldManager.Instance.pointLight.SetActive(false);
    }

    public override IEnumerator Assess(Individual individual)
    {
        if (individual.phenotype == null) { individual.Cull(); yield break; }

        Vector3 startPosition = individual.phenotype.GetBounds().center;

        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;
        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);
            if (individual.phenotype == null || individual.phenotype.lostLimbs) { individual.Cull(); yield break; }

            Bounds bounds = individual.phenotype.GetBounds();
            Vector3 currentPosition = bounds.center;

            if (currentPosition.y < 0) { individual.Cull(); yield break; } // Phenotype fell below ground somehow.

            float planarDisplacement = Vector3.Magnitude(new Vector3(currentPosition.x - startPosition.x, 0, currentPosition.z - startPosition.z));
            individual.fitness = planarDisplacement;
            individual.assessmentProgress = (float)(i + 1) / (float)fitnessUpdateIntervals;
        }
    }
}

public class WaterDistanceAssessment : Assessment
{
    public WaterDistanceAssessment() : base(WorldManager.Instance.waterOrigin.transform, 5, 15) { }

    public override void BeforeSimulationStart()
    {
        WorldManager.Instance.simulateFluid = true;
        WorldManager.Instance.gravity = false;
        WorldManager.Instance.pointLight.SetActive(false);
    }

    public override IEnumerator Assess(Individual individual)
    {
        if (individual.phenotype == null) { individual.Cull(); yield break; }

        // Kill momentum.
        List<Rigidbody> rigidbodies = individual.phenotype.GetComponentsInChildren<Rigidbody>().ToList();
        rigidbodies.ForEach(x => { x.isKinematic = true; });
        yield return new WaitForFixedUpdate();
        if (individual.phenotype == null) { individual.Cull(); yield break; }
        rigidbodies.ForEach(x => { x.isKinematic = false; });

        Vector3 startPosition = individual.phenotype.GetBounds().center;

        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;
        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);
            if (individual.phenotype == null || individual.phenotype.lostLimbs) { individual.Cull(); yield break; }

            Bounds bounds = individual.phenotype.GetBounds();
            Vector3 currentPosition = bounds.center;

            float displacement = Vector3.Distance(startPosition, currentPosition);
            individual.fitness = displacement;
            individual.assessmentProgress = (float)(i + 1) / (float)fitnessUpdateIntervals;
        }
    }
}

public abstract class LightClosenessAssessment : Assessment
{
    public LightClosenessAssessment(Transform trialOrigin, int settleSeconds, int runtimeSeconds) : base(trialOrigin, settleSeconds, runtimeSeconds) { }

    public override IEnumerator Assess(Individual individual)
    {
        if (individual.phenotype == null) { individual.Cull(); yield break; }

        Vector3 startPosition = individual.phenotype.GetBounds().center;

        float maxDistance = Vector3.Distance(startPosition, WorldManager.Instance.pointLight.transform.position);

        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;
        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);
            if (individual.phenotype == null || individual.phenotype.lostLimbs) { individual.Cull(); yield break; }

            Bounds bounds = individual.phenotype.GetBounds();
            Vector3 currentPosition = bounds.center;

            float currentDistance = Vector3.Distance(currentPosition, WorldManager.Instance.pointLight.transform.position);
            individual.fitness = Mathf.Max(0, (maxDistance - currentDistance) / maxDistance);
            individual.assessmentProgress = (float)(i + 1) / (float)fitnessUpdateIntervals;
        }
    }
}

public class GroundLightClosenessAssessment : LightClosenessAssessment
{
    public GroundLightClosenessAssessment() : base(WorldManager.Instance.groundOrigin.transform, 10, 10) { }

    public override void BeforeSimulationStart()
    {
        WorldManager.Instance.simulateFluid = false;
        WorldManager.Instance.gravity = true;
        WorldManager.Instance.pointLight.SetActive(false);
    }

    public override void BeforeIterationStart()
    {
        WorldManager.Instance.pointLight.SetActive(false);
    }

    public override void BeforeAssessmentStart()
    {
        WorldManager.Instance.pointLight.SetActive(true);
        WorldManager.Instance.pointLight.transform.position =
            trialOrigin.position
            + Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0)
            * Vector3.forward * UnityEngine.Random.Range(0f, 20f)
            + Vector3.up * 1f;
    }
}

public class WaterLightClosenessAssessment : LightClosenessAssessment
{
    public WaterLightClosenessAssessment() : base(WorldManager.Instance.waterOrigin.transform, 5, 15) { }

    public override void BeforeSimulationStart()
    {
        WorldManager.Instance.simulateFluid = true;
        WorldManager.Instance.gravity = false;
        WorldManager.Instance.pointLight.SetActive(false);
    }

    public override void BeforeIterationStart()
    {
        WorldManager.Instance.pointLight.SetActive(false);
    }

    public override void BeforeAssessmentStart()
    {
        WorldManager.Instance.pointLight.SetActive(true);
        WorldManager.Instance.pointLight.transform.position =
            trialOrigin.position
            + Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360))
            * Vector3.forward * UnityEngine.Random.Range(0f, 40f);
    }
}
