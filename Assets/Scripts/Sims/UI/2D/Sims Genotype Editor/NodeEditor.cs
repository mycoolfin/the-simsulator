using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.UI.TwoD
{
    using TheSimsulator.Sims.Genotype;

    public class NodeEditor
    {
        private readonly VisualElement nodeEditorContainer;
        private readonly VisualElement nodeEditorElement;
        private Node editedNode;
        private int nodeIndex;
        private readonly Action<Node, int> applyChanges;

        public NodeEditor(
            VisualElement nodeEditorContainer,
            VisualElement nodeEditorElement,
            Action<Node, int> applyChanges,
            Action<int> deleteNode
        )
        {
            this.nodeEditorContainer = nodeEditorContainer;
            this.nodeEditorElement = nodeEditorElement;
            this.applyChanges = applyChanges;

            nodeEditorContainer.style.display = DisplayStyle.None;

            nodeEditorElement.Q<Button>("delete-node").clicked += () => deleteNode(nodeIndex);

            InitialiseDimensions();
            InitialiseJoint();
            InitialiseColor();
            InitialiseRecursiveLimit();
        }

        public void StartEditing(Node node, int nodeIndex, int nodeCount)
        {
            nodeEditorContainer.style.display = DisplayStyle.Flex;
            nodeEditorElement.Q<Label>("node-name").text = $"Node {nodeIndex}";
            nodeEditorElement.Q<Button>("delete-node").style.display = nodeCount > 1 ? DisplayStyle.Flex : DisplayStyle.None;

            // Copy the node to edit (struct copy preserves Gid).
            editedNode = node;
            this.nodeIndex = nodeIndex;

            PopulateDimensions();
            PopulateColor();
            PopulateJoint();
            PopulateRecursiveLimit();
        }

        public void StopEditing()
        {
            nodeEditorContainer.style.display = DisplayStyle.None;
        }

        private void InitialiseDimensions()
        {
            VisualElement dimensions = nodeEditorElement.Q("dimensions");

            Slider x = dimensions.Q<Slider>("x");
            Slider y = dimensions.Q<Slider>("y");
            Slider z = dimensions.Q<Slider>("z");

            // Configure ranges.
            x.lowValue = y.lowValue = z.lowValue = Node.MIN_DIMENSION;
            x.highValue = y.highValue = z.highValue = Node.MAX_DIMENSION;

            // Update on value change with debounce to avoid expensive phenotype rebuilds during dragging.
            IVisualElementScheduledItem dimensionsCommit = null;
            void DebouncedUpdateDimensions(ChangeEvent<float> evt)
            {
                dimensionsCommit?.Pause();
                dimensionsCommit = nodeEditorElement.schedule.Execute(() =>
                {
                    var numericValue = new System.Numerics.Vector3(x.value, y.value, z.value);
                    editedNode.Dimensions = numericValue;
                    applyChanges?.Invoke(editedNode, nodeIndex);
                }).StartingIn(200);
            }

            x.RegisterValueChangedCallback(DebouncedUpdateDimensions);
            y.RegisterValueChangedCallback(DebouncedUpdateDimensions);
            z.RegisterValueChangedCallback(DebouncedUpdateDimensions);
        }

        private void PopulateDimensions()
        {
            VisualElement dimensions = nodeEditorElement.Q("dimensions");
            var dims = editedNode.Dimensions;

            dimensions.Q<Slider>("x").SetValueWithoutNotify(dims.X);
            dimensions.Q<Slider>("y").SetValueWithoutNotify(dims.Y);
            dimensions.Q<Slider>("z").SetValueWithoutNotify(dims.Z);
        }

        private void InitialiseJoint()
        {
            VisualElement joint = nodeEditorElement.Q("joint");

            EnumField type = joint.Q<EnumField>("type");
            type.RegisterValueChangedCallback(evt =>
            {
                var newJointDef = new JointDefinition(
                    (JointType)evt.newValue,
                    editedNode.JointDefinition.AngleLimits,
                    editedNode.JointDefinition.PrimaryAxisInputs,
                    editedNode.JointDefinition.SecondaryAxisInputs,
                    editedNode.JointDefinition.TertiaryAxisInputs
                );
                editedNode = editedNode.CopyWithSameGid(newJointDef);
                applyChanges?.Invoke(editedNode, nodeIndex);
            });

            // Angle limits for each axis.
            VisualElement[] axes =
            {
                joint.Q<VisualElement>("primary-axis"),
                joint.Q<VisualElement>("secondary-axis"),
                joint.Q<VisualElement>("tertiary-axis")
            };

            for (int i = 0; i < axes.Length; i++)
            {
                Slider limit = axes[i].Q<Slider>("limit");
                limit.lowValue = JointDefinition.MIN_ANGLE_LIMIT * Mathf.Rad2Deg;
                limit.highValue = JointDefinition.MAX_ANGLE_LIMIT * Mathf.Rad2Deg;

                int axisIndex = i;
                IVisualElementScheduledItem jointCommit = null;
                // Update with debounce to avoid expensive phenotype rebuilds during dragging.
                limit.RegisterValueChangedCallback(evt =>
                {
                    jointCommit?.Pause();
                    jointCommit = nodeEditorElement.schedule.Execute(() =>
                    {
                        var limits = editedNode.JointDefinition.AngleLimits;
                        float radianValue = limit.value * Mathf.Deg2Rad;
                        var newLimits = axisIndex switch
                        {
                            0 => new System.Numerics.Vector3(radianValue, limits.Y, limits.Z),
                            1 => new System.Numerics.Vector3(limits.X, radianValue, limits.Z),
                            2 => new System.Numerics.Vector3(limits.X, limits.Y, radianValue),
                            _ => limits
                        };

                        var newJointDef = new JointDefinition(
                            editedNode.JointDefinition.JointType,
                            newLimits,
                            editedNode.JointDefinition.PrimaryAxisInputs,
                            editedNode.JointDefinition.SecondaryAxisInputs,
                            editedNode.JointDefinition.TertiaryAxisInputs
                        );
                        editedNode = editedNode.CopyWithSameGid(newJointDef);
                        applyChanges?.Invoke(editedNode, nodeIndex);
                    }).StartingIn(200);
                });
            }
        }

        private void PopulateJoint()
        {
            VisualElement joint = nodeEditorElement.Q("joint");

            EnumField type = joint.Q<EnumField>("type");
            type.SetValueWithoutNotify(editedNode.JointDefinition.JointType);

            VisualElement[] axes =
            {
                joint.Q<VisualElement>("primary-axis"),
                joint.Q<VisualElement>("secondary-axis"),
                joint.Q<VisualElement>("tertiary-axis")
            };

            var limits = editedNode.JointDefinition.AngleLimits;
            axes[0].Q<Slider>("limit").SetValueWithoutNotify(limits.X * Mathf.Rad2Deg);
            axes[1].Q<Slider>("limit").SetValueWithoutNotify(limits.Y * Mathf.Rad2Deg);
            axes[2].Q<Slider>("limit").SetValueWithoutNotify(limits.Z * Mathf.Rad2Deg);
        }

        private void InitialiseColor()
        {
            VisualElement colorContainer = nodeEditorElement.Q("color");

            Slider hue = colorContainer.Q<Slider>("hue");
            Slider saturation = colorContainer.Q<Slider>("saturation");
            Slider value = colorContainer.Q<Slider>("value");
            Slider recursiveOffsetH = colorContainer.Q<Slider>("recursive-offset-h");
            Slider recursiveOffsetS = colorContainer.Q<Slider>("recursive-offset-s");
            Slider recursiveOffsetV = colorContainer.Q<Slider>("recursive-offset-v");
            VisualElement colorPreview = colorContainer.Q<VisualElement>("color-preview");

            // Configure ranges.
            hue.lowValue = saturation.lowValue = value.lowValue = 0f;
            hue.highValue = saturation.highValue = value.highValue = 1f;
            recursiveOffsetH.lowValue = recursiveOffsetS.lowValue = recursiveOffsetV.lowValue = NodeColor.MIN_COLOR_RECURSIVE_OFFSET;
            recursiveOffsetH.highValue = recursiveOffsetS.highValue = recursiveOffsetV.highValue = NodeColor.MAX_COLOR_RECURSIVE_OFFSET;

            // Update color with debounce - preview updates immediately, commit after 200ms of no changes.
            IVisualElementScheduledItem colorCommit = null;
            void DebouncedUpdateColor(ChangeEvent<float> evt)
            {
                // Update preview immediately for visual feedback.
                NodeColor previewColor = new(
                    hue.value, saturation.value, value.value,
                    recursiveOffsetH.value, recursiveOffsetS.value, recursiveOffsetV.value
                );
                UpdateColorPreview(colorPreview, previewColor);

                // Debounce the actual commit.
                colorCommit?.Pause();
                colorCommit = nodeEditorElement.schedule.Execute(() =>
                {
                    var newColor = new NodeColor(
                        hue.value, saturation.value, value.value,
                        recursiveOffsetH.value, recursiveOffsetS.value, recursiveOffsetV.value
                    );
                    editedNode.Color = newColor;
                    applyChanges?.Invoke(editedNode, nodeIndex);
                }).StartingIn(200);
            }

            hue.RegisterValueChangedCallback(DebouncedUpdateColor);
            saturation.RegisterValueChangedCallback(DebouncedUpdateColor);
            value.RegisterValueChangedCallback(DebouncedUpdateColor);
            recursiveOffsetH.RegisterValueChangedCallback(DebouncedUpdateColor);
            recursiveOffsetS.RegisterValueChangedCallback(DebouncedUpdateColor);
            recursiveOffsetV.RegisterValueChangedCallback(DebouncedUpdateColor);
        }

        private void PopulateColor()
        {
            VisualElement colorContainer = nodeEditorElement.Q("color");
            var color = editedNode.Color;

            colorContainer.Q<Slider>("hue").SetValueWithoutNotify(color.H);
            colorContainer.Q<Slider>("saturation").SetValueWithoutNotify(color.S);
            colorContainer.Q<Slider>("value").SetValueWithoutNotify(color.V);
            colorContainer.Q<Slider>("recursive-offset-h").SetValueWithoutNotify(color.RecursiveOffsetH);
            colorContainer.Q<Slider>("recursive-offset-s").SetValueWithoutNotify(color.RecursiveOffsetS);
            colorContainer.Q<Slider>("recursive-offset-v").SetValueWithoutNotify(color.RecursiveOffsetV);

            UpdateColorPreview(colorContainer.Q<VisualElement>("color-preview"), color);
        }

        private void UpdateColorPreview(VisualElement preview, NodeColor color)
        {
            // Convert HSV to RGB for preview (using base color without recursive offset)
            var rgbColor = Color.HSVToRGB(color.H, color.S, color.V);
            preview.style.backgroundColor = new StyleColor(rgbColor);
        }

        private void InitialiseRecursiveLimit()
        {
            VisualElement recursiveLimitContainer = nodeEditorElement.Q("recursive-limit");
            SliderInt recursiveLimit = recursiveLimitContainer.Q<SliderInt>();
            recursiveLimit.lowValue = Node.MIN_RECURSIVE_LIMIT;
            recursiveLimit.highValue = Node.MAX_RECURSIVE_LIMIT;

            // Update with debounce to avoid expensive phenotype rebuilds during dragging
            IVisualElementScheduledItem recursiveLimitCommit = null;
            recursiveLimit.RegisterValueChangedCallback(evt =>
            {
                recursiveLimitCommit?.Pause();
                recursiveLimitCommit = nodeEditorElement.schedule.Execute(() =>
                {
                    editedNode.RecursiveLimit = recursiveLimit.value;
                    applyChanges?.Invoke(editedNode, nodeIndex);
                }).StartingIn(200);
            });
        }

        private void PopulateRecursiveLimit()
        {
            VisualElement recursiveLimitContainer = nodeEditorElement.Q("recursive-limit");
            SliderInt recursiveLimit = recursiveLimitContainer.Q<SliderInt>();
            recursiveLimit.SetValueWithoutNotify(editedNode.RecursiveLimit);
        }
    }
}
