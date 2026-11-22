using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.AI
{
    public class ZombieSensorySystem : MonoBehaviour
    {
        [Header("Vision Settings per Archetype")]
        [SerializeField] protected ZombieArchetype archetype = ZombieArchetype.LoneRoamer;
        [SerializeField] protected float eyeHeight = 1.6f;
        [SerializeField] protected LayerMask obstacleMask = ~0;

        [Header("Hearing Settings")]
        [SerializeField] protected float hearingMultiplier = 1.0f;

        protected Transform playerTransform;
        protected StealthSystem playerStealth;
        protected FlashlightController playerFlashlight;

        protected bool isNightTime = false;
        protected bool isPlayerFlashlightOn = false;

        public ZombieArchetype Archetype { get => archetype; set => archetype = value; }
        public float HearingMultiplier { get => hearingMultiplier; set => hearingMultiplier = value; }
        public float EyeHeight { get => eyeHeight; set => eyeHeight = value; }

        public float ViewDistance
        {
            get => (archetype == ZombieArchetype.LoneRoamer) ? 20f : ((archetype == ZombieArchetype.Scavenger) ? 13f : 7.5f);
            set { }
        }

        public float ViewAngle
        {
            get => (archetype == ZombieArchetype.LoneRoamer) ? 125f : ((archetype == ZombieArchetype.Scavenger) ? 75f : 360f);
            set { }
        }

        public bool CanSeePlayer(out Transform target)
        {
            return CheckVision(out target, out _);
        }

        public bool CanSeeTarget(out Transform target)
        {
            return CheckVision(out target, out _);
        }

        protected virtual void Awake()
        {
            // Mask out Player and Ignore Raycast layers from obstacleMask
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) obstacleMask &= ~(1 << playerLayer);
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycast != -1) obstacleMask &= ~(1 << ignoreRaycast);
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1) obstacleMask &= ~(1 << uiLayer);
        }

        protected virtual void Start()
        {
            FindPlayer();
        }

        protected virtual void OnEnable()
        {
            GameEvents.OnNoiseEmitted += HandleNoiseHeard;
            GameEvents.OnDayNightChanged += HandleDayNightChanged;
            GameEvents.OnFlashlightToggled += HandleFlashlightToggled;
        }

        protected virtual void OnDisable()
        {
            GameEvents.OnNoiseEmitted -= HandleNoiseHeard;
            GameEvents.OnDayNightChanged -= HandleDayNightChanged;
            GameEvents.OnFlashlightToggled -= HandleFlashlightToggled;
        }

        public void FindPlayer()
        {
            if (playerTransform != null) return;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerStealth = player.GetComponent<StealthSystem>();
                playerFlashlight = player.GetComponentInChildren<FlashlightController>();
            }
        }

        public void SetArchetype(ZombieArchetype newArchetype)
        {
            archetype = newArchetype;
        }

        public bool CheckVision(out Transform target, out AwarenessState awareness)
        {
            target = null;
            awareness = AwarenessState.Unaware;

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return false;
            }

            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
            Vector3 targetCenter = playerTransform.position + Vector3.up * 1f;
            Vector3 toTarget = targetCenter - eyePos;
            float distance = toTarget.magnitude;

            // 1. DORMANT LURKER SENSORY RULES
            if (archetype == ZombieArchetype.DormantLurker)
            {
                // Immediate wake-up on close proximity (< 7.5m)
                if (distance <= 7.5f)
                {
                    if (HasClearLineOfSight(eyePos, toTarget, distance))
                    {
                        target = playerTransform;
                        awareness = AwarenessState.Alerted;
                        return true;
                    }
                }

                // Immediate wake-up if flashlight beam points directly at it (< 18m)
                if (isPlayerFlashlightOn && distance <= 18f)
                {
                    Vector3 playerForward = playerTransform.forward;
                    Vector3 fromPlayerToZombie = (eyePos - playerTransform.position).normalized;
                    if (Vector3.Dot(playerForward, fromPlayerToZombie) > 0.75f)
                    {
                        if (HasClearLineOfSight(eyePos, toTarget, distance))
                        {
                            target = playerTransform;
                            awareness = AwarenessState.Alerted;
                            return true;
                        }
                    }
                }

                return false;
            }

            // 2. VISION CONE PARAMETERS (LoneRoamer vs Scavenger)
            float maxViewDist = (archetype == ZombieArchetype.LoneRoamer) ? 20f : 13f;
            float maxViewAngle = (archetype == ZombieArchetype.LoneRoamer) ? 125f : 75f;

            // Crouch reduction: LoneRoamer range drops to 11m, Scavenger range drops to 7m
            if (playerStealth != null && playerStealth.IsCrouched)
            {
                maxViewDist = (archetype == ZombieArchetype.LoneRoamer) ? 11f : 7.0f;
                if (ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null)
                {
                    maxViewDist *= ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetZombieVisionRangeMultiplier();
                }
            }

            // Night time adjustment: reduced slightly unless player has flashlight
            if (isNightTime)
            {
                maxViewDist = isPlayerFlashlightOn ? (maxViewDist * 1.35f) : (maxViewDist * 0.75f);
            }

            if (ProjectSurvival.Environment.DynamicWeatherManager.Instance != null)
            {
                maxViewDist *= ProjectSurvival.Environment.DynamicWeatherManager.Instance.GetWeatherZombieSightMultiplier();
            }

            // Phase 4: Dynamic Stealth Visibility & Exposure Scaling
            if (StealthVisibilityController.Instance != null)
            {
                maxViewDist *= StealthVisibilityController.Instance.GetVisionDistanceMultiplier();
            }

            if (distance > maxViewDist) return false;

            Vector3 horizontalDir = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
            float angle = Vector3.Angle(transform.forward, horizontalDir);

            if (angle <= maxViewAngle * 0.5f)
            {
                if (HasClearLineOfSight(eyePos, toTarget, distance))
                {
                    target = playerTransform;
                    awareness = AwarenessState.Alerted;
                    return true;
                }
            }

            // Also check friendly survivors (NPCs)
            for (int i = 0; i < SurvivorNPC_AI.ActiveSurvivors.Count; i++)
            {
                var survivor = SurvivorNPC_AI.ActiveSurvivors[i];
                if (survivor == null || survivor.IsDead) continue;

                Vector3 sCenter = survivor.transform.position + Vector3.up * 1f;
                Vector3 toS = sCenter - eyePos;
                float distS = toS.magnitude;
                if (distS > maxViewDist) continue;

                Vector3 sHDir = new Vector3(toS.x, 0f, toS.z).normalized;
                if (Vector3.Angle(transform.forward, sHDir) <= maxViewAngle * 0.5f)
                {
                    if (HasClearLineOfSight(eyePos, toS, distS))
                    {
                        target = survivor.transform;
                        awareness = AwarenessState.Alerted;
                        return true;
                    }
                }
            }

            // Also check armed human raiders (Cross-Faction Combat)
            var raiders = HumanRaiderAI.ActiveRaiders;
            if (raiders != null)
            {
                for (int i = 0; i < raiders.Count; i++)
                {
                    var raider = raiders[i];
                    if (raider == null || raider.IsDead) continue;

                    Vector3 rCenter = raider.transform.position + Vector3.up * 1f;
                    Vector3 toR = rCenter - eyePos;
                    float distR = toR.magnitude;
                    if (distR > maxViewDist) continue;

                    Vector3 rHDir = new Vector3(toR.x, 0f, toR.z).normalized;
                    if (Vector3.Angle(transform.forward, rHDir) <= maxViewAngle * 0.5f)
                    {
                        if (HasClearLineOfSight(eyePos, toR, distR))
                        {
                            target = raider.transform;
                            awareness = AwarenessState.Alerted;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HasClearLineOfSight(Vector3 origin, Vector3 dirVec, float dist)
        {
            if (Physics.Raycast(origin, dirVec.normalized, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // If it hits an obstacle before the target
                bool isTarget = hit.collider.CompareTag("Player") ||
                                hit.collider.GetComponentInParent<Player.PlayerController>() != null ||
                                hit.collider.GetComponentInParent<SurvivorNPC_AI>() != null ||
                                hit.collider.name.Contains("NPC") ||
                                hit.collider.name.Contains("Player");
                if (!isTarget)
                {
                    return false;
                }
            }
            return true;
        }

        private void HandleNoiseHeard(Vector3 noiseSource, float noiseRadius)
        {
            float distance = Vector3.Distance(transform.position, noiseSource);

            // Archetype specific hearing response
            float effectiveHearingRange = noiseRadius * hearingMultiplier;

            // Scavenger has a 9m close-range acoustic radius that catches sprinting
            if (archetype == ZombieArchetype.Scavenger)
            {
                effectiveHearingRange = Mathf.Max(effectiveHearingRange, 9.0f);
            }

            // Dormant lurker awakens to loud noises (gunshots, soundbombs > 20m) within 25m
            if (archetype == ZombieArchetype.DormantLurker)
            {
                if (noiseRadius >= 18f && distance <= 25f)
                {
                    SendMessage("OnLurkerNoiseAwaken", noiseSource, SendMessageOptions.DontRequireReceiver);
                    return;
                }
                return; // Lurkers ignore soft ambient noises while dormant
            }

            // Wall occlusion linecast
            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
            Vector3 soundPos = noiseSource + Vector3.up * 0.5f;
            if (Physics.Linecast(soundPos, eyePos, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                effectiveHearingRange *= 0.55f;
            }

            if (distance <= effectiveHearingRange)
            {
                SendMessage("HearNoiseStimulus", noiseSource, SendMessageOptions.DontRequireReceiver);
            }
        }

        private void HandleDayNightChanged(bool isNight, float hour)
        {
            isNightTime = isNight;
        }

        private void HandleFlashlightToggled(bool isOn, float batteryPercent)
        {
            isPlayerFlashlightOn = isOn;
        }
    }
}
