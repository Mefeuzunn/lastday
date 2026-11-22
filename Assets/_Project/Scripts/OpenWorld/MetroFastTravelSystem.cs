using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;
using ProjectSurvival.AI;
using ProjectSurvival.Environment;

namespace ProjectSurvival.OpenWorld
{
    [Serializable]
    public class MetroStationNode
    {
        public string stationId;
        public string displayName;
        public Vector3 worldPosition;
        public bool isUnlocked = false;
        public string description;
    }

    /// <summary>
    /// Phase 4 Decoupled Metro Fast Travel System.
    /// Manages network topology, station unlock states, combat-lock checks (22m),
    /// survival exertion penalties (-8 Hunger, -10 Thirst), sector prewarming,
    /// and clean player/companion NavMesh teleportation.
    /// </summary>
    public class MetroFastTravelSystem : MonoBehaviour
    {
        public static MetroFastTravelSystem Instance { get; private set; }

        [Header("Transit Network Topology")]
        [SerializeField] private List<MetroStationNode> stations = new List<MetroStationNode>()
        {
            new MetroStationNode
            {
                stationId = "safehouse_tunnel",
                displayName = "Sığınak Servis Tüneli (Merkez)",
                worldPosition = new Vector3(-35f, 0.8f, -30f),
                isUnlocked = true,
                description = "Karantina merkezi altındaki korunaklı metro bakım tüneli."
            },
            new MetroStationNode
            {
                stationId = "south_metro_hub",
                displayName = "Güney Metro Aktarma Meydanı",
                worldPosition = new Vector3(0f, -4.2f, -190f),
                isUnlocked = true,
                description = "AVM Meydanı altındaki iki katlı batık metro istasyonu."
            },
            new MetroStationNode
            {
                stationId = "north_highway",
                displayName = "Kuzey Tahliye Otoyolu İstasyonu",
                worldPosition = new Vector3(0f, 6.5f, 190f),
                isUnlocked = false,
                description = "Askeri viyadük kontrol noktası acil kaçış tüneli."
            },
            new MetroStationNode
            {
                stationId = "east_rail_station",
                displayName = "Doğu Sanayi Tren Garı İstasyonu",
                worldPosition = new Vector3(290f, 1.2f, 50f),
                isUnlocked = false,
                description = "Yük vagonları ve hangar arkasındaki yer altı kablo kanalı."
            },
            new MetroStationNode
            {
                stationId = "northwest_ranger_tower",
                displayName = "Kuzeybatı Göl Kenarı Korucu İstasyonu",
                worldPosition = new Vector3(-215f, 0.5f, 250f),
                isUnlocked = false,
                description = "Göl kıyısı su pompa istasyonu yeraltı menfez girişi."
            }
        };

        [Header("Survival Exertion & Combat Rules")]
        [SerializeField] private float combatLockRadius = 22f;
        [SerializeField] private float travelHungerCost = 8f;
        [SerializeField] private float travelThirstCost = 10f;

        public IReadOnlyList<MetroStationNode> Stations => stations;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public MetroStationNode FindStation(string id)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                if (stations[i].stationId == id) return stations[i];
            }
            return null;
        }

        public bool IsStationUnlocked(string id)
        {
            var st = FindStation(id);
            return st != null && st.isUnlocked;
        }

        public void UnlockStation(string id)
        {
            var st = FindStation(id);
            if (st != null && !st.isUnlocked)
            {
                st.isUnlocked = true;
                GameEvents.TriggerObjectiveToastRequested($"[METRO HATTI] {st.displayName} transit ağına bağlandı!", 3.5f);
                ProceduralAudioManager.Instance?.PlayItemPickup();
                Debug.Log($"[MetroFastTravelSystem] Station '{st.displayName}' unlocked for fast travel.");
            }
        }

        public bool CanFastTravel(out string failureReason)
        {
            failureReason = string.Empty;

            // 1. Combat-lock validation (22m radius)
            GameObject player = null;
            try { player = GameObject.FindWithTag("Player"); } catch {}
            if (player == null)
            {
                PlayerController pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Exclude);
                if (pc != null) player = pc.gameObject;
            }

            if (player != null)
            {
                ZombieAI[] zombies = UnityEngine.Object.FindObjectsByType<ZombieAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                int alertZombiesNearby = 0;
                for (int i = 0; i < zombies.Length; i++)
                {
                    if (zombies[i] != null && !zombies[i].IsDead)
                    {
                        if (Vector3.Distance(zombies[i].transform.position, player.transform.position) <= combatLockRadius)
                        {
                            alertZombiesNearby++;
                        }
                    }
                }

                if (alertZombiesNearby > 0)
                {
                    failureReason = $"Çatışma sırasında hızlı seyahat kullanılamaz! ({alertZombiesNearby} tehdit yakında).";
                    return false;
                }

                // 2. Exertion resources check
                SurvivalStats stats = player.GetComponent<SurvivalStats>();
                if (stats != null)
                {
                    if (stats.CurrentHunger < travelHungerCost || stats.CurrentThirst < travelThirstCost)
                    {
                        failureReason = $"Aşırı bitkinlik! Transit için en az {travelHungerCost} Açlık ve {travelThirstCost} Susuzluk gereklidir.";
                        return false;
                    }
                }
            }

            return true;
        }

        public bool ExecuteFastTravel(string destinationStationId)
        {
            if (!CanFastTravel(out string reason))
            {
                GameEvents.TriggerObjectiveToastRequested(reason, 3.0f);
                ProceduralAudioManager.Instance?.PlaySoftError();
                return false;
            }

            var dest = FindStation(destinationStationId);
            if (dest == null)
            {
                GameEvents.TriggerObjectiveToastRequested("Hedef metro istasyonu bulunamadı!", 2.5f);
                return false;
            }

            if (!dest.isUnlocked)
            {
                GameEvents.TriggerObjectiveToastRequested($"{dest.displayName} henüz kilitli. Önce yüzeyden keşfedilmelidir.", 3.0f);
                ProceduralAudioManager.Instance?.PlaySoftError();
                return false;
            }

            StartCoroutine(PerformFastTravelRoutine(dest));
            return true;
        }

        private IEnumerator PerformFastTravelRoutine(MetroStationNode dest)
        {
            GameEvents.TriggerCameraShakeRequested(0.4f, 0.4f);
            GameEvents.TriggerObjectiveToastRequested($"Tünel transit hattı kullanılıyor... Hedef: {dest.displayName}", 3.0f);
            ProceduralAudioManager.Instance?.PlayDoorPoundImpact(dest.worldPosition, true);

            GameObject player = null;
            try { player = GameObject.FindWithTag("Player"); } catch {}
            if (player == null)
            {
                PlayerController pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Exclude);
                if (pc != null) player = pc.gameObject;
            }

            // Apply survival exertion penalties
            if (player != null)
            {
                SurvivalStats stats = player.GetComponent<SurvivalStats>();
                if (stats != null)
                {
                    stats.Feed(-travelHungerCost);
                    stats.Hydrate(-travelThirstCost);
                }
            }

            yield return new WaitForSeconds(0.4f);

            // Prewarm destination sector
            if (WorldSectorStreamer.Instance != null)
            {
                WorldSectorStreamer.Instance.PrewarmSectorAtPosition(dest.worldPosition);
            }

            yield return new WaitForSeconds(0.2f);

            // Warp player to destination NavMesh position
            Vector3 targetPos = dest.worldPosition;
            if (NavMesh.SamplePosition(dest.worldPosition, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                targetPos = hit.position;
            }

            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = targetPos;
                if (cc != null) cc.enabled = true;
            }

            // Warp companion
            if (CompanionManager.Instance != null && CompanionManager.Instance.HasActiveCompanion)
            {
                SurvivorNPC_AI comp = CompanionManager.Instance.ActiveCompanion;
                if (comp != null)
                {
                    Vector3 compTarget = targetPos - Vector3.forward * 2.5f;
                    if (NavMesh.SamplePosition(compTarget, out NavMeshHit cHit, 5.0f, NavMesh.AllAreas))
                    {
                        compTarget = cHit.position;
                    }

                    NavMeshAgent agent = comp.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.Warp(compTarget);
                    }
                    else
                    {
                        comp.transform.position = compTarget;
                    }
                }
            }

            GameEvents.TriggerCameraShakeRequested(0.25f, 0.3f);
            GameEvents.TriggerObjectiveToastRequested($"İstasyona ulaşıldı: {dest.displayName}", 3.0f);
            ProceduralAudioManager.Instance?.PlayFootstep(false);
        }

        public List<string> GetUnlockedStationIds()
        {
            List<string> list = new List<string>();
            for (int i = 0; i < stations.Count; i++)
            {
                if (stations[i].isUnlocked) list.Add(stations[i].stationId);
            }
            return list;
        }

        public void RestoreUnlockedStations(List<string> ids)
        {
            if (ids == null) return;
            for (int i = 0; i < ids.Count; i++)
            {
                var st = FindStation(ids[i]);
                if (st != null) st.isUnlocked = true;
            }
            Debug.Log($"[MetroFastTravelSystem] Restored {ids.Count} unlocked stations from SaveData.");
        }
    }
}
