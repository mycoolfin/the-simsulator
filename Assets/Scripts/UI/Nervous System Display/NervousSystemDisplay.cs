using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class NervousSystemDisplay : MonoBehaviour
{
    public VisualTreeAsset nervousSystemSection;
    public VisualTreeAsset nervousSystemNode;

    private List<Sensor> sensors;
    private List<Neuron> neurons;
    private List<Effector> effectors;

    private List<VisualElement> nodes;
    private List<Action> updateFunctions;

    private Phenotype phenotype;

    private void Update()
    {
        if (phenotype == null)
        {
            Phenotype[] phenotypes = FindObjectsOfType<Phenotype>();
            if (phenotypes.Length != 0)
            {
                Initialise(phenotypes[^1]);
            }
            else
                updateFunctions = null;
        }
        else
        {
            foreach (Action updateFunction in updateFunctions)
                updateFunction();
        }
    }

    private void Initialise(Phenotype phenotype)
    {
        nodes = new List<VisualElement>();
        updateFunctions = new List<Action>();

        UIDocument uiDocument = GetComponent<UIDocument>();
        uiDocument.enabled = false;
        uiDocument.enabled = true;
        ScrollView scrollView = uiDocument.rootVisualElement.Q<ScrollView>("scroll-view");

        VisualElement brainSection = nervousSystemSection.Instantiate();
        brainSection.Q<Label>("instance-id").text = phenotype.genotype.Id;
        scrollView.Add(brainSection);
        VisualElement brainNeuronsPanel = brainSection.Q<VisualElement>("neurons");
        for (int i = 0; i < phenotype.brain.neurons.Count; i++)
            brainNeuronsPanel.Add(InstantiateNervousSystemNode(phenotype.brain.neurons[i].GetType().Name, phenotype.brain.neurons[i].Processor.Receiver, phenotype.brain.neurons[i].Processor.Emitter));

        foreach (Limb limb in phenotype.limbs)
        {
            sensors = (limb.joint?.sensors.Cast<Sensor>().ToList() ?? new List<Sensor>()).Concat(limb.photoSensors).ToList();
            neurons = limb.neurons;
            effectors = limb.joint?.effectors.Cast<Effector>().ToList() ?? new List<Effector>();

            VisualElement section = nervousSystemSection.Instantiate();
            section.Q<Label>("instance-id").text = limb.instanceId;
            scrollView.Add(section);

            VisualElement sensorsPanel = section.Q<VisualElement>("sensors");
            VisualElement neuronsPanel = section.Q<VisualElement>("neurons");
            VisualElement effectorsPanel = section.Q<VisualElement>("effectors");

            for (int i = 0; i < sensors.Count; i++)
                sensorsPanel.Add(InstantiateNervousSystemNode(sensors[i].GetType().Name, null, sensors[i].Processor.Emitter));
            for (int i = 0; i < neurons.Count; i++)
                neuronsPanel.Add(InstantiateNervousSystemNode(neurons[i].GetType().Name, neurons[i].Processor.Receiver, neurons[i].Processor.Emitter));
            for (int i = 0; i < effectors.Count; i++)
                effectorsPanel.Add(InstantiateNervousSystemNode(effectors[i].GetType().Name, effectors[i].Processor.Receiver, null));
        }

        scrollView.generateVisualContent += OnGenerateVisualContent;
    }

    private void RemoveAllSections()
    {

    }

    private VisualElement InstantiateNervousSystemNode(string nodeName, SignalReceiver selfReceiver, SignalEmitter selfEmitter)
    {
        VisualElement node = nervousSystemNode.Instantiate();
        if (selfEmitter != null && ((SignalEmitter)selfEmitter).Disabled)
            node.Q<VisualElement>("block").style.backgroundColor = Color.red;
        node.Q<Label>("node-name").text = nodeName;
        Label input1Label = node.Q<Label>("input-1");
        Label input2Label = node.Q<Label>("input-2");
        Label input3Label = node.Q<Label>("input-3");
        Label outputLabel = node.Q<Label>("output");

        Action updateFunction = () =>
        {
            float[] inputValues = selfReceiver?.WeightedInputValues;
            float? outputValue = selfEmitter?.OutputValue;
            input1Label.text = inputValues?.Length > 0 ? inputValues[0].ToString("0.00") : "";
            input2Label.text = inputValues?.Length > 1 ? inputValues[1].ToString("0.00") : "";
            input3Label.text = inputValues?.Length > 2 ? inputValues[2].ToString("0.00") : "";
            outputLabel.text = outputValue?.ToString("0.00") ?? "";
        };
        updateFunctions.Add(updateFunction);

        node.userData = (selfReceiver?.Inputs, selfEmitter);

        nodes.Add(node);

        return node;
    }

    void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Painter2D painter = mgc.painter2D;

        painter.strokeColor = Color.yellow;
        painter.lineJoin = LineJoin.Round;
        painter.lineCap = LineCap.Round;

        foreach (TemplateContainer node in nodes)
        {
            (List<SignalEmitter> inputs, SignalEmitter output) = ((List<SignalEmitter>, SignalEmitter))node.userData;

            for (int i = 0; i < inputs?.Count; i++)
            {
                foreach (TemplateContainer otherNode in nodes)
                {
                    (List<SignalEmitter> otherInputs, SignalEmitter otherOutput) = ((List<SignalEmitter>, SignalEmitter))otherNode.userData;
                    if (otherOutput != null && inputs[i].Equals((SignalEmitter)otherOutput))
                    {
                        VisualElement inputLabel = node.Q("input-" + (i + 1));
                        VisualElement outputLabel = otherNode.Q("output");
                        Vector2 start = new Vector2(inputLabel.worldBound.xMin, inputLabel.worldBound.center.y);
                        Vector2 control = new Vector2(start.x - 100f, start.y);
                        Vector2 end = new Vector2(outputLabel.worldBound.xMax, outputLabel.worldBound.center.y);
                        painter.BeginPath();
                        painter.BezierCurveTo(start, control, end);
                        painter.Stroke();
                    }
                }
            }
        }
    }
}
