using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Crosstales.FB;
using cakeslice;

public class Phenotype : MonoBehaviour, ISelectable, IPlaceable
{
    public Genotype genotype;
    public Brain brain;
    public List<Limb> limbs;
    private Color originalLimbColor;
    public List<Collider> activeColliders;
    private List<MeshRenderer> meshRenderers;
    private List<Outline> outlines;
    public bool lostLimbs;

    [SerializeField]
    private bool saveGenotypeToFile; // Editor only.

    private void Awake()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>().ToList();
        originalLimbColor = meshRenderers.Count == 0 ? Color.white : meshRenderers[0].sharedMaterial.color;
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
        bool atLeastTwoLimbs = limbs.Count > 1;

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

    public void ClearRGB()
    {
        foreach (MeshRenderer meshRenderer in meshRenderers)
            meshRenderer.material.color = originalLimbColor;
    }

    public void SetLayer(string layerName)
    {
        LayerMask layerMask = LayerMask.NameToLayer(layerName);
        gameObject.layer = layerMask;
        foreach (Transform child in transform)
            child.gameObject.layer = layerMask;
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
        WorldManager.Instance.SendGameObjectToTrashCan(limb.gameObject);

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
        phenotype.activeColliders = limbs.SelectMany(limb => limb.activeColliders).ToList();

        // Wire up nervous system.
        NervousSystem.Configure(brain, limbs);

        return phenotype;
    }

    public void SetLimbsSelectable(bool limbsSelectable)
    {
        limbs.ForEach(l => l.passSelectionToParent = !limbsSelectable);
    }

    public void Select(bool toggle, bool multiselect)
    {
        if (toggle && SelectionManager.Instance.Selected.Contains(this))
        {
            SelectionManager.Instance.RemoveFromSelection(this);
        }
        else
        {
            outlines.Where(o => o != null).ToList().ForEach(o => o.enabled = true);
            void handler(List<ISelectable> previouslySelected, List<ISelectable> selected)
            {
                if (!selected.Contains(this))
                {
                    outlines.Where(o => o != null).ToList().ForEach(o => o.enabled = false);
                    SelectionManager.Instance.OnSelectionChange -= handler;
                }
            }
            SelectionManager.Instance.OnSelectionChange += handler;
            if (multiselect)
                SelectionManager.Instance.AddToSelection(this);
            else
                SelectionManager.Instance.SetSelected(new() { this });
        }
    }

    public void SaveGenotypeToFile()
    {
        string savePath = FileBrowser.Instance.SaveFile(
            "Save Genotype",
            FileBrowser.Instance.CurrentSaveFile ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            genotype.Id,
            "genotype"
        );
        string savedName = Path.GetFileNameWithoutExtension(savePath);
        bool saveSuccess = false;
        if (!string.IsNullOrEmpty(savedName))
        {
            Genotype genotypeToSave = Genotype.Construct(
                savedName,
                genotype.Ancestry,
                genotype.BrainNeuronDefinitions,
                genotype.LimbNodes
            );
            saveSuccess = !string.IsNullOrEmpty(genotypeToSave.SaveToFile(savePath));
        }
    }
}
