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
    public virtual void BeforePreparationStart() { }
    public virtual void BeforeAssessmentStart() { }

    public virtual IEnumerator PreProcess(Individual individual, Population population)
    {
        if (individual.phenotype == null) { individual.Cull(); yield break; }
        if (individual.phenotype == null) { individual.Cull(); yield break; }
        PlacePhenotype(individual.phenotype, trialOrigin);
        individual.preProcessingComplete = true;
    }

    public virtual IEnumerator WaitUntilSettled(Individual individual)
    {
        yield return new WaitForSeconds(settleSeconds);
    }

    public abstract IEnumerator Assess(Individual individual);

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
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Surface);
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
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Underwater);
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

    public override void BeforeSimulationStart()
    {
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Surface);
        WorldManager.Instance.pointLight.SetActive(false);
        WorldManager.Instance.pointLight.AddComponent<RandomTeleporter>();
    }

    public override void BeforePreparationStart()
    {
        WorldManager.Instance.pointLight.SetActive(false);
    }

    public override void BeforeAssessmentStart()
    {
        WorldManager.Instance.pointLight.SetActive(true);
    }

    public override IEnumerator Assess(Individual individual)
    {
        if (individual.phenotype == null) { individual.Cull(); yield break; }

        Vector3 startPosition = individual.phenotype.GetBounds().center;

        Vector3 currentLightPosition = WorldManager.Instance.pointLight.transform.position;
        float maxDistance = Vector3.Distance(startPosition, currentLightPosition);

        List<float> segmentFitnesses = new();

        float intervalLength = (float)runtimeSeconds / (float)fitnessUpdateIntervals;
        for (int i = 0; i < fitnessUpdateIntervals; i++)
        {
            yield return new WaitForSeconds(intervalLength);
            if (individual.phenotype == null || individual.phenotype.lostLimbs) { individual.Cull(); yield break; }

            Bounds bounds = individual.phenotype.GetBounds();
            Vector3 currentPosition = bounds.center;

            float currentDistance = Vector3.Distance(currentPosition, currentLightPosition);
            float segmentFitness = (maxDistance - currentDistance) / maxDistance;

            // Check if the light moved.
            if (WorldManager.Instance.pointLight.transform.position != currentLightPosition)
            {
                // Update for the new light position.
                currentLightPosition = WorldManager.Instance.pointLight.transform.position;
                maxDistance = Vector3.Distance(currentPosition, currentLightPosition);

                // Record segment.
                segmentFitnesses.Add(segmentFitness);
            }

            individual.fitness = Mathf.Max(0, segmentFitnesses.Sum() + segmentFitness);
            individual.assessmentProgress = (float)(i + 1) / (float)fitnessUpdateIntervals;
        }
    }
}

public class GroundLightClosenessAssessment : LightClosenessAssessment
{
    public GroundLightClosenessAssessment() : base(WorldManager.Instance.groundOrigin.transform, 10, 20) { }

    public override void BeforeSimulationStart()
    {
        base.BeforeSimulationStart();
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Surface);
        RandomTeleporter r = WorldManager.Instance.pointLight.GetComponent<RandomTeleporter>();
        r.radii = new Vector3(20f, 0f, 20f);
        r.waitTime = runtimeSeconds / 2f;
    }

    public override void BeforePreparationStart()
    {
        base.BeforePreparationStart();
        WorldManager.Instance.pointLight.transform.position = trialOrigin.position + Vector3.up * 2f;
    }
}

public class WaterLightClosenessAssessment : LightClosenessAssessment
{
    public WaterLightClosenessAssessment() : base(WorldManager.Instance.waterOrigin.transform, 5, 15) { }

    public override void BeforePreparationStart()
    {
        base.BeforePreparationStart();
        WorldManager.Instance.pointLight.transform.position = trialOrigin.position;
    }

    public override void BeforeSimulationStart()
    {
        base.BeforeSimulationStart();
        WorldManager.Instance.ChangeEnvironment(WorldEnvironment.Underwater);
        RandomTeleporter r = WorldManager.Instance.pointLight.GetComponent<RandomTeleporter>();
        r.radii = new Vector3(20f, 20f, 20f);
        r.waitTime = runtimeSeconds / 2f;
    }
}
