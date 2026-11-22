using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;

namespace ProjectSurvival.Interaction
{
    public enum UtilityFixtureType
    {
        SinkOrCooler,
        FireHydrant,
        BedOrCouch
    }

    /// <summary>
    /// Interactive Utility Fixture & Rest Archetype.
    /// Manages:
    /// - Indoor Sinks & Coolers: "drink" (+25 Thirst) and "refill" (+1 Water Bottle, limited 2 uses).
    /// - Street Fire Hydrants: "open_hydrant" (creates 10m water geyser, 30m noise decoy, slows zombies 40%).
    /// - Beds & Couches: "rest" (advances 2 hours, +30 Health, +100 Stamina, -15 Hunger/Thirst).
    /// </summary>
    public class UtilityFixtureInteractable : WorldInteractableObject
    {
        [Header("Fixture Classification")]
        [SerializeField] private UtilityFixtureType fixtureType = UtilityFixtureType.SinkOrCooler;
        [SerializeField] private int remainingWaterUses = 2;
        [SerializeField] private bool isHydrantActive = false;
        [SerializeField] private ParticleSystem waterGeyserParticles;

        public override string ObjectSubtitle
        {
            get
            {
                switch (fixtureType)
                {
                    case UtilityFixtureType.FireHydrant:
                        return isHydrantActive ? "BASINÇLI SU FIŞKIRIYOR" : "YANGIN MUSLUĞU";
                    case UtilityFixtureType.BedOrCouch:
                        return "DİNLENME NOKTASI";
                    default:
                        return remainingWaterUses > 0 ? $"SU KAYNAĞI ({remainingWaterUses} KULLANIM)" : "SU TÜKENDİ";
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            DetectFixtureType();
        }

        private void DetectFixtureType()
        {
            string n = gameObject.name.ToLower();
            if (n.Contains("hydrant") || n.Contains("musluk"))
            {
                fixtureType = UtilityFixtureType.FireHydrant;
                objectTitle = "Sokak Yangın Musluğu";
            }
            else if (n.Contains("bed") || n.Contains("couch") || n.Contains("yatak") || n.Contains("koltuk") || n.Contains("bench"))
            {
                fixtureType = UtilityFixtureType.BedOrCouch;
                objectTitle = "Dinlenme Yatağı";
            }
            else
            {
                fixtureType = UtilityFixtureType.SinkOrCooler;
                objectTitle = "Su Bataryası";
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            switch (fixtureType)
            {
                case UtilityFixtureType.SinkOrCooler:
                    if (remainingWaterUses > 0)
                    {
                        verbs.Add(new InteractionVerb("drink", "SU İÇ (+25 SUSUZLUK)", 1.0f));
                        verbs.Add(new InteractionVerb("refill", "ŞİŞE DOLDUR (+1 SU ŞİŞESİ)", 1.5f));
                    }
                    break;

                case UtilityFixtureType.FireHydrant:
                    if (!isHydrantActive)
                    {
                        bool hasTool = InventorySystem.Instance != null && (InventorySystem.Instance.HasItem("item_crowbar") || InventorySystem.Instance.HasItem("weapon_axe"));
                        verbs.Add(new InteractionVerb("open_hydrant", "VANAYI AÇ (SU GEYSERİ & YEM)", 1.8f, "item_crowbar", "Levye / Balta", 30f));
                    }
                    break;

                case UtilityFixtureType.BedOrCouch:
                    verbs.Add(new InteractionVerb("rest", "DİNLEN VE UYU (2 SAAT)", 1.5f));
                    break;
            }

            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            switch (verbId)
            {
                case "drink":
                    ExecuteDrink(interactor);
                    break;
                case "refill":
                    ExecuteRefill();
                    break;
                case "open_hydrant":
                    ExecuteOpenHydrant();
                    break;
                case "rest":
                    ExecuteRest(interactor);
                    break;
            }
        }

        private void ExecuteDrink(GameObject player)
        {
            if (remainingWaterUses <= 0) return;
            remainingWaterUses--;

            var stats = player != null ? player.GetComponent<SurvivalStats>() : null;
            if (stats != null)
            {
                stats.Hydrate(25f);
            }

            ProceduralAudioManager.Instance?.PlayDrink();
            GameEvents.TriggerObjectiveToastRequested("TEMİZ SU İÇİLDİ (+25 SUSUZLUK GİDERİLDİ)", 2.0f);
        }

        private void ExecuteRefill()
        {
            if (remainingWaterUses <= 0) return;
            remainingWaterUses--;

            if (InventorySystem.Instance != null)
            {
                ItemData water = LoadItem("item_water_bottle");
                if (water != null) InventorySystem.Instance.AddItem(water, 1);
            }

            ProceduralAudioManager.Instance?.PlayItemPickup();
            GameEvents.TriggerObjectiveToastRequested("SU ŞİŞESİ DOLDURULDU (+1)", 2.0f);
        }

        private void ExecuteOpenHydrant()
        {
            isHydrantActive = true;
            EnsureWaterGeyser();
            if (waterGeyserParticles != null) waterGeyserParticles.Play();

            StartCoroutine(HydrantDecoyAndSlowRoutine());
            GameEvents.TriggerObjectiveToastRequested("YANGIN MUSLUĞU AÇILDI! (30m SES YEMİ & YAVAŞLATMA)", 3.0f);
        }

        private void EnsureWaterGeyser()
        {
            if (waterGeyserParticles != null) return;

            GameObject geyserObj = new GameObject("WaterGeyser_VFX");
            geyserObj.transform.SetParent(transform, false);
            geyserObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);

            waterGeyserParticles = geyserObj.AddComponent<ParticleSystem>();
            var main = waterGeyserParticles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 1.4f;
            main.startSpeed = 8.5f;
            main.startSize = 0.25f;
            main.startColor = new Color(0.6f, 0.85f, 1.0f, 0.7f);
            main.gravityModifier = 2.2f;

            var shape = waterGeyserParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.15f;

            var emission = waterGeyserParticles.emission;
            emission.rateOverTime = 65f;

            var renderer = geyserObj.GetComponent<ParticleSystemRenderer>();
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (s != null)
            {
                renderer.sharedMaterial = new Material(s);
            }
        }

        private IEnumerator HydrantDecoyAndSlowRoutine()
        {
            float elapsed = 0f;
            float duration = 25.0f;

            while (elapsed < duration)
            {
                elapsed += 1.2f;
                // Emit continuous 30m acoustic decoy
                GameEvents.TriggerNoiseEmitted(transform.position, 30f);

                // Slow zombies walking through water spray (6m radius)
                Collider[] hits = Physics.OverlapSphere(transform.position, 6.0f);
                for (int i = 0; i < hits.Length; i++)
                {
                    ZombieAI z = hits[i].GetComponentInParent<ZombieAI>();
                    if (z != null && !z.IsDead && z.NavAgent != null && z.NavAgent.isOnNavMesh)
                    {
                        z.NavAgent.speed = Mathf.Min(z.NavAgent.speed, 2.0f);
                    }
                }

                yield return new WaitForSeconds(1.2f);
            }

            if (waterGeyserParticles != null) waterGeyserParticles.Stop();
            isHydrantActive = false;
        }

        private void ExecuteRest(GameObject player)
        {
            if (DayNightManager.Instance != null)
            {
                float newHour = (DayNightManager.Instance.CurrentHour + 2.0f) % 24f;
                DayNightManager.Instance.SetTime(newHour);
            }

            var stats = player != null ? player.GetComponent<SurvivalStats>() : null;
            if (stats != null)
            {
                stats.Heal(30f);
                stats.RestoreStamina(100f);
                stats.Feed(-15f);
                stats.Hydrate(-15f);
            }

            ProceduralAudioManager.Instance?.PlayFirstAid();
            GameEvents.TriggerCameraShakeRequested(0.15f, 0.2f);
            GameEvents.TriggerObjectiveToastRequested("2 SAAT DİNLENİLDİ (+30 CAN, +100 STAMİNA)", 2.5f);
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
