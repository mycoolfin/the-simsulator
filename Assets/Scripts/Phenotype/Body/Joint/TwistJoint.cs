using UnityEngine;

public class TwistJoint : JointBase
{
    protected override JointType TypeOfJoint => JointType.Twist;

    public override void ApplySpecificJointSettings()
    {
        joint.axis = Vector3.forward;
        joint.secondaryAxis = Vector3.right;
        joint.anchor = new Vector3(0, 0, -0.5f);
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.lowAngularXLimit = new SoftJointLimit
        {
            limit = -dofAngleLimits[0]
        };
        joint.highAngularXLimit = new SoftJointLimit
        {
            limit = dofAngleLimits[0]
        };
    }
}
