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

    private UIDocument doc;

    private VisualElement nodeGraphTab;
    private VisualElement nodeGraph;
    private VisualElement phenotypeTab;
    private Button expandRightButton;
    private Button splitTabsButton;
    private Button expandLeftButton;

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
        InitialiseNodeGraphTab();
        InitialisePhenotypeTab();

        SelectionManager.Instance.OnSelectionChange += (previouslySelected, selected) =>
        {
            int? nodeIndex = selected.Count == 0 ? null : phenotype.genotype.LimbNodes.IndexOf((selected[0] as Limb).limbNode);
            if (nodeIndex != focusedLimbNodeIndex)
                SetFocusedLimbNode(nodeIndex);
        };
    }

    private void InitialiseHeader()
    {
        VisualElement header = doc.rootVisualElement.Q<VisualElement>("header");
        Button exitButton = header.Q<Button>("exit");
        exitButton.clicked += () => SceneManager.LoadScene("MainMenu");

        splitTabsButton = header.Q<Button>("split-tabs");
        expandLeftButton = header.Q<Button>("expand-left");
        expandRightButton = header.Q<Button>("expand-right");
        splitTabsButton.clicked += () => ToggleTabLayout(TabLayout.SplitTabs);
        expandLeftButton.clicked += () => ToggleTabLayout(TabLayout.ExpandLeft);
        expandRightButton.clicked += () => ToggleTabLayout(TabLayout.ExpandRight);
    }

    private void InitialiseNodeGraphTab()
    {
        nodeGraphTab = doc.rootVisualElement.Q<VisualElement>("node-graph-tab");
        nodeGraph = nodeGraphTab.Q<VisualElement>("node-graph");

        nodeGraph.generateVisualContent += OnGenerateVisualContent;
        nodeGraph.AddManipulator(new Clickable(e => SetFocusedLimbNode(null)));

        VisualElement menu = doc.rootVisualElement.Q<VisualElement>("node-graph-menu");
        Button loadedGenotypeButton = menu.Q<Button>("loaded-genotype");
        Button removeLoadedGenotypeButton = menu.Q<Button>("remove-loaded-genotype");
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
                SetLoadedGenotype(null);
            }
            else
            {
                loadedGenotypeButton.text = "Loaded '" + genotype.Id + "'";
                loadedGenotypeButton.style.backgroundColor = buttonActiveColor;
                removeLoadedGenotypeButton.style.display = DisplayStyle.Flex;
                SetLoadedGenotype(genotype);
            }
        };
        removeLoadedGenotypeButton.clicked += () =>
        {
            loadedGenotypeButton.text = "Select a genotype...";
            loadedGenotypeButton.style.backgroundColor = buttonInactiveColor;
            removeLoadedGenotypeButton.style.display = DisplayStyle.None;
            SetLoadedGenotype(null);
        };
        removeLoadedGenotypeButton.style.display = DisplayStyle.None;
    }

    private void InitialisePhenotypeTab()
    {
        phenotypeTab = doc.rootVisualElement.Q<VisualElement>("phenotype-tab");
    }

    private void SetLoadedGenotype(Genotype genotype)
    {
        ClearPhenotypeWindow();
        loadedGenotype = genotype;

        if (loadedGenotype == null)
        {
            nodeGraph.Clear();
            phenotype = null;
        }
        else
        {
            for (int nodeIndex = 0; nodeIndex < loadedGenotype.LimbNodes.Count; nodeIndex++)
                nodeGraph.Add(InstantiateLimbNodeElement(nodeIndex));

            WireUpLimbNodeElements();

            playerController.orbitTarget = null;
            phenotype = Phenotype.Construct(loadedGenotype);
            phenotype.SetLimbsSelectable(true);
            PickAndPlaceManager.Instance.PickUp(phenotype, null, null);
            PickAndPlaceManager.Instance.Place(Vector3.zero);
            playerController.orbitTarget = phenotype.limbs[0].transform;
        }
    }

    private VisualElement InstantiateLimbNodeElement(int nodeIndex)
    {
        TemplateContainer limbNodeContainer = limbNodeTemplate.Instantiate();
        Button limbNodeButton = limbNodeContainer.Q<Button>("limb-node");
        limbNodeButton.text = "Node " + nodeIndex.ToString();
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
        nodeGraphTab.style.display = tabLayout == TabLayout.ExpandLeft ? DisplayStyle.None : DisplayStyle.Flex;
        phenotypeTab.style.display = tabLayout == TabLayout.ExpandRight ? DisplayStyle.None : DisplayStyle.Flex;
        ToggleCameraViewport(tabLayout != TabLayout.ExpandLeft);
    }

    private void SetFocusedLimbNode(int? nodeIndex)
    {
        if (focusedLimbNodeIndex != null)
        {
            VisualElement focusedLimbNodeElement = nodeGraph.Children().ElementAt((int)focusedLimbNodeIndex).Q<Button>("limb-node");
            focusedLimbNodeElement?.RemoveFromClassList("active");
        }

        focusedLimbNodeIndex = nodeIndex;

        if (nodeIndex == null) // Deselect.
        {
            SelectionManager.Instance.SetSelected(null);
        }
        else // Select.
        {
            VisualElement newLimbNodeElement = nodeGraph.Children().ElementAt((int)nodeIndex).Q<Button>("limb-node");
            newLimbNodeElement.AddToClassList("active");

            // Select all limbs that were spawned from the focused limb node template.
            LimbNode limbNode = loadedGenotype.LimbNodes.ElementAt((int)nodeIndex);
            List<Limb> limbNodeLimbs = phenotype.limbs.Where(l => l.limbNode == limbNode).ToList();
            for (int i = 0; i < limbNodeLimbs.Count; i++)
            {
                limbNodeLimbs[i].Select(false, i > 0); // Disable multiselect for first selection, then multiselect the rest.
            }
        }
    }

    private void ClearPhenotypeWindow()
    {
        if (phenotype != null)
        {
            Destroy(phenotype.gameObject);
        }
    }

    private void ToggleCameraViewport(bool halve)
    {
        playerController.playerCamera.rect = new Rect() { x = halve ? 0.5f : 0f, y = 0f, width = 1f, height = 1f };
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

        Vector2 origin = new(nodeGraphTab.worldBound.xMin, nodeGraphTab.worldBound.yMin);
        void DrawSpline(Vector2 start, Vector2 end, Vector2 startTangent, Vector2 endTangent)
        {
            painter.BeginPath();
            painter.MoveTo(start - origin);
            int interpolationSteps = 100;
            for (int i = 0; i < interpolationSteps; i++)
                painter.LineTo(HermiteInterpolate(start - origin, end - origin, startTangent - origin, endTangent - origin, i / (float)interpolationSteps));
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
                DrawSpline(incomingWaypointForward.worldBound.center, incomingEndpoint.worldBound.center, Vector2.right * 200f, Vector2.up * 100f);
            if (hasBackwardIncoming)
                DrawSpline(incomingWaypointBackward.worldBound.center, incomingEndpoint.worldBound.center, Vector2.left * 200f, Vector2.up * 100f);
            if (hasForwardIncoming || hasBackwardIncoming)
            {
                painter.BeginPath();
                painter.MoveTo(incomingEndpoint.worldBound.center - origin + new Vector2(10, 10));
                painter.LineTo(incomingEndpoint.worldBound.center - origin + new Vector2(-10, -10));
                painter.LineTo(incomingEndpoint.worldBound.center - origin + new Vector2(10, -10));
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
                DrawSpline(forwardConnectionRoot.worldBound.center, childIncomingWaypointForward.worldBound.center, 3 * (numberOfNodesBetween + 1) * -toNodeCenter, Vector2.right * 200f);
            }
            foreach (VisualElement backwardConnectionRoot in limbNodeElement.Q("backward-connections").Children())
            {
                int childNodeIndex = (int)backwardConnectionRoot.userData;
                VisualElement childNodeElement = limbNodeElements.ElementAt((int)backwardConnectionRoot.userData);
                VisualElement childIncomingWaypointBackward = childNodeElement.Q("incoming-waypoint-backward");
                int numberOfNodesBetween = i - childNodeIndex;
                Vector2 toNodeCenter = limbNodeElement.worldBound.center - backwardConnectionRoot.worldBound.center;
                DrawSpline(backwardConnectionRoot.worldBound.center, childIncomingWaypointBackward.worldBound.center, 3 * (numberOfNodesBetween + 1) * -toNodeCenter, Vector2.left * 200f);
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
