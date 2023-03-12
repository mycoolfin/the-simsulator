using System.Collections.Generic;
using UnityEngine;

public class UniversalJoint : JointBase
{
    protected override JointType TypeOfJoint => JointType.Universal;

    public override void ApplySpecificJointSettings(List<float> dofAngleLimits)
    {
        joint.axis = Vector3.forward;
        joint.secondaryAxis = Vector3.right;
        joint.anchor = new Vector3(0, 0, -0.5f);
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;
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
