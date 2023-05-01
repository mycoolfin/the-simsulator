using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using cakeslice;

public class Phenotype : MonoBehaviour, ISelectable, IPlaceable
{
    public Genotype genotype;
    public Brain brain;
    public List<Limb> limbs;
    private List<MeshRenderer> meshRenderers;
    public ComputeShader neuronComputeShader;

    public bool lostLimbs;

    public bool saveGenotypeToFile; // Editor only.

    private List<Outline> outlines;

    private void Awake()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>().ToList();
        outlines = GetComponentsInChildren<Outline>().ToList();
    }

    private void Update()
    {
        if (saveGenotypeToFile)
        {
            saveGenotypeToFile = false;
            genotype.SaveToFile(genotype.Id + ".genotype");
        }
    }

    private void FixedUpdate()
    {
        UpdateNeurons();
    }

    private void UpdateNeurons()
    {
        foreach (NeuronBase neuron in brain.neurons)
            neuron.PropagatePhaseOne();
        foreach (Limb limb in limbs)
            foreach (NeuronBase neuron in limb.neurons)
                neuron.PropagatePhaseOne();

        foreach (NeuronBase neuron in brain.neurons)
            neuron.PropagatePhaseTwo();
        foreach (Limb limb in limbs)
            foreach (NeuronBase neuron in limb.neurons)
                neuron.PropagatePhaseTwo();
    }

    public Bounds GetBounds()
    {
        List<BoxCollider> colliders = limbs.Select(limb => limb.fullBodyCollider).ToList();
        if (colliders.Count == 0) return new Bounds(transform.position, Vector3.zero);
        Bounds bounds = colliders[0].bounds;
        foreach (BoxCollider r in colliders)
        {
            bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }

    public bool IsValid()
    {
        bool atLeastTwoLimbs = this.limbs.Count > 1;

        Bounds phenotypeBounds = GetBounds();
        bool validSize = phenotypeBounds.extents.x < PhenotypeParameters.MaxSize && phenotypeBounds.extents.y < PhenotypeParameters.MaxSize && phenotypeBounds.extents.z < PhenotypeParameters.MaxSize;

        return atLeastTwoLimbs && validSize;
    }

    public void SetRGB(float? r, float? g, float? b)
    {
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.material.color = new Color(
                r.HasValue ? Mathf.Clamp((float)r, 0f, 1f) : meshRenderer.material.color.r,
                g.HasValue ? Mathf.Clamp((float)g, 0f, 1f) : meshRenderer.material.color.g,
                b.HasValue ? Mathf.Clamp((float)b, 0f, 1f) : meshRenderer.material.color.b
            );
        }
    }

    public void SetVisible(bool visible)
    {
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.enabled = visible;
        }
    }

    public void DetachLimb(Limb limb)
    {
        limb.transform.parent = null;
        limbs.Remove(limb);
        MeshRenderer limbMeshRenderer = limb.GetComponent<MeshRenderer>();
        meshRenderers.Remove(limbMeshRenderer);
        limbMeshRenderer.material = ResourceManager.Instance.deadLimbMaterial;
        Outline limbOutline = limb.GetComponent<Outline>();
        outlines.Remove(limbOutline);
        limbOutline.enabled = false;
        WorldManager.Instance.AddGameObjectToTrashCan(limb.gameObject);

        lostLimbs = true;
    }

    public static Phenotype Construct(Genotype genotype)
    {
        // Create brain.
        Brain brain = new Brain(genotype.BrainNeuronDefinitions);

        // Create limbs.
        GameObject limbContainer = new(genotype.Id);
        List<Limb> limbs = Limb.InstantiateLimbs(genotype.InstancedLimbNodes, limbContainer.transform);

        Phenotype phenotype = limbContainer.AddComponent<Phenotype>();
        phenotype.genotype = genotype;
        phenotype.brain = brain;
        phenotype.limbs = limbs;

        // Wire up nervous system.
        NervousSystem.Configure(brain, limbs);

        return phenotype;
    }

    public void Select()
    {
        SelectionManager.Instance.Selected = this;
        outlines.ForEach(o => o.enabled = true);
        SelectionManager.Instance.OnSelection += () => outlines.ForEach(o =>
        {
            if (o != null)
                o.enabled = false;
        });
    }
}
