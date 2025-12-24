using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.UI.TwoD
{
    using TheSimsulator.Sims.Genotype;

    public class ConnectionEditor
    {
        private readonly VisualElement connectionEditorContainer;
        private readonly VisualElement connectionEditorElement;
        private Connection editedConnection;
        private int connectionIndex;
        private List<Node> availableNodes;
        private readonly Action<Connection, int> applyChanges;

        public ConnectionEditor(
            VisualElement connectionEditorContainer,
            VisualElement connectionEditorElement,
            Action<Connection, int> applyChanges,
            Action<int> deleteConnection
        )
        {
            this.connectionEditorContainer = connectionEditorContainer;
            this.connectionEditorElement = connectionEditorElement;
            this.applyChanges = applyChanges;

            connectionEditorContainer.style.display = DisplayStyle.None;

            connectionEditorElement.Q<Button>("delete-connection").clicked += () => deleteConnection(connectionIndex);

            InitialiseParentNodeDropdown();
            InitialiseParentFace();
            InitialiseChildNodeDropdown();
            InitialisePosition();
            InitialiseOrientation();
            InitialiseScale();
            InitialiseReflection();
            InitialiseTerminalOnly();
        }

        public void StartEditing(Connection connection, int connectionIndex, List<Node> nodes)
        {
            connectionEditorContainer.style.display = DisplayStyle.Flex;
            connectionEditorElement.Q<Label>().text = $"Connection {connectionIndex}";

            editedConnection = connection;
            this.connectionIndex = connectionIndex;
            availableNodes = nodes;

            PopulateParentNodeDropdown();
            PopulateParentFace();
            PopulateChildNodeDropdown();
            PopulatePosition();
            PopulateOrientation();
            PopulateScale();
            PopulateReflection();
            PopulateTerminalOnly();
        }

        public void StopEditing()
        {
            connectionEditorContainer.style.display = DisplayStyle.None;
        }

        private void InitialiseParentFace()
        {
            SliderInt parentFace = connectionEditorElement.Q<SliderInt>("parent-face");
            parentFace.lowValue = Connection.MIN_PARENT_FACE;
            parentFace.highValue = Connection.MAX_PARENT_FACE;

            IVisualElementScheduledItem parentFaceCommit = null;
            parentFace.RegisterValueChangedCallback(evt =>
            {
                parentFaceCommit?.Pause();
                parentFaceCommit = connectionEditorElement.schedule.Execute(() =>
                {
                    editedConnection.ParentFace = parentFace.value;
                    applyChanges?.Invoke(editedConnection, connectionIndex);
                }).StartingIn(200);
            });
        }

        private void PopulateParentFace()
        {
            SliderInt parentFace = connectionEditorElement.Q<SliderInt>("parent-face");
            parentFace.SetValueWithoutNotify(editedConnection.ParentFace);
        }

        private void InitialiseParentNodeDropdown()
        {
            DropdownField parentNodeDropdown = connectionEditorElement.Q<DropdownField>("parent-node-id");

            parentNodeDropdown.RegisterValueChangedCallback(evt =>
            {
                int selectedIndex = parentNodeDropdown.index;
                if (selectedIndex >= 0 && selectedIndex < availableNodes.Count)
                {
                    editedConnection.ParentNodeGid = availableNodes[selectedIndex].Gid;
                    applyChanges?.Invoke(editedConnection, connectionIndex);
                }
            });
        }

        private void PopulateParentNodeDropdown()
        {
            DropdownField parentNodeDropdown = connectionEditorElement.Q<DropdownField>("parent-node-id");

            parentNodeDropdown.choices = availableNodes
                .Select((node, index) => $"Node {index}")
                .ToList();

            int selectedIndex = availableNodes.FindIndex(n => n.Gid == editedConnection.ParentNodeGid);
            parentNodeDropdown.SetValueWithoutNotify(parentNodeDropdown.choices[selectedIndex >= 0 ? selectedIndex : 0]);
        }

        private void InitialiseChildNodeDropdown()
        {
            DropdownField childNodeDropdown = connectionEditorElement.Q<DropdownField>("child-node-id");

            childNodeDropdown.RegisterValueChangedCallback(evt =>
            {
                int selectedIndex = childNodeDropdown.index;
                if (selectedIndex >= 0 && selectedIndex < availableNodes.Count)
                {
                    editedConnection.ChildNodeGid = availableNodes[selectedIndex].Gid;
                    applyChanges?.Invoke(editedConnection, connectionIndex);
                }
            });
        }

        private void PopulateChildNodeDropdown()
        {
            DropdownField childNodeDropdown = connectionEditorElement.Q<DropdownField>("child-node-id");

            childNodeDropdown.choices = availableNodes
                .Select((node, index) => $"Node {index}")
                .ToList();

            int selectedIndex = availableNodes.FindIndex(n => n.Gid == editedConnection.ChildNodeGid);
            childNodeDropdown.SetValueWithoutNotify(childNodeDropdown.choices[selectedIndex >= 0 ? selectedIndex : 0]);
        }

        private void InitialisePosition()
        {
            VisualElement position = connectionEditorElement.Q("position");

            Slider x = position.Q<Slider>("x");
            Slider y = position.Q<Slider>("y");

            x.lowValue = y.lowValue = Connection.MIN_POSITION;
            x.highValue = y.highValue = Connection.MAX_POSITION;

            IVisualElementScheduledItem positionCommit = null;
            void DebouncedUpdatePosition(ChangeEvent<float> evt)
            {
                positionCommit?.Pause();
                positionCommit = connectionEditorElement.schedule.Execute(() =>
                {
                    editedConnection.Position = new System.Numerics.Vector2(x.value, y.value);
                    applyChanges?.Invoke(editedConnection, connectionIndex);
                }).StartingIn(200);
            }

            x.RegisterValueChangedCallback(DebouncedUpdatePosition);
            y.RegisterValueChangedCallback(DebouncedUpdatePosition);
        }

        private void PopulatePosition()
        {
            VisualElement position = connectionEditorElement.Q("position");
            var pos = editedConnection.Position;

            position.Q<Slider>("x").SetValueWithoutNotify(pos.X);
            position.Q<Slider>("y").SetValueWithoutNotify(pos.Y);
        }

        private void InitialiseOrientation()
        {
            VisualElement orientation = connectionEditorElement.Q("orientation");

            Slider x = orientation.Q<Slider>("x");
            Slider y = orientation.Q<Slider>("y");
            Slider z = orientation.Q<Slider>("z");

            x.lowValue = y.lowValue = z.lowValue = Connection.MIN_ORIENTATION;
            x.highValue = y.highValue = z.highValue = Connection.MAX_ORIENTATION;

            IVisualElementScheduledItem orientationCommit = null;
            void DebouncedUpdateOrientation(ChangeEvent<float> evt)
            {
                orientationCommit?.Pause();
                orientationCommit = connectionEditorElement.schedule.Execute(() =>
                {
                    editedConnection.Orientation = new System.Numerics.Vector3(x.value, y.value, z.value);
                    applyChanges?.Invoke(editedConnection, connectionIndex);
                }).StartingIn(200);
            }

            x.RegisterValueChangedCallback(DebouncedUpdateOrientation);
            y.RegisterValueChangedCallback(DebouncedUpdateOrientation);
            z.RegisterValueChangedCallback(DebouncedUpdateOrientation);
        }

        private void PopulateOrientation()
        {
            VisualElement orientation = connectionEditorElement.Q("orientation");
            var ori = editedConnection.Orientation;

            orientation.Q<Slider>("x").SetValueWithoutNotify(ori.X);
            orientation.Q<Slider>("y").SetValueWithoutNotify(ori.Y);
            orientation.Q<Slider>("z").SetValueWithoutNotify(ori.Z);
        }

        private void InitialiseScale()
        {
            VisualElement scale = connectionEditorElement.Q("scale");

            Slider x = scale.Q<Slider>("x");
            Slider y = scale.Q<Slider>("y");
            Slider z = scale.Q<Slider>("z");

            x.lowValue = y.lowValue = z.lowValue = Connection.MIN_SCALE;
            x.highValue = y.highValue = z.highValue = Connection.MAX_SCALE;

            IVisualElementScheduledItem scaleCommit = null;
            void DebouncedUpdateScale(ChangeEvent<float> evt)
            {
                scaleCommit?.Pause();
                scaleCommit = connectionEditorElement.schedule.Execute(() =>
                {
                    editedConnection.Scale = new System.Numerics.Vector3(x.value, y.value, z.value);
                    applyChanges?.Invoke(editedConnection, connectionIndex);
                }).StartingIn(200);
            }

            x.RegisterValueChangedCallback(DebouncedUpdateScale);
            y.RegisterValueChangedCallback(DebouncedUpdateScale);
            z.RegisterValueChangedCallback(DebouncedUpdateScale);
        }

        private void PopulateScale()
        {
            VisualElement scale = connectionEditorElement.Q("scale");
            var scl = editedConnection.Scale;

            scale.Q<Slider>("x").SetValueWithoutNotify(scl.X);
            scale.Q<Slider>("y").SetValueWithoutNotify(scl.Y);
            scale.Q<Slider>("z").SetValueWithoutNotify(scl.Z);
        }

        private void InitialiseReflection()
        {
            VisualElement reflection = connectionEditorElement.Q("reflection");

            Toggle reflectionX = reflection.Q<Toggle>("x");
            Toggle reflectionY = reflection.Q<Toggle>("y");
            Toggle reflectionZ = reflection.Q<Toggle>("z");

            reflectionX.RegisterValueChangedCallback(evt => { editedConnection.ReflectionX = evt.newValue; applyChanges?.Invoke(editedConnection, connectionIndex); });
            reflectionY.RegisterValueChangedCallback(evt => { editedConnection.ReflectionY = evt.newValue; applyChanges?.Invoke(editedConnection, connectionIndex); });
            reflectionZ.RegisterValueChangedCallback(evt => { editedConnection.ReflectionZ = evt.newValue; applyChanges?.Invoke(editedConnection, connectionIndex); });
        }

        private void PopulateReflection()
        {
            VisualElement reflection = connectionEditorElement.Q("reflection");

            reflection.Q<Toggle>("x").SetValueWithoutNotify(editedConnection.ReflectionX);
            reflection.Q<Toggle>("y").SetValueWithoutNotify(editedConnection.ReflectionY);
            reflection.Q<Toggle>("z").SetValueWithoutNotify(editedConnection.ReflectionZ);
        }

        private void InitialiseTerminalOnly()
        {
            Toggle terminalOnly = connectionEditorElement.Q<Toggle>("terminal-only");
            terminalOnly.RegisterValueChangedCallback(evt => { editedConnection.TerminalOnly = evt.newValue; applyChanges?.Invoke(editedConnection, connectionIndex); });
        }

        private void PopulateTerminalOnly()
        {
            Toggle terminalOnly = connectionEditorElement.Q<Toggle>("terminal-only");
            terminalOnly.SetValueWithoutNotify(editedConnection.TerminalOnly);
        }
    }
}
