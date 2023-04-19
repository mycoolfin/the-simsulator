using UnityEngine;

public class SphericalJoint : JointBase
{
    protected override JointType TypeOfJoint => JointType.Spherical;

    public override void ApplySpecificJointSettings()
    {
        joint.anchor = new Vector3(0, 0, -0.5f);
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;
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
        joint.angularZLimit = new SoftJointLimit
        {
            limit = dofAngleLimits[2]
        };
    }
}
