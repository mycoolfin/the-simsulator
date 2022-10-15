using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NervousSystemDisplay : MonoBehaviour
{
    public VisualTreeAsset nervousSystemSection;
    public VisualTreeAsset nervousSystemNode;

    private SensorBase[] sensors;
    private NeuronBase[] neurons;
    private EffectorBase[] effectors;

    private List<VisualElement> nodes;
    private List<Action> updateFunctions;

    private void OnEnable()
    {
        nodes = new List<VisualElement>();
        updateFunctions = new List<Action>();

        UIDocument uiDocument = GetComponent<UIDocument>();
        ScrollView scrollView = uiDocument.rootVisualElement.Q<ScrollView>("scroll-view");

        Phenotype phenotype = FindObjectOfType<Phenotype>();

        VisualElement brainSection = nervousSystemSection.Instantiate();
        scrollView.Add(brainSection);
        VisualElement brainNeuronsPanel = brainSection.Q<VisualElement>("neurons");
        for (int i = 0; i < phenotype.brain.neurons.Length; i++)
            brainNeuronsPanel.Add(InstantiateNervousSystemNode(phenotype.brain.neurons[i].GetType().Name, phenotype.brain.neurons[i], phenotype.brain.neurons[i]));

        foreach (Limb limb in phenotype.limbs)
        {
            sensors = limb.joint?.sensors ?? new SensorBase[0];
            neurons = limb.neurons;
            effectors = limb.joint?.effectors ?? new EffectorBase[0];

            VisualElement section = nervousSystemSection.Instantiate();
            scrollView.Add(section);

            VisualElement sensorsPanel = section.Q<VisualElement>("sensors");
            VisualElement neuronsPanel = section.Q<VisualElement>("neurons");
            VisualElement effectorsPanel = section.Q<VisualElement>("effectors");

            for (int i = 0; i < sensors.Length; i++)
                sensorsPanel.Add(InstantiateNervousSystemNode(sensors[i].GetType().Name, null, sensors[i]));
            for (int i = 0; i < neurons.Length; i++)
                neuronsPanel.Add(InstantiateNervousSystemNode(neurons[i].GetType().Name, neurons[i], neurons[i]));
            for (int i = 0; i < effectors.Length; i++)
                effectorsPanel.Add(InstantiateNervousSystemNode(effectors[i].GetType().Name, effectors[i], null));
        }

        scrollView.generateVisualContent += OnGenerateVisualContent;
    }

    private void Update()
    {
        foreach (Action updateFunction in updateFunctions)
            updateFunction();
    }

    private VisualElement InstantiateNervousSystemNode(string nodeName, ISignalReceiver selfReceiver, ISignalEmitter selfEmitter)
    {
        VisualElement node = nervousSystemNode.Instantiate();
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
            (ISignalEmitter[] inputs, ISignalEmitter output) = ((ISignalEmitter[], ISignalEmitter))node.userData;

            for (int i = 0; i < inputs?.Length; i++)
            {
                foreach (TemplateContainer otherNode in nodes)
                {
                    (ISignalEmitter[] otherInputs, ISignalEmitter otherOutput) = ((ISignalEmitter[], ISignalEmitter))otherNode.userData;
                    if (otherOutput != null && inputs[i] == otherOutput)
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
