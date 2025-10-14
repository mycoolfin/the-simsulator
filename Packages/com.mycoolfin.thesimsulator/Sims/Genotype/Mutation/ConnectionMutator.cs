using System;
using System.Numerics;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    public static class ConnectionMutator
    {
        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext, int>> ConnectionMutations = new()
        {
            { MutateChildNodeId, 1f },
            { MutateParentFace, 1f },
            { MutatePosition, 1f },
            { MutateOrientation, 1f },
            { MutateScale, 1f },
            { MutateReflectionX, 1f },
            { MutateReflectionY, 1f },
            { MutateReflectionZ, 1f },
            { MutateTerminalOnly, 1f }
        };

        public static void MutateConnection(SimsGenotypeCreationContext context, int connectionIndex)
        {
            ConnectionMutations.Choose().Invoke(context, connectionIndex);
        }

        public static void MutateChildNodeId(SimsGenotypeCreationContext context, int connectionIndex)
        {
            Connection connection = context.Connections[connectionIndex];
            ulong newChildNodeId = context.Nodes[SharedRandom.Next(context.Nodes.Count)].Gid;
            Connection newConnection = new(connection.ParentNodeGid, newChildNodeId, connection.ParentFace, connection.Position, connection.Orientation, connection.Scale, connection.ReflectionX, connection.ReflectionY, connection.ReflectionZ, connection.TerminalOnly);
            context.Connections[connectionIndex] = newConnection;
        }

        public static void MutateParentFace(SimsGenotypeCreationContext context, int connectionIndex)
        {
            Connection connection = context.Connections[connectionIndex];
            int newParentFace = SharedRandom.Next(0, 6);
            Connection newConnection = new(connection.ParentNodeGid, connection.ChildNodeGid, newParentFace, connection.Position, connection.Orientation, connection.Scale, connection.ReflectionX, connection.ReflectionY, connection.ReflectionZ, connection.TerminalOnly);
            context.Connections[connectionIndex] = newConnection;
        }

        public static void MutatePosition(SimsGenotypeCreationContext context, int connectionIndex)
        {
            float sigma = (Connection.MAX_POSITION - Connection.MIN_POSITION) / 20f;
            Connection connection = context.Connections[connectionIndex];
            Vector2 newPosition = new(
                Math.Clamp(connection.Position.X + SharedRandom.DrawGaussian(sigma), Connection.MIN_POSITION, Connection.MAX_POSITION),
                Math.Clamp(connection.Position.Y + SharedRandom.DrawGaussian(sigma), Connection.MIN_POSITION, Connection.MAX_POSITION)
            );
            Connection newConnection = new(connection.ParentNodeGid, connection.ChildNodeGid, connection.ParentFace, newPosition, connection.Orientation, connection.Scale, connection.ReflectionX, connection.ReflectionY, connection.ReflectionZ, connection.TerminalOnly);
            context.Connections[connectionIndex] = newConnection;
        }

        public static void MutateOrientation(SimsGenotypeCreationContext context, int connectionIndex)
        {
            float sigma = (Connection.MAX_ORIENTATION - Connection.MIN_ORIENTATION) / 20f;
            Connection connection = context.Connections[connectionIndex];
            Vector3 newOrientation = new(
                Math.Clamp(connection.Orientation.X + SharedRandom.DrawGaussian(sigma), Connection.MIN_ORIENTATION, Connection.MAX_ORIENTATION),
                Math.Clamp(connection.Orientation.Y + SharedRandom.DrawGaussian(sigma), Connection.MIN_ORIENTATION, Connection.MAX_ORIENTATION),
                Math.Clamp(connection.Orientation.Z + SharedRandom.DrawGaussian(sigma), Connection.MIN_ORIENTATION, Connection.MAX_ORIENTATION)
            );
            Connection newConnection = new(connection.ParentNodeGid, connection.ChildNodeGid, connection.ParentFace, connection.Position, newOrientation, connection.Scale, connection.ReflectionX, connection.ReflectionY, connection.ReflectionZ, connection.TerminalOnly);
            context.Connections[connectionIndex] = newConnection;
        }

        public static void MutateScale(SimsGenotypeCreationContext context, int connectionIndex)
        {
            float sigma = 0.1f;
            Connection connection = context.Connections[connectionIndex];
            Vector3 newScale = new(
                Math.Clamp(connection.Scale.X * (1.0f + SharedRandom.DrawGaussian(sigma)), Connection.MIN_SCALE, Connection.MAX_SCALE),
                Math.Clamp(connection.Scale.Y * (1.0f + SharedRandom.DrawGaussian(sigma)), Connection.MIN_SCALE, Connection.MAX_SCALE),
                Math.Clamp(connection.Scale.Z * (1.0f + SharedRandom.DrawGaussian(sigma)), Connection.MIN_SCALE, Connection.MAX_SCALE)
            );
            Connection newConnection = new(connection.ParentNodeGid, connection.ChildNodeGid, connection.ParentFace, connection.Position, connection.Orientation, newScale, connection.ReflectionX, connection.ReflectionY, connection.ReflectionZ, connection.TerminalOnly);
            context.Connections[connectionIndex] = newConnection;
        }

        public static void MutateReflectionX(SimsGenotypeCreationContext context, int connectionIndex)
        {
            Connection connection = context.Connections[connectionIndex];
            bool newReflectionX = !connection.ReflectionX;
            Connection newConnection = new(connection.ParentNodeGid, connection.ChildNodeGid, connection.ParentFace, connection.Position, connection.Orientation, connection.Scale, newReflectionX, connection.ReflectionY, connection.ReflectionZ, connection.TerminalOnly);
            context.Connections[connectionIndex] = newConnection;
        }

        public static void MutateReflectionY(SimsGenotypeCreationContext context, int connectionIndex)
        {
            Connection connection = context.Connections[connectionIndex];
            bool newReflectionY = !connection.ReflectionY;
            Connection newConnection = new(connection.ParentNodeGid, connection.ChildNodeGid, connection.ParentFace, connection.Position, connection.Orientation, connection.Scale, connection.ReflectionX, newReflectionY, connection.ReflectionZ, connection.TerminalOnly);
            context.Connections[connectionIndex] = newConnection;
        }

        public static void MutateReflectionZ(SimsGenotypeCreationContext context, int connectionIndex)
        {
            Connection connection = context.Connections[connectionIndex];
            bool newReflectionZ = !connection.ReflectionZ;
            Connection newConnection = new(connection.ParentNodeGid, connection.ChildNodeGid, connection.ParentFace, connection.Position, connection.Orientation, connection.Scale, connection.ReflectionX, connection.ReflectionY, newReflectionZ, connection.TerminalOnly);
            context.Connections[connectionIndex] = newConnection;
        }

        public static void MutateTerminalOnly(SimsGenotypeCreationContext context, int connectionIndex)
        {
            Connection connection = context.Connections[connectionIndex];
            bool newTerminalOnly = !connection.TerminalOnly;
            Connection newConnection = new(connection.ParentNodeGid, connection.ChildNodeGid, connection.ParentFace, connection.Position, connection.Orientation, connection.Scale, connection.ReflectionX, connection.ReflectionY, connection.ReflectionZ, newTerminalOnly);
            context.Connections[connectionIndex] = newConnection;
        }
    }
}
