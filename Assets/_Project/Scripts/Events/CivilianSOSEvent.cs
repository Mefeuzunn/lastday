using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.AI;
using ProjectSurvival.Animation;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Player;
using ProjectSurvival.Progression;
using ProjectSurvival.Managers;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Events
{
    /// <summary>
    /// Phase 2 (Step 2B): Dynamic Civilian SOS Rescue Encounter.
    /// Manages the 6-minute timed distress event, grounded civilian spawning,
    /// 3-4 cornering zombies, procedural speech bubbles, dynamic map markers,
    /// and rewarding resolution (+20 Morale, +80 XP, supply bundle).
    /// </summary>
    public class CivilianSOSEvent : MonoBehaviour
    {
        [Header("Event Parameters")]
        [SerializeField] private float eventDuration = 360.0f; // 6-minute window
        [SerializeField] private float distressSpeechInterval = 8.0f;
        [SerializeField] private float clearCheckRadius = 14.0f;

        [Header("State")]
        [SerializeField] private string sosId = "SOS_Alpha";
        [SerializeField] private string locationDisplayName = "Sektör Dışı Harabe";
        [SerializeField] private Vector3 targetGroundPos;
        [SerializeField] private float timeRemaining = 360.0f;
        [SerializeField] private bool isEventActive = false;
        [SerializeField] private bool isResolved = false;
        [SerializeField] private bool isFailed = false;

        private GameObject civilianRoot;
        private HumanoidProceduralAnimator civilianAnimator;
        private WorldSpeechBubbleUI speechBubble;
        private CivilianRescueInteractable rescueInteractable;
        private NavMeshAgent civilianAgent;

        private readonly List<ZombieBrain> spawnedAttackers = new List<ZombieBrain>();
        private float speechTimer = 0f;
        private bool dangerClearedNotified = false;

        public bool IsEventActive => isEventActive;
        public bool IsResolved => isResolved;
        public bool IsFailed => isFailed;
        public float TimeRemaining => timeRemaining;
        public string SosId => sosId;
        public string LocationDisplayName => locationDisplayName;
        public Vector3 TargetGroundPos => targetGroundPos;
        public GameObject CivilianRoot => civilianRoot;
        public IReadOnlyList<ZombieBrain> SpawnedAttackers => spawnedAttackers;

        private readonly string[] distressCries = new string[]
        {
            "Yardım edin! Zombiler beni sıkıştırdı!",
            "Lütfen biri beni duysun!",
            "Mermim bitti, daha fazla dayanamıyorum!",
            "Buradayım, yardım çağrımı duyan var mı?!"
        };

        /// <summary>
        /// Launches the dynamic SOS rescue encounter at the target coordinates.
        /// </summary>
        public void LaunchSOS(Vector3 groundPos, string id = "SOS_Alpha", string locName = "Harabe Bölgesi")
        {
            if (isEventActive) return;

            sosId = id;
            locationDisplayName = locName;
            timeRemaining = eventDuration;
            isResolved = false;
            isFailed = false;
            dangerClearedNotified = false;

            // 1. Ensure position sits on NavMesh
            targetGroundPos = groundPos;
            if (NavMesh.SamplePosition(groundPos, out NavMeshHit navHit, 8.0f, NavMesh.AllAreas))
            {
                targetGroundPos = navHit.position;
            }

            // 2. Play 3D positional distress audio
            if (ProceduralAudioManager.Instance != null)
            {
                ProceduralAudioManager.Instance.PlayCivilianDistress(targetGroundPos);
            }

            // 3. Broadcast tactical radio banner / toast
            GameEvents.TriggerObjectiveToastRequested(
                $"ACİL SOS: {locationDisplayName} mevkiinde bir sivil zombiler tarafından kuşatıldı!",
                5.0f
            );

            // 4. Register map markers (cyan/amber pulsing SOS pin)
            RegisterMapMarkers();

            // 5. Construct & Spawn Grounded Civilian
            SpawnCivilian();

            // 6. Spawn 3-4 Attacking Zombies
            SpawnAttackingZombies();

            isEventActive = true;
            speechTimer = distressSpeechInterval * 0.4f; // Trigger first call quickly
        }

        private void RegisterMapMarkers()
        {
            Vector2 mapPos = new Vector2(targetGroundPos.x, targetGroundPos.z);
            Color sosColor = new Color(0.2f, 0.9f, 0.95f, 0.95f);

            if (MinimapHUDWidget.Instance != null)
            {
                MinimapHUDWidget.Instance.RegisterDynamicPoi(sosId, mapPos, sosColor);
            }

            if (FullTacticalMapUI.Instance != null)
            {
                FullTacticalMapUI.Instance.RegisterSOSMarker(sosId, $"Sivil İmdat ({locationDisplayName})", mapPos);
            }
        }

        private void UnregisterMapMarkers()
        {
            if (MinimapHUDWidget.Instance != null)
            {
                MinimapHUDWidget.Instance.UnregisterDynamicPoi(sosId);
            }

            if (FullTacticalMapUI.Instance != null)
            {
                FullTacticalMapUI.Instance.UnregisterLandmarkMarker(sosId);
            }
        }

        private void SpawnCivilian()
        {
            civilianRoot = new GameObject($"Civilian_Survivor_{sosId}");
            civilianRoot.transform.position = targetGroundPos;

            // NavMeshAgent
            civilianAgent = civilianRoot.AddComponent<NavMeshAgent>();
            civilianAgent.speed = 3.8f;
            civilianAgent.stoppingDistance = 0.5f;
            civilianAgent.acceleration = 8.0f;
            civilianAgent.radius = 0.35f;
            civilianAgent.height = 1.8f;

            // CapsuleCollider
            CapsuleCollider cc = civilianRoot.AddComponent<CapsuleCollider>();
            cc.radius = 0.4f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // AudioSource (3D Spatial Positional Audio)
            AudioSource audio = civilianRoot.AddComponent<AudioSource>();
            audio.spatialBlend = 1.0f;
            audio.minDistance = 2.0f;
            audio.maxDistance = 35.0f;
            audio.rolloffMode = AudioRolloffMode.Logarithmic;
            audio.playOnAwake = false;

            // Visual Model (Root -> Visual_Model)
            GameObject visualModel = BuildCivilianVisualModel(civilianRoot.transform);

            // HumanoidProceduralAnimator
            civilianAnimator = civilianRoot.AddComponent<HumanoidProceduralAnimator>();
            civilianAnimator.SetVisualModel(visualModel.transform);
            civilianAnimator.AutoBindLimbs();
            civilianAnimator.SetCowering(true);

            // Speech Bubble UI
            GameObject bubbleObj = new GameObject("WorldSpeechBubbleUI");
            bubbleObj.transform.SetParent(civilianRoot.transform, false);
            bubbleObj.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            speechBubble = bubbleObj.AddComponent<WorldSpeechBubbleUI>();
            speechBubble.BindTarget(civilianRoot.transform);

            // CivilianRescueInteractable
            rescueInteractable = civilianRoot.AddComponent<CivilianRescueInteractable>();
            rescueInteractable.BindEvent(this);
            rescueInteractable.SetDangerStatus(false);
        }

        private GameObject BuildCivilianVisualModel(Transform rootParent)
        {
            GameObject visual = new GameObject("Visual_Model");
            visual.transform.SetParent(rootParent, false);

            // Material Caches (instanced URP Lit)
            Material skinMat = GetOrCreateMaterial("Mat_Civilian_Skin", new Color(0.86f, 0.72f, 0.60f));
            Material jacketMat = GetOrCreateMaterial("Mat_Civilian_Jacket", new Color(0.22f, 0.35f, 0.48f));
            Material pantsMat = GetOrCreateMaterial("Mat_Civilian_Pants", new Color(0.32f, 0.28f, 0.22f));

            skinMat.enableInstancing = true;
            jacketMat.enableInstancing = true;
            pantsMat.enableInstancing = true;

            // Hips
            GameObject bodyHips = new GameObject("Body_Hips");
            bodyHips.transform.SetParent(visual.transform, false);
            bodyHips.transform.localPosition = new Vector3(0f, 0.88f, 0f);

            // Pelvis
            GameObject pelvis = CreatePart(PrimitiveType.Capsule, "Pelvis", bodyHips.transform,
                Vector3.zero, new Vector3(0.26f, 0.16f, 0.20f), pantsMat);
            pelvis.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // Torso
            GameObject torso = CreatePart(PrimitiveType.Capsule, "Torso", bodyHips.transform,
                new Vector3(0f, 0.30f, 0f), new Vector3(0.32f, 0.22f, 0.22f), jacketMat);

            // Neck & Head
            GameObject neck = CreatePart(PrimitiveType.Cylinder, "Neck", torso.transform,
                new Vector3(0f, 0.50f, 0.02f), new Vector3(0.16f, 0.10f, 0.16f), skinMat);

            CreatePart(PrimitiveType.Sphere, "Head", neck.transform,
                new Vector3(0f, 1.15f, 0f), new Vector3(1.20f, 1.30f, 1.20f), skinMat);

            // Left Arm
            GameObject shoulderL = new GameObject("Shoulder_L");
            shoulderL.transform.SetParent(torso.transform, false);
            shoulderL.transform.localPosition = new Vector3(-0.32f, 0.22f, 0f);

            GameObject upperArmL = CreatePart(PrimitiveType.Capsule, "UpperArm_L", shoulderL.transform,
                new Vector3(0f, -0.15f, 0f), new Vector3(0.13f, 0.15f, 0.13f), jacketMat);

            GameObject forearmL = CreatePart(PrimitiveType.Capsule, "Forearm_L", upperArmL.transform,
                new Vector3(0f, -0.95f, 0f), new Vector3(0.85f, 0.85f, 0.85f), skinMat);

            CreatePart(PrimitiveType.Sphere, "Hand_L", forearmL.transform,
                new Vector3(0f, -1.0f, 0f), new Vector3(0.75f, 0.55f, 0.65f), skinMat);

            // Right Arm
            GameObject shoulderR = new GameObject("Shoulder_R");
            shoulderR.transform.SetParent(torso.transform, false);
            shoulderR.transform.localPosition = new Vector3(0.32f, 0.22f, 0f);

            GameObject upperArmR = CreatePart(PrimitiveType.Capsule, "UpperArm_R", shoulderR.transform,
                new Vector3(0f, -0.15f, 0f), new Vector3(0.13f, 0.15f, 0.13f), jacketMat);

            GameObject forearmR = CreatePart(PrimitiveType.Capsule, "Forearm_R", upperArmR.transform,
                new Vector3(0f, -0.95f, 0f), new Vector3(0.85f, 0.85f, 0.85f), skinMat);

            CreatePart(PrimitiveType.Sphere, "Hand_R", forearmR.transform,
                new Vector3(0f, -1.0f, 0f), new Vector3(0.75f, 0.55f, 0.65f), skinMat);

            // Left Leg
            GameObject hipL = new GameObject("Hip_L");
            hipL.transform.SetParent(bodyHips.transform, false);
            hipL.transform.localPosition = new Vector3(-0.13f, -0.06f, 0f);

            GameObject thighL = CreatePart(PrimitiveType.Capsule, "Thigh_L", hipL.transform,
                new Vector3(0f, -0.22f, 0f), new Vector3(0.16f, 0.22f, 0.16f), pantsMat);

            GameObject calfL = CreatePart(PrimitiveType.Capsule, "Calf_L", thighL.transform,
                new Vector3(0f, -0.95f, 0f), new Vector3(0.88f, 0.88f, 0.88f), pantsMat);

            CreatePart(PrimitiveType.Cube, "Foot_L", calfL.transform,
                new Vector3(0f, -1.0f, 0.10f), new Vector3(0.75f, 0.35f, 1.25f), pantsMat);

            // Right Leg
            GameObject hipR = new GameObject("Hip_R");
            hipR.transform.SetParent(bodyHips.transform, false);
            hipR.transform.localPosition = new Vector3(0.13f, -0.06f, 0f);

            GameObject thighR = CreatePart(PrimitiveType.Capsule, "Thigh_R", hipR.transform,
                new Vector3(0f, -0.22f, 0f), new Vector3(0.16f, 0.22f, 0.16f), pantsMat);

            GameObject calfR = CreatePart(PrimitiveType.Capsule, "Calf_R", thighR.transform,
                new Vector3(0f, -0.95f, 0f), new Vector3(0.88f, 0.88f, 0.88f), pantsMat);

            CreatePart(PrimitiveType.Cube, "Foot_R", calfR.transform,
                new Vector3(0f, -1.0f, 0.10f), new Vector3(0.75f, 0.35f, 1.25f), pantsMat);

            return visual;
        }

        private GameObject CreatePart(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            Collider col = obj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            Renderer r = obj.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            return obj;
        }

        private void SpawnAttackingZombies()
        {
            spawnedAttackers.Clear();

            // Find an existing ZombieAI in scene to clone if available
            ZombieAI template = FindFirstObjectByType<ZombieAI>();

            int count = 4;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = (i * angleStep + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
                float dist = Random.Range(2.8f, 4.2f);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                Vector3 spawnPos = targetGroundPos + offset;

                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                }

                Quaternion lookRot = Quaternion.LookRotation((targetGroundPos - spawnPos).normalized);

                ZombieAI spawned = null;
                if (template != null)
                {
                    GameObject zObj = Instantiate(template.gameObject, spawnPos, lookRot);
                    zObj.name = $"CivilianSOS_Attacker_{i + 1}";
                    zObj.SetActive(true);
                    spawned = zObj.GetComponent<ZombieAI>();
                    if (spawned != null)
                    {
                        spawned.ConfigureStats(50f, 10f, 3.8f);
                        spawned.TriggerInvestigate(targetGroundPos);
                    }
                }

                if (spawned != null)
                {
                    spawnedAttackers.Add(spawned);
                }
            }
        }

        private void Update()
        {
            if (!isEventActive || isResolved || isFailed) return;

            // Countdown timer
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                OnEventFailed();
                return;
            }

            // Periodic distress speech
            speechTimer -= Time.deltaTime;
            if (speechTimer <= 0f)
            {
                speechTimer = distressSpeechInterval;
                PlayRandomDistressSpeech();
            }

            // Check if attackers within clearCheckRadius are eliminated
            CheckAttackerThreat();
        }

        private void PlayRandomDistressSpeech()
        {
            if (civilianRoot == null || dangerClearedNotified) return;

            // Voice audio
            if (ProceduralAudioManager.Instance != null)
            {
                ProceduralAudioManager.Instance.PlayCivilianDistress(civilianRoot.transform.position);
            }

            // Speech bubble
            if (speechBubble != null && distressCries.Length > 0)
            {
                string cry = distressCries[Random.Range(0, distressCries.Length)];
                speechBubble.ShowSpeech(cry, 3.2f);
            }
        }

        private void CheckAttackerThreat()
        {
            if (civilianRoot == null) return;

            int activeThreatCount = 0;
            Vector3 civPos = civilianRoot.transform.position;

            for (int i = 0; i < spawnedAttackers.Count; i++)
            {
                var z = spawnedAttackers[i];
                if (z != null && !z.IsDead && z.gameObject.activeInHierarchy)
                {
                    float d = Vector3.Distance(z.transform.position, civPos);
                    if (d <= clearCheckRadius)
                    {
                        activeThreatCount++;
                    }
                }
            }

            bool isDangerCleared = (activeThreatCount == 0);

            if (isDangerCleared && !dangerClearedNotified)
            {
                dangerClearedNotified = true;

                // Unlock rescue interaction
                if (rescueInteractable != null)
                {
                    rescueInteractable.SetDangerStatus(true);
                }

                // Stand up from cowering
                if (civilianAnimator != null)
                {
                    civilianAnimator.SetCowering(false);
                }

                // Relief speech
                if (speechBubble != null)
                {
                    speechBubble.ShowSpeech("Tanrıya şükür, kurtuldum! Teşekkür ederim!", 3.8f);
                }

                ProceduralAudioManager.Instance?.PlaySurvivorRelief();
            }
            else if (!isDangerCleared && dangerClearedNotified)
            {
                // Danger re-engaged if new enemies enter radius
                dangerClearedNotified = false;
                if (rescueInteractable != null)
                {
                    rescueInteractable.SetDangerStatus(false);
                }
                if (civilianAnimator != null)
                {
                    civilianAnimator.SetCowering(true);
                }
            }
        }

        /// <summary>
        /// Called when the player successfully completes the 0.6s hold "SİVİLİ KURTAR" interaction.
        /// </summary>
        public void OnCivilianRescued()
        {
            if (isResolved || isFailed) return;
            isResolved = true;

            // 1. Award +20 Community Morale
            if (CommunityMoraleManager.Instance != null)
            {
                CommunityMoraleManager.Instance.AddMorale(20f);
            }

            // 2. Award +80 Progression XP
            if (SurvivorProgressionManager.Instance != null)
            {
                SurvivorProgressionManager.Instance.AddXP(80, "Sivil Kuşatmadan Kurtarıldı");
            }

            // 3. Deliver Gratitude Supplies (2x Bandage, 1x Water, 2x Scrap)
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.AddItemById("item_bandage", 2);
                InventorySystem.Instance.AddItemById("item_purified_water", 1);
                InventorySystem.Instance.AddItemById("item_scrap", 2);
            }

            // 4. Audio & Haptic Feedback
            ProceduralAudioManager.Instance?.PlayVaultUnlock();
            ProceduralAudioManager.Instance?.PlayItemPickup();
            MobileHapticManager.TriggerSuccess();

            // 5. Objective Banner
            GameEvents.TriggerObjectiveToastRequested(
                "Sivil Başarıyla Kurtarıldı! (+80 XP & +20 Topluluk Morali)",
                4.2f
            );

            // 6. Speech & Safe Escape
            if (speechBubble != null)
            {
                speechBubble.ShowSpeech("Güvenli sığınağa doğru gidiyorum, minnettarım!", 3.5f);
            }

            // 7. Remove Map Markers
            UnregisterMapMarkers();

            // 8. Route Civilian Safely Away
            StartCoroutine(CivilianEscapeRoutine());
        }

        private IEnumerator CivilianEscapeRoutine()
        {
            if (civilianAgent != null && civilianAgent.isOnNavMesh)
            {
                civilianAgent.isStopped = false;
                civilianAgent.speed = 4.2f;

                // Pick an escape direction pointing outward away from origin
                Vector3 escapeDir = (targetGroundPos - Vector3.zero).normalized;
                Vector3 escapeDest = targetGroundPos + escapeDir * 28f;

                if (NavMesh.SamplePosition(escapeDest, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    civilianAgent.SetDestination(hit.position);
                }
            }

            yield return new WaitForSeconds(6.0f);

            // Clean up civilian object smoothly
            if (civilianRoot != null)
            {
                Destroy(civilianRoot);
            }

            isEventActive = false;
        }

        private void OnEventFailed()
        {
            if (isResolved || isFailed) return;
            isFailed = true;
            isEventActive = false;

            GameEvents.TriggerObjectiveToastRequested(
                $"ACİL SOS BAŞARISIZ: {locationDisplayName} mevkiindeki sivilden sinyal kesildi.",
                3.8f
            );

            UnregisterMapMarkers();

            if (civilianRoot != null)
            {
                Destroy(civilianRoot, 2.0f);
            }
        }

        public void CancelEvent()
        {
            isEventActive = false;
            UnregisterMapMarkers();

            if (civilianRoot != null)
            {
                Destroy(civilianRoot);
            }

            for (int i = 0; i < spawnedAttackers.Count; i++)
            {
                if (spawnedAttackers[i] != null)
                {
                    Destroy(spawnedAttackers[i].gameObject);
                }
            }
            spawnedAttackers.Clear();
        }

        private void OnDestroy()
        {
            UnregisterMapMarkers();
        }

        private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        private static Material GetOrCreateMaterial(string name, Color color)
        {
            if (materialCache.TryGetValue(name, out Material mat) && mat != null) return mat;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = name;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.enableInstancing = true;
            materialCache[name] = mat;
            return mat;
        }
    }
}
