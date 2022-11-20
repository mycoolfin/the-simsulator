using UnityEngine;

public class BendTwistJoint : JointBase
{
    protected override JointType TypeOfJoint => JointType.BendTwist;

    public override void Initialise(Rigidbody connectedBody, float maximumJointStrength, float[] dofAngleLimits)
    {
        this.maximumJointStrength = maximumJointStrength;

        InitialiseDOFs(dofAngleLimits);

        ApplyCommonJointSettings(connectedBody, maximumJointStrength);

        joint.anchor = new Vector3(0, 0, -0.5f);
        joint.axis = Vector3.right;
        joint.secondaryAxis = Vector3.forward;
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.lowAngularXLimit = new SoftJointLimit
        {
            limit = -dofAngleLimits[0]
        };
        joint.highAngularXLimit = new SoftJointLimit
        {
            limit = dofAngleLimits[0]
        };
        joint.angularYLimit = new SoftJointLimit
        {
            limit = dofAngleLimits[1]
        };
    }
}
