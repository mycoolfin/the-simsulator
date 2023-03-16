using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Phenotype : MonoBehaviour
{
    public Genotype genotype;
    public List<string> lineage;
    public Brain brain;
    public List<Limb> limbs;
    private List<MeshRenderer> meshRenderers;
    public ComputeShader neuronComputeShader;

    public bool lostLimbs;

    private void Start()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>().ToList();
        lineage = genotype.lineage.ToList(); // TODO: Remove this once we work out how to display genotype in inspector.
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
        WorldManager.Instance.AddGameObjectToTrashCan(limb.gameObject);

        lostLimbs = true;
    }
}
