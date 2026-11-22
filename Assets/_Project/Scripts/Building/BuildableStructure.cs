using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Building
{
    public enum StructureType
    {
        DefenseWall = 0,    // Savunma Duvarı: 350 HP, NavMeshObstacle Carving, blocks movement
        RazorWire = 1,      // Jiletli Tel: 180 HP, Trigger slow 70%, 18 bleed dmg/s to zombies
        Floodlight = 2,     // Projektör Lambası: 220 HP, 26m SpotLight (Shadows.None), -20% zombie speed
        RainCollector = 3,  // Yağmur Toplayıcı: 200 HP, produces 1 purified water / 3h (1.5h in rain)
        HerbPlanter = 4     // Şifalı Bitki Saksısı: 160 HP, produces 1 cloth / 4h
    }

    [System.Serializable]
    public struct ResourceCost
    {
        public string itemId;
        public int amount;

        public ResourceCost(string id, int count)
        {
            itemId = id;
            amount = count;
        }
    }

    /// <summary>
    /// Active Phase 3: Modular Buildable Fortification & Survival Utility Structure.
    /// Handles procedural AAA mesh instantiation, NavMesh carving, HP durability,
    /// zombie barricade collision/traps, passive resource production, and persistence.
    /// </summary>
    public class BuildableStructure : MonoBehaviour
    {
        public static readonly List<BuildableStructure> ActiveStructures = new List<BuildableStructure>();

        [Header("Structure Identity")]
        [SerializeField] private string structureId = string.Empty;
        [SerializeField] private StructureType structureType = StructureType.DefenseWall;

        [Header("Durability & Health")]
        [SerializeField] private float maxHP = 350f;
        [SerializeField] private float currentHP = 350f;

        [Header("Passive Resource Generation")]
        [SerializeField] private int accumulatedResources = 0;
        [SerializeField] private float resourceTimer = 0f;

        [Header("Components")]
        [SerializeField] private NavMeshObstacle navObstacle;
        [SerializeField] private BoxCollider physicalCollider;
        [SerializeField] private Light spotLightComponent;

        private float dazzleTimer = 0f;
        private Coroutine flashRoutine;

        public string StructureId => structureId;
        public StructureType Type => structureType;
        public float MaxHP => maxHP;
        public float CurrentHP => currentHP;
        public bool IsDamaged => currentHP < maxHP;
        public int AccumulatedResources => accumulatedResources;
        public NavMeshObstacle NavObstacle => navObstacle;

        public static ResourceCost[] GetCosts(StructureType type)
        {
            switch (type)
            {
                case StructureType.DefenseWall:
                    return new ResourceCost[] { new ResourceCost("item_scrap", 4), new ResourceCost("item_cloth", 1) };
                case StructureType.RazorWire:
                    return new ResourceCost[] { new ResourceCost("item_scrap", 3) };
                case StructureType.Floodlight:
                    return new ResourceCost[] { new ResourceCost("item_scrap", 3), new ResourceCost("item_battery", 1) };
                case StructureType.RainCollector:
                    return new ResourceCost[] { new ResourceCost("item_scrap", 2), new ResourceCost("item_cloth", 2) };
                case StructureType.HerbPlanter:
                    return new ResourceCost[] { new ResourceCost("item_scrap", 2), new ResourceCost("item_water_bottle", 1) };
                default:
                    return new ResourceCost[0];
            }
        }

        public static float GetDefaultMaxHP(StructureType type)
        {
            switch (type)
            {
                case StructureType.DefenseWall: return 350f;
                case StructureType.RazorWire: return 180f;
                case StructureType.Floodlight: return 220f;
                case StructureType.RainCollector: return 200f;
                case StructureType.HerbPlanter: return 160f;
                default: return 100f;
            }
        }

        public static string GetStructureDisplayName(StructureType type)
        {
            switch (type)
            {
                case StructureType.DefenseWall: return "Savunma Barikatı";
                case StructureType.RazorWire: return "Jiletli Tel Tuzağı";
                case StructureType.Floodlight: return "Projektör Lambası";
                case StructureType.RainCollector: return "Yağmur Toplayıcı";
                case StructureType.HerbPlanter: return "Şifalı Bitki Saksısı";
                default: return "Yapı";
            }
        }

        private void OnEnable()
        {
            if (!ActiveStructures.Contains(this))
            {
                ActiveStructures.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveStructures.Remove(this);
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(structureId))
            {
                structureId = "struct_" + Guid.NewGuid().ToString("N").Substring(0, 10);
            }
        }

        private void Start()
        {
            ConfigureComponents();
        }

        public void Initialize(StructureType type, string id = null, float hp = -1f, int resources = 0, float timer = 0f)
        {
            structureType = type;
            maxHP = GetDefaultMaxHP(type);
            currentHP = (hp >= 0f) ? Mathf.Min(hp, maxHP) : maxHP;
            accumulatedResources = resources;
            resourceTimer = timer;

            if (!string.IsNullOrEmpty(id))
            {
                structureId = id;
            }
            else if (string.IsNullOrEmpty(structureId))
            {
                structureId = "struct_" + Guid.NewGuid().ToString("N").Substring(0, 10);
            }

            gameObject.name = $"Structure_{type}_{structureId}";
            ConfigureComponents();
        }

        private void ConfigureComponents()
        {
            if (physicalCollider == null)
            {
                physicalCollider = GetComponent<BoxCollider>();
                if (physicalCollider == null) physicalCollider = gameObject.AddComponent<BoxCollider>();
            }

            switch (structureType)
            {
                case StructureType.DefenseWall:
                    physicalCollider.size = new Vector3(2.4f, 2.2f, 0.5f);
                    physicalCollider.center = new Vector3(0f, 1.1f, 0f);
                    physicalCollider.isTrigger = false;

                    if (navObstacle == null) navObstacle = GetComponent<NavMeshObstacle>() ?? gameObject.AddComponent<NavMeshObstacle>();
                    navObstacle.shape = NavMeshObstacleShape.Box;
                    navObstacle.size = new Vector3(2.4f, 2.2f, 0.55f);
                    navObstacle.center = new Vector3(0f, 1.1f, 0f);
                    navObstacle.carving = true;
                    navObstacle.carveOnlyStationary = false;
                    break;

                case StructureType.RazorWire:
                    physicalCollider.size = new Vector3(2.0f, 0.9f, 0.9f);
                    physicalCollider.center = new Vector3(0f, 0.45f, 0f);
                    physicalCollider.isTrigger = true; // Trigger for slowing / bleeding
                    if (navObstacle != null) Destroy(navObstacle);
                    break;

                case StructureType.Floodlight:
                    physicalCollider.size = new Vector3(0.9f, 2.6f, 0.9f);
                    physicalCollider.center = new Vector3(0f, 1.3f, 0f);
                    physicalCollider.isTrigger = false;

                    if (navObstacle == null) navObstacle = GetComponent<NavMeshObstacle>() ?? gameObject.AddComponent<NavMeshObstacle>();
                    navObstacle.shape = NavMeshObstacleShape.Box;
                    navObstacle.size = new Vector3(0.9f, 2.6f, 0.9f);
                    navObstacle.center = new Vector3(0f, 1.3f, 0f);
                    navObstacle.carving = true;
                    break;

                case StructureType.RainCollector:
                    physicalCollider.size = new Vector3(1.1f, 1.8f, 1.1f);
                    physicalCollider.center = new Vector3(0f, 0.9f, 0f);
                    physicalCollider.isTrigger = false;

                    if (navObstacle == null) navObstacle = GetComponent<NavMeshObstacle>() ?? gameObject.AddComponent<NavMeshObstacle>();
                    navObstacle.shape = NavMeshObstacleShape.Box;
                    navObstacle.size = new Vector3(1.1f, 1.8f, 1.1f);
                    navObstacle.center = new Vector3(0f, 0.9f, 0f);
                    navObstacle.carving = true;
                    break;

                case StructureType.HerbPlanter:
                    physicalCollider.size = new Vector3(1.6f, 0.7f, 0.9f);
                    physicalCollider.center = new Vector3(0f, 0.35f, 0f);
                    physicalCollider.isTrigger = false;

                    if (navObstacle == null) navObstacle = GetComponent<NavMeshObstacle>() ?? gameObject.AddComponent<NavMeshObstacle>();
                    navObstacle.shape = NavMeshObstacleShape.Box;
                    navObstacle.size = new Vector3(1.6f, 0.7f, 0.9f);
                    navObstacle.center = new Vector3(0f, 0.35f, 0f);
                    navObstacle.carving = true;
                    break;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            switch (structureType)
            {
                case StructureType.Floodlight:
                    UpdateFloodlight();
                    break;

                case StructureType.RainCollector:
                    UpdateRainCollector();
                    break;

                case StructureType.HerbPlanter:
                    UpdateHerbPlanter();
                    break;
            }
        }

        private void UpdateFloodlight()
        {
            bool isNight = DayNightManager.Instance != null && DayNightManager.Instance.IsNight;
            if (spotLightComponent != null)
            {
                spotLightComponent.enabled = isNight;
            }

            if (!isNight) return;

            dazzleTimer += Time.deltaTime;
            if (dazzleTimer >= 0.5f)
            {
                dazzleTimer = 0f;
                DazzleZombiesInCone();
            }
        }

        private void DazzleZombiesInCone()
        {
            Vector3 origin = transform.position + Vector3.up * 2.4f;
            Vector3 forward = transform.forward;
            float maxDist = 26f;
            float halfAngle = 35f;

            Collider[] hits = Physics.OverlapSphere(origin, maxDist);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col == null) continue;

                var agent = col.GetComponent<NavMeshAgent>() ?? col.GetComponentInParent<NavMeshAgent>();
                var brain = col.GetComponent<AI.ZombieBrain>() ?? col.GetComponentInParent<AI.ZombieBrain>();
                if (agent != null && (brain != null || col.name.Contains("Zombie") || col.CompareTag("Enemy")))
                {
                    Vector3 dirToZombie = (col.transform.position - origin).normalized;
                    if (Vector3.Angle(forward, dirToZombie) <= halfAngle)
                    {
                        // Dazzled: reduce speed by 20% briefly
                        agent.speed = Mathf.Max(agent.speed * 0.8f, 0.8f);
                    }
                }
            }
        }

        private void UpdateRainCollector()
        {
            // Accumulates water: baseline 1 per 3 in-game hours
            // If raining, 2x faster (1 per 1.5 in-game hours)
            if (accumulatedResources >= 5) return;

            float speedMult = 1.0f;
            if (DynamicWeatherManager.Instance != null)
            {
                var weather = DynamicWeatherManager.Instance.CurrentWeather;
                if (weather == WeatherState.LightDrizzle || weather == WeatherState.HeavyStorm)
                {
                    speedMult = 2.0f;
                }
            }

            // In our DayNight cycle, 1 game hour ~ 60 real seconds by default
            float secondsPerHour = 60f;
            float targetInterval = (3.0f * secondsPerHour) / speedMult;

            resourceTimer += Time.deltaTime;
            if (resourceTimer >= targetInterval)
            {
                resourceTimer = 0f;
                accumulatedResources = Mathf.Clamp(accumulatedResources + 1, 0, 5);
            }
        }

        private void UpdateHerbPlanter()
        {
            // Accumulates medicinal cloth/herbs: 1 per 4 in-game hours
            if (accumulatedResources >= 4) return;

            float secondsPerHour = 60f;
            float targetInterval = 4.0f * secondsPerHour;

            resourceTimer += Time.deltaTime;
            if (resourceTimer >= targetInterval)
            {
                resourceTimer = 0f;
                accumulatedResources = Mathf.Clamp(accumulatedResources + 1, 0, 4);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (structureType != StructureType.RazorWire || other == null) return;

            // Razor wire slows down and damages zombies passing through
            var brain = other.GetComponent<AI.ZombieBrain>() ?? other.GetComponentInParent<AI.ZombieBrain>();
            if (brain != null || other.name.Contains("Zombie") || other.CompareTag("Enemy"))
            {
                var agent = other.GetComponent<NavMeshAgent>() ?? other.GetComponentInParent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.velocity *= 0.3f; // 70% slow down
                }

                if (brain != null)
                {
                    brain.TakeDamage(18f * Time.deltaTime, transform.position, Vector3.up);
                }

                // Barbed wire takes wear damage
                TakeStructuralDamage(2.5f * Time.deltaTime);
            }
        }

        public void TakeStructuralDamage(float damage)
        {
            if (currentHP <= 0f) return;

            currentHP = Mathf.Max(0f, currentHP - damage);
            TriggerDamageFlash();

            if (currentHP <= 0f)
            {
                Collapse();
            }
        }

        private void TriggerDamageFlash()
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(DamageFlashRoutine());
        }

        private IEnumerator DamageFlashRoutine()
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] != null && rends[i].material != null)
                {
                    rends[i].material.color = Color.Lerp(rends[i].material.color, Color.red, 0.4f);
                }
            }

            yield return new WaitForSeconds(0.12f);

            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] != null && rends[i].material != null)
                {
                    rends[i].material.color = Color.white;
                }
            }
        }

        public bool TryRepair()
        {
            if (!IsDamaged)
            {
                GameEvents.TriggerObjectiveToastRequested($"[{GetStructureDisplayName(structureType)}] Zaten tam dayanıklılıkta!", 2.5f);
                return false;
            }

            if (InventoryManager.Instance == null || !InventoryManager.Instance.HasItem("item_scrap", 1))
            {
                GameEvents.TriggerObjectiveToastRequested("[ONARIM BAŞARISIZ] Yetersiz Hurda! (Gereken: 1 Hurda)", 3.0f);
                return false;
            }

            InventoryManager.RemoveItem("item_scrap", 1);
            float restored = Mathf.Min(120f, maxHP - currentHP);
            currentHP = Mathf.Min(maxHP, currentHP + 120f);

            ProceduralAudioManager.Instance?.PlayWrenchRepair();
            GameEvents.TriggerObjectiveToastRequested($"[YAPI ONARILDI] +{restored:F0} Can! ({currentHP:F0}/{maxHP:F0})", 3.0f);
            return true;
        }

        public void Dismantle()
        {
            // 50% material refund
            ResourceCost[] costs = GetCosts(structureType);
            for (int i = 0; i < costs.Length; i++)
            {
                int refund = Mathf.Max(1, costs[i].amount / 2);
                InventoryManager.AddItem(costs[i].itemId, refund);
            }

            ProceduralAudioManager.Instance?.PlayItemPickup();
            GameEvents.TriggerObjectiveToastRequested($"[SÖKÜLDÜ] {GetStructureDisplayName(structureType)} söküldü, malzemelerin %50'si geri alındı.", 3.0f);

            ActiveStructures.Remove(this);
            Destroy(gameObject);
        }

        public bool TryHarvest()
        {
            if (accumulatedResources <= 0)
            {
                GameEvents.TriggerObjectiveToastRequested("[TOPLAMA] Henüz toplanabilir kaynak birikmedi.", 2.5f);
                return false;
            }

            if (structureType == StructureType.RainCollector)
            {
                InventoryManager.AddItem("item_purified_water", accumulatedResources);
                GameEvents.TriggerObjectiveToastRequested($"[HASAT] +{accumulatedResources} Arıtılmış Su toplandı!", 3.0f);
                accumulatedResources = 0;
                ProceduralAudioManager.Instance?.PlayItemPickup();
                return true;
            }
            else if (structureType == StructureType.HerbPlanter)
            {
                InventoryManager.AddItem("item_cloth", accumulatedResources);
                GameEvents.TriggerObjectiveToastRequested($"[HASAT] +{accumulatedResources} Şifalı Bitki / Kumaş toplandı!", 3.0f);
                accumulatedResources = 0;
                ProceduralAudioManager.Instance?.PlayItemPickup();
                return true;
            }

            return false;
        }

        private void Collapse()
        {
            GameEvents.TriggerObjectiveToastRequested($"[YIKILDI] {GetStructureDisplayName(structureType)} parçalandı!", 3.5f);
            ProceduralAudioManager.Instance?.PlayExplosion();
            GameEvents.TriggerCameraShakeRequested(0.35f, 0.25f);

            ActiveStructures.Remove(this);
            Destroy(gameObject);
        }

        public SavedBaseStructure ToSaveRecord()
        {
            Vector3 pos = transform.position;
            return new SavedBaseStructure
            {
                structureId = structureId,
                structureType = (int)structureType,
                posX = pos.x,
                posY = pos.y,
                posZ = pos.z,
                rotY = transform.eulerAngles.y,
                currentHP = currentHP,
                accumulatedResources = accumulatedResources,
                resourceTimer = resourceTimer
            };
        }

        // ==========================================
        // PROCEDURAL AAA 3D MESH BUILDER
        // ==========================================
        public static GameObject CreateStructureGameObject(StructureType type, Vector3 position, Quaternion rotation, bool isHologram = false)
        {
            GameObject root = new GameObject(isHologram ? $"Ghost_{type}" : $"Structure_{type}");
            root.transform.position = position;
            root.transform.rotation = rotation;

            Material sharedLitMat = GetStructureMaterial(type, isHologram);

            switch (type)
            {
                case StructureType.DefenseWall:
                    BuildDefenseWallMesh(root.transform, sharedLitMat, isHologram);
                    break;
                case StructureType.RazorWire:
                    BuildRazorWireMesh(root.transform, sharedLitMat, isHologram);
                    break;
                case StructureType.Floodlight:
                    BuildFloodlightMesh(root.transform, sharedLitMat, isHologram);
                    break;
                case StructureType.RainCollector:
                    BuildRainCollectorMesh(root.transform, sharedLitMat, isHologram);
                    break;
                case StructureType.HerbPlanter:
                    BuildHerbPlanterMesh(root.transform, sharedLitMat, isHologram);
                    break;
            }

            if (!isHologram)
            {
                BuildableStructure bs = root.AddComponent<BuildableStructure>();
                bs.Initialize(type);
            }

            return root;
        }

        private static Material GetStructureMaterial(StructureType type, bool isHologram)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (isHologram)
            {
                Material holoMat = new Material(litShader);
                holoMat.name = "Mat_Building_Hologram_Instance";
                holoMat.SetFloat("_Surface", 1); // Transparent
                holoMat.SetFloat("_Blend", 0);   // Alpha
                holoMat.renderQueue = 3000;
                holoMat.color = new Color(0.02f, 0.84f, 0.63f, 0.55f); // Default Emerald Green
                holoMat.enableInstancing = true;
                return holoMat;
            }

            Material mat = new Material(litShader);
            mat.name = $"Mat_Structure_{type}_Instance";
            mat.enableInstancing = true;

            switch (type)
            {
                case StructureType.DefenseWall:
                    mat.color = new Color(0.42f, 0.38f, 0.34f, 1f); // Weathered sandbag & timber
                    mat.SetFloat("_Metallic", 0.15f);
                    mat.SetFloat("_Smoothness", 0.25f);
                    break;
                case StructureType.RazorWire:
                    mat.color = new Color(0.35f, 0.36f, 0.38f, 1f); // Galvanized rebar & sharp steel
                    mat.SetFloat("_Metallic", 0.85f);
                    mat.SetFloat("_Smoothness", 0.65f);
                    break;
                case StructureType.Floodlight:
                    mat.color = new Color(0.22f, 0.24f, 0.25f, 1f); // Industrial dark matte frame
                    mat.SetFloat("_Metallic", 0.70f);
                    mat.SetFloat("_Smoothness", 0.45f);
                    break;
                case StructureType.RainCollector:
                    mat.color = new Color(0.18f, 0.32f, 0.48f, 1f); // High-density polyethylene blue
                    mat.SetFloat("_Metallic", 0.05f);
                    mat.SetFloat("_Smoothness", 0.55f);
                    break;
                case StructureType.HerbPlanter:
                    mat.color = new Color(0.36f, 0.26f, 0.18f, 1f); // Planter redwood timber & rich loam
                    mat.SetFloat("_Metallic", 0.05f);
                    mat.SetFloat("_Smoothness", 0.20f);
                    break;
            }

            return mat;
        }

        private static void BuildDefenseWallMesh(Transform root, Material mat, bool isHologram)
        {
            // Solid base / sandbag breastwork
            GameObject baseWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseWall.name = "Sandbag_Base";
            baseWall.transform.SetParent(root, false);
            baseWall.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            baseWall.transform.localScale = new Vector3(2.4f, 1.2f, 0.48f);
            ApplyMeshSettings(baseWall, mat, isHologram);

            // Upper corrugated iron & wooden barricade
            GameObject upperWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upperWall.name = "Corrugated_Sheeting";
            upperWall.transform.SetParent(root, false);
            upperWall.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            upperWall.transform.localScale = new Vector3(2.35f, 0.95f, 0.22f);
            ApplyMeshSettings(upperWall, mat, isHologram);

            // Left & Right timber posts
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"Timber_Post_{i}";
                post.transform.SetParent(root, false);
                post.transform.localPosition = new Vector3(i * 1.15f, 1.1f, 0f);
                post.transform.localScale = new Vector3(0.18f, 2.2f, 0.24f);
                ApplyMeshSettings(post, mat, isHologram);
            }
        }

        private static void BuildRazorWireMesh(Transform root, Material mat, bool isHologram)
        {
            // Two X-cross rebar supports
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject cross1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cross1.name = $"Rebar_CrossA_{i}";
                cross1.transform.SetParent(root, false);
                cross1.transform.localPosition = new Vector3(i * 0.85f, 0.42f, 0f);
                cross1.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
                cross1.transform.localScale = new Vector3(0.06f, 0.55f, 0.06f);
                ApplyMeshSettings(cross1, mat, isHologram);

                GameObject cross2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cross2.name = $"Rebar_CrossB_{i}";
                cross2.transform.SetParent(root, false);
                cross2.transform.localPosition = new Vector3(i * 0.85f, 0.42f, 0f);
                cross2.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
                cross2.transform.localScale = new Vector3(0.06f, 0.55f, 0.06f);
                ApplyMeshSettings(cross2, mat, isHologram);
            }

            // Concertina wire spiral cylinder representations
            for (int r = 0; r < 2; r++)
            {
                GameObject wireCoil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wireCoil.name = $"Concertina_Coil_{r}";
                wireCoil.transform.SetParent(root, false);
                wireCoil.transform.localPosition = new Vector3(0f, 0.35f + (r * 0.35f), 0f);
                wireCoil.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wireCoil.transform.localScale = new Vector3(0.55f, 1.0f, 0.55f);
                ApplyMeshSettings(wireCoil, mat, isHologram);
            }
        }

        private static void BuildFloodlightMesh(Transform root, Material mat, bool isHologram)
        {
            // Vertical telescopic steel mast
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Mast_Pole";
            mast.transform.SetParent(root, false);
            mast.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            mast.transform.localScale = new Vector3(0.08f, 1.25f, 0.08f);
            ApplyMeshSettings(mast, mat, isHologram);

            // Tripod stabilizing base
            for (int a = 0; a < 3; a++)
            {
                float rad = a * 120f * Mathf.Deg2Rad;
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"Tripod_Leg_{a}";
                leg.transform.SetParent(root, false);
                leg.transform.localPosition = new Vector3(Mathf.Sin(rad) * 0.35f, 0.35f, Mathf.Cos(rad) * 0.35f);
                leg.transform.localRotation = Quaternion.Euler(Mathf.Cos(rad) * 32f, 0f, -Mathf.Sin(rad) * 32f);
                leg.transform.localScale = new Vector3(0.05f, 0.42f, 0.05f);
                ApplyMeshSettings(leg, mat, isHologram);
            }

            // Halogen lamp housing head
            GameObject lampBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lampBox.name = "Lamp_Housing";
            lampBox.transform.SetParent(root, false);
            lampBox.transform.localPosition = new Vector3(0f, 2.5f, 0.12f);
            lampBox.transform.localRotation = Quaternion.Euler(22f, 0f, 0f); // Tilted downward
            lampBox.transform.localScale = new Vector3(0.52f, 0.38f, 0.28f);
            ApplyMeshSettings(lampBox, mat, isHologram);

            if (!isHologram)
            {
                // SpotLight with Shadows.None for mobile 60 FPS compliance
                GameObject lightGo = new GameObject("SpotLight_Source");
                lightGo.transform.SetParent(lampBox.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 0f, 0.15f);
                lightGo.transform.localRotation = Quaternion.identity;

                Light spot = lightGo.AddComponent<Light>();
                spot.type = LightType.Spot;
                spot.range = 26f;
                spot.spotAngle = 60f;
                spot.innerSpotAngle = 42f;
                spot.color = new Color(1.0f, 0.94f, 0.77f, 1f); // Warm halogen glow
                spot.intensity = 3.5f;
                spot.shadows = LightShadows.None; // Mandatory mobile budget: zero real-time shadow cost

                BuildableStructure bs = root.GetComponent<BuildableStructure>();
                if (bs != null) bs.spotLightComponent = spot;
            }
        }

        private static void BuildRainCollectorMesh(Transform root, Material mat, bool isHologram)
        {
            // Barrel drum
            GameObject drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = "Water_Drum";
            drum.transform.SetParent(root, false);
            drum.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            drum.transform.localScale = new Vector3(0.95f, 0.6f, 0.95f);
            ApplyMeshSettings(drum, mat, isHologram);

            // Funnel basin on top
            GameObject funnel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            funnel.name = "Catchment_Funnel";
            funnel.transform.SetParent(root, false);
            funnel.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            funnel.transform.localScale = new Vector3(1.25f, 0.18f, 1.25f);
            ApplyMeshSettings(funnel, mat, isHologram);

            // Filter cloth mesh / tap
            GameObject tap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tap.name = "Dispense_Spigot";
            tap.transform.SetParent(root, false);
            tap.transform.localPosition = new Vector3(0f, 0.25f, 0.52f);
            tap.transform.localScale = new Vector3(0.08f, 0.08f, 0.18f);
            ApplyMeshSettings(tap, mat, isHologram);
        }

        private static void BuildHerbPlanterMesh(Transform root, Material mat, bool isHologram)
        {
            // Wooden planter box trough
            GameObject planter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            planter.name = "Planter_Trough";
            planter.transform.SetParent(root, false);
            planter.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            planter.transform.localScale = new Vector3(1.6f, 0.55f, 0.88f);
            ApplyMeshSettings(planter, mat, isHologram);

            // Soil layer
            GameObject soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soil.name = "Loam_Soil";
            soil.transform.SetParent(root, false);
            soil.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            soil.transform.localScale = new Vector3(1.48f, 0.08f, 0.76f);
            ApplyMeshSettings(soil, mat, isHologram);

            // Little sprout stems
            for (int s = -2; s <= 2; s++)
            {
                GameObject sprout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                sprout.name = $"Herb_Sprout_{s}";
                sprout.transform.SetParent(root, false);
                sprout.transform.localPosition = new Vector3(s * 0.3f, 0.65f, UnityEngine.Random.Range(-0.2f, 0.2f));
                sprout.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
                ApplyMeshSettings(sprout, mat, isHologram);
            }
        }

        private static void ApplyMeshSettings(GameObject go, Material mat, bool isHologram)
        {
            // Remove default primitive collider so structure is governed exclusively by root BoxCollider
            Collider col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = mat;
                rend.shadowCastingMode = isHologram ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                rend.receiveShadows = !isHologram;
            }
        }
    }
}
