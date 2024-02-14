using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Crosstales.FB;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

public class CreatureEditor : MonoBehaviour
{
    public PlayerController playerController;
    public VisualTreeAsset limbNodeTemplate;
    public VisualTreeAsset connectionRootTemplate;
    public VisualTreeAsset editConnectionTemplate;
    public Limb phantomLimb;

    private UIDocument doc;

    private VisualElement editorTab;
    private NodeEditor nodeEditor;
    private ScrollView nodeGraphContainer;
    private VisualElement nodeGraph;
    private Button addNodeButton;
    private VisualElement phenotypeTab;
    private Button expandRightButton;
    private Button splitTabsButton;
    private Button expandLeftButton;
    private Button pauseSimulationButton;
    private Button groundSimulationButton;
    private Button waterSimulationButton;
    private Button normalCameraButton;
    private Button orbitCameraButton;
    private bool orbitPhenotype;

    private Genotype loadedGenotype;
    private int? focusedLimbNodeIndex;
    private Phenotype phenotype;

    private Color buttonInactiveColor = new(0.74f, 0.74f, 0.74f);
    private Color buttonActiveColor = Color.white;
    private Color buttonErrorColor = Color.red;

    private void Start()
    {
        doc = GetComponent<UIDocument>();

        InitialiseHeader();
        InitialiseEditorTab();
        InitialisePhenotypeTab();

        SelectionManager.Instance.OnSelectionChange += (previouslySelected, selected) =>
        {
            int? nodeIndex = selected.Count == 0 ? null : phenotype.genotype.LimbNodes.IndexOf((selected[0] as Limb).limbNode);
            if (nodeIndex == -1) // Phantom limb.
                nodeIndex = focusedLimbNodeIndex;
            if (nodeIndex != focusedLimbNodeIndex)
                SetFocusedLimbNode(nodeIndex);
        };

        SetLoadedGenotype(null);

        ToggleTabLayout(TabLayout.SplitTabs);
        ToggleSimulationEnvironment(SimulationEnvironment.Ground);
        ToggleCameraBehaviour(CameraBehaviour.Orbit);
    }

    private void InitialiseHeader()
    {
        VisualElement header = doc.rootVisualElement.Q<VisualElement>("header");
        Button exitButton = header.Q<Button>("exit");
        exitButton.clicked += () => SceneManager.LoadScene("MainMenu");

        VisualElement tabLayoutToggle = header.Q<VisualElement>("tab-layout-toggle");
        splitTabsButton = tabLayoutToggle.Q<Button>("split-tabs");
        expandLeftButton = tabLayoutToggle.Q<Button>("expand-left");
        expandRightButton = tabLayoutToggle.Q<Button>("expand-right");
        splitTabsButton.clicked += () => ToggleTabLayout(TabLayout.SplitTabs);
        expandLeftButton.clicked += () => ToggleTabLayout(TabLayout.ExpandLeft);
        expandRightButton.clicked += () => ToggleTabLayout(TabLayout.ExpandRight);

        VisualElement simulationEnvironmentToggle = header.Q<VisualElement>("simulation-environment-toggle");
        pauseSimulationButton = simulationEnvironmentToggle.Q<Button>("pause");
        groundSimulationButton = simulationEnvironmentToggle.Q<Button>("ground");
        waterSimulationButton = simulationEnvironmentToggle.Q<Button>("water");
        pauseSimulationButton.clicked += () => ToggleSimulationEnvironment(SimulationEnvironment.Pause);
        groundSimulationButton.clicked += () => ToggleSimulationEnvironment(SimulationEnvironment.Ground);
        waterSimulationButton.clicked += () => ToggleSimulationEnvironment(SimulationEnvironment.Water);

        VisualElement cameraBehaviourToggle = header.Q<VisualElement>("camera-behaviour-toggle");
        normalCameraButton = cameraBehaviourToggle.Q<Button>("normal");
        orbitCameraButton = cameraBehaviourToggle.Q<Button>("orbit");
        normalCameraButton.clicked += () => ToggleCameraBehaviour(CameraBehaviour.Normal);
        orbitCameraButton.clicked += () => ToggleCameraBehaviour(CameraBehaviour.Orbit);

        Button loadedGenotypeButton = header.Q<Button>("loaded-genotype");
        Button removeLoadedGenotypeButton = header.Q<Button>("remove-loaded-genotype");
        Button saveGenotypeButton = header.Q<Button>("save-genotype");
        loadedGenotypeButton.clicked += () =>
        {
            string genotypeFilePath = FileBrowser.Instance.OpenSingleFile(
                "Load Genotype",
                FileBrowser.Instance.CurrentOpenSingleFile ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                string.Empty,
                "genotype"
            );
            if (string.IsNullOrEmpty(genotypeFilePath))
                return;

            Genotype genotype = GenotypeSerializer.ReadGenotypeFromFile(genotypeFilePath);
            if (genotype == null)
            {
                loadedGenotypeButton.text = "Validation Error";
                loadedGenotypeButton.style.backgroundColor = buttonErrorColor;
                removeLoadedGenotypeButton.style.display = DisplayStyle.None;
                saveGenotypeButton.style.display = DisplayStyle.None;
                SetLoadedGenotype(null);
            }
            else
            {
                loadedGenotypeButton.text = "Loaded '" + genotype.Id + "'";
                loadedGenotypeButton.style.backgroundColor = buttonActiveColor;
                removeLoadedGenotypeButton.style.display = DisplayStyle.Flex;
                saveGenotypeButton.style.display = DisplayStyle.Flex;
                SetLoadedGenotype(genotype);
            }
        };
        removeLoadedGenotypeButton.clicked += () =>
        {
            loadedGenotypeButton.text = "Select a genotype...";
            loadedGenotypeButton.style.backgroundColor = buttonInactiveColor;
            removeLoadedGenotypeButton.style.display = DisplayStyle.None;
            saveGenotypeButton.style.display = DisplayStyle.None;
            SetLoadedGenotype(null);
        };
        saveGenotypeButton.clicked += () =>
        {
            if (phenotype != null)
                phenotype.SaveGenotypeToFile();
        };
        removeLoadedGenotypeButton.style.display = DisplayStyle.None;
        saveGenotypeButton.style.display = DisplayStyle.None;
    }

    private void InitialiseEditorTab()
    {
        editorTab = doc.rootVisualElement.Q<VisualElement>("editor-tab");
        nodeGraph = editorTab.Q<VisualElement>("node-graph");
        nodeGraphContainer = nodeGraph.parent as ScrollView;
        nodeGraph.generateVisualContent += OnGenerateVisualContent;
        nodeGraph.AddManipulator(new Clickable(e => SetFocusedLimbNode(null)));
        addNodeButton = nodeGraphContainer.Q<Button>("add-node");
        addNodeButton.clicked += AddLimbNode;

        VisualElement nodeEditorElement = editorTab.Q<VisualElement>("node-editor");
        VisualElement nodeEditorContainer = nodeEditorElement.parent as ScrollView;
        nodeEditor = new(nodeEditorContainer, nodeEditorElement, editConnectionTemplate, EditLimbNode, DeleteLimbNode);
    }

    private void InitialisePhenotypeTab()
    {
        phenotypeTab = doc.rootVisualElement.Q<VisualElement>("phenotype-tab");
    }

    private void SetUpNodeEditor(int? nodeIndex)
    {
        if (nodeIndex == null)
            nodeEditor.StopEditing();
        else
            nodeEditor.StartEditing(loadedGenotype.LimbNodes[(int)nodeIndex], (int)nodeIndex, loadedGenotype.LimbNodes.Count);
    }

    private void SetLoadedGenotype(Genotype genotype)
    {
        nodeGraph.Clear();
        ClearPhenotypeWindow();
        SetFocusedLimbNode(null);
        loadedGenotype = genotype;

        if (loadedGenotype != null)
        {
            phenotype = Phenotype.Construct(loadedGenotype);
            phenotype.SetLimbsSelectable(true);
            PickAndPlaceManager.Instance.PickUp(phenotype, null, null);
            PickAndPlaceManager.Instance.Place(Vector3.zero);
            playerController.orbitTarget = orbitPhenotype && phenotype.limbs.Count > 0 ? phenotype.limbs[0].transform : null;

            for (int nodeIndex = 0; nodeIndex < loadedGenotype.LimbNodes.Count; nodeIndex++)
                nodeGraph.Add(InstantiateLimbNodeElement(nodeIndex));

            WireUpLimbNodeElements();
        }

        addNodeButton.style.display = loadedGenotype != null && loadedGenotype.LimbNodes.Count < GenotypeParameters.MaxLimbNodes ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private VisualElement InstantiateLimbNodeElement(int nodeIndex)
    {
        TemplateContainer limbNodeContainer = limbNodeTemplate.Instantiate();
        Button limbNodeButton = limbNodeContainer.Q<Button>("limb-node");
        limbNodeButton.text = "Node " + (nodeIndex + 1).ToString();
        LimbNode limbNode = loadedGenotype.LimbNodes.ElementAt(nodeIndex);
        if (phenotype.limbs.Where(l => l.limbNode == limbNode).Count() == 0)
            limbNodeButton.AddToClassList("unused");
        limbNodeButton.clicked += () => SetFocusedLimbNode(nodeIndex != focusedLimbNodeIndex ? nodeIndex : null);
        return limbNodeContainer;
    }

    private void WireUpLimbNodeElements()
    {
        List<VisualElement> limbNodeElements = nodeGraph.Children().Select(x => x.Q("limb-node")).ToList();

        // Create connection roots and store connection data for later drawing.
        for (int i = 0; i < limbNodeElements.Count; i++)
        {
            VisualElement limbNodeElement = limbNodeElements.ElementAt(i);
            VisualElement forwardConnections = limbNodeElement.Q("forward-connections");
            VisualElement backwardConnections = limbNodeElement.Q("backward-connections");
            VisualElement forwardConnectionRootTemplate = forwardConnections.Q("connection-root-template");
            VisualElement backwardConnectionRootTemplate = backwardConnections.Q("connection-root-template");

            LimbNode limbNode = loadedGenotype.LimbNodes.ElementAt(i);
            foreach (LimbConnection connection in limbNode.Connections)
            {
                bool isForwardConnection = connection.ChildNodeId > i;
                VisualElement connectionRoot = connectionRootTemplate.Instantiate();
                connectionRoot.userData = connection.ChildNodeId;
                (isForwardConnection ? forwardConnections : backwardConnections).Add(connectionRoot);
                VisualElement childNodeElement = limbNodeElements.ElementAt(connection.ChildNodeId);
                childNodeElement.userData ??= new List<int>();
                (childNodeElement.userData as List<int>).Add(i);
            }
        }

        nodeGraph.MarkDirtyRepaint();
    }

    private enum TabLayout
    {
        SplitTabs,
        ExpandLeft,
        ExpandRight
    }

    private void ToggleTabLayout(TabLayout tabLayout)
    {
        if (tabLayout == TabLayout.SplitTabs) splitTabsButton.AddToClassList("active"); else splitTabsButton.RemoveFromClassList("active");
        if (tabLayout == TabLayout.ExpandLeft) expandLeftButton.AddToClassList("active"); else expandLeftButton.RemoveFromClassList("active");
        if (tabLayout == TabLayout.ExpandRight) expandRightButton.AddToClassList("active"); else expandRightButton.RemoveFromClassList("active");
        editorTab.style.display = tabLayout == TabLayout.ExpandLeft ? DisplayStyle.None : DisplayStyle.Flex;
        phenotypeTab.style.display = tabLayout == TabLayout.ExpandRight ? DisplayStyle.None : DisplayStyle.Flex;
        playerController.playerCamera.rect = new Rect() { x = tabLayout != TabLayout.ExpandLeft ? 0.5f : 0f, y = 0f, width = 1f, height = 1f };
    }

    private enum SimulationEnvironment
    {
        Pause,
        Ground,
        Water
    }

    private void ToggleSimulationEnvironment(SimulationEnvironment simulationEnvironment)
    {
        if (simulationEnvironment == SimulationEnvironment.Pause) pauseSimulationButton.AddToClassList("active"); else pauseSimulationButton.RemoveFromClassList("active");
        if (simulationEnvironment == SimulationEnvironment.Ground) groundSimulationButton.AddToClassList("active"); else groundSimulationButton.RemoveFromClassList("active");
        if (simulationEnvironment == SimulationEnvironment.Water) waterSimulationButton.AddToClassList("active"); else waterSimulationButton.RemoveFromClassList("active");
        WorldManager.Instance.timeScale = simulationEnvironment == SimulationEnvironment.Pause ? 0f : 1f;
        if (simulationEnvironment != SimulationEnvironment.Pause)
            WorldManager.Instance.ChangeEnvironment(simulationEnvironment == SimulationEnvironment.Water ? WorldEnvironment.Underwater : WorldEnvironment.Surface);
    }

    private enum CameraBehaviour
    {
        Normal,
        Orbit
    }

    private void ToggleCameraBehaviour(CameraBehaviour cameraBehaviour)
    {
        if (cameraBehaviour == CameraBehaviour.Normal) normalCameraButton.AddToClassList("active"); else normalCameraButton.RemoveFromClassList("active");
        if (cameraBehaviour == CameraBehaviour.Orbit) orbitCameraButton.AddToClassList("active"); else orbitCameraButton.RemoveFromClassList("active");
        orbitPhenotype = cameraBehaviour == CameraBehaviour.Orbit;
        playerController.orbitTarget = orbitPhenotype && phenotype != null && phenotype.limbs.Count > 0 ? phenotype.limbs[0].transform : null;
    }

    private void SetFocusedLimbNode(int? nodeIndex)
    {
        if (focusedLimbNodeIndex != null && nodeGraph.Children().Count() > 0)
        {
            VisualElement focusedLimbNodeElement = nodeGraph.Children().ElementAt((int)focusedLimbNodeIndex);
            focusedLimbNodeElement.Q<Button>("limb-node").RemoveFromClassList("active");
        }

        focusedLimbNodeIndex = nodeIndex;

        if (nodeIndex == null) // Deselect.
        {
            SetUpNodeEditor(null);
            SelectionManager.Instance.SetSelected(null);
        }
        else // Select.
        {
            VisualElement newLimbNodeElement = nodeGraph.Children().ElementAt((int)nodeIndex);
            newLimbNodeElement?.Q<Button>("limb-node").AddToClassList("active");
            nodeGraphContainer.ScrollTo(newLimbNodeElement);

            SetUpNodeEditor(nodeIndex);

            // Select all limbs that were spawned from the focused limb node template.
            LimbNode limbNode = loadedGenotype.LimbNodes.ElementAt((int)nodeIndex);
            List<Limb> limbNodeLimbs = phenotype.limbs.Where(l => l.limbNode == limbNode).ToList();
            if (limbNodeLimbs.Count == 0)
                phantomLimb.Select(false, false);
            else
                for (int i = 0; i < limbNodeLimbs.Count; i++)
                    limbNodeLimbs[i].Select(false, i > 0); // Disable multiselect for first selection, then multiselect the rest.
        }
    }

    private void AddLimbNode()
    {
        UnfinishedLimbNode unfinishedLimbNode = UnfinishedLimbNode.CreateRandom(new List<LimbConnection>());
        EmitterAvailabilityMap emitterAvailabilityMap = EmitterAvailabilityMap.GenerateMapForLimbNode(
            0,
            loadedGenotype.LimbNodes.Cast<ILimbNodeEssentialInfo>().Concat(new List<ILimbNodeEssentialInfo>() { unfinishedLimbNode }).ToList(),
            loadedGenotype.LimbNodes.Count
        );
        LimbNode newLimbNode = LimbNode.CreateRandom(emitterAvailabilityMap, unfinishedLimbNode);
        SetLoadedGenotype(Genotype.Construct(
            loadedGenotype.Id,
            loadedGenotype.BrainNeuronDefinitions,
            loadedGenotype.LimbNodes.Concat(new List<LimbNode>() { newLimbNode }).ToList()
        ));
    }

    private void EditLimbNode(LimbNode editedLimbNode, int limbNodeIndex)
    {
        SetLoadedGenotype(Genotype.Construct(
            loadedGenotype.Id,
            loadedGenotype.BrainNeuronDefinitions,
            loadedGenotype.LimbNodes.Select((l, i) => i == limbNodeIndex ? editedLimbNode : l).ToList()
        ));
        SetFocusedLimbNode(limbNodeIndex);
    }

    private void DeleteLimbNode(int limbNodeIndex)
    {
        SetFocusedLimbNode(null);
        SetLoadedGenotype(
            loadedGenotype = Genotype.Construct(
                loadedGenotype.Id,
                loadedGenotype.BrainNeuronDefinitions,
                loadedGenotype.LimbNodes.Where((l, i) => i != limbNodeIndex).Select((l, i) =>
                    l.CreateCopy(l.Connections.Where(c => c.ChildNodeId < loadedGenotype.LimbNodes.Count - 1).ToList())
                ).ToList()
            )
        );
    }

    private void ClearPhenotypeWindow()
    {
        if (phenotype != null)
        {
            Destroy(phenotype.gameObject);
        }
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (nodeGraph?.Children().Count() < 1)
            return;

        Painter2D painter = mgc.painter2D;

        painter.strokeColor = Color.white;
        painter.fillColor = Color.white;
        painter.lineJoin = LineJoin.Round;
        painter.lineCap = LineCap.Round;
        painter.lineWidth = 2f;

        Vector2 origin = new(nodeGraph.worldBound.xMin, nodeGraph.worldBound.yMin);
        void DrawSpline(Vector2 start, Vector2 end, Vector2 startTangent, Vector2 endTangent)
        {
            painter.BeginPath();
            painter.MoveTo(start - origin);
            int interpolationSteps = 100;
            for (int i = 0; i < interpolationSteps; i++)
                painter.LineTo(HermiteInterpolate(start - origin, end - origin, startTangent, endTangent, i / (float)interpolationSteps));
            painter.Stroke();
        }

        List<VisualElement> limbNodeElements = nodeGraph.Children().Select(x => x.Q("limb-node")).ToList();

        for (int i = 0; i < limbNodeElements.Count; i++)
        {
            VisualElement limbNodeElement = limbNodeElements.ElementAt(i);
            VisualElement incomingWaypointForward = limbNodeElement.Q("incoming-waypoint-forward");
            VisualElement incomingWaypointBackward = limbNodeElement.Q("incoming-waypoint-backward");
            VisualElement incomingEndpoint = limbNodeElement.Q("incoming-endpoint");
            VisualElement forwardConnections = limbNodeElement.Q("forward-connections");
            VisualElement backwardConnections = limbNodeElement.Q("backward-connections");

            bool hasForwardIncoming = limbNodeElement.userData != null && (limbNodeElement.userData as List<int>).Any(nodeId => nodeId < i);
            bool hasBackwardIncoming = limbNodeElement.userData != null && (limbNodeElement.userData as List<int>).Any(nodeId => nodeId >= i);

            if (hasForwardIncoming)
                DrawSpline(incomingWaypointForward.worldBound.center, incomingEndpoint.worldBound.center, Vector2.up * 100f, Vector2.right * 100f);
            if (hasBackwardIncoming)
                DrawSpline(incomingWaypointBackward.worldBound.center, incomingEndpoint.worldBound.center, Vector2.down * 100f, Vector2.right * 100f);
            if (hasForwardIncoming || hasBackwardIncoming)
            {
                painter.BeginPath();
                painter.MoveTo(incomingEndpoint.worldBound.center - origin + new Vector2(0, 0));
                painter.LineTo(incomingEndpoint.worldBound.center - origin + new Vector2(-10, -10));
                painter.LineTo(incomingEndpoint.worldBound.center - origin + new Vector2(-10, 10));
                painter.LineTo(incomingEndpoint.worldBound.center - origin);
                painter.Fill();
            }

            foreach (VisualElement forwardConnectionRoot in limbNodeElement.Q("forward-connections").Children())
            {
                int childNodeIndex = (int)forwardConnectionRoot.userData;
                VisualElement childNodeElement = limbNodeElements.ElementAt(childNodeIndex);
                VisualElement childIncomingWaypointForward = childNodeElement.Q("incoming-waypoint-forward");
                int numberOfNodesBetween = childNodeIndex - i;
                Vector2 toNodeCenter = limbNodeElement.worldBound.center - forwardConnectionRoot.worldBound.center;
                DrawSpline(forwardConnectionRoot.worldBound.center, childIncomingWaypointForward.worldBound.center, 3 * (numberOfNodesBetween + 1) * -toNodeCenter, Vector2.up * 200f);
            }
            foreach (VisualElement backwardConnectionRoot in limbNodeElement.Q("backward-connections").Children())
            {
                int childNodeIndex = (int)backwardConnectionRoot.userData;
                VisualElement childNodeElement = limbNodeElements.ElementAt((int)backwardConnectionRoot.userData);
                VisualElement childIncomingWaypointBackward = childNodeElement.Q("incoming-waypoint-backward");
                int numberOfNodesBetween = i - childNodeIndex;
                Vector2 toNodeCenter = limbNodeElement.worldBound.center - backwardConnectionRoot.worldBound.center;
                DrawSpline(backwardConnectionRoot.worldBound.center, childIncomingWaypointBackward.worldBound.center, 3 * (numberOfNodesBetween + 1) * -toNodeCenter, Vector2.down * 200f);
            }
        }
    }

    private Vector2 HermiteInterpolate(Vector2 start, Vector2 end, Vector2 startTangent, Vector2 endTangent, float step)
    {
        float stepSquared = Mathf.Pow(step, 2);
        float stepCubed = Mathf.Pow(step, 3);
        float h1 = 2 * stepCubed - 3 * stepSquared + 1;
        float h2 = -2 * stepCubed + 3 * stepSquared;
        float h3 = stepCubed - 2 * stepSquared + step;
        float h4 = stepCubed - stepSquared;
        return h1 * start + h2 * end + h3 * startTangent + h4 * endTangent;
    }
}
