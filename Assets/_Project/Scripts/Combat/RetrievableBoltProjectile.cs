using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Combat
{
    /// <summary>
    /// Phase 3 (Step 3A): Physical Retrievable Crossbow Bolt Projectile.
    /// Manages sub-stepped kinematic trajectory (68 m/s with realistic gravity arc),
    /// non-allocating physics raycasting (0 GC spikes), physical target embedding,
    /// and 120s retrievable WorldInteractableObject pickup with 85% recovery integrity.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RetrievableBoltProjectile : WorldInteractableObject
    {
        [Header("Ballistics & Flight")]
        [SerializeField] private float launchSpeed = 68.0f;
        [SerializeField] private float gravityScale = 9.81f;
        [SerializeField] private float baseDamage = 95.0f;
        [SerializeField] private float headshotMultiplier = 2.75f;
        [SerializeField] private float stealthBonusMultiplier = 1.35f;
        [SerializeField] private LayerMask collisionLayers = ~0;

        [Header("State")]
        [SerializeField] private bool isFlying = false;
        [SerializeField] private bool isEmbedded = false;
        [SerializeField] private bool isIntact = true;
        [SerializeField] private float lifespanSeconds = 120.0f;

        private Vector3 currentVelocity;
        private Vector3 flightDirection;
        private float despawnTimer = 120.0f;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[8];
        private GameObject glintVfxObj;

        public bool IsFlying => isFlying;
        public bool IsEmbedded => isEmbedded;
        public bool IsIntact => isIntact;
        public float BaseDamage => baseDamage;
        public float HeadshotMultiplier => headshotMultiplier;

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Tatar Yayı Oku";
            objectSubtitle = "Fırlatılmış Mühimmat";
        }

        public void InitializeFlight(Vector3 direction, float speed, float damage, float hsMult, LayerMask mask)
        {
            launchSpeed = speed;
            flightDirection = direction.normalized;
            currentVelocity = flightDirection * launchSpeed;
            baseDamage = damage;
            headshotMultiplier = hsMult;
            collisionLayers = mask;
            isFlying = true;
            isEmbedded = false;
            despawnTimer = lifespanSeconds;

            // Roll 85% recovery integrity (15% snaps on impact)
            isIntact = UnityEngine.Random.value <= 0.85f;

            // Trigger initial whoosh audio
            ProceduralAudioManager.Instance?.PlayBoltWhoosh(transform.position);
        }

        private void FixedUpdate()
        {
            if (!isFlying) return;

            float dt = Time.fixedDeltaTime;
            Vector3 stepVector = currentVelocity * dt;
            float stepDistance = stepVector.magnitude;
            Vector3 stepDir = stepVector.normalized;

            // Sub-stepped non-allocating raycast check
            int hitCount = Physics.RaycastNonAlloc(transform.position, stepDir, hitBuffer, stepDistance, collisionLayers, QueryTriggerInteraction.Ignore);

            if (hitCount > 0)
            {
                // Find closest valid hit (ignoring player and triggers)
                RaycastHit closestHit = default;
                float closestDist = float.MaxValue;
                bool foundValid = false;

                for (int i = 0; i < hitCount; i++)
                {
                    var h = hitBuffer[i];
                    if (h.collider == null || h.collider.isTrigger) continue;
                    if (h.collider.CompareTag("Player") || h.collider.name.Contains("Player")) continue;

                    if (h.distance < closestDist)
                    {
                        closestDist = h.distance;
                        closestHit = h;
                        foundValid = true;
                    }
                }

                if (foundValid)
                {
                    ProcessImpact(closestHit);
                    return;
                }
            }

            // Advance position and apply gravity arc
            transform.position += stepVector;
            currentVelocity += Vector3.down * gravityScale * dt;
            if (currentVelocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(currentVelocity.normalized);
            }
        }

        private void Update()
        {
            if (isFlying) return;

            // Despawn timer countdown for embedded/spent bolts
            despawnTimer -= Time.deltaTime;
            if (despawnTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void ProcessImpact(RaycastHit hit)
        {
            isFlying = false;
            isEmbedded = true;
            flightDirection = currentVelocity.normalized;

            bool isEnemy = false;
            bool isHeadshot = false;

            // 1. Check Zombie Hit
            ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
            if (zombie != null && !zombie.IsDead)
            {
                isEnemy = true;
                float distFromHead = Vector3.Distance(hit.point, zombie.transform.position + Vector3.up * 1.55f);
                isHeadshot = distFromHead <= 0.35f || hit.collider.name.ToLower().Contains("head");

                float damage = baseDamage;
                if (isHeadshot)
                {
                    damage *= headshotMultiplier;
                }

                // Check undetected stealth bonus (+35%)
                if (StealthVisibilityController.Instance != null && StealthVisibilityController.Instance.CurrentVisibilityScore < 0.40f)
                {
                    damage *= stealthBonusMultiplier;
                }

                var res = zombie.ProcessHitZoneDamage(damage, hit.point, flightDirection);
                zombie.SetLastHitDetails(flightDirection, hit.point, isHeadshot ? "Crossbow_Headshot" : "Crossbow_Bolt", isHeadshot);

                // Blood impact decal & combat juice
                ImpactDecalPoolManager.Instance?.SpawnImpact(hit.point, hit.normal, true);
                if (isHeadshot && CombatJuiceController.Instance != null)
                {
                    CombatJuiceController.Instance.TriggerCriticalHeadshotJuice(hit.point, hit.normal, zombie.IsDead);
                }
                else if (CombatJuiceController.Instance != null)
                {
                    CombatJuiceController.Instance.TriggerHitmarker(isHeadshot, zombie.IsDead, hit.point);
                }
                else
                {
                    GameEvents.TriggerHitConfirmed(isHeadshot, hit.point);
                }
            }

            // 2. Check Human Raider Hit
            HumanRaiderAI raider = hit.collider.GetComponentInParent<HumanRaiderAI>();
            if (raider != null && !raider.IsDead)
            {
                isEnemy = true;
                float distFromHead = Vector3.Distance(hit.point, raider.transform.position + Vector3.up * 1.6f);
                isHeadshot = distFromHead <= 0.35f || hit.collider.name.ToLower().Contains("head");

                float damage = baseDamage;
                if (isHeadshot)
                {
                    damage *= headshotMultiplier;
                }

                if (StealthVisibilityController.Instance != null && StealthVisibilityController.Instance.CurrentVisibilityScore < 0.40f)
                {
                    damage *= stealthBonusMultiplier;
                }

                raider.TakeDamage(damage, hit.point, flightDirection);
                ImpactDecalPoolManager.Instance?.SpawnImpact(hit.point, hit.normal, true);

                if (CombatJuiceController.Instance != null)
                {
                    CombatJuiceController.Instance.TriggerHitmarker(isHeadshot, raider.IsDead, hit.point);
                }
            }

            // 3. Check Wildlife Animal Hit
            WildlifeAnimalAI animal = hit.collider.GetComponentInParent<WildlifeAnimalAI>();
            if (animal != null && !animal.IsDead)
            {
                isEnemy = true;
                float distFromHead = Vector3.Distance(hit.point, animal.transform.position + Vector3.up * 1.1f);
                isHeadshot = distFromHead <= 0.45f || hit.collider.name.ToLower().Contains("head");

                float damage = baseDamage;
                if (isHeadshot)
                {
                    damage *= headshotMultiplier;
                }
                if (StealthVisibilityController.Instance != null && StealthVisibilityController.Instance.CurrentVisibilityScore < 0.40f)
                {
                    damage *= stealthBonusMultiplier;
                }

                animal.TakeDamage(damage, hit.point, flightDirection, isHeadshot);
                ImpactDecalPoolManager.Instance?.SpawnImpact(hit.point, hit.normal, true);
                if (CombatJuiceController.Instance != null)
                {
                    CombatJuiceController.Instance.TriggerHitmarker(isHeadshot, animal.IsDead, hit.point);
                }
            }

            // 4. Audio Thud (3D Positional)
            ProceduralAudioManager.Instance?.PlayBoltImpactThud(hit.point, isEnemy);

            // 4. Physical Embedding
            transform.position = hit.point + flightDirection * 0.12f;
            transform.rotation = Quaternion.LookRotation(flightDirection);

            if (hit.transform != null)
            {
                transform.SetParent(hit.transform, true);
            }

            // 5. Environmental Surface Hit Effects (Bullet hole / spark & glint)
            if (!isEnemy)
            {
                ImpactDecalPoolManager.Instance?.SpawnImpact(hit.point, hit.normal, false);
                AttachGlintHighlight();
            }

            // Configure interactable retrieval details
            objectTitle = isIntact ? "Tatar Yayı Oku" : "Kırık Ok Parçası";
            objectSubtitle = isIntact ? "Geri Alınabilir Mühimmat" : "Çarpma Etkisiyle Kırılmış";
        }

        private void AttachGlintHighlight()
        {
            if (glintVfxObj != null) return;

            glintVfxObj = new GameObject("Bolt_Glint_Highlight");
            glintVfxObj.transform.SetParent(transform, false);
            glintVfxObj.transform.localPosition = new Vector3(0f, 0f, -0.28f);

            Light glintLight = glintVfxObj.AddComponent<Light>();
            glintLight.type = LightType.Point;
            glintLight.range = 1.8f;
            glintLight.intensity = 0.85f;
            glintLight.color = new Color(0.25f, 0.90f, 1.0f); // Subtle cyan glint
            glintLight.shadows = LightShadows.None; // Zero mobile shadow budget cost
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (isEmbedded && !hasBeenSearched)
            {
                if (isIntact)
                {
                    verbs.Add(new InteractionVerb(
                        "retrieve",
                        "OKU GERİ AL",
                        0.0f, // Instant pickup
                        null,
                        null,
                        15.0f,
                        true
                    ));
                }
                else
                {
                    verbs.Add(new InteractionVerb(
                        "broken",
                        "KIRILMIŞ OKU KONTROL ET",
                        0.0f,
                        null,
                        null,
                        0.0f,
                        true
                    ));
                }
            }

            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (hasBeenSearched || !isEmbedded) return;
            hasBeenSearched = true;

            SetFocused(false);

            if (isIntact && verbId == "retrieve")
            {
                // Return +1 bolt to InventoryManager
                if (InventorySystem.Instance != null)
                {
                    InventorySystem.Instance.AddItemById("ammo_crossbow_bolt", 1);
                }

                // Feedback
                ProceduralAudioManager.Instance?.PlayQuiverClick(transform.position);
                MobileHapticManager.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested("[ALINDI] 1x Tatar Yayı Oku", 2.0f);

                Destroy(gameObject);
            }
            else
            {
                // Broken bolt feedback
                ProceduralAudioManager.Instance?.PlayPush();
                GameEvents.TriggerObjectiveToastRequested("Ok Kırıldı (Kullanılamaz)", 2.0f);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Instantiates a fully modeled procedural retrievable bolt projectile in the world.
        /// </summary>
        public static RetrievableBoltProjectile SpawnBolt(Vector3 origin, Vector3 direction, float speed, float damage, float hsMult, LayerMask mask)
        {
            GameObject boltObj = new GameObject("RetrievableBolt_Projectile");
            boltObj.transform.position = origin;
            boltObj.transform.rotation = Quaternion.LookRotation(direction);

            // Interaction SphereCollider
            SphereCollider col = boltObj.AddComponent<SphereCollider>();
            col.radius = 0.5f;
            col.isTrigger = true;

            // Materials
            Material shaftMat = GetOrCreateMaterial("Mat_Bolt_Shaft", new Color(0.18f, 0.16f, 0.15f));
            Material steelMat = GetOrCreateMaterial("Mat_Weapon_Steel", new Color(0.12f, 0.14f, 0.15f));
            Material fletchMat = GetOrCreateMaterial("Mat_Bolt_Fletching", new Color(0.20f, 0.85f, 0.95f));

            // Shaft
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Bolt_Shaft";
            shaft.transform.SetParent(boltObj.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0f, 0f);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.022f, 0.32f, 0.022f);
            DestroyImmediate(shaft.GetComponent<Collider>());
            shaft.GetComponent<Renderer>().sharedMaterial = shaftMat;

            // Broadhead Steel Tip
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tip.name = "Bolt_Broadhead";
            tip.transform.SetParent(boltObj.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0f, 0.33f);
            tip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tip.transform.localScale = new Vector3(0.035f, 0.06f, 0.035f);
            DestroyImmediate(tip.GetComponent<Collider>());
            tip.GetComponent<Renderer>().sharedMaterial = steelMat;

            // Fletching Vane L
            GameObject fletchL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fletchL.name = "Fletch_L";
            fletchL.transform.SetParent(boltObj.transform, false);
            fletchL.transform.localPosition = new Vector3(-0.03f, 0f, -0.28f);
            fletchL.transform.localScale = new Vector3(0.005f, 0.04f, 0.10f);
            DestroyImmediate(fletchL.GetComponent<Collider>());
            fletchL.GetComponent<Renderer>().sharedMaterial = fletchMat;

            // Fletching Vane R
            GameObject fletchR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fletchR.name = "Fletch_R";
            fletchR.transform.SetParent(boltObj.transform, false);
            fletchR.transform.localPosition = new Vector3(0.03f, 0f, -0.28f);
            fletchR.transform.localScale = new Vector3(0.005f, 0.04f, 0.10f);
            DestroyImmediate(fletchR.GetComponent<Collider>());
            fletchR.GetComponent<Renderer>().sharedMaterial = fletchMat;

            var proj = boltObj.AddComponent<RetrievableBoltProjectile>();
            proj.InitializeFlight(direction, speed, damage, hsMult, mask);

            return proj;
        }

        private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        public static Material GetOrCreateMaterialRuntime(string name, Color color)
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

        private static Material GetOrCreateMaterial(string name, Color color) => GetOrCreateMaterialRuntime(name, color);
    }
}
