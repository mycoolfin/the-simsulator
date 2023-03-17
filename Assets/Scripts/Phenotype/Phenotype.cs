using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Phenotype : MonoBehaviour
{
    public Genotype genotype;
    public Brain brain;
    public List<Limb> limbs;
    private List<MeshRenderer> meshRenderers;
    public ComputeShader neuronComputeShader;

    public bool lostLimbs;

    public bool saveGenotypeToFile;

    private void Start()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>().ToList();
    }

    private void Update()
    {
        if (saveGenotypeToFile)
        {
            saveGenotypeToFile = false;
            GenotypeSerializer.WriteGenotypeToFile(genotype, genotype.Id + ".genotype.json");
        }
    }

    private void FixedUpdate()
    {
        UpdateNeurons();
    }

    private void UpdateNeurons()
    {
        if (WorldManager.Instance.useComputeShaderForNNs)
            UpdateNeuronsGPU();
        else
            UpdateNeuronsCPU();
    }

    private void UpdateNeuronsCPU()
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

    private void UpdateNeuronsGPU()
    {

        // ///// Map up weighted neuron inputs in parallel

        // // Serialise neuron data.
        // List<float> neuronInputs = new List<float>(); // TODO
        // List<float> neuronWeights = new List<float>(); // TODO
        // List<float> neuronOutputs = new List<float>(); // TODO

        // ComputeBuffer neuronInputsBuffer = new ComputeBuffer(neuronInputs.Count, sizeof(float));
        // ComputeBuffer neuronWeightsBuffer = new ComputeBuffer(neuronWeights.Count, sizeof(float));
        // ComputeBuffer neuronOutputsBuffer = new ComputeBuffer(neuronOutputs.Count, sizeof(float));
        // neuronInputsBuffer.SetData(neuronInputs);
        // neuronWeightsBuffer.SetData(neuronWeights);
        // neuronOutputsBuffer.SetData(neuronOutputs);

        // neuronComputeShader.SetBuffer(0, "neuronInputs", neuronInputsBuffer);
        // neuronComputeShader.SetBuffer(0, "neuronWeights", neuronWeightsBuffer);
        // neuronComputeShader.SetBuffer(0, "neuronOutputs", neuronOutputsBuffer);

        // // Generate random indices for droplet placement
        // int[] randomIndices = new int[numErosionIterations];
        // for (int i = 0; i < numErosionIterations; i++)
        // {
        //     int randomX = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
        //     int randomY = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
        //     randomIndices[i] = randomY * mapSize + randomX;
        // }

        // // Send random indices to compute shader
        // ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
        // randomIndexBuffer.SetData(randomIndices);
        // erosion.SetBuffer(0, "randomIndices", randomIndexBuffer);

        // // Heightmap buffer
        // ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(float));
        // mapBuffer.SetData(map);
        // erosion.SetBuffer(0, "map", mapBuffer);

        // // Settings
        // erosion.SetInt("borderSize", erosionBrushRadius);
        // erosion.SetInt("mapSize", mapSizeWithBorder);
        // erosion.SetInt("brushLength", brushIndexOffsets.Count);
        // erosion.SetInt("maxLifetime", maxLifetime);
        // erosion.SetFloat("inertia", inertia);
        // erosion.SetFloat("sedimentCapacityFactor", sedimentCapacityFactor);
        // erosion.SetFloat("minSedimentCapacity", minSedimentCapacity);
        // erosion.SetFloat("depositSpeed", depositSpeed);
        // erosion.SetFloat("erodeSpeed", erodeSpeed);
        // erosion.SetFloat("evaporateSpeed", evaporateSpeed);
        // erosion.SetFloat("gravity", gravity);
        // erosion.SetFloat("startSpeed", startSpeed);
        // erosion.SetFloat("startWater", startWater);

        // // Run compute shader
        // erosion.Dispatch(0, numThreads, 1, 1);
        // mapBuffer.GetData(map);

        // // Release buffers
        // mapBuffer.Release();
        // randomIndexBuffer.Release();
        // neuronInputsBuffer.Release();
        // brushWeightBuffer.Release();
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
