﻿using UnityEngine;

public class UniversalJoint : JointBase
{
    protected override JointType TypeOfJoint => JointType.Universal;

    protected override void ApplySpecificJointSettings()
    {
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
