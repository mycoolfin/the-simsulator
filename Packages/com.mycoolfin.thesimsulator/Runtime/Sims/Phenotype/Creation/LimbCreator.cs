using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public static class LimbCreator
    {
        public static Limb CreateChildLimb(Limb parentLimb, ref Vector3 parentScale, Node node, Connection? connection, List<NeuronDefinition> neuronDefinitions,
            bool mirrorX, bool mirrorY, bool mirrorZ,
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetGidMap
        )
        {
            // 1. Compute mirroring scale.
            Vector3 mirrorScale = new(
                mirrorX ? -1f : 1f,
                mirrorY ? -1f : 1f,
                mirrorZ ? -1f : 1f
            );

            // 2. Compute new scale and dimensions (dimensions always positive).
            parentScale = connection == null ? parentScale : Vector3.Scale(parentScale, connection.Value.Scale);
            Vector3 dimensions = CalculateChildDimensions(parentScale, node);
            Limb newLimb = new(dimensions);

            // 3. If no connection, return root limb.
            if (connection == null)
                return newLimb;

            // 4. Compute effective parent face.
            int effectiveParentFace = mirrorZ ? (connection.Value.ParentFace + 3) % 6 : connection.Value.ParentFace;

            // 5. Compute face axes and normal.
            GetFaceAxes(effectiveParentFace, parentLimb.Rotation, out var parentFaceNormal, out _, out _);

            // 6. Compute mirrored orientation.
            Vector3 mirroredEuler = Vector3.Scale(connection.Value.Orientation, mirrorScale);

            // 7. Compute child rotation.
            Quaternion rotation = CalculateChildRotation(parentLimb, parentFaceNormal, effectiveParentFace, mirroredEuler);

            // 8. Compute local anchors.
            Vector3 localChildAnchor = Vector3.Scale(new Vector3(0, 0, -0.5f * dimensions.Z), mirrorScale);
            Vector3 localParentAnchor = GetLocalParentAnchor(parentLimb.Dimensions, effectiveParentFace, mirrorScale);

            // 9. Compute world position.
            Vector3 position = CalculateChildPosition(parentLimb, parentFaceNormal, connection.Value, rotation, localChildAnchor, effectiveParentFace);

            // 10. Set transform.
            newLimb.SetPositionAndRotation(position, rotation);

            // 11. Create and assign joint.
            JointBase joint = JointFactory.CreateJoint(
                node.JointDefinition.JointType,
                parentLimb,
                newLimb,
                localParentAnchor,
                localChildAnchor,
                node.JointDefinition.AngleLimits
            );
            newLimb.SetJoint(joint);
            receiverToInputDefinitionSetGidMap[joint.XAxis.Actuator] = node.JointDefinition.XAxisInputs;
            receiverToInputDefinitionSetGidMap[joint.YAxis.Actuator] = node.JointDefinition.YAxisInputs;
            receiverToInputDefinitionSetGidMap[joint.ZAxis.Actuator] = node.JointDefinition.ZAxisInputs;

            // 12. Create and assign unwired neurons.
            List<Neuron> neurons = new();
            foreach (NeuronDefinition nd in neuronDefinitions)
            {
                Neuron neuron = new(nd.ActivationFunction);
                neurons.Add(neuron);
                receiverToInputDefinitionSetGidMap[neuron] = nd.Inputs;
            }
            newLimb.SetNeurons(neurons);

            return newLimb;
        }

        // Compute local anchor on parent limb for a given face and mirroring.
        private static Vector3 GetLocalParentAnchor(Vector3 parentDimensions, int face, Vector3 mirrorScale)
        {
            float x = (face == 0 ? +0.5f : face == 3 ? -0.5f : 0f) * parentDimensions.X;
            float y = (face == 1 ? +0.5f : face == 4 ? -0.5f : 0f) * parentDimensions.Y;
            float z = (face == 2 ? +0.5f : face == 5 ? -0.5f : 0f) * parentDimensions.Z;
            return Vector3.Scale(new Vector3(x, y, z), mirrorScale);
        }

        // The child's Z axis is aligned with the chosen parent face normal.
        private static Quaternion CalculateChildRotation(Limb parentLimb, Vector3 parentFaceNormal, int effectiveParentFace, Vector3 eulerAngles)
        {
            Vector3 parentUp = (effectiveParentFace == 1 || effectiveParentFace == 4)
                ? parentLimb.Rotation * new Vector3(0, 0, 1)
                : parentLimb.Rotation * new Vector3(0, 1, 0);
            Quaternion alignToFace = Quaternion.LookRotation(parentFaceNormal, parentUp);
            return alignToFace * Quaternion.Euler(eulerAngles.X, eulerAngles.Y, eulerAngles.Z);
        }

        private static Vector3 CalculateChildDimensions(Vector3 currentScale, Node node)
        {
            return new(
                node.Dimensions.X * currentScale.X,
                node.Dimensions.Y * currentScale.Y,
                node.Dimensions.Z * currentScale.Z
            );
        }

        // Returns the face normal, right, and up axes for a given face index, ensuring consistent orientation.
        private static void GetFaceAxes(int face, Quaternion parentRotation, out Vector3 faceNormal, out Vector3 faceRight, out Vector3 faceUp)
        {
            // Convention: when looking at the face from outside, right is +X, up is +Y in face-local space.
            switch (face)
            {
                case 0: // +X
                    faceNormal = parentRotation * new Vector3(1, 0, 0);
                    faceRight = parentRotation * new Vector3(0, 0, 1); // +Z
                    faceUp = parentRotation * new Vector3(0, 1, 0); // +Y
                    break;
                case 1: // +Y
                    faceNormal = parentRotation * new Vector3(0, 1, 0);
                    faceRight = parentRotation * new Vector3(1, 0, 0); // +X
                    faceUp = parentRotation * new Vector3(0, 0, -1); // -Z
                    break;
                case 2: // +Z
                    faceNormal = parentRotation * new Vector3(0, 0, 1);
                    faceRight = parentRotation * new Vector3(1, 0, 0); // +X
                    faceUp = parentRotation * new Vector3(0, 1, 0); // +Y
                    break;
                case 3: // -X
                    faceNormal = parentRotation * new Vector3(-1, 0, 0);
                    faceRight = parentRotation * new Vector3(0, 0, -1); // -Z
                    faceUp = parentRotation * new Vector3(0, 1, 0); // +Y
                    break;
                case 4: // -Y
                    faceNormal = parentRotation * new Vector3(0, -1, 0);
                    faceRight = parentRotation * new Vector3(1, 0, 0); // +X
                    faceUp = parentRotation * new Vector3(0, 0, 1); // +Z
                    break;
                case 5: // -Z
                    faceNormal = parentRotation * new Vector3(0, 0, -1);
                    faceRight = parentRotation * new Vector3(-1, 0, 0); // -X
                    faceUp = parentRotation * new Vector3(0, 1, 0); // +Y
                    break;
                default:
                    faceNormal = faceRight = faceUp = Vector3.Zero;
                    break;
            }
        }

        // The center of the Z- face of the child limb equals the specified position on the parent limb's face.
        private static Vector3 CalculateChildPosition(Limb parentLimb, Vector3 parentFaceNormal, Connection connection, Quaternion childLimbRotation, Vector3 localChildAnchor, int effectiveParentFace)
        {
            // Use new helper for axes
            GetFaceAxes(effectiveParentFace, parentLimb.Rotation, out var faceNormal, out var faceRight, out var faceUp);

            Vector3 parentFaceCenter = parentLimb.Position + faceNormal * 0.5f * (effectiveParentFace switch
            {
                0 or 3 => parentLimb.Dimensions.X,
                1 or 4 => parentLimb.Dimensions.Y,
                2 or 5 => parentLimb.Dimensions.Z,
                _ => 0f
            });

            // Calculate the offset on the parent face (connection.Position is in [-1, 1]x[-1, 1] on the face).
            Vector3 offset = faceRight * connection.Position.X * 0.5f * (effectiveParentFace switch
            {
                0 or 3 => parentLimb.Dimensions.Z,
                1 or 4 => parentLimb.Dimensions.X,
                2 or 5 => parentLimb.Dimensions.X,
                _ => 0f
            }) + faceUp * connection.Position.Y * 0.5f * (effectiveParentFace switch
            {
                0 or 3 => parentLimb.Dimensions.Y,
                1 or 4 => parentLimb.Dimensions.Z,
                2 or 5 => parentLimb.Dimensions.Y,
                _ => 0f
            });

            Vector3 anchorWorld = parentFaceCenter + offset;

            // Transform this offset into world space using the child's rotation.
            Vector3 childFaceWorldOffset = childLimbRotation * localChildAnchor;

            // The child position is the anchor minus the offset.
            return anchorWorld - childFaceWorldOffset;
        }
    }
}
