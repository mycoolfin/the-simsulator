using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class NervousSystemDisplay : MonoBehaviour
{
    public VisualTreeAsset nervousSystemSection;
    public VisualTreeAsset nervousSystemNode;

    private List<SensorBase> sensors;
    private List<NeuronBase> neurons;
    private List<EffectorBase> effectors;

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
        for (int i = 0; i < phenotype.brain.neurons.Count; i++)
            brainNeuronsPanel.Add(InstantiateNervousSystemNode(phenotype.brain.neurons[i].GetType().Name, phenotype.brain.neurons[i], phenotype.brain.neurons[i]));

        foreach (Limb limb in phenotype.limbs)
        {
            sensors = limb.joint?.sensors.Cast<SensorBase>().ToList() ?? new List<SensorBase>();
            neurons = limb.neurons;
            effectors = limb.joint?.effectors.Cast<EffectorBase>().ToList() ?? new List<EffectorBase>();

            VisualElement section = nervousSystemSection.Instantiate();
            scrollView.Add(section);

            VisualElement sensorsPanel = section.Q<VisualElement>("sensors");
            VisualElement neuronsPanel = section.Q<VisualElement>("neurons");
            VisualElement effectorsPanel = section.Q<VisualElement>("effectors");

            for (int i = 0; i < sensors.Count; i++)
                sensorsPanel.Add(InstantiateNervousSystemNode(sensors[i].GetType().Name, null, sensors[i]));
            for (int i = 0; i < neurons.Count; i++)
                neuronsPanel.Add(InstantiateNervousSystemNode(neurons[i].GetType().Name, neurons[i], neurons[i]));
            for (int i = 0; i < effectors.Count; i++)
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
            List<float> inputValues = selfReceiver?.GetWeightedInputValues();
            float? outputValue = selfEmitter?.OutputValue;
            input1Label.text = inputValues?.Count > 0 ? inputValues[0].ToString("0.00") : "";
            input2Label.text = inputValues?.Count > 1 ? inputValues[1].ToString("0.00") : "";
            input3Label.text = inputValues?.Count > 2 ? inputValues[2].ToString("0.00") : "";
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
            (List<ISignalEmitter> inputs, ISignalEmitter output) = ((List<ISignalEmitter>, ISignalEmitter))node.userData;

            for (int i = 0; i < inputs?.Count; i++)
            {
                foreach (TemplateContainer otherNode in nodes)
                {
                    (List<ISignalEmitter> otherInputs, ISignalEmitter otherOutput) = ((List<ISignalEmitter>, ISignalEmitter))otherNode.userData;
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
