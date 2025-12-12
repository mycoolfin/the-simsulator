using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using mycoolfin.TheSimsulator.Sims.Genotype;
using mycoolfin.TheSimsulator.Sims.Phenotype;
using mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API;
using mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.API;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Quaternion = Unity.Mathematics.quaternion;
using Vector3 = System.Numerics.Vector3;

/// <summary>
/// Tests all joint types to detect 360-degree flip bugs after refactoring to use tertiary axis for RotationMotor constraints.
/// Creates 6 simple creatures for each joint type, evaluates them sequentially, and reports any detected flip bugs.
/// </summary>
public class JointFlipBugTester : MonoBehaviour
{
    [Header("Test Configuration")]
    [Tooltip("Delay between spawning each joint type test (seconds)")]
    public float testDelay = 3f;
    
    [Tooltip("Threshold for detecting rotation flips (degrees)")]
    public float flipThreshold = 90f;
    
    [Tooltip("Number of frames to wait before checking for flips")]
    public int framesToWaitBeforeCheck = 60;

    private SimsPhenotypeFactory phenotypeFactory;
    private SimsPhenotypeEntityManagement phenotypeEntityManagement;
    
    private List<TestResult> testResults = new List<TestResult>();
    private bool isTesting = false;

    private struct TestResult
    {
        public JointType JointType;
        public int TestIndex;
        public bool FlipDetected;
        public float MaxRotationDelta;
        public string Details;
    }

    private class CreatureTestData
    {
        public SimsPhenotype Phenotype;
        public Entity RootEntity;
        public List<Entity> JointEntities;
        public Dictionary<Entity, float3> InitialChildRotations;
        public JointType JointType;
        public int TestIndex;
    }

    void Start()
    {
        phenotypeFactory = new SimsPhenotypeFactory();
        phenotypeEntityManagement = new SimsPhenotypeEntityManagement();
        
        StartCoroutine(RunAllJointTypeTests());
    }

    IEnumerator RunAllJointTypeTests()
    {
        isTesting = true;
        
        Debug.Log("=== Starting Joint Flip Bug Testing ===");
        Debug.Log($"Testing all 7 joint types with 6 creatures each (face index 0-5)");
        Debug.Log($"Flip threshold: {flipThreshold} degrees");
        
        JointType[] allJointTypes = new[]
        {
            JointType.Rigid,
            JointType.Revolute,
            JointType.Twist,
            JointType.BendTwist,
            JointType.TwistBend,
            JointType.Universal,
            JointType.Spherical
        };

        foreach (JointType jointType in allJointTypes)
        {
            Debug.Log($"\n--- Testing {jointType} ---");
            yield return StartCoroutine(TestJointType(jointType));
            
            // Wait between tests
            yield return new WaitForSeconds(testDelay);
        }
        
        // Print summary
        PrintTestSummary();
        
        isTesting = false;
    }

    IEnumerator TestJointType(JointType jointType)
    {
        List<CreatureTestData> testCreatures = new List<CreatureTestData>();
        
        // Create 6 creatures with this joint type (one for each face)
        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            SimsPhenotype phenotype = CreateTestCreature(jointType, faceIndex);
            
            PhenotypeEntityCreationInfo<SimsPhenotype> creationInfo = new()
            {
                Phenotype = phenotype,
                AllowInterPhenotypeCollisions = false
            };
            
            // Position them in a line
            float spacing = 5f;
            float3 position = new float3(faceIndex * spacing, 2f, 0f);
            
            // Create entity synchronously
            yield return StartCoroutine(phenotypeEntityManagement.CreateEntitiesFromPhenotypes(
                World.DefaultGameObjectInjectionWorld, 
                new List<PhenotypeEntityCreationInfo<SimsPhenotype>> { creationInfo }
            ));
            
            // Find the created entities
            CreatureTestData testData = new CreatureTestData
            {
                Phenotype = phenotype,
                JointType = jointType,
                TestIndex = faceIndex,
                InitialChildRotations = new Dictionary<Entity, float3>()
            };
            
            testCreatures.Add(testData);
        }
        
        // Wait one frame for entities to be fully created
        yield return null;
        
        // Capture initial state
        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            var entityManager = world.EntityManager;
            
            foreach (var testData in testCreatures)
            {
                // Find joint entities for this phenotype
                testData.JointEntities = FindJointEntitiesForPhenotype(entityManager, testData.Phenotype);
                
                // Capture initial child limb rotations
                foreach (var jointEntity in testData.JointEntities)
                {
                    if (entityManager.HasComponent<Unity.Physics.PhysicsConstrainedBodyPair>(jointEntity))
                    {
                        var pair = entityManager.GetComponentData<Unity.Physics.PhysicsConstrainedBodyPair>(jointEntity);
                        if (entityManager.HasComponent<LocalTransform>(pair.EntityB))
                        {
                            var childTransform = entityManager.GetComponentData<LocalTransform>(pair.EntityB);
                            float3 initialEuler = GetEulerAngles(childTransform.Rotation);
                            testData.InitialChildRotations[pair.EntityB] = initialEuler;
                        }
                    }
                }
            }
        }
        
        // Log initial state immediately
        Debug.Log($"[Frame 0] Captured initial rotations for {testCreatures.Count} creatures");
        
        // Check immediately after first physics frame
        yield return null;
        
        if (world != null && world.IsCreated)
        {
            var entityManager = world.EntityManager;
            Debug.Log($"[Frame 1] Checking for immediate flips...");
            
            foreach (var testData in testCreatures)
            {
                float maxDelta = 0f;
                foreach (var jointEntity in testData.JointEntities)
                {
                    if (entityManager.HasComponent<Unity.Physics.PhysicsConstrainedBodyPair>(jointEntity))
                    {
                        var pair = entityManager.GetComponentData<Unity.Physics.PhysicsConstrainedBodyPair>(jointEntity);
                        if (entityManager.HasComponent<LocalTransform>(pair.EntityB))
                        {
                            var childTransform = entityManager.GetComponentData<LocalTransform>(pair.EntityB);
                            float3 currentEuler = GetEulerAngles(childTransform.Rotation);
                            
                            if (testData.InitialChildRotations.TryGetValue(pair.EntityB, out float3 initialEuler))
                            {
                                float3 delta = math.abs(currentEuler - initialEuler);
                                delta = math.min(delta, 360f - delta);
                                float maxAxisDelta = math.max(delta.x, math.max(delta.y, delta.z));
                                if (maxAxisDelta > maxDelta) maxDelta = maxAxisDelta;
                            }
                        }
                    }
                }
                if (maxDelta > flipThreshold)
                {
                    Debug.Log($"  {testData.JointType} Face {testData.TestIndex}: Immediate flip of {maxDelta:F1}°");
                }
            }
        }
        
        // Wait for physics simulation to settle
        for (int i = 0; i < framesToWaitBeforeCheck - 1; i++)
        {
            yield return null;
        }
        
        Debug.Log($"[Frame {framesToWaitBeforeCheck}] Checking for flips after physics settle");
        
        // Check for flips
        if (world != null && world.IsCreated)
        {
            var entityManager = world.EntityManager;
            
            foreach (var testData in testCreatures)
            {
                bool flipDetected = false;
                float maxDelta = 0f;
                string details = "";
                
                foreach (var jointEntity in testData.JointEntities)
                {
                    if (entityManager.HasComponent<Unity.Physics.PhysicsConstrainedBodyPair>(jointEntity))
                    {
                        var pair = entityManager.GetComponentData<Unity.Physics.PhysicsConstrainedBodyPair>(jointEntity);
                        if (entityManager.HasComponent<LocalTransform>(pair.EntityB))
                        {
                            var childTransform = entityManager.GetComponentData<LocalTransform>(pair.EntityB);
                            float3 currentEuler = GetEulerAngles(childTransform.Rotation);
                            
                            if (testData.InitialChildRotations.TryGetValue(pair.EntityB, out float3 initialEuler))
                            {
                                float3 delta = math.abs(currentEuler - initialEuler);
                                
                                // Normalize to 0-180 range
                                delta = math.min(delta, 360f - delta);
                                
                                float maxAxisDelta = math.max(delta.x, math.max(delta.y, delta.z));
                                
                                if (maxAxisDelta > flipThreshold)
                                {
                                    flipDetected = true;
                                    maxDelta = math.max(maxDelta, maxAxisDelta);
                                    details += $"Joint {jointEntity.Index}: Delta={maxAxisDelta:F1}° ";
                                }
                            }
                        }
                    }
                }
                
                TestResult result = new TestResult
                {
                    JointType = testData.JointType,
                    TestIndex = testData.TestIndex,
                    FlipDetected = flipDetected,
                    MaxRotationDelta = maxDelta,
                    Details = details
                };
                
                testResults.Add(result);
                
                string status = flipDetected ? $"❌ FLIP DETECTED ({maxDelta:F1}°)" : "✓ OK";
                Debug.Log($"  Face {testData.TestIndex}: {status} {details}");
            }
        }
        
        // Clean up - destroy all test creatures
        if (world != null && world.IsCreated)
        {
            var entityManager = world.EntityManager;
            foreach (var testData in testCreatures)
            {
                // Destroy phenotype entities (this should cascade to limbs and joints)
                // Note: In a real scenario you'd want proper cleanup through the API
            }
        }
    }

    SimsPhenotype CreateTestCreature(JointType jointType, int faceIndex)
    {
        // Create simple test creature: one root node, recursive limit 2, one connection on specified face
        SignalEmitterAddress biasAddy = new SignalEmitterAddress(RelativeSignalPort.Bias, 0);
        SignalEmitterAddress brainAddy = new SignalEmitterAddress(RelativeSignalPort.Brain, 0);
        SignalEmitterAddress emptyAddy = new SignalEmitterAddress();
        
        Node rootNode = new Node(
            new Vector3(0.5f, 0.5f, 0.5f),
            new JointDefinition(
                jointType,
                new Vector3((float)Math.PI / 4f, (float)Math.PI / 4f, (float)Math.PI / 4f),
                new InputSetDefinition(new(brainAddy, 1f), new(emptyAddy, 0f), new(emptyAddy, 0f)),
                new InputSetDefinition(new(brainAddy, 1f), new(emptyAddy, 0f), new(emptyAddy, 0f)),
                new InputSetDefinition(new(brainAddy, 1f), new(emptyAddy, 0f), new(emptyAddy, 0f))
            ),
            2, // recursive limit
            new NodeColor(1f, 0.5f, 0.2f, 0f, 0f, 0f)
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

    List<Entity> FindJointEntitiesForPhenotype(EntityManager entityManager, SimsPhenotype phenotype)
    {
        List<Entity> jointEntities = new List<Entity>();
        
        using (var query = entityManager.CreateEntityQuery(
            typeof(Unity.Physics.PhysicsJoint),
            typeof(Unity.Physics.PhysicsConstrainedBodyPair)))
        {
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    // Note: This is a simplified approach - in practice you'd want to match by phenotype GID
                    // For now, assume all joints in the query belong to our test creatures
                    jointEntities.Add(entity);
                }
            }
        }
        
        return jointEntities;
    }

    float3 GetEulerAngles(Quaternion q)
    {
        // Convert quaternion to Euler angles (in degrees)
        float3 euler;
        
        // Roll (x-axis rotation)
        float sinr_cosp = 2 * (q.value.w * q.value.x + q.value.y * q.value.z);
        float cosr_cosp = 1 - 2 * (q.value.x * q.value.x + q.value.y * q.value.y);
        euler.x = math.atan2(sinr_cosp, cosr_cosp);

        // Pitch (y-axis rotation)
        float sinp = 2 * (q.value.w * q.value.y - q.value.z * q.value.x);
        if (math.abs(sinp) >= 1)
            euler.y = math.PI / 2 * math.sign(sinp);
        else
            euler.y = math.asin(sinp);

        // Yaw (z-axis rotation)
        float siny_cosp = 2 * (q.value.w * q.value.z + q.value.x * q.value.y);
        float cosy_cosp = 1 - 2 * (q.value.y * q.value.y + q.value.z * q.value.z);
        euler.z = math.atan2(siny_cosp, cosy_cosp);

        // Convert to degrees
        return math.degrees(euler);
    }

    void PrintTestSummary()
    {
        Debug.Log("\n=== TEST SUMMARY ===");
        
        var groupedResults = testResults.GroupBy(r => r.JointType);
        
        foreach (var group in groupedResults)
        {
            int groupTestCount = group.Count();
            int flipsDetected = group.Count(r => r.FlipDetected);
            float avgMaxDelta = group.Average(r => r.MaxRotationDelta);
            
            string status = flipsDetected == 0 ? "✓ PASS" : $"❌ FAIL ({flipsDetected}/{groupTestCount} flips)";
            Debug.Log($"{group.Key}: {status} (Avg max delta: {avgMaxDelta:F1}°)");
        }
        
        int totalFlips = testResults.Count(r => r.FlipDetected);
        int totalTests = testResults.Count;
        
        Debug.Log($"\nOverall: {totalTests - totalFlips}/{totalTests} tests passed");
        
        if (totalFlips == 0)
        {
            Debug.Log("✓✓✓ ALL TESTS PASSED - No flip bugs detected! ✓✓✓");
        }
        else
        {
            Debug.LogWarning($"⚠ {totalFlips} flip bugs detected across all joint types");
        }
    }

    void OnGUI()
    {
        if (!isTesting && testResults.Count > 0)
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.Label("Joint Flip Bug Test Results", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            
            var groupedResults = testResults.GroupBy(r => r.JointType);
            
            foreach (var group in groupedResults)
            {
                int flipsDetected = group.Count(r => r.FlipDetected);
                string status = flipsDetected == 0 ? "✓" : "❌";
                GUILayout.Label($"{status} {group.Key}: {flipsDetected}/6 flips");
            }
            
            GUILayout.EndArea();
        }
        else if (isTesting)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label("Testing in progress...", new GUIStyle(GUI.skin.label) { fontSize = 14 });
            GUILayout.Label($"Progress: {testResults.Count} tests completed");
            GUILayout.EndArea();
        }
    }
}
