using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;

namespace ProjectSurvival.Interaction
{
    public enum TacticalPropType
    {
        ExplosiveBarrel,
        WoodenCrate,
        WoodenPallet
    }

    /// <summary>
    /// Interactive Tactical Props & Explosive Barrels Archetype.
    /// Manages:
    /// 1. Fuel Barrels: "push" (rolls barrel forward into chokepoints) and firearm detonation (8m AoE blast + 55m horde alert).
    /// 2. Wooden Crates & Pallets: "smash" (harvests Scrap/Wood for crafting and barricades).
    /// </summary>
    public class TacticalPropInteractable : WorldInteractableObject
    {
        [Header("Tactical Prop Type")]
        [SerializeField] private TacticalPropType propType = TacticalPropType.ExplosiveBarrel;
        [SerializeField] private Rigidbody propRigidbody;
        [SerializeField] private float explosionRadius = 8.0f;
        [SerializeField] private float explosionDamage = 180f;
        [SerializeField] private float hordeAlertRadius = 55.0f;
        [SerializeField] private bool hasExploded = false;

        public override string ObjectSubtitle => hasExploded ? "PATLAMIŞ" : (propType == TacticalPropType.ExplosiveBarrel ? "YANICI / PATLAYICI" : "PARÇALANABİLİR");

        protected override void Awake()
        {
            base.Awake();
            DetectPropType();
            propRigidbody = GetComponent<Rigidbody>();
        }

        private void DetectPropType()
        {
            string n = gameObject.name.ToLower();
            if (n.Contains("barrel") || n.Contains("varil") || n.Contains("fuel"))
            {
                propType = TacticalPropType.ExplosiveBarrel;
                objectTitle = "Yanıcı Yakıt Varili";
            }
            else if (n.Contains("pallet") || n.Contains("palet"))
            {
                propType = TacticalPropType.WoodenPallet;
                objectTitle = "Ahşap Palet";
            }
            else
            {
                propType = TacticalPropType.WoodenCrate;
                objectTitle = "Tedarik Sandığı";
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (isDamaged || hasExploded) return verbs;

            if (propType == TacticalPropType.ExplosiveBarrel)
            {
                verbs.Add(new InteractionVerb("push", "VARİLİ İLERİ YUVARLA", 0.4f));
                verbs.Add(new InteractionVerb("detonate", "VARİLİ PATLAT (TUZAK)", 0.6f, null, null, 55f));
            }
            else
            {
                verbs.Add(new InteractionVerb("smash", "PARÇALA (MALZEME TOPLA)", 1.5f, null, null, 14f));
            }

            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            switch (verbId)
            {
                case "push":
                    ExecutePush(interactor);
                    break;
                case "detonate":
                    ExecuteDetonate();
                    break;
                case "smash":
                    ExecuteSmash();
                    break;
            }
        }

        private void ExecutePush(GameObject player)
        {
            if (propRigidbody == null) propRigidbody = gameObject.AddComponent<Rigidbody>();

            propRigidbody.isKinematic = false;
            propRigidbody.mass = 45f;

            Vector3 pushDir = player != null ? player.transform.forward : transform.forward;
            pushDir.y = 0.1f;
            propRigidbody.AddForce(pushDir.normalized * 350f, ForceMode.Impulse);

            ProceduralAudioManager.Instance?.PlayDoorPoundImpact(transform.position, true);
            GameEvents.TriggerObjectiveToastRequested("VARİL YUVARLANDI!", 1.5f);
        }

        public void ExecuteDetonate()
        {
            if (hasExploded) return;
            hasExploded = true;
            isDamaged = true;

            // Fiery explosion VFX
            CreateExplosionVFX();

            // Deal 180 AoE damage to all zombies in 8m radius
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                ZombieAI z = hits[i].GetComponentInParent<ZombieAI>();
                if (z != null && !z.IsDead)
                {
                    z.TakeDamage(explosionDamage, transform);
                }
            }

            // Audio, camera shake, and 55m horde alert
            ProceduralAudioManager.Instance?.PlayGunfire(true, 1.5f);
            GameEvents.TriggerCameraShakeRequested(0.45f, 0.35f);
            GameEvents.TriggerNoiseEmitted(transform.position, hordeAlertRadius);
            GameEvents.TriggerObjectiveToastRequested("VARİL PATLADI! (55m SÜRÜ ÇAĞRISI)", 3.0f);

            // Hide visual mesh or destroy
            Transform visual = transform.Find("Visual_Model") ?? transform;
            var renderers = visual.GetComponentsInChildren<Renderer>();
            for (int r = 0; r < renderers.Length; r++) renderers[r].enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private void CreateExplosionVFX()
        {
            GameObject fx = new GameObject("Explosion_VFX");
            fx.transform.position = transform.position + Vector3.up * 0.5f;

            ParticleSystem ps = fx.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.0f;
            main.startSpeed = 8f;
            main.startSize = 1.8f;
            main.startColor = new Color(1.0f, 0.45f, 0.1f, 0.9f);
            main.loop = false;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });

            var renderer = fx.GetComponent<ParticleSystemRenderer>();
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (s != null) renderer.sharedMaterial = new Material(s);

            Destroy(fx, 2.0f);
        }

        private void ExecuteSmash()
        {
            isDamaged = true;

            if (InventorySystem.Instance != null)
            {
                ItemData scrap = LoadItem("item_scrap");
                if (scrap != null) InventorySystem.Instance.AddItem(scrap, 2);
            }

            ProceduralAudioManager.Instance?.PlayDoorPoundImpact(transform.position, false);
            GameEvents.TriggerObjectiveToastRequested("SANDIK PARÇALANDI (+2 HURDA/AHŞAP)", 2.0f);

            Transform visual = transform.Find("Visual_Model") ?? transform;
            var renderers = visual.GetComponentsInChildren<Renderer>();
            for (int r = 0; r < renderers.Length; r++) renderers[r].enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private ItemData LoadItem(string itemId)
        {
            ItemData it = Resources.Load<ItemData>($"Items/{itemId}") ?? Resources.Load<ItemData>($"Data/Items/{itemId}");
#if UNITY_EDITOR
            if (it == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"{itemId} t:ItemData");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    it = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                }
            }
#endif
            return it;
        }
    }
}
