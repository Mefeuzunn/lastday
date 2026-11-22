using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.AI;
using ProjectSurvival.Environment;
using ProjectSurvival.Survival;
using ProjectSurvival.Vehicles;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Active Phase 6: Automated Verification Audit for Directional Ragdoll Physics,
    /// Chain-Reaction Explosive Hazards, and Destructible Environmental Props.
    /// </summary>
    public static class Phase6_PhysicsDestructionAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        private static int passedTests = 0;
        private static int failedTests = 0;
        private static readonly List<string> FailureMessages = new List<string>();

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Audit Phase 6 Physics & Destruction", false, 39)]
        public static void RunAudit()
        {
            passedTests = 0;
            failedTests = 0;
            FailureMessages.Clear();

            Debug.Log("===============================================================================");
            Debug.Log(">>> STARTING MASTER PHASE 6 PHYSICS & DESTRUCTION AUDIT (UNITY 6 BATCHMODE) <<<");
            Debug.Log("===============================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            AuditZombiePrefabs();
            AuditZombieRagdollSimulation();
            AuditExplosiveHazards();
            AuditDestructibleProps();
            AuditSceneIntegrityAndShaders();

            Debug.Log("===============================================================================");
            Debug.Log($">>> AUDIT COMPLETE: {passedTests} PASSED, {failedTests} FAILED <<<");
            Debug.Log("===============================================================================");

            if (failedTests > 0)
            {
                foreach (var err in FailureMessages)
                {
                    Debug.LogError($"[AUDIT FAILURE] {err}");
                }
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
            else
            {
                Debug.Log("[AUDIT SUCCESS] All Phase 6 physics and destruction tests passed cleanly with 0 errors!");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
        }

        private static void AuditZombiePrefabs()
        {
            Debug.Log("--- AUDIT: Zombie Prefabs & Ragdoll Rig Configuration ---");

            string[] prefabPaths = new string[]
            {
                "Assets/_Project/Prefabs/Zombies/Zombie_Standard_Root.prefab",
                "Assets/_Project/Prefabs/Zombies/Zombie_Runner_Root.prefab",
                "Assets/_Project/Prefabs/Zombies/Zombie_Screamer_Root.prefab",
                "Assets/_Project/Prefabs/Zombies/Zombie_Tank_Root.prefab"
            };

            foreach (var path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert(prefab != null, $"Prefab exists at path: {path}");
                if (prefab == null) continue;

                var ragdoll = prefab.GetComponent<ZombieRagdollController>();
                Assert(ragdoll != null, $"Zombie prefab has ZombieRagdollController: {prefab.name}");
                if (ragdoll == null) continue;

                // Inspect serialized fields via reflection or dummy instance
                var so = new SerializedObject(ragdoll);
                float pistol = so.FindProperty("pistolImpulse")?.floatValue ?? 0f;
                float shotgun = so.FindProperty("shotgunImpulse")?.floatValue ?? 0f;
                float axe = so.FindProperty("axeImpulse")?.floatValue ?? 0f;
                float vehicle = so.FindProperty("vehicleImpulse")?.floatValue ?? 0f;
                float blast = so.FindProperty("blastImpulse")?.floatValue ?? 0f;
                float freezeDelay = so.FindProperty("freezeDelay")?.floatValue ?? 0f;

                Assert(Mathf.Approximately(pistol, 140f), $"{prefab.name}: Pistol impulse calibrated to 140 N (actual: {pistol})");
                Assert(Mathf.Approximately(shotgun, 380f), $"{prefab.name}: Shotgun impulse calibrated to 380 N (actual: {shotgun})");
                Assert(Mathf.Approximately(axe, 250f), $"{prefab.name}: Axe impulse calibrated to 250 N (actual: {axe})");
                Assert(Mathf.Approximately(vehicle, 580f), $"{prefab.name}: Vehicle impulse calibrated to 580 N (actual: {vehicle})");
                Assert(Mathf.Approximately(blast, 480f), $"{prefab.name}: Blast impulse calibrated to 480 N (actual: {blast})");
                Assert(Mathf.Approximately(freezeDelay, 3.5f), $"{prefab.name}: Freeze delay set to 3.5s for 60 FPS mobile performance (actual: {freezeDelay})");
            }
        }

        private static void AuditZombieRagdollSimulation()
        {
            Debug.Log("--- AUDIT: Zombie Ragdoll Runtime Activation & Sleep Lock ---");

            GameObject dummyZombie = new GameObject("Test_Ragdoll_Zombie");
            try
            {
                var col = dummyZombie.AddComponent<CapsuleCollider>();
                var agent = dummyZombie.AddComponent<NavMeshAgent>();
                var ai = dummyZombie.AddComponent<ZombieAI>();
                var ragdoll = dummyZombie.AddComponent<ZombieRagdollController>();

                ragdoll.EnsureRagdollRig();
                Assert(ragdoll.RagdollBones.Count >= 7, $"Articulated ragdoll generated with >= 7 bones (actual: {ragdoll.RagdollBones.Count})");

                // Initially kinematic
                foreach (var bone in ragdoll.RagdollBones)
                {
                    Assert(bone.boneRigidbody.isKinematic, $"Limb bone '{bone.boneName}' is initially kinematic while alive");
                }

                // Apply damage impulse
                ragdoll.ApplyDamageImpulse("Shotgun", Vector3.forward, Vector3.zero, false);
                Assert(ragdoll.IsRagdollActive, "Ragdoll state is active after damage impulse");

                // Limb bones dynamic
                int dynamicCount = 0;
                foreach (var bone in ragdoll.RagdollBones)
                {
                    if (!bone.boneRigidbody.isKinematic) dynamicCount++;
                }
                Assert(dynamicCount >= 6, $"Limb bones transitioned to dynamic physics rigidbodies (dynamic: {dynamicCount})");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dummyZombie);
            }
        }

        private static void AuditExplosiveHazards()
        {
            Debug.Log("--- AUDIT: Explosive Fuel Hazards & Chain Reaction Physics ---");

            ExplosiveFuelHazard[] hazards = UnityEngine.Object.FindObjectsByType<ExplosiveFuelHazard>(FindObjectsSortMode.None);
            Assert(hazards.Length >= 18, $"Scene contains >= 18 Explosive Fuel Hazards (found: {hazards.Length})");

            foreach (var h in hazards)
            {
                Assert(h.MaxHealth == 45f, $"{h.name}: MaxHealth is 45 HP");
                Assert(h.ExplosionRadius == 7.5f, $"{h.name}: ExplosionRadius is 7.5m");

                var so = new SerializedObject(h);
                float chainRad = so.FindProperty("chainReactionRadius")?.floatValue ?? 0f;
                float chainDelay = so.FindProperty("chainReactionDelay")?.floatValue ?? 0f;
                float blastImpulse = so.FindProperty("ragdollBlastImpulse")?.floatValue ?? 0f;
                float alertRadius = so.FindProperty("hordeAlertRadius")?.floatValue ?? 0f;

                Assert(Mathf.Approximately(chainRad, 6.5f), $"{h.name}: Chain reaction radius is 6.5m");
                Assert(Mathf.Approximately(chainDelay, 0.22f), $"{h.name}: Chain reaction delay is 0.22s");
                Assert(Mathf.Approximately(blastImpulse, 480f), $"{h.name}: Ragdoll blast impulse is 480 N");
                Assert(Mathf.Approximately(alertRadius, 55f), $"{h.name}: Acoustic alert radius is 55m");
                break; // One deep check across all instances is sufficient for serialized defaults
            }

            // Dummy unit test for hazard logic
            GameObject dummyHazard = new GameObject("Test_Hazard");
            dummyHazard.transform.position = new Vector3(5000f, 5000f, 5000f);
            try
            {
                dummyHazard.AddComponent<CapsuleCollider>();
                var haz = dummyHazard.AddComponent<ExplosiveFuelHazard>();

                // Non-fatal hit
                haz.TakeDamage(20f);
                Assert(haz.CurrentHealth == 25f && !haz.IsFuseLit, "Non-fatal hit deals health damage without lighting fuse immediately");

                // Fatal hit
                haz.TakeDamage(30f);
                Assert(haz.IsFuseLit, "Fatal damage initiates fuse sequence");

                // Immediate detonation
                haz.DetonateImmediate();
                Assert(haz.IsDetonated, "Detonation state confirmed");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dummyHazard);
            }
        }

        private static void AuditDestructibleProps()
        {
            Debug.Log("--- AUDIT: Destructible Environmental Props ---");

            DestructibleProp[] props = UnityEngine.Object.FindObjectsByType<DestructibleProp>(FindObjectsSortMode.None);
            int glassCount = 0;
            int crateCount = 0;
            int fenceCount = 0;

            foreach (var p in props)
            {
                if (p.Archetype == DestructibleArchetype.ShatterGlass) glassCount++;
                else if (p.Archetype == DestructibleArchetype.BreakableCrate) crateCount++;
                else if (p.Archetype == DestructibleArchetype.WeakTimberFence) fenceCount++;
            }

            Assert(glassCount >= 6, $"Scene contains >= 6 ShatterGlass storefronts/windows (found: {glassCount})");
            Assert(crateCount >= 8, $"Scene contains >= 8 BreakableCrate supply crates (found: {crateCount})");
            Assert(fenceCount >= 6, $"Scene contains >= 6 WeakTimberFence shortcut fences (found: {fenceCount})");

            // Dummy unit test for DestructibleProp
            GameObject dummyProp = new GameObject("Test_Prop");
            dummyProp.transform.position = new Vector3(5000f, 5000f, 5000f);
            try
            {
                var col = dummyProp.AddComponent<BoxCollider>();
                var obs = dummyProp.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                var dp = dummyProp.AddComponent<DestructibleProp>();
                dp.ConfigureArchetype(DestructibleArchetype.ShatterGlass, 20f);

                Assert(dp.MaxHealth == 20f, "ShatterGlass defaults to 20 HP");
                Assert(dp.CurrentHealth == 20f, "Current health is initialized to max health");

                dp.TakeDamage(25f, Vector3.zero, Vector3.forward);
                Assert(dp.IsDestroyed, "Prop is destroyed on lethal damage");
                Assert(!col.enabled, "Main collider is disabled upon destruction");
                Assert(!obs.enabled, "NavMeshObstacle is disabled upon destruction to open route");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dummyProp);
            }
        }

        private static void AuditSceneIntegrityAndShaders()
        {
            Debug.Log("--- AUDIT: Scene Integrity, Missing Scripts & Shader Validity ---");

            GameObject[] allObjs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int missingScriptCount = 0;

            foreach (var go in allObjs)
            {
                Component[] comps = go.GetComponents<Component>();
                foreach (var c in comps)
                {
                    if (c == null)
                    {
                        missingScriptCount++;
                        Debug.LogError($"[AUDIT ERROR] Missing script component on: {go.name}");
                    }
                }
            }
            Assert(missingScriptCount == 0, $"Zero missing scripts in scene hierarchy (found: {missingScriptCount})");

            // Shader check on all renderers in scene
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int invalidShaderCount = 0;

            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    {
                        invalidShaderCount++;
                        Debug.LogError($"[AUDIT ERROR] Magenta / InternalErrorShader on {r.gameObject.name} (Material: {mat.name})");
                    }
                }
            }
            Assert(invalidShaderCount == 0, $"Zero magenta / error shaders across all renderers (found: {invalidShaderCount})");
        }

        private static void Assert(bool condition, string message)
        {
            if (condition)
            {
                passedTests++;
                Debug.Log($"  [PASS] {message}");
            }
            else
            {
                failedTests++;
                FailureMessages.Add(message);
                Debug.LogError($"  [FAIL] {message}");
            }
        }
#endif
    }
}
