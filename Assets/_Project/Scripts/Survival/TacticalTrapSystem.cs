using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.AI;
using ProjectSurvival.Player;

namespace ProjectSurvival.Survival
{
    /// <summary>
    /// Deployable Tactical Trap System.
    /// Manages:
    /// 1. Spiked Barricade Trap (trap_spiked_barricade):
    ///    Physical wooden & rebar barrier (160 HP, NavMesh obstacle). Entangled zombies take 22 dmg/s and are slowed by 65%.
    /// 2. Acoustic Sound Bomb (trap_sound_bomb):
    ///    Throwable physics device. Emits 48m acoustic lure with pulsing siren and flashing amber light for 5.0s,
    ///    then detonates in a 6.5m AoE dealing 140 damage with knockback shockwave.
    /// </summary>
    public class TacticalTrapSystem : MonoBehaviour
    {
        private static TacticalTrapSystem _instance;
        public static TacticalTrapSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TacticalTrapSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("TacticalTrapSystem");
                        _instance = go.AddComponent<TacticalTrapSystem>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private Material woodMaterial;
        private Material metalMaterial;
        private Material amberLightMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("TacticalTrapSystem");
                Instance = go.AddComponent<TacticalTrapSystem>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeMaterials();
        }

        private void InitializeMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            woodMaterial = new Material(litShader);
            woodMaterial.name = "Mat_Trap_Wood";
            Color woodCol = new Color(0.38f, 0.25f, 0.15f);
            if (woodMaterial.HasProperty("_BaseColor")) woodMaterial.SetColor("_BaseColor", woodCol);
            else woodMaterial.color = woodCol;
            if (woodMaterial.HasProperty("_Smoothness")) woodMaterial.SetFloat("_Smoothness", 0.25f);

            metalMaterial = new Material(litShader);
            metalMaterial.name = "Mat_Trap_Metal";
            Color metalCol = new Color(0.20f, 0.22f, 0.25f);
            if (metalMaterial.HasProperty("_BaseColor")) metalMaterial.SetColor("_BaseColor", metalCol);
            else metalMaterial.color = metalCol;
            if (metalMaterial.HasProperty("_Metallic")) metalMaterial.SetFloat("_Metallic", 0.85f);
            if (metalMaterial.HasProperty("_Smoothness")) metalMaterial.SetFloat("_Smoothness", 0.65f);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            amberLightMaterial = new Material(unlitShader);
            amberLightMaterial.name = "Mat_SoundBomb_AmberGlow";
            Color amber = new Color(1.0f, 0.72f, 0.01f, 1f);
            if (amberLightMaterial.HasProperty("_BaseColor")) amberLightMaterial.SetColor("_BaseColor", amber);
            else amberLightMaterial.color = amber;
        }

        // =========================================================================
        // 1. SPIKED BARRICADE TRAP
        // =========================================================================
        public bool DeploySpikedBarricade()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return false;

            Vector3 spawnOrigin = player.transform.position + player.transform.forward * 1.8f;
            Vector3 spawnNormal = Vector3.up;

            if (Physics.Raycast(spawnOrigin + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 4f))
            {
                spawnOrigin = hit.point;
                spawnNormal = hit.normal;
            }

            Quaternion rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(player.transform.forward, spawnNormal).normalized, spawnNormal);

            GameObject barricadeObj = new GameObject("Spiked_Barricade_Trap");
            barricadeObj.transform.position = spawnOrigin;
            barricadeObj.transform.rotation = rot;

            // Build 3D Barricade Geometry
            // 1. Main Base Crossbeam
            GameObject beam1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam1.name = "Crossbeam_Bottom";
            beam1.transform.SetParent(barricadeObj.transform, false);
            beam1.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            beam1.transform.localScale = new Vector3(2.2f, 0.25f, 0.35f);
            SetupPart(beam1, woodMaterial);

            GameObject beam2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam2.name = "Crossbeam_Top";
            beam2.transform.SetParent(barricadeObj.transform, false);
            beam2.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            beam2.transform.localScale = new Vector3(2.2f, 0.20f, 0.30f);
            SetupPart(beam2, woodMaterial);

            // 2. Heavy Angled Support Legs
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"SupportLeg_{i}";
                leg.transform.SetParent(barricadeObj.transform, false);
                leg.transform.localPosition = new Vector3(i * 0.95f, 0.50f, -0.25f);
                leg.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
                leg.transform.localScale = new Vector3(0.18f, 1.15f, 0.18f);
                SetupPart(leg, woodMaterial);
            }

            // 3. Protruding Rebar / Wooden Spikes (5 Spikes angled forward)
            for (int s = 0; s < 5; s++)
            {
                float xOffset = -0.80f + s * 0.40f;
                GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spike.name = $"Spike_{s + 1}";
                spike.transform.SetParent(barricadeObj.transform, false);
                spike.transform.localPosition = new Vector3(xOffset, 0.50f, 0.22f);
                spike.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
                spike.transform.localScale = new Vector3(0.06f, 0.55f, 0.06f);
                SetupPart(spike, metalMaterial);
            }

            // Physical BoxCollider
            BoxCollider box = barricadeObj.AddComponent<BoxCollider>();
            box.size = new Vector3(2.2f, 1.1f, 0.85f);
            box.center = new Vector3(0f, 0.55f, 0f);

            // NavMesh Obstacle
            NavMeshObstacle obstacle = barricadeObj.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(2.4f, 1.2f, 1.0f);
            obstacle.center = new Vector3(0f, 0.6f, 0f);
            obstacle.carving = true;

            // Spiked Barricade Script component
            barricadeObj.AddComponent<SpikedBarricadeInstance>();

            ProceduralAudioManager.Instance?.PlayTrapDeploy();
            GameEvents.TriggerObjectiveToastRequested("[TUZAK KURULDU] Çivili barikat kuruldu! (160 Dayanıklılık)", 3.0f);
            return true;
        }

        // =========================================================================
        // 2. ACOUSTIC SOUND BOMB
        // =========================================================================
        public bool ThrowSoundBomb()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return false;

            Camera cam = Camera.main;
            Vector3 camFwd = cam != null ? cam.transform.forward : player.transform.forward;
            Vector3 spawnPos = player.transform.position + Vector3.up * 1.4f + camFwd * 0.65f;
            Vector3 throwVector = (camFwd * 15f) + (Vector3.up * 3.5f);

            PlayerAnimationBridge anim = player.GetComponent<PlayerAnimationBridge>();
            if (anim != null) anim.TriggerThrow();

            GameObject bombObj = new GameObject("TacticalSoundBomb_Thrown");
            bombObj.transform.position = spawnPos;

            // 3D Visual Mesh
            GameObject canister = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            canister.name = "CanisterBody";
            canister.transform.SetParent(bombObj.transform, false);
            canister.transform.localScale = new Vector3(0.14f, 0.18f, 0.14f);
            SetupPart(canister, metalMaterial);

            // LED Beacon Top
            GameObject led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            led.name = "Amber_LED";
            led.transform.SetParent(bombObj.transform, false);
            led.transform.localPosition = new Vector3(0f, 0.19f, 0f);
            led.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            SetupPart(led, amberLightMaterial);

            // Flashing Point Light
            Light pLight = bombObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.72f, 0.01f);
            pLight.range = 7f;
            pLight.intensity = 0f;

            // Physics
            SphereCollider sc = bombObj.AddComponent<SphereCollider>();
            sc.radius = 0.16f;
            Rigidbody rb = bombObj.AddComponent<Rigidbody>();
            rb.mass = 0.65f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.AddForce(throwVector, ForceMode.Impulse);

            // Controller
            SoundBombInstance instance = bombObj.AddComponent<SoundBombInstance>();
            instance.Initialize(pLight);

            return true;
        }

        private static void SetupPart(GameObject part, Material mat)
        {
            Collider col = part.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Renderer r = part.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
        }
    }

    /// <summary>
    /// Runtime instance for deployed Spiked Barricade.
    /// Deals 22 dmg/s and slows entangled zombies by 65%.
    /// Has 160 Structural HP.
    /// </summary>
    public class SpikedBarricadeInstance : MonoBehaviour
    {
        private float structuralHP = 160f;
        private const float DamagePerSecond = 22f;
        private const float SlowMultiplier = 0.35f; // 65% slow -> 35% speed remaining

        private void Update()
        {
            // Check for zombies in contact/vicinity
            Collider[] hits = Physics.OverlapBox(transform.position + Vector3.up * 0.5f, new Vector3(1.2f, 0.6f, 0.7f), transform.rotation);
            for (int i = 0; i < hits.Length; i++)
            {
                ZombieBrain zombie = hits[i].GetComponentInParent<ZombieBrain>();
                if (zombie != null && !zombie.IsDead)
                {
                    // Deal bleed / spike entanglement damage
                    zombie.TakeDamage(DamagePerSecond * Time.deltaTime);

                    // Apply slow to NavMeshAgent
                    NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.speed = Mathf.Min(agent.speed, 1.2f);
                    }

                    // Barricade absorbs retaliatory pressure
                    structuralHP -= 3.5f * Time.deltaTime;
                    if (structuralHP <= 0f)
                    {
                        Collapse();
                        return;
                    }
                }
            }
        }

        public void TakeDamage(float dmg)
        {
            structuralHP -= dmg;
            if (structuralHP <= 0f)
            {
                Collapse();
            }
        }

        private void Collapse()
        {
            ProceduralAudioManager.Instance?.PlayGlassShatter();
            GameEvents.TriggerCameraShakeRequested(0.2f, 0.15f);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Runtime instance for thrown Acoustic Sound Bomb.
    /// 5.0 seconds lure with 48m acoustic broadcast, followed by 6.5m AoE 140 dmg detonation.
    /// </summary>
    public class SoundBombInstance : MonoBehaviour
    {
        private Light pointLight;
        private bool hasLanded = false;
        private float activeTimer = 5.0f;
        private float beepInterval = 0.55f;
        private float nextBeepTime = 0f;

        public void Initialize(Light light)
        {
            pointLight = light;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasLanded) return;
            hasLanded = true;
            nextBeepTime = Time.time;
        }

        private void Update()
        {
            if (!hasLanded) return;

            activeTimer -= Time.deltaTime;

            // Pulse strobe light & acoustic siren
            if (Time.time >= nextBeepTime)
            {
                nextBeepTime = Time.time + beepInterval;
                StartCoroutine(FlashLightRoutine());

                // Broadcast 48m acoustic lure
                HordeAlertSystem.Instance?.BroadcastAcousticLure(transform.position, 48f);
                GameEvents.TriggerNoiseEmitted(transform.position, 48f);
                ProceduralAudioManager.Instance?.PlayButtonClick();
            }

            if (activeTimer <= 0f)
            {
                Detonate();
            }
        }

        private IEnumerator FlashLightRoutine()
        {
            if (pointLight == null) yield break;
            pointLight.intensity = 4.5f;
            yield return new WaitForSeconds(0.15f);
            if (pointLight != null) pointLight.intensity = 0f;
        }

        private void Detonate()
        {
            Vector3 explosionPos = transform.position;

            // 1. Audio & Camera Shake
            ProceduralAudioManager.Instance?.PlayExplosion();
            GameEvents.TriggerCameraShakeRequested(0.48f, 0.30f);

            // 2. 6.5m AoE Damage & Cripple Shockwave (scaled by Munitions Master)
            float baseRad = 6.5f;
            if (ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null)
            {
                baseRad *= ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetExplosiveTrapRadiusMultiplier();
            }
            Collider[] hits = Physics.OverlapSphere(explosionPos, baseRad);
            List<ZombieBrain> hitZombies = new List<ZombieBrain>();

            for (int i = 0; i < hits.Length; i++)
            {
                ZombieBrain z = hits[i].GetComponentInParent<ZombieBrain>();
                if (z != null && !z.IsDead && !hitZombies.Contains(z))
                {
                    hitZombies.Add(z);
                    float dist = Vector3.Distance(explosionPos, z.transform.position);
                    float damageFalloff = Mathf.Clamp01(1f - (dist / baseRad));
                    float finalDamage = Mathf.Lerp(80f, 140f, damageFalloff);

                    z.TakeDamage(finalDamage);

                    // Knockback / Cripple
                    Vector3 knockDir = (z.transform.position - explosionPos).normalized;
                    knockDir.y = 0.2f;
                    z.ApplyStaggerKnockback(knockDir * 4.5f);
                }
            }

            Debug.Log($"[SoundBomb] Detonated at {explosionPos}! Dealt up to 140 AoE damage to {hitZombies.Count} zombies!");
            Destroy(gameObject);
        }
    }
}
