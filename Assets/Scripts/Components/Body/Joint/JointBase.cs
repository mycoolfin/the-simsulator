using UnityEngine;

public abstract class JointBase : MonoBehaviour
{
    protected ConfigurableJoint joint;
    protected abstract void InitialiseJoint();

    private float maximumJointStrength;
    private Quaternion intialOriginRotation;

    private float primaryTorque;
    private float secondaryTorque;
    private float tertiaryTorque;

    private JointAngleEffector jointAngleEffector;
    private JointAngleSensor jointAngleSensor;

    // Debug.
    public Vector3 xVectorDisplay;
    public Vector3 yVectorDisplay;
    public Vector3 zVectorDisplay;


    private void Start()
    {

        intialOriginRotation = joint.connectedBody.transform.rotation * Quaternion.Inverse(transform.rotation);
    }

    private void FixedUpdate()
    {
        Quaternion updatedOriginRotation = Quaternion.Inverse(intialOriginRotation) * joint.connectedBody.transform.rotation;

        Vector3 primaryAxis = updatedOriginRotation * joint.axis; // Stays fixed with parent.
        Vector3 tertiaryAxis = transform.rotation * Vector3.Cross(joint.axis, joint.secondaryAxis); // Affected by primary and secondary axes.
        Vector3 secondaryAxis = -Vector3.Cross(primaryAxis, tertiaryAxis).normalized; // Affected by primary axis angle, but not tertiary axis angle.

        Vector3 primaryVector = maximumJointStrength * primaryTorque * primaryAxis;
        Vector3 secondaryVector = maximumJointStrength * secondaryTorque * secondaryAxis;
        Vector3 tertiaryVector = maximumJointStrength * tertiaryTorque * tertiaryAxis;

        GetComponent<Rigidbody>().AddTorque(primaryVector + secondaryVector + tertiaryVector);

        xVectorDisplay = primaryVector;
        yVectorDisplay = secondaryVector;
        zVectorDisplay = tertiaryVector;
    }

    public float getPrimaryAngle()
    {

    }

    public float getSecondaryAngle()
    {

    }

    public float getTertiaryAngle()
    {

    }

    public void setPrimaryTorque(float torque)
    {
        primaryTorque = torque;
    }

    public void setSecondaryTorque(float torque)
    {
        secondaryTorque = torque;
    }

    public void setTertiaryTorque(float torque)
    {
        tertiaryTorque = torque;
    }

    private void OnDrawGizmos()
    {
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + xVectorDisplay / maximumJointStrength, Color.red);
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + yVectorDisplay / maximumJointStrength, Color.green);
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + zVectorDisplay / maximumJointStrength, Color.blue);
    }
}
