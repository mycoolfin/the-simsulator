using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public struct CreatedLimb
    {
        public Limb Limb { get; }
        public Vector3 CurrentScale { get; }

        public CreatedLimb(Limb limb, Vector3 currentScale)
        {
            Limb = limb;
            CurrentScale = currentScale;
        }
    }

    public static class LimbCreator
    {
        public static CreatedLimb CreateChildLimb(Limb parentLimb, Vector3 parentScale, Node node, Connection? connection, List<NeuronDefinition> neuronDefinitions,
            bool isParentLimbFaceRightMirrored, bool isParentLimbFaceUpMirrored, bool isParentLimbFaceForwardMirrored, bool mirrorByFaceRight, bool mirrorByFaceUp, bool mirrorByFaceForward,
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetGidMap
        )
        {
            // Compute new scale and dimensions (dimensions always positive).
            Vector3 currentScale = connection == null ? parentScale : Vector3.Scale(parentScale, connection.Value.Scale);
            Vector3 dimensions = new(
                node.Dimensions.X * currentScale.X,
                node.Dimensions.Y * currentScale.Y,
                node.Dimensions.Z * currentScale.Z
            );
            Limb newLimb = new(dimensions);
            newLimb.debugMirroredX = mirrorByFaceRight; // TODO: remove these.
            newLimb.debugMirroredY = mirrorByFaceUp;
            newLimb.debugMirroredZ = mirrorByFaceForward;

            // If no connection, return root limb here.
            if (connection == null)
                return new CreatedLimb(newLimb, currentScale);

            // Compute face axes and normal.
            // Left-handed coordinate system (Unity compatible).
            GetFaceAxes(connection.Value.ParentFace, parentLimb.Rotation, out Vector3 faceRight, out Vector3 faceUp);
            Vector3 faceForward = Vector3.Cross(faceUp, faceRight);

            // Apply existing mirroring transformation based on parent limb's face axes.
            if (isParentLimbFaceRightMirrored)
            {
                faceRight = -faceRight;
                faceForward = Vector3.Cross(faceUp, faceRight);
            }
            if (isParentLimbFaceUpMirrored)
            {
                faceUp = -faceUp;
                faceForward = Vector3.Cross(faceUp, faceRight);
            }
            if (isParentLimbFaceForwardMirrored)
            {
                faceForward = -faceForward;
                faceRight = Vector3.Cross(faceForward, faceUp);
            }

            // Compute local anchors.
            Vector2 connectionPosition = new(
                connection.Value.Position.X,
                connection.Value.Position.Y
            );
            Quaternion connectionOrientation = Quaternion.Euler(
                connection.Value.Orientation.X,
                connection.Value.Orientation.Y,
                connection.Value.Orientation.Z
            );
            Vector3 parentHalfExtents = parentLimb.Dimensions * 0.5f;
            Vector3 localParentAnchor = Vector3.Scale(parentHalfExtents, -faceForward)
                                      + Vector3.Scale(parentHalfExtents, faceRight) * connectionPosition.X
                                      + Vector3.Scale(parentHalfExtents, faceUp) * connectionPosition.Y;

            // Apply mirroring transformations to orientation only.
            if (mirrorByFaceRight)
            {
                connectionOrientation = ReflectRotationAcrossPlane(connectionOrientation, faceRight);
                faceRight = -faceRight;
                faceForward = Vector3.Cross(faceUp, faceRight);
            }
            if (mirrorByFaceUp)
            {
                connectionOrientation = ReflectRotationAcrossPlane(connectionOrientation, faceUp);
                faceUp = -faceUp;
                faceForward = Vector3.Cross(faceUp, faceRight);
            }
            if (mirrorByFaceForward)
            {
                connectionOrientation = ReflectRotationAcrossPlane(connectionOrientation, faceForward);
                faceRight = -faceRight;
                faceForward = Vector3.Cross(faceUp, faceRight);
            }

            // Compute world position.
            Vector3 worldParentAnchor = parentLimb.Position + parentLimb.Rotation * localParentAnchor;

            // Compute child rotation first 
            Quaternion worldChildRotation = parentLimb.Rotation * connectionOrientation;

            // Compute child anchor in child local space.
            // Always at the center of the -Z face, regardless of mirroring.
            Vector3 localChildAnchor = new(0f, 0f, -0.5f * dimensions.Z);

            Vector3 childFaceWorldOffset = worldChildRotation * localChildAnchor;
            Vector3 worldChildPosition = worldParentAnchor - childFaceWorldOffset;

            // Apply mirroring transformations to final position.
            if (mirrorByFaceRight)
            {
                Vector3 parentRight = parentLimb.Rotation * new Vector3(1, 0, 0);
                worldChildPosition = ReflectPointAcrossPlane(worldChildPosition, parentLimb.Position, parentRight);
            }
            if (mirrorByFaceUp)
            {
                Vector3 parentUp = parentLimb.Rotation * new Vector3(0, 1, 0);
                worldChildPosition = ReflectPointAcrossPlane(worldChildPosition, parentLimb.Position, parentUp);
            }
            if (mirrorByFaceForward)
            {
                Vector3 parentForward = parentLimb.Rotation * new Vector3(0, 0, 1);
                worldChildPosition = ReflectPointAcrossPlane(worldChildPosition, parentLimb.Position, parentForward);
            }

            // Set transform.
            newLimb.SetPositionAndRotation(worldChildPosition, worldChildRotation);

            // Create and assign joint.
            JointBase joint = JointFactory.CreateJoint(
                node.JointDefinition.JointType,
                parentLimb,
                newLimb,
                localParentAnchor,
                localChildAnchor,
                node.JointDefinition.AngleLimits
            );
            newLimb.SetJoint(joint);
            if (joint.XAxis != null)
                receiverToInputDefinitionSetGidMap[joint.XAxis.Actuator] = node.JointDefinition.XAxisInputs;
            if (joint.YAxis != null)
                receiverToInputDefinitionSetGidMap[joint.YAxis.Actuator] = node.JointDefinition.YAxisInputs;
            if (joint.ZAxis != null)
                receiverToInputDefinitionSetGidMap[joint.ZAxis.Actuator] = node.JointDefinition.ZAxisInputs;

            // Create and assign unwired neurons.
            List<Neuron> neurons = new();
            if (neuronDefinitions != null)
            {
                foreach (NeuronDefinition nd in neuronDefinitions)
                {
                    Neuron neuron = new(nd.ActivationFunction);
                    neurons.Add(neuron);
                    receiverToInputDefinitionSetGidMap[neuron] = nd.Inputs;
                }
            }
            newLimb.SetNeurons(neurons);

            return new CreatedLimb(newLimb, currentScale);
        }

        private static void GetFaceAxes(int face, Quaternion parentRotation, out Vector3 faceRight, out Vector3 faceUp)
        {
            switch (face)
            {
                case 0: // -X (left face)
                    faceRight = parentRotation * new Vector3(0, 0, -1);
                    faceUp = parentRotation * new Vector3(0, 1, 0);
                    break;
                case 1: // -Y (bottom face)
                    faceRight = parentRotation * new Vector3(1, 0, 0);
                    faceUp = parentRotation * new Vector3(0, 0, 1);
                    break;
                case 2: // -Z (back face)
                    faceRight = parentRotation * new Vector3(-1, 0, 0);
                    faceUp = parentRotation * new Vector3(0, 1, 0);
                    break;
                case 3: // +X (right face)
                    faceRight = parentRotation * new Vector3(0, 0, 1);
                    faceUp = parentRotation * new Vector3(0, 1, 0);
                    break;
                case 4: // +Y (top face)
                    faceRight = parentRotation * new Vector3(-1, 0, 0);
                    faceUp = parentRotation * new Vector3(0, 0, 1);
                    break;
                case 5: // +Z (front face)
                    faceRight = parentRotation * new Vector3(1, 0, 0);
                    faceUp = parentRotation * new Vector3(0, 1, 0);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(face), $"Invalid face index {face}. Must be 0-5.");
            }
        }

        private static Vector3 ReflectPointAcrossPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
        {
            Vector3 n = planeNormal.Normalized;
            Vector3 toPoint = point - planePoint;
            float distance = Vector3.Dot(toPoint, n);
            return point - 2 * distance * n;
        }

        private static Quaternion ReflectRotationAcrossPlane(Quaternion rotation, Vector3 planeNormal)
        {
            // Normalize plane normal.
            Vector3 n = planeNormal.Normalized;

            // Rotate local basis vectors.
            Vector3 right = rotation * new Vector3(1, 0, 0);
            Vector3 up = rotation * new Vector3(0, 1, 0);
            Vector3 forward = rotation * new Vector3(0, 0, 1);

            // Reflect each basis vector across the plane.
            right -= 2 * Vector3.Dot(right, n) * n;
            up -= 2 * Vector3.Dot(up, n) * n;
            forward -= 2 * Vector3.Dot(forward, n) * n;

            // Reconstruct the quaternion from the reflected basis.
            return Quaternion.LookRotation(forward, up);
        }
    }
}
