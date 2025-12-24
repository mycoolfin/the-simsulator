using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using SFB;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.IO
{
    using TheSimsulator.Core.Phenotype;
    using Core.ECS.Components.Shared;
    using Core.ECS.Components.Phenotype;

    public static class PhenotypeMeshExporter
    {
        /// <summary>
        /// Exports a phenotype snapshot to a 3MF file.
        /// </summary>
        /// <param name="name">The name of the phenotype (used for default file naming).</param>
        /// <param name="world">The ECS world containing the entities.</param>
        /// <param name="phenotype">The phenotype instance.</param>
        /// <param name="getBaseMesh">Function that returns the base mesh to use for all entities.</param>
        /// <param name="scaleFactor">Scale factor to apply to the entire model.</param>
        /// <param name="filePath">The output file path (.3mf extension). If null, a save file dialog will be shown.</param>
        public static void SavePhenotypeModelToFilePath<TPhenotype>(string name, World world, Func<Mesh> getBaseMesh, TPhenotype phenotype, float scaleFactor, Action<FileOperationResult, string> OnComplete, string filePath = null)
            where TPhenotype : IPhenotype<TPhenotype>
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = StandaloneFileBrowser.SaveFilePanel("Export Phenotype Model", "", $"{name}.3mf", "3mf");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                OnComplete?.Invoke(FileOperationResult.Cancelled, string.Empty);
                return; // User cancelled the save dialog.
            }

            try
            {
                // Normalize the path from the file dialog to ensure consistent separators.
                filePath = Path.GetFullPath(filePath);
            }
            catch (Exception)
            {
                OnComplete?.Invoke(FileOperationResult.Failure, string.Empty);
                return;
            }

            bool success = ExportToThreeMF(world, getBaseMesh, phenotype, scaleFactor, filePath);

            if (success)
            {
                OnComplete?.Invoke(FileOperationResult.Success, filePath);
            }
            else
            {
                OnComplete?.Invoke(FileOperationResult.Failure, string.Empty);
            }
        }

        private static bool ExportToThreeMF<TPhenotype>(World world, Func<Mesh> getBaseMesh, TPhenotype phenotype, float scaleFactor, string filePath)
            where TPhenotype : IPhenotype<TPhenotype>
        {
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("World is null or not created.");
                return false;
            }

            if (getBaseMesh == null)
            {
                Debug.LogError("getBaseMesh delegate is null.");
                return false;
            }

            if (phenotype == null)
            {
                Debug.LogError("Phenotype is null.");
                return false;
            }

            // Get the base mesh.
            Mesh baseMesh = getBaseMesh();
            if (baseMesh == null)
            {
                Debug.LogError("Failed to get base mesh from provided delegate.");
                return false;
            }

            EntityManager entityManager = world.EntityManager;

            // Find the root phenotype entity.
            EntityQuery rootQuery = entityManager.CreateEntityQuery(typeof(PhenotypeGid));
            using NativeArray<Entity> entities = rootQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<PhenotypeGid> phenotypeGids = rootQuery.ToComponentDataArray<PhenotypeGid>(Allocator.Temp);
            Entity rootPhenotypeEntity = Entity.Null;
            for (int i = 0; i < phenotypeGids.Length; i++)
            {
                if (phenotypeGids[i].Value == phenotype.Gid)
                {
                    rootPhenotypeEntity = entities[i];
                    break;
                }
            }

            if (rootPhenotypeEntity == Entity.Null)
            {
                Debug.LogError("Root phenotype entity does not exist in the world.");
                return false;
            }

            // Query all unit entities for this phenotype.
            EntityQuery unitQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<RootPhenotypeEntity>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<PostTransformMatrix>(),
                ComponentType.ReadOnly<URPMaterialPropertyBaseColor>()
            );

            using NativeArray<RootPhenotypeEntity> rootEntities = unitQuery.ToComponentDataArray<RootPhenotypeEntity>(Allocator.Temp);
            using NativeArray<LocalTransform> transforms = unitQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            using NativeArray<PostTransformMatrix> postTransforms = unitQuery.ToComponentDataArray<PostTransformMatrix>(Allocator.Temp);
            using NativeArray<URPMaterialPropertyBaseColor> colors = unitQuery.ToComponentDataArray<URPMaterialPropertyBaseColor>(Allocator.Temp);

            // Find all units belonging to the specified root phenotype.
            List<TransformedMeshData> unitMeshes = new();
            for (int i = 0; i < rootEntities.Length; i++)
            {
                if (rootEntities[i].Value == rootPhenotypeEntity)
                {
                    unitMeshes.Add(new TransformedMeshData
                    {
                        Transform = transforms[i],
                        PostTransform = postTransforms[i],
                        Color = colors[i].Value
                    });
                }
            }

            if (unitMeshes.Count == 0)
            {
                Debug.LogWarning("No units found for the specified root phenotype entity.");
                return false;
            }

            Debug.Log($"Found {unitMeshes.Count} units for export.");

            // Get bounding box center for centering.
            float3 boundingBoxCenter = float3.zero;
            if (entityManager.HasComponent<PhenotypeBoundingBox>(rootPhenotypeEntity))
            {
                PhenotypeBoundingBox boundingBox = entityManager.GetComponentData<PhenotypeBoundingBox>(rootPhenotypeEntity);
                boundingBoxCenter = boundingBox.Center;
            }

            // Combine meshes.
            CombinedMeshData combinedMesh = CombineMeshes(baseMesh, unitMeshes, scaleFactor, boundingBoxCenter);

            // Generate 3MF file.
            try
            {
                GenerateThreeMFFile(combinedMesh, filePath);
                Debug.Log($"Successfully exported phenotype to {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to generate 3MF file: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        private static CombinedMeshData CombineMeshes(Mesh baseMesh, List<TransformedMeshData> unitMeshes, float scaleFactor, float3 boundingBoxCenter)
        {
            Vector3[] baseVertices = baseMesh.vertices;
            int[] baseTriangles = baseMesh.triangles;

            List<Vector3> allVertices = new();
            List<Color> allColors = new();
            List<int> allTriangles = new();

            int vertexOffset = 0;

            // Convert from Unity's left-handed Y-up coordinate system to right-handed Z-up (standard for 3D printing).
            // Unity: +X right, +Y up, +Z forward (left-handed)
            // Standard: +X right, +Y forward, +Z up (right-handed)
            // Negate Z to flip handedness.
            float3 convertedBoundingBoxCenter = new(boundingBoxCenter.x, -boundingBoxCenter.z, boundingBoxCenter.y);
            convertedBoundingBoxCenter *= scaleFactor;

            foreach (var unitData in unitMeshes)
            {
                // Build transformation matrix.
                float4x4 transformMatrix = float4x4.TRS(
                    unitData.Transform.Position,
                    unitData.Transform.Rotation,
                    unitData.Transform.Scale
                );

                // Apply post-transform (scale/dimensions).
                float4x4 fullTransform = math.mul(transformMatrix, unitData.PostTransform.Value);

                // Apply global scale factor.
                fullTransform = math.mul(float4x4.Scale(scaleFactor), fullTransform);

                // Transform vertices.
                foreach (Vector3 vertex in baseVertices)
                {
                    float4 transformedVertex = math.mul(fullTransform, new float4(vertex.x, vertex.y, vertex.z, 1.0f));

                    // Convert coordinate system.
                    float3 convertedVertex = new(transformedVertex.x, -transformedVertex.z, transformedVertex.y);

                    // Apply centering offset (after coordinate conversion and scaling).
                    float3 centeredVertex = convertedVertex - convertedBoundingBoxCenter;

                    allVertices.Add(new Vector3(centeredVertex.x, centeredVertex.y, centeredVertex.z));

                    // Add color for this vertex.
                    Color color = new(unitData.Color.x, unitData.Color.y, unitData.Color.z, unitData.Color.w);
                    allColors.Add(color);
                }

                // Add triangles with offset.
                foreach (int triangle in baseTriangles)
                {
                    allTriangles.Add(triangle + vertexOffset);
                }

                vertexOffset += baseVertices.Length;
            }

            return new CombinedMeshData
            {
                Vertices = allVertices.ToArray(),
                Colors = allColors.ToArray(),
                Triangles = allTriangles.ToArray()
            };
        }

        private static void GenerateThreeMFFile(CombinedMeshData meshData, string filePath)
        {
            // Create a temporary directory for 3MF contents.
            string tempDir = Path.Combine(Path.GetTempPath(), $"3mf_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.Combine(tempDir, "_rels"));
            Directory.CreateDirectory(Path.Combine(tempDir, "3D"));

            try
            {
                // Generate [Content_Types].xml
                string contentTypesXml = GenerateContentTypesXml();
                File.WriteAllText(Path.Combine(tempDir, "[Content_Types].xml"), contentTypesXml);

                // Generate _rels/.rels
                string relsXml = GenerateRelsXml();
                File.WriteAllText(Path.Combine(tempDir, "_rels", ".rels"), relsXml);

                // Generate 3D/3dmodel.model
                string modelXml = Generate3DModelXml(meshData);
                File.WriteAllText(Path.Combine(tempDir, "3D", "3dmodel.model"), modelXml);

                // Create ZIP archive.
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                ZipFile.CreateFromDirectory(tempDir, filePath, System.IO.Compression.CompressionLevel.Optimal, false);
            }
            finally
            {
                // Clean up temp directory.
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private static string GenerateContentTypesXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
    <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
    <Default Extension=""model"" ContentType=""application/vnd.ms-package.3dmanufacturing-3dmodel+xml""/>
</Types>";
        }

        private static string GenerateRelsXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
    <Relationship Target=""/3D/3dmodel.model"" Type=""http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel"" Id=""rel0""/>
</Relationships>";
        }

        private static string Generate3DModelXml(CombinedMeshData meshData)
        {
            StringBuilder sb = new();

            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<model unit=""millimeter"" xml:lang=""en-US"" xmlns=""http://schemas.microsoft.com/3dmanufacturing/core/2015/02"">");
            sb.AppendLine(@"  <resources>");

            // Build unique color palette and mapping.
            Dictionary<string, int> colorToIdMap = new();
            List<Color> uniqueColors = new();

            foreach (Color color in meshData.Colors)
            {
                string colorKey = ColorToHex(color);
                if (!colorToIdMap.ContainsKey(colorKey))
                {
                    colorToIdMap[colorKey] = uniqueColors.Count;
                    uniqueColors.Add(color);
                }
            }

            // Define base materials (colors).
            sb.AppendLine(@"    <basematerials id=""1"">");
            for (int i = 0; i < uniqueColors.Count; i++)
            {
                Color color = uniqueColors[i];
                string colorHex = ColorToHex(color);
                sb.AppendLine($"      <base name=\"Color{i}\" displaycolor=\"{colorHex}\"/>");
            }
            sb.AppendLine(@"    </basematerials>");

            sb.AppendLine(@"    <object id=""2"" type=""model"">");
            sb.AppendLine(@"      <mesh>");

            // Vertices.
            sb.AppendLine(@"        <vertices>");
            for (int i = 0; i < meshData.Vertices.Length; i++)
            {
                Vector3 v = meshData.Vertices[i];
                // Convert to millimeters (assuming Unity units are meters).
                sb.AppendLine($"          <vertex x=\"{v.x * 1000:F6}\" y=\"{v.y * 1000:F6}\" z=\"{v.z * 1000:F6}\"/>");
            }
            sb.AppendLine(@"        </vertices>");

            // Triangles with material references.
            sb.AppendLine(@"        <triangles>");
            for (int i = 0; i < meshData.Triangles.Length; i += 3)
            {
                int v1 = meshData.Triangles[i];
                int v2 = meshData.Triangles[i + 1];
                int v3 = meshData.Triangles[i + 2];

                // Get color from first vertex of triangle (all vertices of same unit have same color).
                Color color = meshData.Colors[v1];
                string colorKey = ColorToHex(color);
                int materialId = colorToIdMap[colorKey];

                sb.AppendLine($"          <triangle v1=\"{v1}\" v2=\"{v2}\" v3=\"{v3}\" pid=\"1\" p1=\"{materialId}\" p2=\"{materialId}\" p3=\"{materialId}\"/>");
            }
            sb.AppendLine(@"        </triangles>");

            sb.AppendLine(@"      </mesh>");
            sb.AppendLine(@"    </object>");
            sb.AppendLine(@"  </resources>");
            sb.AppendLine(@"  <build>");
            sb.AppendLine(@"    <item objectid=""2""/>");
            sb.AppendLine(@"  </build>");
            sb.AppendLine(@"</model>");

            return sb.ToString();
        }

        private static string ColorToHex(Color color)
        {
            // Convert RGBA to hex format for 3MF: #RRGGBBAA.
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);
            int a = Mathf.RoundToInt(color.a * 255);
            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        private struct TransformedMeshData
        {
            public LocalTransform Transform;
            public PostTransformMatrix PostTransform;
            public float4 Color;
        }

        private struct CombinedMeshData
        {
            public Vector3[] Vertices;
            public Color[] Colors;
            public int[] Triangles;
        }
    }
}
