using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using cakeslice;

public class Limb : MonoBehaviour, ISelectable
{
    public LimbNode limbNode;
    public string instanceId;

    public BoxCollider fullBodyCollider;
    public BoxCollider xyMidwayCollider;
    public BoxCollider yzMidwayCollider;
    public BoxCollider xzMidwayCollider;
    public List<Collider> activeColliders;

    private Vector3 unscaledDimensions;
    public Vector3 Dimensions
    {
        get { return transform.localScale; }
        set
        {
            transform.localScale = value;
            GetComponent<Rigidbody>().mass = Mathf.Abs(value.x * value.y * value.z); // Mass is proportional to volume.
        }
    }

    public JointBase joint;

    public List<Sensor> sensors;
    public List<Neuron> neurons;
    public List<Effector> effectors;
    public List<Sensor> photoSensors;

    public Limb parentLimb;
    public List<Limb> childLimbs;

    private Outline outline;
    public bool passSelectionToParent = true;

    [SerializeField] private bool reflectedX;
    [SerializeField] private bool reflectedY;
    [SerializeField] private bool reflectedZ;

    public List<SignalReceiver> GetSignalReceivers()
    {
        return neurons.Select(n => n.Processor.Receiver).Concat(effectors.Select(e => e.Processor.Receiver)).ToList();
    }

    public List<SignalEmitter> GetSignalEmitters()
    {
        return neurons.Select(n => n.Processor.Emitter).Concat(sensors.Select(s => s.Processor.Emitter)).ToList();
    }

    private void Awake()
    {
        outline = GetComponent<Outline>();
    }

    private void FixedUpdate()
    {
        UpdatePhotoSensors();
    }

    private void UpdatePhotoSensors()
    {
        if (WorldManager.Instance.pointLight != null && WorldManager.Instance.pointLight.activeInHierarchy)
        {
            List<Vector3> limbAxes = new() { transform.right, transform.up, transform.forward };
            Vector3 displacement = WorldManager.Instance.pointLight.transform.position - transform.position;
            float luminosityAtPoint = 1 / (1 + (displacement.sqrMagnitude / 100f));
            Vector3 lightDirection = displacement.normalized;
            for (int i = 0; i < 3; i++)
                if (!photoSensors[i].Processor.Emitter.Disabled)
                    photoSensors[i].Excitation = Vector3.Dot(limbAxes[i], lightDirection) * luminosityAtPoint;
        }
        else
            for (int i = 0; i < 3; i++)
                if (!photoSensors[i].Processor.Emitter.Disabled)
                    photoSensors[i].Excitation = 0f;
    }

    private Limb AddChildLimb(InstancedLimbNode node)
    {
        if (node == null)
            return AbandonChildCreation();

        // Find the local position and rotation that attaches the back face of the new limb to the chosen face of the parent limb.
        int perpendicularAxis = node.ConnectionToParent.ParentFace % 3;
        Vector3 localParentFaceNormal = Vector3.zero;
        localParentFaceNormal[perpendicularAxis] = node.ConnectionToParent.ParentFace > 2 ? 1f : -1f;

        Vector3 localParentAnchorPoint = Vector3.one;
        localParentAnchorPoint[perpendicularAxis] *= node.ConnectionToParent.ParentFace > 2 ? 0.5f : -0.5f;
        localParentAnchorPoint[(perpendicularAxis + 1) % 3] *= 0.5f * node.ConnectionToParent.Position[0];
        localParentAnchorPoint[(perpendicularAxis + 2) % 3] *= 0.5f * node.ConnectionToParent.Position[1];

        // Apply limb rotations.
        Quaternion localChildRotation = Quaternion.AngleAxis(node.ConnectionToParent.Orientation.z, Vector3.forward)
         * Quaternion.AngleAxis(node.ConnectionToParent.Orientation.y, Vector3.up)
         * Quaternion.AngleAxis(node.ConnectionToParent.Orientation.x, Vector3.right)
         * Quaternion.FromToRotation(Vector3.forward, localParentFaceNormal);

        Vector3 existingScaling = Vector3.Scale(transform.localScale, new Vector3(1f / unscaledDimensions.x, 1f / unscaledDimensions.y, 1f / unscaledDimensions.z));
        Vector3 childDimensions = Vector3.Scale(Vector3.Scale(node.LimbNode.Dimensions, existingScaling), node.ConnectionToParent.Scale);
        if (childDimensions.x < ParameterManager.Instance.LimbNode.MinSize || childDimensions.x > ParameterManager.Instance.LimbNode.MaxSize ||
            childDimensions.y < ParameterManager.Instance.LimbNode.MinSize || childDimensions.y > ParameterManager.Instance.LimbNode.MaxSize ||
            childDimensions.z < ParameterManager.Instance.LimbNode.MinSize || childDimensions.z > ParameterManager.Instance.LimbNode.MaxSize)
            return AbandonChildCreation();

        // Only apply these reflections if the parent and child have opposing reflections.
        if (reflectedX ^ node.ReflectedX)
        {
            localParentAnchorPoint = Vector3.Reflect(localParentAnchorPoint, Vector3.right);
            localChildRotation = ReflectRotation(localChildRotation, Vector3.right);
        }
        if (reflectedY ^ node.ReflectedY)
        {
            localParentAnchorPoint = Vector3.Reflect(localParentAnchorPoint, Vector3.up);
            localChildRotation = ReflectRotation(localChildRotation, Vector3.up);
        }
        if (reflectedZ ^ node.ReflectedZ)
        {
            localParentAnchorPoint = Vector3.Reflect(localParentAnchorPoint, Vector3.forward);
            localChildRotation = ReflectRotation(localChildRotation, Vector3.forward);
        }

        // Correct for parent reflections.
        int reflectedCount = new List<bool>() { reflectedX, reflectedY, reflectedZ }.Count(x => x == true);
        if (reflectedCount == 1 || reflectedCount == 3)
        {
            localParentAnchorPoint = Vector3.Reflect(localParentAnchorPoint, Vector3.right);
            localChildRotation = ReflectRotation(localChildRotation, Vector3.right);
        }

        Quaternion childRotation = transform.rotation * localChildRotation;
        Vector3 childPosition = transform.TransformPoint(localParentAnchorPoint) + (childRotation * Vector3.forward * childDimensions.z / 2f);

        // Abandon creation if the new limb would intersect an existing, non-parent limb, or the relevant parent midway collider.
        List<BoxCollider> parentLimbMidwayColliders = new List<BoxCollider> { yzMidwayCollider, xzMidwayCollider, xyMidwayCollider };
        Collider[] fullBodyCollisions = Physics.OverlapBox(childPosition, childDimensions / 2f, childRotation);
        foreach (Collider col in fullBodyCollisions)
        {
            Limb collidedLimb = col.gameObject.GetComponent<Limb>();
            bool collidedWithAnotherLimb = collidedLimb != null && collidedLimb.transform.parent == transform.parent && collidedLimb != this;
            bool collidedWithTheParentMidwayCollider = col == parentLimbMidwayColliders[perpendicularAxis];
            if (collidedWithAnotherLimb || collidedWithTheParentMidwayCollider)
                return AbandonChildCreation();
        }

        // Abandon creation if the relevant child midway collider intersects the parent limb.
        Vector3 childMidwayColliderDimensions = Vector3.Scale(xyMidwayCollider.size, childDimensions);
        Collider[] midwayColliderCollisions = Physics.OverlapBox(childPosition, childMidwayColliderDimensions / 1.9f, childRotation);
        foreach (Collider col in midwayColliderCollisions)
        {
            Limb collidedLimb = col.gameObject.GetComponent<Limb>();
            if (collidedLimb == this)
                return AbandonChildCreation();
        }

        // Create the limb.
        Limb childLimb = Construct(node, transform.parent);
        childLimbs.Add(childLimb);
        childLimb.parentLimb = this;
        childLimb.transform.parent = transform.parent;
        childLimb.transform.position = childPosition;
        childLimb.transform.rotation = childRotation;
        childLimb.Dimensions = childDimensions;
        childLimb.reflectedX = node.ReflectedX;
        childLimb.reflectedY = node.ReflectedY;
        childLimb.reflectedZ = node.ReflectedZ;

        // Add and configure the joint between the child and the parent.
        float childCrossSectionalArea = childLimb.transform.localScale.x * childLimb.transform.localScale.y;
        float parentCrossSectionalArea = transform.localScale[(perpendicularAxis + 1) % 3] * transform.localScale[(perpendicularAxis + 2) % 3];
        float maximumJointStrength = Mathf.Min(childCrossSectionalArea, parentCrossSectionalArea);
        childLimb.joint = JointBase.CreateJoint(
            node.LimbNode.JointDefinition,
            childLimb.gameObject,
            GetComponent<Rigidbody>(),
            maximumJointStrength,
            node.LimbNode.JointDefinition.AxisDefinitions.Select(a => a.Limit).ToList(),
            node.ReflectedX,
            node.ReflectedY,
            node.ReflectedZ
        );
        childLimb.sensors = (childLimb.joint != null ? childLimb.joint.sensors : new()).Concat(childLimb.sensors).ToList();
        childLimb.effectors = (childLimb.joint != null ? childLimb.joint.effectors : new()).Concat(childLimb.effectors).ToList();

        // Enable parent/child interpenetration.
        Physics.IgnoreCollision(fullBodyCollider, childLimb.fullBodyCollider);
        Physics.IgnoreCollision(fullBodyCollider, childLimb.yzMidwayCollider);
        Physics.IgnoreCollision(fullBodyCollider, childLimb.xzMidwayCollider);
        for (int i = 0; i < 3; i++)
        {
            if (i != perpendicularAxis) // Ignore the midway colliders that run parallel to the perpendicular axis.
            {
                Physics.IgnoreCollision(childLimb.fullBodyCollider, parentLimbMidwayColliders[i]);
                Physics.IgnoreCollision(childLimb.xyMidwayCollider, parentLimbMidwayColliders[i]);
                Physics.IgnoreCollision(childLimb.yzMidwayCollider, parentLimbMidwayColliders[i]);
                Physics.IgnoreCollision(childLimb.xzMidwayCollider, parentLimbMidwayColliders[i]);
            }
        }

        // Note which midway colliders have been used so that we can disable the unused ones later.
        if (!activeColliders.Contains(parentLimbMidwayColliders[perpendicularAxis]))
            activeColliders.Add(parentLimbMidwayColliders[perpendicularAxis]);
        if (!childLimb.activeColliders.Contains(childLimb.xyMidwayCollider))
            childLimb.activeColliders.Add(childLimb.xyMidwayCollider);

        // Ensure that colliders are positioned correctly for potential children.
        Physics.SyncTransforms();

        return childLimb;
    }

    private Limb AbandonChildCreation()
    {
        // If child creation fails for any reason, we still need to preserve the order of this limb's children.
        // Adding a null child ensures that parent-to-child neuron connections are not accidentally reassigned.
        childLimbs.Add(null);
        return null;
    }

    private void Optimise()
    {
        // Disable unused colliders.
        if (!activeColliders.Contains(xyMidwayCollider)) xyMidwayCollider.enabled = false;
        if (!activeColliders.Contains(yzMidwayCollider)) yzMidwayCollider.enabled = false;
        if (!activeColliders.Contains(xzMidwayCollider)) xzMidwayCollider.enabled = false;
    }

    private static Limb Construct(InstancedLimbNode node, Transform containerTransform)
    {
        Limb limb = Instantiate(ResourceManager.Instance.limbPrefab).GetComponent<Limb>();

        limb.limbNode = node.LimbNode;

        limb.transform.parent = containerTransform;
        limb.name = "Limb " + containerTransform.childCount;
        limb.instanceId = node.InstanceId;

        // Add sensors, neurons and effectors, but don't wire them up until later (when we have a complete morphology to reference).
        limb.photoSensors = new() { new(SensorType.PhotoSensor), new(SensorType.PhotoSensor), new(SensorType.PhotoSensor) };
        limb.neurons = node.LimbNode.NeuronDefinitions.Select(neuronDefinition => new Neuron(neuronDefinition)).ToList();
        limb.sensors = limb.photoSensors.ToList();
        limb.effectors = new();

        limb.activeColliders = new List<Collider> { limb.fullBodyCollider };

        Vector3 clampedDimensions = ClampDimensions(node.LimbNode.Dimensions);
        limb.unscaledDimensions = clampedDimensions;
        limb.Dimensions = clampedDimensions;

        limb.childLimbs = new List<Limb>();

        // Ensure that colliders are positioned correctly for potential children.
        Physics.SyncTransforms();

        return limb;
    }

    public static List<Limb> InstantiateLimbs(ReadOnlyCollection<InstancedLimbNode> instancedLimbNodes, Transform containerTransform)
    {
        List<Limb> limbs = new();

        Queue<(Limb, InstancedLimbNode)> nodeQueue = new();
        nodeQueue.Enqueue((null, instancedLimbNodes[0]));

        while (nodeQueue.Count > 0)
        {
            (Limb, InstancedLimbNode) item = nodeQueue.Dequeue();
            Limb parentLimb = item.Item1;
            InstancedLimbNode nextNode = item.Item2;

            Limb newLimb = parentLimb == null ? Limb.Construct(instancedLimbNodes[0], containerTransform) : parentLimb.AddChildLimb(nextNode);
            if (newLimb == null)
                continue;
            else
            {
                limbs.Add(newLimb);
                foreach (InstancedLimbNode childNode in nextNode.ChildLimbNodeInstances)
                    nodeQueue.Enqueue((newLimb, childNode));
            }
        }

        limbs.ForEach(limb => limb.Optimise());
        return limbs;
    }

    private static Vector3 ClampDimensions(Vector3 dimensions)
    {
        return new Vector3(
            Mathf.Clamp(dimensions.x, ParameterManager.Instance.Limb.MinSize, ParameterManager.Instance.Limb.MaxSize),
            Mathf.Clamp(dimensions.y, ParameterManager.Instance.Limb.MinSize, ParameterManager.Instance.Limb.MaxSize),
            Mathf.Clamp(dimensions.z, ParameterManager.Instance.Limb.MinSize, ParameterManager.Instance.Limb.MaxSize)
        );
    }

    private static Quaternion ReflectRotation(Quaternion source, Vector3 normal)
    {
        return Quaternion.LookRotation(Vector3.Reflect(source * Vector3.forward, normal), Vector3.Reflect(source * Vector3.up, normal));
    }

    public void Select(bool toggle, bool multiselect)
    {
        if (passSelectionToParent)
        {
            Phenotype parent = GetComponentInParent<Phenotype>();
            if (parent != null)
                parent.Select(toggle, multiselect);
        }
        else
        {
            if (toggle && SelectionManager.Instance.Selected.Contains(this))
            {
                if (multiselect)
                    SelectionManager.Instance.RemoveFromSelection(this);
                else
                    SelectionManager.Instance.SetSelected(null);
            }
            else
            {
                outline.enabled = true;
                void handler(List<ISelectable> previouslySelected, List<ISelectable> selected)
                {
                    if (!selected.Contains(this))
                    {
                        if (outline != null)
                            outline.enabled = false;
                        SelectionManager.Instance.OnSelectionChange -= handler;
                    }
                }
                SelectionManager.Instance.OnSelectionChange += handler;
                if (multiselect)
                    SelectionManager.Instance.AddToSelection(this);
                else
                    SelectionManager.Instance.SetSelected(new() { this });
            }
        }
    }

    public Bounds GetBounds()
    {
        return fullBodyCollider.bounds;
    }
}
