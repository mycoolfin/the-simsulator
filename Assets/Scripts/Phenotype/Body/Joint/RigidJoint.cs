using UnityEngine;

public class RigidJoint : JointBase
{
    protected override int DegreesOfFreedom => 0;

    public override void Initialise(Rigidbody connectedBody, float maximumJointStrength, float[] dofAngleLimits)
    {
        joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = connectedBody;
        joint.anchor = new Vector3(0, 0, -0.5f);
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        this.maximumJointStrength = maximumJointStrength;
    }
}
