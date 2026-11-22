using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.Events
{
    [Serializable]
    public class AirdropLandingZone
    {
        public string zoneId;
        public string zoneDisplayName;
        public Vector3 landingPosition;

        public AirdropLandingZone(string id, string name, Vector3 pos)
        {
            zoneId = id;
            zoneDisplayName = name;
            landingPosition = pos;
        }
    }

    [Serializable]
    public class CivilianSOSLocation
    {
        public string locationId;
        public string locationDisplayName;
        public Vector3 locationPosition;

        public CivilianSOSLocation(string id, string name, Vector3 pos)
        {
            locationId = id;
            locationDisplayName = name;
            locationPosition = pos;
        }
    }

    /// <summary>
    /// Phase 2 (Step 2B): Dynamic World Events Director.
    /// Orchestrates high-stakes emergent open-world events across outer sectors:
    /// 1. Contested Helicopter Supply Airdrops (every 10 in-game hours, ~100s real time).
    /// 2. Dynamic Civilian SOS Rescue Encounters (every 180s - 240s).
    /// Strictly protects the 55m radius around StarterHouse_Safehome (Sector_0_0_Center)
    /// and suppresses automatic random events until StarterPrologueDirector completes.
    /// </summary>
    public class DynamicWorldEventsDirector : MonoBehaviour
    {
        public static DynamicWorldEventsDirector Instance { get; private set; }

        [Header("Starter Zone Protection")]
        [SerializeField] private float minSafeDistanceFromCenter = 55.0f;

        [Header("Scheduling & Intervals")]
        [SerializeField] private float airdropHourInterval = 10.0f; // Every 10 in-game hours
        [SerializeField] private float minSosIntervalSeconds = 180.0f; // 3 minutes
        [SerializeField] private float maxSosIntervalSeconds = 240.0f; // 4 minutes
        [SerializeField] private bool autoScheduleEvents = true;

        [Header("Curated Airdrop Landing Zones (6 Outer Sectors)")]
        [SerializeField] private List<AirdropLandingZone> landingZones = new List<AirdropLandingZone>();

        [Header("Curated Civilian SOS Encounters (6 Outer Sectors)")]
        [SerializeField] private List<CivilianSOSLocation> sosLocations = new List<CivilianSOSLocation>();

        private readonly List<HelicopterAirdropEvent> activeAirdrops = new List<HelicopterAirdropEvent>();
        private readonly List<CivilianSOSEvent> activeSOSEvents = new List<CivilianSOSEvent>();

        private float lastAirdropHour = -99f;
        private float nextSosEventTime = 0f;
        private float fallbackAirdropTimer = 0f;
        private int nextAirdropIndex = 0;
        private int nextSosIndex = 0;

        public IReadOnlyList<AirdropLandingZone> LandingZones => landingZones;
        public IReadOnlyList<CivilianSOSLocation> SOSLocations => sosLocations;
        public IReadOnlyList<HelicopterAirdropEvent> ActiveAirdrops => activeAirdrops;
        public IReadOnlyList<CivilianSOSEvent> ActiveSOSEvents => activeSOSEvents;

        public bool IsAirdropActive => activeAirdrops.Count > 0;
        public bool IsSOSActive => activeSOSEvents.Count > 0;
        public int ActiveAirdropCount => activeAirdrops.Count;
        public int ActiveSOSCount => activeSOSEvents.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultLocationsIfEmpty();
        }

        private void Start()
        {
            ScheduleNextSOSEvent();

            if (DayNightManager.Instance != null)
            {
                lastAirdropHour = DayNightManager.Instance.CurrentHour;
            }
            fallbackAirdropTimer = 100.0f; // ~10 in-game hours
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void InitializeDefaultLocationsIfEmpty()
        {
            // 6 Curated Airdrop Landing Zones (all >= 55m from Vector3.zero)
            if (landingZones == null || landingZones.Count < 6)
            {
                landingZones = new List<AirdropLandingZone>
                {
                    new AirdropLandingZone("Airdrop_WestForest", "Batı Koruluğu Açıklığı", new Vector3(-180f, 0f, 25f)),
                    new AirdropLandingZone("Airdrop_EastRail", "Doğu Tren Peronu", new Vector3(210f, 0f, 40f)),
                    new AirdropLandingZone("Airdrop_NorthHighway", "Kuzey Otoyol Üst Geçidi", new Vector3(35f, 0f, 230f)),
                    new AirdropLandingZone("Airdrop_NortheastHarbor", "Kuzeydoğu Liman İskelesi", new Vector3(220f, 0f, 225f)),
                    new AirdropLandingZone("Airdrop_SoutheastSubstation", "Güneydoğu Trafo Sahası", new Vector3(215f, 0f, -215f)),
                    new AirdropLandingZone("Airdrop_NorthwestLake", "Kuzeybatı Gölet Çayırı", new Vector3(-175f, 0f, 195f))
                };
            }

            // 6 Curated Civilian SOS Encounters (all >= 55m from Vector3.zero)
            if (sosLocations == null || sosLocations.Count < 6)
            {
                sosLocations = new List<CivilianSOSLocation>
                {
                    new CivilianSOSLocation("SOS_WestWoods", "Batı Ormanı Bekçi Kulübesi", new Vector3(-160f, 0f, -45f)),
                    new CivilianSOSLocation("SOS_EastWarehouses", "Doğu Hangarlar Aralığı", new Vector3(190f, 0f, -30f)),
                    new CivilianSOSLocation("SOS_NorthGasStation", "Kuzey Otoyol Akaryakıt İstasyonu", new Vector3(-40f, 0f, 210f)),
                    new CivilianSOSLocation("SOS_NortheastHarbor", "Kuzeydoğu Liman Deposu", new Vector3(195f, 0f, 210f)),
                    new CivilianSOSLocation("SOS_SoutheastSubstation", "Güneydoğu Trafo Dış Çiti", new Vector3(180f, 0f, -200f)),
                    new CivilianSOSLocation("SOS_SouthwestFarm", "Güneybatı Çiftlik Ambarı", new Vector3(-150f, 0f, -180f))
                };
            }

            // Validate all locations against starter safezone distance
            ValidateLocationsSafeDistance();
        }

        private void ValidateLocationsSafeDistance()
        {
            for (int i = 0; i < landingZones.Count; i++)
            {
                float d = Vector3.Distance(new Vector3(landingZones[i].landingPosition.x, 0, landingZones[i].landingPosition.z), Vector3.zero);
                if (d < minSafeDistanceFromCenter)
                {
                    Debug.LogWarning($"[DynamicWorldEventsDirector] Airdrop zone '{landingZones[i].zoneId}' was {d:F1}m from center. Pushing to safe distance.");
                    landingZones[i].landingPosition = landingZones[i].landingPosition.normalized * (minSafeDistanceFromCenter + 10f);
                }
            }

            for (int i = 0; i < sosLocations.Count; i++)
            {
                float d = Vector3.Distance(new Vector3(sosLocations[i].locationPosition.x, 0, sosLocations[i].locationPosition.z), Vector3.zero);
                if (d < minSafeDistanceFromCenter)
                {
                    Debug.LogWarning($"[DynamicWorldEventsDirector] SOS location '{sosLocations[i].locationId}' was {d:F1}m from center. Pushing to safe distance.");
                    sosLocations[i].locationPosition = sosLocations[i].locationPosition.normalized * (minSafeDistanceFromCenter + 10f);
                }
            }
        }

        private void Update()
        {
            CleanDeadReferences();

            if (!autoScheduleEvents) return;

            // Check prologue completion: world events do not auto-trigger during prologue
            if (StarterPrologueDirector.Instance != null && !StarterPrologueDirector.Instance.IsPrologueCompleted)
            {
                return;
            }

            UpdateAirdropSchedule();
            UpdateSOSSchedule();
        }

        private void CleanDeadReferences()
        {
            activeAirdrops.RemoveAll(a => a == null || !a.IsEventActive);
            activeSOSEvents.RemoveAll(s => s == null || !s.IsEventActive);
        }

        private void UpdateAirdropSchedule()
        {
            if (DayNightManager.Instance != null)
            {
                float currentHour = DayNightManager.Instance.CurrentHour;
                if (lastAirdropHour < 0f) lastAirdropHour = currentHour;

                float hoursElapsed = (currentHour - lastAirdropHour + 24f) % 24f;
                if (hoursElapsed >= airdropHourInterval)
                {
                    lastAirdropHour = currentHour;
                    TriggerAirdrop();
                }
            }
            else
            {
                fallbackAirdropTimer -= Time.deltaTime;
                if (fallbackAirdropTimer <= 0f)
                {
                    fallbackAirdropTimer = 100.0f; // ~10 in-game hours
                    TriggerAirdrop();
                }
            }
        }

        private void UpdateSOSSchedule()
        {
            // Only 1 civilian SOS event active at a time to prevent audio and map clutter
            if (activeSOSEvents.Count > 0) return;

            if (Time.time >= nextSosEventTime)
            {
                ScheduleNextSOSEvent();
                TriggerCivilianSOS();
            }
        }

        private void ScheduleNextSOSEvent()
        {
            float delay = UnityEngine.Random.Range(minSosIntervalSeconds, maxSosIntervalSeconds);
            nextSosEventTime = Time.time + delay;
        }

        /// <summary>
        /// Triggers a helicopter supply airdrop at a specific curated zone index, or cyclical next zone.
        /// </summary>
        public HelicopterAirdropEvent TriggerAirdrop(int zoneIndex = -1)
        {
            if (landingZones == null || landingZones.Count == 0)
            {
                InitializeDefaultLocationsIfEmpty();
            }

            if (zoneIndex < 0 || zoneIndex >= landingZones.Count)
            {
                zoneIndex = nextAirdropIndex % landingZones.Count;
                nextAirdropIndex++;
            }

            AirdropLandingZone zone = landingZones[zoneIndex];

            // Enforce 55m starter protection
            float distFromCenter = Vector3.Distance(new Vector3(zone.landingPosition.x, 0, zone.landingPosition.z), Vector3.zero);
            if (distFromCenter < minSafeDistanceFromCenter)
            {
                Debug.LogError($"[DynamicWorldEventsDirector] Aborted airdrop: zone '{zone.zoneId}' is within {minSafeDistanceFromCenter}m of starter house ({distFromCenter:F1}m)!");
                return null;
            }

            Vector3 spawnPos = zone.landingPosition;
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 12f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject airdropObj = new GameObject($"HelicopterAirdrop_{zone.zoneId}");
            airdropObj.transform.SetParent(transform, true);

            HelicopterAirdropEvent airdropEvent = airdropObj.AddComponent<HelicopterAirdropEvent>();
            activeAirdrops.Add(airdropEvent);

            airdropEvent.LaunchAirdrop(spawnPos, zone.zoneId);
            Debug.Log($"[DynamicWorldEventsDirector] Launched Helicopter Airdrop at '{zone.zoneDisplayName}' ({spawnPos}).");

            return airdropEvent;
        }

        /// <summary>
        /// Triggers a dynamic civilian SOS rescue encounter at a specific curated location index, or cyclical next location.
        /// </summary>
        public CivilianSOSEvent TriggerCivilianSOS(int locationIndex = -1)
        {
            if (sosLocations == null || sosLocations.Count == 0)
            {
                InitializeDefaultLocationsIfEmpty();
            }

            if (locationIndex < 0 || locationIndex >= sosLocations.Count)
            {
                locationIndex = nextSosIndex % sosLocations.Count;
                nextSosIndex++;
            }

            CivilianSOSLocation loc = sosLocations[locationIndex];

            // Enforce 55m starter protection
            float distFromCenter = Vector3.Distance(new Vector3(loc.locationPosition.x, 0, loc.locationPosition.z), Vector3.zero);
            if (distFromCenter < minSafeDistanceFromCenter)
            {
                Debug.LogError($"[DynamicWorldEventsDirector] Aborted SOS event: location '{loc.locationId}' is within {minSafeDistanceFromCenter}m of starter house ({distFromCenter:F1}m)!");
                return null;
            }

            Vector3 spawnPos = loc.locationPosition;
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 12f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject sosObj = new GameObject($"CivilianSOS_{loc.locationId}");
            sosObj.transform.SetParent(transform, true);

            CivilianSOSEvent sosEvent = sosObj.AddComponent<CivilianSOSEvent>();
            activeSOSEvents.Add(sosEvent);

            sosEvent.LaunchSOS(spawnPos, loc.locationId, loc.locationDisplayName);
            Debug.Log($"[DynamicWorldEventsDirector] Launched Civilian SOS encounter at '{loc.locationDisplayName}' ({spawnPos}).");

            return sosEvent;
        }

        /// <summary>
        /// Cancels and cleans up all active airdrops and civilian SOS encounters.
        /// </summary>
        public void CancelAllEvents()
        {
            for (int i = 0; i < activeAirdrops.Count; i++)
            {
                if (activeAirdrops[i] != null)
                {
                    Destroy(activeAirdrops[i].gameObject);
                }
            }
            activeAirdrops.Clear();

            for (int i = 0; i < activeSOSEvents.Count; i++)
            {
                if (activeSOSEvents[i] != null)
                {
                    activeSOSEvents[i].CancelEvent();
                    Destroy(activeSOSEvents[i].gameObject);
                }
            }
            activeSOSEvents.Clear();

            Debug.Log("[DynamicWorldEventsDirector] All active dynamic world events cancelled.");
        }
    }
}
