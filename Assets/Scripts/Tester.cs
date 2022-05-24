using UnityEngine;
using System.Collections;

public class Tester : MonoBehaviour
{
    void Start()
    {
        Physics.gravity = Vector3.zero;

        // Create a test creature.
        GameObject creature = new GameObject("Test Creature");
        LimbNode root = LimbCreator.CreateLimb();
        root.transform.parent = creature.transform;
        root.Dimensions = new Vector3(0.3f, 0.3f, 2);

        root.transform.position = new Vector3(0, 2, 0);
        //root.transform.rotation = new Quaternion(0f, 0.2f, 0.4f, 1f);

        root.transform.GetComponent<Rigidbody>().isKinematic = true;

        LimbNode fll = root.AddChildLimb(0, 0f, 0.8f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.Rigid);
        LimbNode frl = root.AddChildLimb(3, 0f, 0.8f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.Rigid);
        LimbNode mll = root.AddChildLimb(0, 0f, 0f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.Rigid);
        LimbNode mrl = root.AddChildLimb(3, 0f, 0f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.Rigid);
        LimbNode bll = root.AddChildLimb(0, 0f, -0.8f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.Rigid);
        LimbNode brl = root.AddChildLimb(3, 0f, -0.8f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.Rigid);
        fll.AddChildLimb(5, 0f, 0f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.Revolute);
        frl.AddChildLimb(5, 0f, 0f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.Twist);
        mll.AddChildLimb(5, 0f, 0f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.BendTwist);
        mrl.AddChildLimb(5, 0f, 0f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.TwistBend);
        bll.AddChildLimb(5, 0f, 0f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.Spherical);
        brl.AddChildLimb(5, 0f, 0f, 45f, 0f, 0f, new Vector3(0.1f, 0.1f, 1f), JointType.Revolute);

        LimbNode antl = root.AddChildLimb(4, 1f, -1f, 0f, -20f, 0f, new Vector3(0.05f, 0.1f, 0.5f), JointType.Rigid);
        LimbNode antr = root.AddChildLimb(4, 1f, 1f, 0f, 20f, 0f, new Vector3(0.05f, 0.1f, 0.5f), JointType.Rigid);


    }

    void Update()
    {

    }
}
