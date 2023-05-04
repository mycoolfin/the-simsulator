using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class EffectorBase : ISignalReceiver
{
    protected abstract EffectorType TypeOfEffector { get; }
    private ReadOnlyCollection<InputDefinition> inputDefinitions;
    public ReadOnlyCollection<InputDefinition> InputDefinitions
    {
        get { return inputDefinitions; }
        set
        {
            if (value.Count == TypeOfEffector.NumberOfInputs())
                inputDefinitions = value;
            else
                throw new System.ArgumentException("Expected " + TypeOfEffector.NumberOfInputs().ToString() + " input definitions, got " + value.Count);
        }
    }
    private List<ISignalEmitter> inputs;
    public List<ISignalEmitter> Inputs
    {
        get { return inputs; }
        set
        {
            if (value.Count == TypeOfEffector.NumberOfInputs())
                inputs = value;
            else
                throw new System.ArgumentException("Expected " + TypeOfEffector.NumberOfInputs().ToString() + " inputs, got " + value.Count + ".");

        }
    }
    private List<float> weights;
    public List<float> Weights
    {
        get { return weights; }
        set
        {
            if (value.Count == TypeOfEffector.NumberOfInputs())
                weights = value;
            else
                throw new System.ArgumentException("Expected " + TypeOfEffector.NumberOfInputs().ToString() + " weights, got " + value.Count);
        }
    }
    private List<float> weightedInputValues;
    public List<float> WeightedInputValues
    {
        get
        {
            // If there is no input specified, use the weight as a constant input.
            for (int i = 0; i < Inputs.Count; i++)
                weightedInputValues[i] = (Inputs[i]?.OutputValue ?? 1f) * Weights[i];
            return weightedInputValues;
        }
        set { weightedInputValues = value; }
    }
    public float GetExcitation()
    {
        return Mathf.Clamp(WeightedInputValues[0], -1f, 1f);
    }

    protected EffectorBase()
    {
        InputDefinitions = new InputDefinition[TypeOfEffector.NumberOfInputs()].ToList().AsReadOnly();
        Inputs = new ISignalEmitter[TypeOfEffector.NumberOfInputs()].ToList();
        Weights = new float[TypeOfEffector.NumberOfInputs()].ToList();
        WeightedInputValues = new float[TypeOfEffector.NumberOfInputs()].ToList();
    }

    public static EffectorBase CreateEffector(EffectorType type, ReadOnlyCollection<InputDefinition> inputDefinitions)
    {
        EffectorBase effector;
        switch (type)
        {
            case EffectorType.JointAngle:
                effector = new JointAngleEffector();
                break;
            default:
                throw new System.ArgumentException("Unknown effector type '" + type + "'");
        }
        effector.inputDefinitions = inputDefinitions;
        return effector;
    }
}
