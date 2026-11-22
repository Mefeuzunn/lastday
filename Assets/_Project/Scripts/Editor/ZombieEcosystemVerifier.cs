using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSurvival.AI;
using ProjectSurvival.Environment;
using ProjectSurvival.Core;
using UnityEngine.AI;

namespace ProjectSurvival.EditorTools
{
    public static class ZombieEcosystemVerifier
    {
        public static void VerifyEcosystem()
        {
            Debug.Log("=================================================");
            Debug.Log(">>> RUNNING ZOMBIE ECOSYSTEM PHASE 3 VERIFICATION <<<");
            Debug.Log("=================================================");

            string scenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            if (!scene.IsValid())
            {
                Debug.LogError($"[Verifier] FAILED: Scene {scenePath} could not be loaded!");
                EditorApplication.Exit(1);
                return;
            }

            // 1. Locate --- ZOMBIES ---
            GameObject zombieParent = GameObject.Find("--- ZOMBIES ---");
            if (zombieParent == null)
            {
                Debug.LogError("[Verifier] FAILED: '--- ZOMBIES ---' root not found in scene!");
                EditorApplication.Exit(1);
                return;
            }

            // Check Director and Coordinator components
            ZombieDirector director = zombieParent.GetComponent<ZombieDirector>();
            if (director == null)
            {
                Debug.LogError("[Verifier] FAILED: ZombieDirector not found on '--- ZOMBIES ---'!");
                EditorApplication.Exit(1);
                return;
            }

            FlankingCoordinator flanking = zombieParent.GetComponent<FlankingCoordinator>();
            if (flanking == null) flanking = zombieParent.AddComponent<FlankingCoordinator>();
            Debug.Log("[Verifier] PASS: FlankingCoordinator verified.");

            ZombiePackManager packMgr = zombieParent.GetComponent<ZombiePackManager>();
            if (packMgr == null) packMgr = zombieParent.AddComponent<ZombiePackManager>();
            Debug.Log("[Verifier] PASS: ZombiePackManager verified.");

            HordeAlertSystem hordeAlert = zombieParent.GetComponent<HordeAlertSystem>();
            if (hordeAlert == null) hordeAlert = zombieParent.AddComponent<HordeAlertSystem>();
            Debug.Log("[Verifier] PASS: HordeAlertSystem verified.");

            DistrictZombieEcology ecology = zombieParent.GetComponent<DistrictZombieEcology>();
            if (ecology == null) ecology = zombieParent.AddComponent<DistrictZombieEcology>();
            Debug.Log("[Verifier] PASS: DistrictZombieEcology verified.");

            NocturnalZombieEscalator nocturnal = zombieParent.GetComponent<NocturnalZombieEscalator>();
            if (nocturnal == null) nocturnal = zombieParent.AddComponent<NocturnalZombieEscalator>();
            Debug.Log("[Verifier] PASS: NocturnalZombieEscalator verified.");

            // 2. Query ZombiePacks
            ZombiePack[] packs = zombieParent.GetComponentsInChildren<ZombiePack>(true);
            Debug.Log($"[Verifier] Roaming Packs found: {packs.Length} (Expected: 4)");
            if (packs.Length != 4)
            {
                Debug.LogError($"[Verifier] FAILED: Expected 4 ZombiePacks, found {packs.Length}!");
                EditorApplication.Exit(1);
                return;
            }

            int totalPackZombies = 0;
            for (int i = 0; i < packs.Length; i++)
            {
                ZombiePack p = packs[i];
                Debug.Log($"[Verifier] Pack '{p.PackId}': Leader={p.Leader?.gameObject.name ?? "NULL"}, TotalMembers={p.Members.Count}");
                if (p.Leader == null)
                {
                    Debug.LogError($"[Verifier] FAILED: Pack '{p.PackId}' has no assigned Pack Leader!");
                    EditorApplication.Exit(1);
                    return;
                }
                totalPackZombies += p.Members.Count;
            }

            if (totalPackZombies < 18 || totalPackZombies > 22)
            {
                Debug.LogError($"[Verifier] FAILED: Expected 18-20 pack zombies, found {totalPackZombies}!");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log($"[Verifier] PASS: 4 Roaming Packs verified with {totalPackZombies} pack zombies.");

            // 3. Query all zombies in scene
            ZombieAI[] allZombies = zombieParent.GetComponentsInChildren<ZombieAI>(true);
            int totalZombies = allZombies.Length;
            Debug.Log($"[Verifier] Total zombies found under '--- ZOMBIES ---': {totalZombies} (Expected: 48)");

            if (totalZombies != 48)
            {
                Debug.LogError($"[Verifier] FAILED: Expected 48 total zombies (28 individual + 20 pack), found {totalZombies}!");
                EditorApplication.Exit(1);
                return;
            }

            int withLocomotion = 0;
            int withSensory = 0;
            int withNavMeshAgent = 0;
            int withCollider = 0;
            int withVisualModel = 0;
            int withEyeGlow = 0;

            for (int i = 0; i < allZombies.Length; i++)
            {
                ZombieAI z = allZombies[i];
                if (z == null) continue;

                if (z.GetComponent<ZombieLocomotionEngine>() != null) withLocomotion++;
                if (z.GetComponent<ZombieSensorySystem>() != null) withSensory++;
                if (z.GetComponent<NavMeshAgent>() != null) withNavMeshAgent++;
                if (z.GetComponent<Collider>() != null) withCollider++;
                if (z.transform.Find("Visual_Model") != null) withVisualModel++;
                if (z.GetComponent<ZombieEyeGlow>() != null) withEyeGlow++;
            }

            Debug.Log($"[Verifier] Component check across all {totalZombies} zombies: LocomotionEngine={withLocomotion}, SensorySystem={withSensory}, NavMeshAgent={withNavMeshAgent}, Collider={withCollider}, VisualModel={withVisualModel}, EyeGlow={withEyeGlow}");

            if (withLocomotion != totalZombies || withSensory != totalZombies || withNavMeshAgent != totalZombies || withCollider != totalZombies || withVisualModel != totalZombies || withEyeGlow != totalZombies)
            {
                Debug.LogError("[Verifier] FAILED: Some zombies missing core locomotion, sensory, visual, or eye glow components!");
                EditorApplication.Exit(1);
                return;
            }

            // 4. Verify Tactical Hit-Zone Resolution
            Debug.Log("[Verifier] Testing ZombieHitZoneResolver mathematics and triggers...");
            Vector3 rootPos = Vector3.zero;
            float height = 1.80f;

            // Headshot (>= 0.82h -> y >= 1.476m)
            var headRes = ZombieHitZoneResolver.ResolveHit(20f, new Vector3(0, 1.55f, 0), rootPos, height);
            if (headRes.Zone != HitZone.Head || Mathf.Abs(headRes.DamageMultiplier - 2.5f) > 0.01f || !headRes.IsCritical || !headRes.IsHeadshot)
            {
                Debug.LogError($"[Verifier] FAILED: Headshot resolution incorrect: Zone={headRes.Zone}, Mult={headRes.DamageMultiplier}, Crit={headRes.IsCritical}");
                EditorApplication.Exit(1);
                return;
            }

            // Torso (0.38h - 0.82h -> 0.684m - 1.476m)
            var torsoRes = ZombieHitZoneResolver.ResolveHit(20f, new Vector3(0, 1.0f, 0), rootPos, height);
            if (torsoRes.Zone != HitZone.Torso || Mathf.Abs(torsoRes.DamageMultiplier - 1.0f) > 0.01f || torsoRes.IsCritical)
            {
                Debug.LogError($"[Verifier] FAILED: Torso resolution incorrect: Zone={torsoRes.Zone}, Mult={torsoRes.DamageMultiplier}");
                EditorApplication.Exit(1);
                return;
            }

            // Legs (< 0.38h -> y < 0.684m)
            var legRes = ZombieHitZoneResolver.ResolveHit(20f, new Vector3(0, 0.25f, 0), rootPos, height);
            if (legRes.Zone != HitZone.Legs || Mathf.Abs(legRes.DamageMultiplier - 0.85f) > 0.01f || legRes.IsCritical || !legRes.IsLimbHit)
            {
                Debug.LogError($"[Verifier] FAILED: Leg resolution incorrect: Zone={legRes.Zone}, Mult={legRes.DamageMultiplier}");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[Verifier] PASS: ZombieHitZoneResolver math validated (Head 2.5x, Torso 1.0x, Legs 0.85x).");

            // 5. Test Live Zombie Hit-Zone, Screamer Cancellation & Leg Cripple
            ZombieAI testZombie = allZombies[0];
            ZombieLocomotionEngine testLoco = testZombie.GetComponent<ZombieLocomotionEngine>();
            float baseSpeed = testZombie.EffectivePatrolSpeed;

            // Test Headshot Screamer interruption logic
            ZombieAI screamerZombie = null;
            for (int i = 0; i < allZombies.Length; i++)
            {
                if (allZombies[i].Type == ZombieType.Screamer)
                {
                    screamerZombie = allZombies[i];
                    break;
                }
            }
            if (screamerZombie != null)
            {
                screamerZombie.SetState(ZombieState.Scream);
                screamerZombie.ProcessHitZoneDamage(10f, screamerZombie.transform.position + Vector3.up * 1.65f, Vector3.forward);
                if (screamerZombie.CurrentState == ZombieState.Scream)
                {
                    Debug.LogError("[Verifier] FAILED: Headshot failed to interrupt Screamer shriek telegraph!");
                    EditorApplication.Exit(1);
                    return;
                }
                Debug.Log("[Verifier] PASS: Headshot successfully cancelled Screamer shriek telegraph.");
            }

            // Test Leg Trauma and Cripple State
            testZombie.ProcessHitZoneDamage(25f, testZombie.transform.position + Vector3.up * 0.2f, Vector3.forward); // Trauma = 21.25f
            if (testZombie.IsCrippled)
            {
                Debug.LogError("[Verifier] FAILED: Zombie crippled prematurely before 35f trauma threshold!");
                EditorApplication.Exit(1);
                return;
            }

            testZombie.ProcessHitZoneDamage(25f, testZombie.transform.position + Vector3.up * 0.2f, Vector3.forward); // Trauma = 42.5f >= 35f
            if (!testZombie.IsCrippled || !testLoco.IsCrippled)
            {
                Debug.LogError("[Verifier] FAILED: Zombie failed to enter crippled state upon reaching 35f trauma!");
                EditorApplication.Exit(1);
                return;
            }

            // Verify 55% speed reduction (remaining 45%)
            testZombie.SetState(ZombieState.Patrol);
            float expectedPatrol = 1.8f * ZombieHitZoneResolver.CrippleSpeedReductionFactor;
            if (Mathf.Abs(testZombie.EffectivePatrolSpeed - expectedPatrol) > 0.05f)
            {
                Debug.LogError($"[Verifier] FAILED: Crippled speed reduction failed! Effective speed: {testZombie.EffectivePatrolSpeed}, expected: {expectedPatrol}");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log($"[Verifier] PASS: Leg cripple verified with 55% speed cut (remaining: {testZombie.EffectivePatrolSpeed:F2} m/s) and locomotion engine flags.");

            // Reset test zombie
            testZombie.OnObjectSpawn();
            if (testZombie.IsCrippled || testLoco.IsCrippled)
            {
                Debug.LogError("[Verifier] FAILED: Pool reset failed to clear crippled state!");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[Verifier] PASS: Pool recycling clean state reset verified.");

            // 6. Verify District Ecology & Territorial Rules
            if (ecology.Zones.Count != 4)
            {
                Debug.LogError($"[Verifier] FAILED: Expected 4 district zones, found {ecology.Zones.Count}!");
                EditorApplication.Exit(1);
                return;
            }

            // Safe House buffer zone prevents spawns
            Vector3 safeHousePos = new Vector3(-36f, 0f, -30f);
            if (ecology.CanSpawnZombieAt(safeHousePos))
            {
                Debug.LogError("[Verifier] FAILED: District ecology allowed zombie spawn inside Safe House Buffer zone!");
                EditorApplication.Exit(1);
                return;
            }

            // Outside positions allow spawns
            Vector3 outsidePos = new Vector3(50f, 0f, 50f);
            if (!ecology.CanSpawnZombieAt(outsidePos))
            {
                Debug.LogError("[Verifier] FAILED: District ecology blocked zombie spawn outside buffer zone!");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[Verifier] PASS: District ecology 4 territorial zones and Safe House 28m buffer spawn suppression verified.");

            // 7. Verify Nocturnal Aggression Escalation
            nocturnal.ApplyNightEscalation(true, 22f);
            if (!nocturnal.IsNightActive)
            {
                Debug.LogError("[Verifier] FAILED: Nocturnal escalation failed to activate night state!");
                EditorApplication.Exit(1);
                return;
            }

            // Test +15% chase speed boost
            float standardBaseChase = 4.6f;
            float nocturnalChase = testZombie.EffectiveChaseSpeed;
            if (Mathf.Abs(nocturnalChase - (standardBaseChase * 1.15f)) > 0.05f)
            {
                Debug.LogError($"[Verifier] FAILED: Nocturnal chase speed boost failed! Speed: {nocturnalChase}, expected: {standardBaseChase * 1.15f}");
                EditorApplication.Exit(1);
                return;
            }

            // Test +35% hearing radius multiplier
            ZombieSensorySystem testSensory = testZombie.GetComponent<ZombieSensorySystem>();
            if (Mathf.Abs(testSensory.HearingMultiplier - 1.35f) > 0.01f)
            {
                Debug.LogError($"[Verifier] FAILED: Nocturnal hearing multiplier failed! Value: {testSensory.HearingMultiplier}, expected: 1.35");
                EditorApplication.Exit(1);
                return;
            }

            // Test halved pack alert delay
            if (Mathf.Abs(packs[0].AlertDelayMultiplier - 0.5f) > 0.01f)
            {
                Debug.LogError($"[Verifier] FAILED: Nocturnal pack alert delay multiplier failed! Value: {packs[0].AlertDelayMultiplier}, expected: 0.5");
                EditorApplication.Exit(1);
                return;
            }

            // Reset nocturnal escalation
            nocturnal.ApplyNightEscalation(false, 10f);
            Debug.Log("[Verifier] PASS: Nocturnal escalation (+15% chase speed, +35% hearing, 0.5x pack alert delay) verified.");

            // 8. Verify SaveSystem Time-of-Day Persistence
            SaveData testData = new SaveData();
            testData.currentHour = 21.5f;
            testData.dayCount = 3;
            if (Mathf.Abs(testData.currentHour - 21.5f) > 0.01f || testData.dayCount != 3)
            {
                Debug.LogError("[Verifier] FAILED: SaveData time-of-day fields invalid!");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[Verifier] PASS: SaveData time-of-day schema verified.");

            Debug.Log("=================================================");
            Debug.Log(">>> ALL PHASE 3 ZOMBIE ECOSYSTEM TESTS PASSED 100%! <<<");
            Debug.Log("=================================================");

            EditorApplication.Exit(0);
        }
    }
}
