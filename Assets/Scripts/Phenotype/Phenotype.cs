using UnityEngine;

public class Phenotype : MonoBehaviour
{
    public Brain brain;
    public Limb[] limbs;

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
}
