using UnityEngine;

public class BendTwistJoint : JointBase
{
    protected override JointType TypeOfJoint => JointType.BendTwist;

    protected override void ApplySpecificJointSettings()
    {
        joint.axis = Vector3.forward;
        joint.secondaryAxis = Vector3.right;
        joint.anchor = new Vector3(0, 0, -0.5f);
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.lowAngularXLimit = new SoftJointLimit
        {
            limit = -dofAngleLimits[1]
        };
        joint.highAngularXLimit = new SoftJointLimit
        {
            limit = dofAngleLimits[1]
        };
        joint.angularYLimit = new SoftJointLimit
        {
            limit = dofAngleLimits[0]
        };

        switchPrimaryAndSecondaryAxes = true; // BendTwist is just TwistBend with switched axes.
    }
}
