using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using mycoolfin.TheSimsulator.Sims.Genotype;
using mycoolfin.TheSimsulator.Sims.Phenotype;
using mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API;
using mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.API;
using mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Limbs;
using Unity.Entities;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

/// <summary>
/// Spawns all permutations of joint types (7) × face indices (6) in a grid layout.
/// Total: 42 creatures displayed simultaneously for visual inspection.
/// </summary>
public class JointPermutationGrid : MonoBehaviour
{
    [Header("Grid Layout")]
    [Tooltip("Spacing between creatures horizontally (per face index)")]
    public float horizontalSpacing = 5f;
    
    [Tooltip("Spacing between creatures vertically (per joint type)")]
    public float verticalSpacing = 5f;
    
    [Tooltip("Starting height for spawning")]
    public float spawnHeight = 2f;

    [Header("Creature Configuration")]
    [Tooltip("Recursive limit for test creatures")]
    public int recursiveLimit = 2;
    
    private SimsPhenotypeFactory phenotypeFactory;
    private SimsPhenotypeEntityManagement phenotypeEntityManagement;
    
    private JointType[] allJointTypes = new[]
    {
        JointType.Rigid,
        JointType.Revolute,
        JointType.Twist,
        JointType.BendTwist,
        JointType.TwistBend,
        JointType.Universal,
        JointType.Spherical
    };

    void Start()
    {
        phenotypeFactory = new SimsPhenotypeFactory();
        phenotypeEntityManagement = new SimsPhenotypeEntityManagement();
        // EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        // entityManager.CreateSingleton<FreezeRootLimbsRequest>();
        
        StartCoroutine(SpawnAllPermutations());
    }

    IEnumerator SpawnAllPermutations()
    {
        Debug.Log("=== Spawning Joint Permutation Grid ===");
        Debug.Log($"Grid: {allJointTypes.Length} joint types × 6 face indices = {allJointTypes.Length * 6} creatures");
        
        List<PhenotypeEntityCreationInfo<SimsPhenotype>> allCreationInfos = new List<PhenotypeEntityCreationInfo<SimsPhenotype>>();
        
        for (int jointTypeIndex = 0; jointTypeIndex < allJointTypes.Length; jointTypeIndex++)
        {
            JointType jointType = allJointTypes[jointTypeIndex];
            
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                SimsPhenotype phenotype = CreateTestCreature(jointType, faceIndex);
                
                // Calculate grid position
                float x = faceIndex * horizontalSpacing;
                float z = jointTypeIndex * verticalSpacing;
                float y = spawnHeight;
                
                PhenotypeEntityCreationInfo<SimsPhenotype> creationInfo = new()
                {
                    Phenotype = phenotype,
                    AllowInterPhenotypeCollisions = false,
                    PhysicsPositionOffset = new Unity.Mathematics.float3(x, y, z)
                };
                
                allCreationInfos.Add(creationInfo);
                
                Debug.Log($"Created: {jointType} (Face {faceIndex}) at grid position ({x}, {y}, {z})");
            }
        }
        
        Debug.Log($"Total creatures prepared: {allCreationInfos.Count}");
        Debug.Log("Spawning all creatures...");
        
        // Spawn all creatures at once
        yield return StartCoroutine(phenotypeEntityManagement.CreateEntitiesFromPhenotypes(
            World.DefaultGameObjectInjectionWorld, 
            allCreationInfos
        ));
        
        Debug.Log("=== Grid spawning complete ===");
    }

    SimsPhenotype CreateTestCreature(JointType jointType, int faceIndex)
    {
        // Create simple test creature: one root node, recursive limit, one connection on specified face
        SignalEmitterAddress biasAddy = new SignalEmitterAddress(RelativeSignalPort.Bias, 0);
        SignalEmitterAddress brainAddy = new SignalEmitterAddress(RelativeSignalPort.Brain, 0);
        
        Node rootNode = new Node(
            new Vector3(0.5f, 0.5f, 0.5f),
            new JointDefinition(
                jointType,
                new Vector3((float)Math.PI / 4f, (float)Math.PI / 4f, (float)Math.PI / 4f),
                new InputSetDefinition(new(brainAddy, 0f), new(brainAddy, 0f), new(brainAddy, 1f)),
                new InputSetDefinition(new(brainAddy, 0f), new(brainAddy, 0f), new(brainAddy, 1f)),
                new InputSetDefinition(new(brainAddy, 0f), new(brainAddy, 0f), new(brainAddy, 1f))
            ),
            recursiveLimit,
            new NodeColor(
                GetColorForJointType(jointType).X,
                GetColorForJointType(jointType).Y,
                GetColorForJointType(jointType).Z,
                0f, 0f, 0f
            )
        );
        
        List<Node> nodes = new List<Node> { rootNode };
        
        Connection connection = new Connection(
            rootNode.Gid,
            rootNode.Gid,
            faceIndex,
            new System.Numerics.Vector2(0.8f, 0.8f),
            new Vector3((float)Math.PI / 6f, (float)Math.PI / 6f, (float)Math.PI / 6f),
            new Vector3(0.5f, 0.5f, 1f),
            true, true, true, false
        );
        
        List<Connection> connections = new List<Connection> { connection };
        
        NeuronDefinition neuron = new NeuronDefinition(
            SimsGenotype.BRAIN_GID,
            ActivationFunction.Sin,
            new InputSetDefinition(new(biasAddy, 1f), new(biasAddy, 1f), new(biasAddy, 1f))
        );
        
        SimsGenotype genotype = new SimsGenotype(nodes, connections, new List<NeuronDefinition> { neuron });
        SimsPhenotype phenotype = phenotypeFactory.ConstructPhenotype(genotype);
        
        // Set initial positions
        for (int i = 0; i < phenotype.Limbs.Count; i++)
        {
            var limb = phenotype.Limbs[i];
            limb.SetPositionAndRotation(limb.Position, limb.Rotation);
        }
        
        return phenotype;
    }

    Vector3 GetColorForJointType(JointType jointType)
    {
        return jointType switch
        {
            JointType.Rigid => new Vector3(0.5f, 0.5f, 0.5f),      // Gray
            JointType.Revolute => new Vector3(1f, 0f, 0f),         // Red
            JointType.Twist => new Vector3(0f, 0f, 1f),            // Blue
            JointType.BendTwist => new Vector3(1f, 0.5f, 0f),      // Orange
            JointType.TwistBend => new Vector3(0f, 0.5f, 1f),      // Cyan
            JointType.Universal => new Vector3(0f, 1f, 0f),        // Green
            JointType.Spherical => new Vector3(1f, 0f, 1f),        // Magenta
            _ => new Vector3(1f, 1f, 1f)                           // White
        };
    }

    void OnGUI()
    {
        // Draw legend
        GUILayout.BeginArea(new Rect(10, 10, 300, 250));
        GUILayout.Label("Joint Type Grid", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
        GUILayout.Label("Rows (Z-axis): Joint Types", new GUIStyle(GUI.skin.label) { fontSize = 12 });
        GUILayout.Label("Columns (X-axis): Face Index (0-5)", new GUIStyle(GUI.skin.label) { fontSize = 12 });
        GUILayout.Space(10);
        
        for (int i = 0; i < allJointTypes.Length; i++)
        {
            JointType jointType = allJointTypes[i];
            Vector3 color = GetColorForJointType(jointType);
            UnityEngine.Color guiColor = new UnityEngine.Color(color.X, color.Y, color.Z);
            
            GUI.color = guiColor;
            GUILayout.Label($"Row {i}: {jointType}");
        }
        GUI.color = UnityEngine.Color.white;
        
        GUILayout.EndArea();
    }
}
