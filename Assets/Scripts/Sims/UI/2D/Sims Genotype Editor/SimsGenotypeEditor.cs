using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.UI.TwoD
{
    using TheSimsulator.Sims.Genotype;
    using Core.UI.TwoD;

    public class SimsGenotypeEditor : GenotypeEditor<SimsGenotype>
    {
        private SimsGenotype currentGenotype;
        private List<Node> nodes;
        private List<Connection> connections;

        private readonly VisualElement emptyState;
        private readonly VisualElement nodeEditorInstance;
        private readonly VisualElement connectionEditorInstance;
        private readonly Button addNodeBtn;
        private readonly Button addConnectionBtn;

        private readonly GenotypeGraph genotypeGraph;
        private NodeEditor nodeEditor;
        private ConnectionEditor connectionEditor;

        public event Action<ulong> OnNodeSelectedByUser; // Event for when user selects a node (fires with Node GID).

        public SimsGenotypeEditor(UIDocument uiDocument, Action<SimsGenotype> onGenotypeChanged) : base(uiDocument, onGenotypeChanged)
        {
            nodes = new();
            connections = new();

            VisualElement graphCanvas = root.Q<VisualElement>("graph-canvas");
            VisualElement connectionOverlay = root.Q<VisualElement>("connection-overlay");
            emptyState = root.Q<VisualElement>("empty-state");
            nodeEditorInstance = root.Q<VisualElement>("node-editor-instance");
            connectionEditorInstance = root.Q<VisualElement>("connection-editor-instance");
            addNodeBtn = root.Q<Button>("add-node-btn");
            addConnectionBtn = root.Q<Button>("add-connection-btn");

            genotypeGraph = new GenotypeGraph(graphCanvas, connectionOverlay);
            genotypeGraph.OnNodeSelected += OnNodeClicked;
            genotypeGraph.OnConnectionSelected += OnConnectionClicked;
            genotypeGraph.OnEmptySpaceClicked += OnEmptySpaceClicked;

            InitializeNodeEditor();
            InitializeConnectionEditor();

            addNodeBtn.clicked += OnAddNodeClicked;
            addConnectionBtn.clicked += OnAddConnectionClicked;

            ShowEmptyState();
        }

        private void InitializeNodeEditor()
        {
            VisualElement nodeEditorContainer = root.Q<VisualElement>("node-editor-container");
            VisualElement nodeEditorElement = nodeEditorInstance.Q<VisualElement>("node-editor");

            nodeEditor = new(
                nodeEditorContainer,
                nodeEditorElement,
                OnNodeEditorApplyChanges,
                OnNodeEditorDelete
            );
        }

        private void InitializeConnectionEditor()
        {
            VisualElement connectionEditorContainer = root.Q<VisualElement>("connection-editor-container");
            VisualElement connectionEditorElement = connectionEditorInstance.Q<VisualElement>("connection-editor");

            connectionEditor = new(
                connectionEditorContainer,
                connectionEditorElement,
                OnConnectionEditorApplyChanges,
                OnConnectionEditorDelete
            );
        }

        public override void LoadGenotype(SimsGenotype genotype)
        {
            currentGenotype = genotype;
            nodes = new(genotype.Nodes);
            connections = new(genotype.Connections);

            ShowEmptyState();

            genotypeGraph.LoadGraph(nodes, connections);
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            addNodeBtn.SetEnabled(nodes.Count < SimsGenotype.MAX_NODES);

            int maxConnections = nodes.Count * Node.MAX_CONNECTIONS;
            addConnectionBtn.SetEnabled(connections.Count < maxConnections);
        }

        private void OnNodeClicked(int nodeIndex)
        {
            ShowNodeEditor(nodes[nodeIndex], nodeIndex);

            // Fire event for external systems.
            OnNodeSelectedByUser?.Invoke(nodes[nodeIndex].Gid);
        }

        private void OnConnectionClicked(int connectionIndex)
        {
            ShowConnectionEditor(connections[connectionIndex], connectionIndex);
        }

        private void OnEmptySpaceClicked()
        {
            ShowEmptyState();
            // Fire event with 0 to signal deselection to external systems.
            OnNodeSelectedByUser?.Invoke(0);
        }

        private void ShowEmptyState()
        {
            emptyState.style.display = DisplayStyle.Flex;
            root.Q<VisualElement>("node-editor-container").style.display = DisplayStyle.None;
            root.Q<VisualElement>("connection-editor-container").style.display = DisplayStyle.None;
        }

        private void ShowNodeEditor(Node node, int nodeIndex)
        {
            emptyState.style.display = DisplayStyle.None;
            root.Q<VisualElement>("node-editor-container").style.display = DisplayStyle.Flex;
            root.Q<VisualElement>("connection-editor-container").style.display = DisplayStyle.None;

            nodeEditor.StartEditing(node, nodeIndex, nodes.Count);
        }

        private void ShowConnectionEditor(Connection connection, int connectionIndex)
        {
            emptyState.style.display = DisplayStyle.None;
            root.Q<VisualElement>("node-editor-container").style.display = DisplayStyle.None;
            root.Q<VisualElement>("connection-editor-container").style.display = DisplayStyle.Flex;

            connectionEditor.StartEditing(connection, connectionIndex, nodes);
        }

        private void OnNodeEditorApplyChanges(Node updatedNode, int nodeIndex)
        {
            nodes[nodeIndex] = updatedNode;
            NotifyGenotypeChanged();
            genotypeGraph.LoadGraph(nodes, connections);
            UpdateButtonStates();
        }

        private void OnNodeEditorDelete(int nodeIndex)
        {
            ulong nodeGid = nodes[nodeIndex].Gid;
            nodes.RemoveAt(nodeIndex);
            connections.RemoveAll(c =>
                c.ParentNodeGid == nodeGid ||
                c.ChildNodeGid == nodeGid
            );

            ShowEmptyState();
            NotifyGenotypeChanged();
            genotypeGraph.LoadGraph(nodes, connections);
            UpdateButtonStates();
        }

        private void OnConnectionEditorApplyChanges(Connection updatedConnection, int connectionIndex)
        {
            connections[connectionIndex] = updatedConnection;
            NotifyGenotypeChanged();
            genotypeGraph.LoadGraph(nodes, connections);
            UpdateButtonStates();
        }

        private void OnConnectionEditorDelete(int connectionIndex)
        {
            connections.RemoveAt(connectionIndex);
            ShowEmptyState();
            NotifyGenotypeChanged();
            genotypeGraph.LoadGraph(nodes, connections);
            UpdateButtonStates();
        }

        private void OnAddNodeClicked()
        {
            SignalInputDefinition defaultSignalInput = new(
                new SignalEmitterAddress(RelativeSignalPort.Bias, 0),
                0f
            );

            Node newNode = new(
                new System.Numerics.Vector3(1f, 1f, 1f),
                new JointDefinition(
                    JointType.Rigid,
                    new System.Numerics.Vector3(0f, 0f, 0f),
                    new InputSetDefinition(defaultSignalInput, defaultSignalInput, defaultSignalInput),
                    new InputSetDefinition(defaultSignalInput, defaultSignalInput, defaultSignalInput),
                    new InputSetDefinition(defaultSignalInput, defaultSignalInput, defaultSignalInput)
                ),
                5,
                new NodeColor(0.5f, 0.5f, 0.5f, 0f, 0f, 0f)
            );

            nodes.Add(newNode);
            int newNodeIndex = nodes.Count - 1;
            ShowNodeEditor(newNode, newNodeIndex);
            NotifyGenotypeChanged();
            genotypeGraph.LoadGraph(nodes, connections);
            UpdateButtonStates();
        }

        private void OnAddConnectionClicked()
        {
            if (nodes.Count < 1)
            {
                Debug.LogWarning("Need at least 1 node to create a connection");
                return;
            }

            // Create a new self-connection on the first node.
            Connection newConnection = new(
                nodes[0].Gid,
                nodes[0].Gid,
                0,
                new System.Numerics.Vector2(0f, 0f),
                new System.Numerics.Vector3(0f, 0f, 0f),
                new System.Numerics.Vector3(1f, 1f, 1f),
                false,
                false,
                false,
                false
            );

            connections.Add(newConnection);
            int newConnectionIndex = connections.Count - 1;
            ShowConnectionEditor(newConnection, newConnectionIndex);
            NotifyGenotypeChanged();
            genotypeGraph.LoadGraph(nodes, connections);
            UpdateButtonStates();
        }

        private void NotifyGenotypeChanged()
        {
            SimsGenotype updatedGenotype = new(
                nodes,
                connections,
                currentGenotype.NeuronDefinitions
            );
            onGenotypeChanged?.Invoke(updatedGenotype);
        }

        /// <summary>
        /// Select a node in the graph by its GID. Called from external systems (e.g., creature visualizer clicks).
        /// </summary>
        public void SelectNodeByGid(ulong nodeGid)
        {
            int nodeIndex = nodes.FindIndex(n => n.Gid == nodeGid);
            if (nodeIndex >= 0)
            {
                genotypeGraph.SetSelectedNode(nodeIndex);
                ShowNodeEditor(nodes[nodeIndex], nodeIndex);
                // Fire event to notify external systems.
                OnNodeSelectedByUser?.Invoke(nodeGid);
            }
        }

        /// <summary>
        /// Clear the current selection and hide editor panels.
        /// </summary>
        public void ClearSelection()
        {
            genotypeGraph.ClearSelection();
            ShowEmptyState();
            // Fire event with 0 to signal deselection to external systems.
            OnNodeSelectedByUser?.Invoke(0);
        }
    }
}
