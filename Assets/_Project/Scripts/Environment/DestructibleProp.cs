using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.AI;
using ProjectSurvival.Vehicles;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Environment
{
    public enum DestructibleArchetype
    {
        ShatterGlass,
        BreakableCrate,
        WeakTimberFence
    }

    /// <summary>
    /// Active Phase 6: Reactive Destructible Environmental Props.
    /// Manages ShatterGlass (20 HP, 8-12 glass shards, 24m acoustic alert),
    /// BreakableCrate (35 HP, 8 plank fragments, 1-2 physical loot pickups),
    /// and WeakTimberFence (50 HP, vehicular ramming shortcut carving, NavMeshObstacle bypass).
    /// All spawned debris disable collisions after 1.2s and despawn after 3.5s for zero-leak mobile 60 FPS.
    /// </summary>
    public class DestructibleProp : MonoBehaviour
    {
        [Header("Archetype Configuration")]
        [SerializeField] private DestructibleArchetype archetype = DestructibleArchetype.BreakableCrate;
        [SerializeField] private float maxHealth = 35f;
        [SerializeField] private float currentHealth = 35f;
        [SerializeField] private bool isDestroyed = false;

        [Header("Components")]
        [SerializeField] private Collider propCollider;
        [SerializeField] private NavMeshObstacle navObstacle;
        [SerializeField] private Renderer[] propRenderers;

        [Header("Loot Drops (BreakableCrate)")]
        [SerializeField] private List<string> possibleLootItemIds = new List<string> { "item_scrap", "item_cloth", "item_canned_food", "item_brick" };

        public DestructibleArchetype Archetype => archetype;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDestroyed => isDestroyed;

        private void Awake()
        {
            if (propCollider == null) propCollider = GetComponent<Collider>();
            if (navObstacle == null) navObstacle = GetComponent<NavMeshObstacle>();
            if (propRenderers == null || propRenderers.Length == 0) propRenderers = GetComponentsInChildren<Renderer>(true);

            ApplyArchetypeDefaults();
        }

        public void ConfigureArchetype(DestructibleArchetype arch, float customHp = 0f)
        {
            archetype = arch;
            ApplyArchetypeDefaults();
            if (customHp > 0f)
            {
                maxHealth = customHp;
                currentHealth = customHp;
            }
        }

        private void ApplyArchetypeDefaults()
        {
            switch (archetype)
            {
                case DestructibleArchetype.ShatterGlass:
                    maxHealth = 20f;
                    break;
                case DestructibleArchetype.BreakableCrate:
                    maxHealth = 35f;
                    break;
                case DestructibleArchetype.WeakTimberFence:
                    maxHealth = 50f;
                    break;
            }
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage, Vector3 hitPoint = default, Vector3 hitDirection = default)
        {
            if (isDestroyed) return;

            currentHealth -= damage;
            if (currentHealth <= 0f)
            {
                DestroyProp(hitDirection, hitPoint);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isDestroyed) return;

            // Check high-speed vehicle impact (>= 20 km/h)
            DrivableVehicleController vehicle = collision.gameObject.GetComponentInParent<DrivableVehicleController>();
            if (vehicle != null && vehicle.CurrentSpeedKmh >= 20f)
            {
                Vector3 impactDir = (transform.position - vehicle.transform.position).normalized;
                DestroyProp(impactDir, collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
            }
        }

        public void DestroyProp(Vector3 impulseDir, Vector3 impactPoint)
        {
            if (isDestroyed) return;
            isDestroyed = true;

            if (impulseDir == Vector3.zero) impulseDir = transform.forward;
            if (impactPoint == Vector3.zero) impactPoint = transform.position;

            if (propCollider == null) propCollider = GetComponent<Collider>();
            if (navObstacle == null) navObstacle = GetComponent<NavMeshObstacle>();
            if (propRenderers == null || propRenderers.Length == 0) propRenderers = GetComponentsInChildren<Renderer>(true);

            // 1. Disable main collider & carve-free NavMeshObstacle
            if (propCollider != null) propCollider.enabled = false;
            if (navObstacle != null)
            {
                navObstacle.carving = false;
                navObstacle.enabled = false;
            }

            // 2. Hide visual renderers
            if (propRenderers != null)
            {
                for (int i = 0; i < propRenderers.Length; i++)
                {
                    if (propRenderers[i] != null) propRenderers[i].enabled = false;
                }
            }

            // 3. Execute Archetype Specific Behavior
            switch (archetype)
            {
                case DestructibleArchetype.ShatterGlass:
                    ExecuteGlassShatter(impulseDir, impactPoint);
                    break;

                case DestructibleArchetype.BreakableCrate:
                    ExecuteCrateBreak(impulseDir, impactPoint);
                    break;

                case DestructibleArchetype.WeakTimberFence:
                    ExecuteFenceSmash(impulseDir, impactPoint);
                    break;
            }

            MobileHapticManager.TriggerMedium();
            Debug.Log($"[{gameObject.name}] Destructible prop destroyed ({archetype}). NavMesh and pathways cleared.");
        }

        private void ExecuteGlassShatter(Vector3 impulseDir, Vector3 impactPoint)
        {
            ProceduralAudioManager.Instance?.PlayGlassShatter();
            HordeAlertSystem.Instance?.BroadcastAcousticLure(transform.position, 24.0f);
            GameEvents.TriggerNoiseEmitted(transform.position, 24.0f);

            // Spawn 8-12 translucent glass shards
            int shardCount = UnityEngine.Random.Range(8, 13);
            Material glassMat = GetOrCreateGlassMaterial();

            for (int i = 0; i < shardCount; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Quad);
                shard.name = $"Debris_Glass_{i}";
                shard.transform.position = impactPoint + UnityEngine.Random.insideUnitSphere * 0.45f;
                shard.transform.rotation = UnityEngine.Random.rotation;
                shard.transform.localScale = new Vector3(0.18f, 0.24f, 0.05f) * UnityEngine.Random.Range(0.7f, 1.3f);

                var r = shard.GetComponent<Renderer>();
                if (r != null && glassMat != null) r.sharedMaterial = glassMat;

                Rigidbody rb = shard.AddComponent<Rigidbody>();
                rb.mass = 0.4f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                Vector3 shardDir = (impulseDir + UnityEngine.Random.insideUnitSphere * 0.6f).normalized;
                rb.AddForce(shardDir * UnityEngine.Random.Range(60f, 130f), ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 40f, ForceMode.Impulse);

                StartCoroutine(CleanDebrisPiece(shard, 1.2f, 3.5f));
            }
        }

        private void ExecuteCrateBreak(Vector3 impulseDir, Vector3 impactPoint)
        {
            ProceduralAudioManager.Instance?.PlayDoorPoundImpact(transform.position, false);

            // Spawn 8 wooden plank fragments
            Material woodMat = GetOrCreateWoodMaterial();
            for (int i = 0; i < 8; i++)
            {
                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"Debris_Wood_{i}";
                plank.transform.position = transform.position + UnityEngine.Random.insideUnitSphere * 0.35f;
                plank.transform.rotation = UnityEngine.Random.rotation;
                plank.transform.localScale = new Vector3(0.12f, 0.08f, 0.65f) * UnityEngine.Random.Range(0.8f, 1.2f);

                var r = plank.GetComponent<Renderer>();
                if (r != null && woodMat != null) r.sharedMaterial = woodMat;

                Rigidbody rb = plank.AddComponent<Rigidbody>();
                rb.mass = 1.2f;

                Vector3 plankDir = (impulseDir + UnityEngine.Random.insideUnitSphere * 0.5f).normalized;
                rb.AddForce(plankDir * UnityEngine.Random.Range(45f, 100f), ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 30f, ForceMode.Impulse);

                StartCoroutine(CleanDebrisPiece(plank, 1.2f, 3.5f));
            }

            // Spawn 1-2 physical loot pickups
            int lootCount = UnityEngine.Random.Range(1, 3);
            for (int l = 0; l < lootCount; l++)
            {
                SpawnLootPickup(transform.position + Vector3.up * 0.25f + UnityEngine.Random.insideUnitSphere * 0.3f);
            }
        }

        private void ExecuteFenceSmash(Vector3 impulseDir, Vector3 impactPoint)
        {
            ProceduralAudioManager.Instance?.PlayDoorPoundImpact(transform.position, false);
            HordeAlertSystem.Instance?.BroadcastAcousticLure(transform.position, 18.0f);

            // Spawn 10 fence splinter fragments
            Material woodMat = GetOrCreateWoodMaterial();
            for (int i = 0; i < 10; i++)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"Debris_Fence_{i}";
                post.transform.position = transform.position + UnityEngine.Random.insideUnitSphere * 0.4f;
                post.transform.rotation = UnityEngine.Random.rotation;
                post.transform.localScale = new Vector3(0.14f, 0.85f, 0.14f) * UnityEngine.Random.Range(0.7f, 1.2f);

                var r = post.GetComponent<Renderer>();
                if (r != null && woodMat != null) r.sharedMaterial = woodMat;

                Rigidbody rb = post.AddComponent<Rigidbody>();
                rb.mass = 2.0f;

                Vector3 postDir = (impulseDir + UnityEngine.Random.insideUnitSphere * 0.4f).normalized;
                rb.AddForce(postDir * UnityEngine.Random.Range(70f, 150f), ForceMode.Impulse);

                StartCoroutine(CleanDebrisPiece(post, 1.2f, 3.5f));
            }
        }

        private void SpawnLootPickup(Vector3 pos)
        {
            string chosenItem = "item_scrap";
            if (possibleLootItemIds.Count > 0)
            {
                chosenItem = possibleLootItemIds[UnityEngine.Random.Range(0, possibleLootItemIds.Count)];
            }

            GameObject lootObj = new GameObject($"Loot_{chosenItem}");
            lootObj.transform.position = pos;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(lootObj.transform, false);
            visual.transform.localScale = new Vector3(0.22f, 0.18f, 0.22f);

            var r = visual.GetComponent<Renderer>();
            if (r != null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material mat = new Material(s);
                mat.color = new Color(0.95f, 0.82f, 0.35f); // Amber glow
                mat.enableInstancing = true;
                r.sharedMaterial = mat;
            }

            SphereCollider sc = lootObj.AddComponent<SphereCollider>();
            sc.radius = 0.5f;
            sc.isTrigger = true;

            var pickup = lootObj.AddComponent<DestructibleLootPickup>();
            pickup.Configure(chosenItem);
        }

        private IEnumerator CleanDebrisPiece(GameObject piece, float disableColliderDelay, float despawnDelay)
        {
            yield return new WaitForSeconds(disableColliderDelay);
            if (piece != null)
            {
                Collider c = piece.GetComponent<Collider>();
                if (c != null) c.enabled = false;
                Rigidbody rb = piece.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }

            yield return new WaitForSeconds(despawnDelay - disableColliderDelay);
            if (piece != null)
            {
                Destroy(piece);
            }
        }

        private Material GetOrCreateGlassMaterial()
        {
            string path = "Assets/_Project/Materials/Mat_Weathered_Glass.mat";
            Material m = Resources.Load<Material>(path);
#if UNITY_EDITOR
            if (m == null) m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
#endif
            if (m == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(s);
                m.color = new Color(0.75f, 0.88f, 0.95f, 0.45f);
                m.enableInstancing = true;
            }
            return m;
        }

        private Material GetOrCreateWoodMaterial()
        {
            string path = "Assets/_Project/Materials/Mat_Wood_Timber.mat";
            Material m = Resources.Load<Material>(path);
#if UNITY_EDITOR
            if (m == null) m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
#endif
            if (m == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(s);
                m.color = new Color(0.45f, 0.32f, 0.20f);
                m.enableInstancing = true;
            }
            return m;
        }
    }

    /// <summary>
    /// Helper component for physical loot spawned from broken crates.
    /// </summary>
    public class DestructibleLootPickup : MonoBehaviour
    {
        [SerializeField] private string itemId = "item_scrap";

        public void Configure(string id)
        {
            itemId = id;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                InventoryManager.AddItem(itemId, 1);
                ProceduralAudioManager.Instance?.PlayItemPickup();
                MobileHapticManager.TriggerLight();
                GameEvents.TriggerObjectiveToastRequested($"Eşya Toplandı: {itemId}", 2.0f);
                Destroy(gameObject);
            }
        }
    }
}
