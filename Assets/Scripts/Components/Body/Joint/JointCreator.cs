using UnityEngine;

public enum JointType
{
    Rigid,
    Revolute,
    Twist,
    Universal,
    BendTwist,
    TwistBend,
    Spherical
}

public static class JointCreator
{
    public static ConfigurableJoint CreateJoint(LimbNode childLimb, LimbNode parentLimb, JointType jointType)
    {
        switch (jointType)
        {
            case JointType.Rigid:
                ConfigurableJoint rigidJoint = childLimb.gameObject.AddComponent<ConfigurableJoint>();
                rigidJoint.connectedBody = parentLimb.GetComponent<Rigidbody>();
                rigidJoint.anchor = new Vector3(0, 0, -childLimb.transform.localScale.z / 2);
                rigidJoint.xMotion = ConfigurableJointMotion.Locked;
                rigidJoint.yMotion = ConfigurableJointMotion.Locked;
                rigidJoint.zMotion = ConfigurableJointMotion.Locked;
                rigidJoint.angularXMotion = ConfigurableJointMotion.Locked;
                rigidJoint.angularYMotion = ConfigurableJointMotion.Locked;
                rigidJoint.angularZMotion = ConfigurableJointMotion.Locked;
                return rigidJoint;
            case JointType.Revolute:
                ConfigurableJoint revoluteJoint = childLimb.gameObject.AddComponent<ConfigurableJoint>();
                revoluteJoint.connectedBody = parentLimb.GetComponent<Rigidbody>();
                revoluteJoint.anchor = new Vector3(0, 0, -childLimb.transform.localScale.z / 2);
                revoluteJoint.xMotion = ConfigurableJointMotion.Locked;
                revoluteJoint.yMotion = ConfigurableJointMotion.Locked;
                revoluteJoint.zMotion = ConfigurableJointMotion.Locked;
                revoluteJoint.angularXMotion = ConfigurableJointMotion.Limited;
                revoluteJoint.angularYMotion = ConfigurableJointMotion.Locked;
                revoluteJoint.angularZMotion = ConfigurableJointMotion.Locked;
                revoluteJoint.lowAngularXLimit = new SoftJointLimit
                {
                    limit = -90f
                };
                revoluteJoint.highAngularXLimit = new SoftJointLimit
                {
                    limit = 90f
                };
                return revoluteJoint;
            case JointType.Twist:
                ConfigurableJoint twistJoint = childLimb.gameObject.AddComponent<ConfigurableJoint>();
                twistJoint.connectedBody = parentLimb.GetComponent<Rigidbody>();
                twistJoint.anchor = new Vector3(0, 0, -childLimb.transform.localScale.z / 2);
                twistJoint.xMotion = ConfigurableJointMotion.Locked;
                twistJoint.yMotion = ConfigurableJointMotion.Locked;
                twistJoint.zMotion = ConfigurableJointMotion.Locked;
                twistJoint.angularXMotion = ConfigurableJointMotion.Locked;
                twistJoint.angularYMotion = ConfigurableJointMotion.Locked;
                twistJoint.angularZMotion = ConfigurableJointMotion.Limited;
                twistJoint.angularZLimit = new SoftJointLimit
                {
                    limit = 90f
                };
                return twistJoint;
            case JointType.Universal:
                ConfigurableJoint universalJoint = childLimb.gameObject.AddComponent<ConfigurableJoint>(); // TODO: Spherical, with one rotation actuation, scaled based on angle.
                return universalJoint;
            case JointType.BendTwist:
                ConfigurableJoint bendTwistJoint = childLimb.gameObject.AddComponent<ConfigurableJoint>();
                bendTwistJoint.connectedBody = parentLimb.GetComponent<Rigidbody>();
                bendTwistJoint.anchor = new Vector3(0, 0, -childLimb.transform.localScale.z / 2);
                bendTwistJoint.xMotion = ConfigurableJointMotion.Locked;
                bendTwistJoint.yMotion = ConfigurableJointMotion.Locked;
                bendTwistJoint.zMotion = ConfigurableJointMotion.Locked;
                bendTwistJoint.angularXMotion = ConfigurableJointMotion.Limited;
                bendTwistJoint.angularYMotion = ConfigurableJointMotion.Locked;
                bendTwistJoint.angularZMotion = ConfigurableJointMotion.Limited;
                bendTwistJoint.lowAngularXLimit = new SoftJointLimit
                {
                    limit = -90f
                };
                bendTwistJoint.highAngularXLimit = new SoftJointLimit
                {
                    limit = 90f
                };
                bendTwistJoint.angularZLimit = new SoftJointLimit
                {
                    limit = 90f
                };
                return bendTwistJoint;
            case JointType.TwistBend:
                ConfigurableJoint twistBendJoint = childLimb.gameObject.AddComponent<ConfigurableJoint>();
                twistBendJoint.connectedBody = parentLimb.GetComponent<Rigidbody>();
                twistBendJoint.anchor = new Vector3(0, 0, -childLimb.transform.localScale.z / 2);
                twistBendJoint.axis = Vector3.forward;
                twistBendJoint.xMotion = ConfigurableJointMotion.Locked;
                twistBendJoint.yMotion = ConfigurableJointMotion.Locked;
                twistBendJoint.zMotion = ConfigurableJointMotion.Locked;
                twistBendJoint.angularXMotion = ConfigurableJointMotion.Limited;
                twistBendJoint.angularYMotion = ConfigurableJointMotion.Locked;
                twistBendJoint.angularZMotion = ConfigurableJointMotion.Limited;
                twistBendJoint.lowAngularXLimit = new SoftJointLimit
                {
                    limit = -90f
                };
                twistBendJoint.highAngularXLimit = new SoftJointLimit
                {
                    limit = 90f
                };
                twistBendJoint.angularZLimit = new SoftJointLimit
                {
                    limit = 90f
                };
                return twistBendJoint;
            case JointType.Spherical:
                ConfigurableJoint sphericalJoint = childLimb.gameObject.AddComponent<ConfigurableJoint>();
                sphericalJoint.connectedBody = parentLimb.GetComponent<Rigidbody>();
                sphericalJoint.anchor = new Vector3(0, 0, -childLimb.transform.localScale.z / 2);
                sphericalJoint.xMotion = ConfigurableJointMotion.Locked;
                sphericalJoint.yMotion = ConfigurableJointMotion.Locked;
                sphericalJoint.zMotion = ConfigurableJointMotion.Locked;
                sphericalJoint.angularXMotion = ConfigurableJointMotion.Limited;
                sphericalJoint.angularYMotion = ConfigurableJointMotion.Limited;
                sphericalJoint.angularZMotion = ConfigurableJointMotion.Limited;
                sphericalJoint.lowAngularXLimit = new SoftJointLimit
                {
                    limit = -90f
                };
                sphericalJoint.highAngularXLimit = new SoftJointLimit
                {
                    limit = 90f
                };
                sphericalJoint.angularYLimit = new SoftJointLimit
                {
                    limit = 80f
                };
                sphericalJoint.angularZLimit = new SoftJointLimit
                {
                    limit = 80f
                };
                return sphericalJoint;
            default:
                throw new UnityException("Joint type not recognised.");
        }
    }
}
