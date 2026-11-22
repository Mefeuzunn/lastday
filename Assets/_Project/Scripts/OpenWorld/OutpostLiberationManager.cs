using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;

namespace ProjectSurvival.OpenWorld
{
    [Serializable]
    public class OutpostNode
    {
        public string outpostId;
        public string displayName;
        public Vector3 worldPosition;
        public float perimeterRadius = 28f;
        public float safeZoneRadius = 35f;
        public bool isLiberated = false;
        public string garrisonNpcName = "Muhafız Grubu";
        public List<Light> outpostLanterns = new List<Light>();
    }

    /// <summary>
    /// Phase 4 Decoupled Outpost Liberation Manager.
    /// Manages generator defense waves, perimeter sweeps, safe-zone suppression,
    /// emerald lantern conversion, and garrison assignments across all 4 outer outposts.
    /// </summary>
    public class OutpostLiberationManager : MonoBehaviour
    {
        public static OutpostLiberationManager Instance { get; private set; }

        [Header("Outpost Definitions")]
        [SerializeField] private List<OutpostNode> outposts = new List<OutpostNode>()
        {
            new OutpostNode
            {
                outpostId = "Outpost_NorthHighway",
                displayName = "Kuzey Tahliye Otoyolu İleri Karakolu",
                worldPosition = new Vector3(0f, 6.5f, 200f),
                garrisonNpcName = "Askeri Tahliye Timi"
            },
            new OutpostNode
            {
                outpostId = "Outpost_EastRailStation",
                displayName = "Doğu Tren Garı Lojistik Karakolu",
                worldPosition = new Vector3(305f, 1.3f, 65f),
                garrisonNpcName = "Demiryolu Muhafızları"
            },
            new OutpostNode
            {
                outpostId = "Outpost_NorthwestRangerTower",
                displayName = "Kuzeybatı Orman Gözetleme Karakolu",
                worldPosition = new Vector3(-220f, 0.35f, 259f),
                garrisonNpcName = "Orman Korucuları"
            },
            new OutpostNode
            {
                outpostId = "Outpost_SouthMetroHub",
                displayName = "Güney Metro Meydanı Yeraltı Karakolu",
                worldPosition = new Vector3(0f, -4.2f, -190f),
                garrisonNpcName = "Metro Direnişçileri"
            }
        };

        [Header("Defense Wave Config")]
        [SerializeField] private float defenseDurationSeconds = 45f;
        [SerializeField] private Color liberatedLanternColor = new Color(0.1f, 0.95f, 0.45f, 1f); // Emerald green

        private OutpostNode activeDefenseOutpost = null;
        private float defenseTimerRemaining = 0f;
        private Coroutine defenseRoutine = null;

        public bool IsDefenseActive => activeDefenseOutpost != null;
        public float DefenseTimerRemaining => defenseTimerRemaining;
        public IReadOnlyList<OutpostNode> Outposts => outposts;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DiscoverOutpostLights();
        }

        public void DiscoverOutpostLights()
        {
            for (int i = 0; i < outposts.Count; i++)
            {
                var op = outposts[i];
                if (op.outpostLanterns == null) op.outpostLanterns = new List<Light>();
                op.outpostLanterns.Clear();

                Collider[] cols = Physics.OverlapSphere(op.worldPosition, op.safeZoneRadius);
                for (int c = 0; c < cols.Length; c++)
                {
                    Light l = cols[c].GetComponentInChildren<Light>();
                    if (l != null && !op.outpostLanterns.Contains(l))
                    {
                        op.outpostLanterns.Add(l);
                    }
                }
            }
        }

        public OutpostNode FindOutpost(string id)
        {
            for (int i = 0; i < outposts.Count; i++)
            {
                if (outposts[i].outpostId == id) return outposts[i];
            }
            return null;
        }

        public bool IsOutpostLiberated(string id)
        {
            var op = FindOutpost(id);
            return op != null && op.isLiberated;
        }

        public bool IsPointInSafeZone(Vector3 worldPos)
        {
            for (int i = 0; i < outposts.Count; i++)
            {
                if (outposts[i].isLiberated)
                {
                    if (Vector3.Distance(worldPos, outposts[i].worldPosition) <= outposts[i].safeZoneRadius)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void StartLiberationDefense(string id)
        {
            var op = FindOutpost(id);
            if (op == null)
            {
                Debug.LogWarning($"[OutpostLiberationManager] Outpost '{id}' not found.");
                return;
            }

            if (op.isLiberated)
            {
                GameEvents.TriggerObjectiveToastRequested($"{op.displayName} zaten kurtarılmış ve güvenli!", 2.5f);
                return;
            }

            if (IsDefenseActive)
            {
                GameEvents.TriggerObjectiveToastRequested("Halihazırda başka bir karakol savunması devam ediyor!", 2.5f);
                return;
            }

            // Check perimeter zombies (28m radius)
            int nearbyZombies = CountPerimeterZombies(op.worldPosition, op.perimeterRadius);

            activeDefenseOutpost = op;
            defenseTimerRemaining = defenseDurationSeconds;

            GameEvents.TriggerObjectiveToastRequested($"[KARAKOL SAVUNMASI] {op.displayName} jeneratörü devrede! 45 sn savunun!", 4.0f);
            ProceduralAudioManager.Instance?.PlayAirRaidSiren();
            GameEvents.TriggerNoiseEmitted(op.worldPosition, 45f);
            GameEvents.TriggerHordeAlertTriggered(op.worldPosition, 45f);

            if (defenseRoutine != null) StopCoroutine(defenseRoutine);
            defenseRoutine = StartCoroutine(DefenseWaveRoutine(op));
        }

        private int CountPerimeterZombies(Vector3 center, float radius)
        {
            int count = 0;
            ZombieAI[] zombies = UnityEngine.Object.FindObjectsByType<ZombieAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < zombies.Length; i++)
            {
                if (zombies[i] != null && !zombies[i].IsDead && Vector3.Distance(zombies[i].transform.position, center) <= radius)
                {
                    count++;
                }
            }
            return count;
        }

        private IEnumerator DefenseWaveRoutine(OutpostNode op)
        {
            float elapsed = 0f;
            while (elapsed < defenseDurationSeconds)
            {
                yield return new WaitForSeconds(1.0f);
                elapsed += 1.0f;
                defenseTimerRemaining = Mathf.Max(0f, defenseDurationSeconds - elapsed);

                if (Mathf.Approximately(elapsed % 15f, 0f))
                {
                    GameEvents.TriggerNoiseEmitted(op.worldPosition, 35f);
                    GameEvents.TriggerObjectiveToastRequested($"Savunma devam ediyor: {defenseTimerRemaining:F0} sn kaldı!", 2.0f);
                }
            }

            CompleteLiberation(op.outpostId);
            activeDefenseOutpost = null;
            defenseRoutine = null;
        }

        public void CompleteLiberation(string id)
        {
            var op = FindOutpost(id);
            if (op == null) return;

            op.isLiberated = true;
            ConvertLanternsToSafeEmerald(op);

            // Morale boost + garrison notification
            if (CommunityOutpostManager.Instance != null)
            {
                CommunityOutpostManager.Instance.ModifyMorale(15f);
                CommunityOutpostManager.Instance.AddResident(op.garrisonNpcName);
            }

            ProceduralAudioManager.Instance?.PlayItemPickup();
            GameEvents.TriggerCameraShakeRequested(0.3f, 0.25f);
            GameEvents.TriggerObjectiveToastRequested($"[BÖLGE GÜVENLİ] {op.displayName} kurtarıldı! Sığınak Morali +%15. Güvenli bölge (35m) aktif.", 5.0f);
            ProjectSurvival.Progression.SurvivorProgressionManager.Instance?.RegisterOutpostLiberated(op.displayName);
            Debug.Log($"[OutpostLiberationManager] Outpost '{op.displayName}' successfully liberated!");
        }

        private void ConvertLanternsToSafeEmerald(OutpostNode op)
        {
            if (op.outpostLanterns == null || op.outpostLanterns.Count == 0)
            {
                DiscoverOutpostLights();
            }

            for (int i = 0; i < op.outpostLanterns.Count; i++)
            {
                if (op.outpostLanterns[i] != null)
                {
                    op.outpostLanterns[i].enabled = true;
                    op.outpostLanterns[i].color = liberatedLanternColor;
                    op.outpostLanterns[i].intensity = Mathf.Max(op.outpostLanterns[i].intensity, 1.5f);
                }
            }
        }

        public bool IsPointInSafeZone(Vector3 point, float radius = 35f)
        {
            if (outposts == null) return false;
            for (int i = 0; i < outposts.Count; i++)
            {
                if (outposts[i].isLiberated)
                {
                    float distSqr = (point - outposts[i].worldPosition).sqrMagnitude;
                    if (distSqr <= radius * radius) return true;
                }
            }
            return false;
        }

        public List<string> GetLiberatedOutpostIds()
        {
            List<string> list = new List<string>();
            for (int i = 0; i < outposts.Count; i++)
            {
                if (outposts[i].isLiberated) list.Add(outposts[i].outpostId);
            }
            return list;
        }

        public void RestoreLiberatedOutposts(List<string> ids)
        {
            if (ids == null) return;
            for (int i = 0; i < ids.Count; i++)
            {
                var op = FindOutpost(ids[i]);
                if (op != null)
                {
                    op.isLiberated = true;
                    ConvertLanternsToSafeEmerald(op);
                }
            }
            Debug.Log($"[OutpostLiberationManager] Restored {ids.Count} liberated outposts from SaveData.");
        }
    }
}
