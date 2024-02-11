using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;

public class NodeEditor
{
    private readonly VisualElement nodeEditorContainer;
    private readonly VisualElement nodeEditorElement;
    private readonly VisualTreeAsset editConnectionTemplate;
    private LimbNode editedLimbNode;
    private int limbNodeIndex;
    private int limbNodeCount;

    public NodeEditor(VisualElement nodeEditorContainer, VisualElement nodeEditorElement, VisualTreeAsset editConnectionTemplate, Action<LimbNode, int> applyChanges, Action<int> deleteNode)
    {
        this.nodeEditorContainer = nodeEditorContainer;
        this.nodeEditorElement = nodeEditorElement;
        this.editConnectionTemplate = editConnectionTemplate;

        nodeEditorContainer.style.display = DisplayStyle.None;

        nodeEditorElement.Q<Button>("apply-changes").clicked += () => applyChanges(editedLimbNode, limbNodeIndex);
        nodeEditorElement.Q<Button>("delete-node").clicked += () => deleteNode(limbNodeIndex);

        InitialiseDimensions();
        InitialiseJoint();
        InitialiseConnections();
        InitialiseRecursiveLimit();
    }

    public void StartEditing(LimbNode limbNode, int limbNodeIndex, int limbNodeCount)
    {
        nodeEditorContainer.style.display = DisplayStyle.Flex;
        nodeEditorElement.Q<Label>("node-name").text = "Node " + (limbNodeIndex + 1).ToString();
        nodeEditorElement.Q<Button>("delete-node").style.display = limbNodeCount > 1 ? DisplayStyle.Flex : DisplayStyle.None;
        editedLimbNode = new(limbNode.Dimensions, limbNode.JointDefinition, limbNode.RecursiveLimit, limbNode.NeuronDefinitions, limbNode.Connections);
        this.limbNodeIndex = limbNodeIndex;
        this.limbNodeCount = limbNodeCount;

        PopulateDimensions();
        PopulateJoint();
        PopulateConnections();
        PopulateRecursiveLimit();
    }

    public void StopEditing()
    {
        nodeEditorContainer.style.display = DisplayStyle.None;
    }

    private void InitialiseDimensions()
    {
        VisualElement dimensions = nodeEditorElement.Q("dimensions");
        void editNodeDimensions(Vector3 newValue)
        {
            editedLimbNode = new(newValue, editedLimbNode.JointDefinition, editedLimbNode.RecursiveLimit, editedLimbNode.NeuronDefinitions, editedLimbNode.Connections);
            // RegisterNodeChanges(editedLimbNode);
        };
        InitialiseVector3Fields(dimensions, LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize, editNodeDimensions);
    }

    private void PopulateDimensions()
    {
        VisualElement dimensions = nodeEditorElement.Q("dimensions");
        PopulateVector3Fields(dimensions, editedLimbNode.Dimensions);
    }

    private void InitialiseJoint()
    {
        VisualElement joint = nodeEditorElement.Q("joint");
        void editJointDefinition(JointDefinition editedJointDefinition)
        {
            editedLimbNode = new(editedLimbNode.Dimensions, editedJointDefinition, editedLimbNode.RecursiveLimit, editedLimbNode.NeuronDefinitions, editedLimbNode.Connections);
        }

        EnumField type = joint.Q<EnumField>("type");
        void editJointType()
        {
            JointDefinition editedJointDefinition = new((JointType)type.value, editedLimbNode.JointDefinition.AxisDefinitions);
            editJointDefinition(editedJointDefinition);
        }
        type.RegisterValueChangedCallback((e) => editJointType());

        VisualElement[] axes =
        {
            joint.Q<VisualElement>("primary-axis"),
            joint.Q<VisualElement>("secondary-axis"),
            joint.Q<VisualElement>("tertiary-axis")
        };

        void editJointAxisDefinition(JointAxisDefinition editedJointAxisDefinition, int index)
        {
            JointDefinition editedJointDefinition = new(editedLimbNode.JointDefinition.Type, editedLimbNode.JointDefinition.AxisDefinitions.Select((a, i) =>
            {
                return i == index ? editedJointAxisDefinition : a;
            }).ToList());
            editJointDefinition(editedJointDefinition);
        }

        void editJointLimit(int index, float newValue)
        {
            JointAxisDefinition editedJointAxisDefinition = new(newValue, editedLimbNode.JointDefinition.AxisDefinitions[index].InputDefinition);
            editJointAxisDefinition(editedJointAxisDefinition, index);
        }

        for (int i = 0; i < axes.Length; i++)
        {
            Slider limit = axes[i].Q<Slider>("limit");
            limit.lowValue = JointDefinitionParameters.MinAngle;
            limit.highValue = JointDefinitionParameters.MaxAngle;
            int index = i;
            limit.RegisterCallback<MouseCaptureOutEvent>((e) => editJointLimit(index, limit.value));
        }
    }

    private void PopulateJoint()
    {
        VisualElement joint = nodeEditorElement.Q("joint");
        EnumField type = joint.Q<EnumField>("type");
        type.value = editedLimbNode.JointDefinition.Type;

        VisualElement[] axes =
        {
            joint.Q<VisualElement>("primary-axis"),
            joint.Q<VisualElement>("secondary-axis"),
            joint.Q<VisualElement>("tertiary-axis")
        };

        for (int i = 0; i < axes.Length; i++)
        {
            Slider limit = axes[i].Q<Slider>("limit");
            limit.value = editedLimbNode.JointDefinition.AxisDefinitions[i].Limit;
        }
    }

    private void InitialiseConnections()
    {
        VisualElement connections = nodeEditorElement.Q("connections");
        VisualElement connectionList = connections.Q<VisualElement>("connection-list");
        Button addConnectionButton = connections.Q<Button>("add");

        void addConnection()
        {
            LimbConnection defaultConnection = new(
                limbNodeIndex,
                0,
                Vector2.zero,
                Vector3.zero,
                Vector3.one,
                false,
                false,
                false,
                false
            );
            editedLimbNode = new(editedLimbNode.Dimensions, editedLimbNode.JointDefinition, editedLimbNode.RecursiveLimit, editedLimbNode.NeuronDefinitions, editedLimbNode.Connections.Concat(new List<LimbConnection>() { defaultConnection }).ToList());
            PopulateConnections();
        }

        addConnectionButton.RegisterCallback<MouseCaptureOutEvent>((e) => addConnection());
    }

    private void PopulateConnections()
    {
        VisualElement connections = nodeEditorElement.Q("connections");
        VisualElement connectionList = connections.Q<VisualElement>("connection-list");
        Button addConnectionButton = connections.Q<Button>("add");
        addConnectionButton.style.display = editedLimbNode.Connections.Count < LimbNodeParameters.MaxLimbConnections ? DisplayStyle.Flex : DisplayStyle.None;

        connectionList.Clear();
        for (int i = 0; i < editedLimbNode.Connections.Count; i++)
        {
            int connectionIndex = i;
            CreateConnectionListItem(editConnectionTemplate, connectionList, connectionIndex);
        }
    }

    private void CreateConnectionListItem(VisualTreeAsset connectionTemplate, VisualElement parent, int connectionIndex)
    {
        VisualElement connection = connectionTemplate.Instantiate();
        parent.Add(connection);

        void editConnection(LimbConnection editedConnection)
        {
            editedLimbNode = new(editedLimbNode.Dimensions, editedLimbNode.JointDefinition, editedLimbNode.RecursiveLimit, editedLimbNode.NeuronDefinitions, editedLimbNode.Connections.Select((c, i) => i == connectionIndex ? editedConnection : c).ToList());
        }

        void removeConnection()
        {
            editedLimbNode = new(editedLimbNode.Dimensions, editedLimbNode.JointDefinition, editedLimbNode.RecursiveLimit, editedLimbNode.NeuronDefinitions, editedLimbNode.Connections.Where((c, i) => i != connectionIndex).ToList());
            PopulateConnections();
        }

        Label connectionName = connection.Q<Label>();
        connectionName.text = "Connection " + (connectionIndex + 1);
        Button deleteButton = connection.Q<Button>();
        deleteButton.clicked += removeConnection;

        DropdownField childNodeId = connection.Q<DropdownField>("child-node-id");
        childNodeId.choices = Enumerable.Range(0, limbNodeCount).Select((i) => "Node " + (i + 1).ToString() + (i == limbNodeIndex ? " (self)" : "")).ToList();
        childNodeId.index = editedLimbNode.Connections[connectionIndex].ChildNodeId;
        void editChildNodeId()
        {
            LimbNode limbNode = editedLimbNode;
            LimbConnection editedLimbConnection = new(
                childNodeId.index,
                editedLimbNode.Connections[connectionIndex].ParentFace,
                editedLimbNode.Connections[connectionIndex].Position,
                editedLimbNode.Connections[connectionIndex].Orientation,
                editedLimbNode.Connections[connectionIndex].Scale,
                editedLimbNode.Connections[connectionIndex].ReflectionX,
                editedLimbNode.Connections[connectionIndex].ReflectionY,
                editedLimbNode.Connections[connectionIndex].ReflectionZ,
                editedLimbNode.Connections[connectionIndex].TerminalOnly
            );
            editConnection(editedLimbConnection);
        }
        childNodeId.RegisterValueChangedCallback((e) => editChildNodeId());

        SliderInt parentFace = connection.Q<SliderInt>("parent-face");
        parentFace.lowValue = 1; // 1-based indexing for UI.
        parentFace.highValue = 6;
        parentFace.value = editedLimbNode.Connections[connectionIndex].ParentFace + 1;
        void editParentFace()
        {
            LimbConnection editedLimbConnection = new(
                editedLimbNode.Connections[connectionIndex].ChildNodeId,
                parentFace.value - 1,
                editedLimbNode.Connections[connectionIndex].Position,
                editedLimbNode.Connections[connectionIndex].Orientation,
                editedLimbNode.Connections[connectionIndex].Scale,
                editedLimbNode.Connections[connectionIndex].ReflectionX,
                editedLimbNode.Connections[connectionIndex].ReflectionY,
                editedLimbNode.Connections[connectionIndex].ReflectionZ,
                editedLimbNode.Connections[connectionIndex].TerminalOnly
            );
            editConnection(editedLimbConnection);
        };
        parentFace.RegisterCallback<MouseCaptureOutEvent>((e) => editParentFace());

        VisualElement position = connection.Q("position");
        void editPosition(Vector2 newValue)
        {
            LimbConnection editedLimbConnection = new(
                editedLimbNode.Connections[connectionIndex].ChildNodeId,
                editedLimbNode.Connections[connectionIndex].ParentFace,
                newValue,
                editedLimbNode.Connections[connectionIndex].Orientation,
                editedLimbNode.Connections[connectionIndex].Scale,
                editedLimbNode.Connections[connectionIndex].ReflectionX,
                editedLimbNode.Connections[connectionIndex].ReflectionY,
                editedLimbNode.Connections[connectionIndex].ReflectionZ,
                editedLimbNode.Connections[connectionIndex].TerminalOnly
            );
            editConnection(editedLimbConnection);
        };
        InitialiseVector2Fields(position, -1f, 1f, editPosition);
        PopulateVector2Fields(position, editedLimbNode.Connections[connectionIndex].Position);

        VisualElement orientation = connection.Q("orientation");
        void editOrientation(Vector3 newValue)
        {
            LimbConnection editedLimbConnection = new(
                editedLimbNode.Connections[connectionIndex].ChildNodeId,
                editedLimbNode.Connections[connectionIndex].ParentFace,
                editedLimbNode.Connections[connectionIndex].Position,
                newValue,
                editedLimbNode.Connections[connectionIndex].Scale,
                editedLimbNode.Connections[connectionIndex].ReflectionX,
                editedLimbNode.Connections[connectionIndex].ReflectionY,
                editedLimbNode.Connections[connectionIndex].ReflectionZ,
                editedLimbNode.Connections[connectionIndex].TerminalOnly
            );
            editConnection(editedLimbConnection);
        };
        InitialiseVector3Fields(orientation, LimbConnectionParameters.MinAngle, LimbConnectionParameters.MaxAngle, editOrientation);
        PopulateVector3Fields(orientation, editedLimbNode.Connections[connectionIndex].Orientation);

        VisualElement scale = connection.Q("scale");
        void editScale(Vector3 newValue)
        {
            LimbConnection editedLimbConnection = new(
                editedLimbNode.Connections[connectionIndex].ChildNodeId,
                editedLimbNode.Connections[connectionIndex].ParentFace,
                editedLimbNode.Connections[connectionIndex].Position,
                editedLimbNode.Connections[connectionIndex].Orientation,
                newValue,
                editedLimbNode.Connections[connectionIndex].ReflectionX,
                editedLimbNode.Connections[connectionIndex].ReflectionY,
                editedLimbNode.Connections[connectionIndex].ReflectionZ,
                editedLimbNode.Connections[connectionIndex].TerminalOnly
            );
            editConnection(editedLimbConnection);
        };
        InitialiseVector3Fields(scale, LimbConnectionParameters.MinScale, LimbConnectionParameters.MaxScale, editScale);
        PopulateVector3Fields(scale, editedLimbNode.Connections[connectionIndex].Scale);

        VisualElement reflection = connection.Q("reflection");
        Toggle reflectionX = reflection.Q<Toggle>("x");
        Toggle reflectionY = reflection.Q<Toggle>("y");
        Toggle reflectionZ = reflection.Q<Toggle>("z");
        reflectionX.value = editedLimbNode.Connections[connectionIndex].ReflectionX;
        reflectionY.value = editedLimbNode.Connections[connectionIndex].ReflectionY;
        reflectionZ.value = editedLimbNode.Connections[connectionIndex].ReflectionZ;
        void editReflections()
        {
            LimbConnection editedLimbConnection = new(
                editedLimbNode.Connections[connectionIndex].ChildNodeId,
                editedLimbNode.Connections[connectionIndex].ParentFace,
                editedLimbNode.Connections[connectionIndex].Position,
                editedLimbNode.Connections[connectionIndex].Orientation,
                editedLimbNode.Connections[connectionIndex].Scale,
                reflectionX.value,
                reflectionY.value,
                reflectionZ.value,
                editedLimbNode.Connections[connectionIndex].TerminalOnly
            );
            editConnection(editedLimbConnection);
        }
        reflectionX.RegisterValueChangedCallback((e) => editReflections());
        reflectionY.RegisterValueChangedCallback((e) => editReflections());
        reflectionZ.RegisterValueChangedCallback((e) => editReflections());

        Toggle terminalOnly = connection.Q<Toggle>("terminal-only");
        terminalOnly.value = editedLimbNode.Connections[connectionIndex].TerminalOnly;
        void editTerminalOnly()
        {
            LimbConnection editedLimbConnection = new(
                editedLimbNode.Connections[connectionIndex].ChildNodeId,
                editedLimbNode.Connections[connectionIndex].ParentFace,
                editedLimbNode.Connections[connectionIndex].Position,
                editedLimbNode.Connections[connectionIndex].Orientation,
                editedLimbNode.Connections[connectionIndex].Scale,
                editedLimbNode.Connections[connectionIndex].ReflectionX,
                editedLimbNode.Connections[connectionIndex].ReflectionY,
                editedLimbNode.Connections[connectionIndex].ReflectionZ,
                terminalOnly.value
            );
            editConnection(editedLimbConnection);
        }
        terminalOnly.RegisterValueChangedCallback((e) => editTerminalOnly());
    }

    private void InitialiseRecursiveLimit()
    {
        VisualElement recursiveLimitContainer = nodeEditorElement.Q("recursive-limit");
        SliderInt recursiveLimit = recursiveLimitContainer.Q<SliderInt>();
        recursiveLimit.lowValue = 0;
        recursiveLimit.highValue = LimbNodeParameters.MaxRecursiveLimit;
        void editRecursiveLimit()
        {
            editedLimbNode = new(editedLimbNode.Dimensions, editedLimbNode.JointDefinition, recursiveLimit.value, editedLimbNode.NeuronDefinitions, editedLimbNode.Connections);
        }
        recursiveLimit.RegisterCallback<MouseCaptureOutEvent>((e) => editRecursiveLimit());
    }

    private void PopulateRecursiveLimit()
    {
        VisualElement recursiveLimitContainer = nodeEditorElement.Q("recursive-limit");
        SliderInt recursiveLimit = recursiveLimitContainer.Q<SliderInt>();
        recursiveLimit.value = editedLimbNode.RecursiveLimit;
    }

    private void InitialiseVector2Fields(VisualElement container, float min, float max, Action<Vector2> editCallback)
    {
        Slider x = container.Q<Slider>("x");
        Slider y = container.Q<Slider>("y");
        x.lowValue = min;
        x.highValue = max;
        y.lowValue = min;
        y.highValue = max;
        x.RegisterCallback<MouseCaptureOutEvent>((e) => editCallback(new Vector2(x.value, y.value)));
        y.RegisterCallback<MouseCaptureOutEvent>((e) => editCallback(new Vector2(x.value, y.value)));
    }

    private void PopulateVector2Fields(VisualElement container, Vector2 value)
    {
        Slider x = container.Q<Slider>("x");
        Slider y = container.Q<Slider>("y");
        x.value = value.x;
        y.value = value.y;
    }

    private void InitialiseVector3Fields(VisualElement container, float min, float max, Action<Vector3> editCallback)
    {
        Slider x = container.Q<Slider>("x");
        Slider y = container.Q<Slider>("y");
        Slider z = container.Q<Slider>("z");
        x.lowValue = min;
        x.highValue = max;
        y.lowValue = min;
        y.highValue = max;
        z.lowValue = min;
        z.highValue = max;
        x.RegisterCallback<MouseCaptureOutEvent>((e) => editCallback(new Vector3(x.value, y.value, z.value)));
        y.RegisterCallback<MouseCaptureOutEvent>((e) => editCallback(new Vector3(x.value, y.value, z.value)));
        z.RegisterCallback<MouseCaptureOutEvent>((e) => editCallback(new Vector3(x.value, y.value, z.value)));
    }

    private void PopulateVector3Fields(VisualElement container, Vector3 value)
    {
        Slider x = container.Q<Slider>("x");
        Slider y = container.Q<Slider>("y");
        Slider z = container.Q<Slider>("z");
        x.value = value.x;
        y.value = value.y;
        z.value = value.z;
    }
}
