using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.AI;
using ProjectSurvival.Environment;
using ProjectSurvival.Missions;

namespace ProjectSurvival.UI
{
    public class MinimapHUDWidget : MonoBehaviour
    {
        public static MinimapHUDWidget Instance { get; private set; }

        [Header("Minimap Core Hierarchy")]
        [SerializeField] private RectTransform minimapRoot;
        [SerializeField] private Button openMapButton;
        [SerializeField] private RectTransform maskContainer;
        [SerializeField] private RectTransform mapRotator;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private RawImage baseMapImage;
        [SerializeField] private RawImage fogMapImage;

        [Header("Bezel & Overlays")]
        [SerializeField] private Image frameImage;
        [SerializeField] private RectTransform playerBlip;
        [SerializeField] private Text cardinalNorth;
        [SerializeField] private Text cardinalEast;
        [SerializeField] private Text cardinalSouth;
        [SerializeField] private Text cardinalWest;
        [SerializeField] private Text fullMapBadgeText;

        [Header("Dynamic Markers")]
        [SerializeField] private RectTransform objectiveMarker;
        [SerializeField] private RectTransform customWaypointMarker;
        [SerializeField] private Transform poiMarkerContainer;
        [SerializeField] private Transform zombieBlipContainer;

        [Header("Scale & Zoom")]
        [SerializeField] private float visibleStreetDiameter = 75f; // Shows ~75x75m around player
        [SerializeField] private float mapUiSize = 164f; // Viewport mask size
        [SerializeField] private float acousticAlertRadius = 25f; // Zombie hearing radar radius

        private Camera mainCamera;
        private Transform playerTransform;
        private float metersToUiScale; // 164 / 75 = ~2.186f

        private RectTransform screamerPulseRing;
        private Image screamerPulseImage;
        private Coroutine screamerPulseCoroutine;

        private struct MinimapPoi
        {
            public string id;
            public Vector2 worldPos;
            public RectTransform markerObj;
        }

        private readonly List<MinimapPoi> minimapPois = new List<MinimapPoi>();
        private readonly List<RectTransform> pooledZombieBlips = new List<RectTransform>();
        private Sprite zombieBlipSprite;
        private Sprite customPinSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            metersToUiScale = mapUiSize / visibleStreetDiameter;

            if (openMapButton != null)
            {
                openMapButton.onClick.AddListener(OnMinimapClicked);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnHordeAlertTriggered += HandleHordeAlert;
            GameEvents.OnScavengerSquadDistress += HandleDistressCall;
        }

        private void OnDisable()
        {
            GameEvents.OnHordeAlertTriggered -= HandleHordeAlert;
            GameEvents.OnScavengerSquadDistress -= HandleDistressCall;
        }

        private void Start()
        {
            mainCamera = Camera.main;
            FindPlayer();

            // Scale content to accommodate 720x720m world
            float totalContentSize = WorldMapConfig.TotalWorldSize * metersToUiScale;
            if (mapContent != null) mapContent.sizeDelta = new Vector2(totalContentSize, totalContentSize);
            if (baseMapImage != null) baseMapImage.rectTransform.sizeDelta = new Vector2(totalContentSize, totalContentSize);
            if (fogMapImage != null) fogMapImage.rectTransform.sizeDelta = new Vector2(totalContentSize, totalContentSize);

            LoadSprites();
            SetupMinimapPois();
            SetupZombieBlipPool(28);
            SetupScreamerPulseRing();

            // Connect Fog texture
            if (FogOfWarManager.Instance != null && fogMapImage != null)
            {
                fogMapImage.texture = FogOfWarManager.Instance.FogTexture;
            }
        }

        private void SetupScreamerPulseRing()
        {
            Transform parent = zombieBlipContainer != null ? zombieBlipContainer : mapContent;
            if (parent == null) return;

            GameObject ringObj = new GameObject("Minimap_ScreamerPulseRing");
            ringObj.transform.SetParent(parent, false);

            screamerPulseRing = ringObj.AddComponent<RectTransform>();
            screamerPulseRing.anchorMin = new Vector2(0.5f, 0.5f);
            screamerPulseRing.anchorMax = new Vector2(0.5f, 0.5f);
            screamerPulseRing.pivot = new Vector2(0.5f, 0.5f);
            screamerPulseRing.sizeDelta = new Vector2(20f, 20f);

            screamerPulseImage = ringObj.AddComponent<Image>();
            screamerPulseImage.color = new Color(0.9f, 0.15f, 0.85f, 0.75f);
            screamerPulseImage.raycastTarget = false;
            if (zombieBlipSprite != null) screamerPulseImage.sprite = zombieBlipSprite;

            ringObj.SetActive(false);
        }

        private void HandleHordeAlert(Vector3 center, float radius)
        {
            if (screamerPulseRing == null) return;
            if (screamerPulseCoroutine != null) StopCoroutine(screamerPulseCoroutine);
            screamerPulseCoroutine = StartCoroutine(AnimateMinimapScreamerRing(center, radius));
        }

        private void HandleDistressCall(Vector3 center, string message, float duration)
        {
            HandleHordeAlert(center, 45f);
        }

        private System.Collections.IEnumerator AnimateMinimapScreamerRing(Vector3 center, float maxRadius)
        {
            screamerPulseRing.gameObject.SetActive(true);
            screamerPulseRing.anchoredPosition = new Vector2(center.x * metersToUiScale, center.z * metersToUiScale);

            float dur = 0.85f;
            float elapsed = 0f;
            float maxUiDiameter = maxRadius * 2f * metersToUiScale;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
                float curSize = Mathf.Lerp(12f, maxUiDiameter, easeOut);
                screamerPulseRing.sizeDelta = new Vector2(curSize, curSize);

                if (screamerPulseImage != null)
                {
                    screamerPulseImage.color = new Color(0.9f, 0.15f, 0.85f, (1f - t) * 0.75f);
                }

                yield return null;
            }

            screamerPulseRing.gameObject.SetActive(false);
            screamerPulseCoroutine = null;
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void LoadSprites()
        {
#if UNITY_EDITOR
            zombieBlipSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/Map/zombie_radar_blip.png");
            customPinSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/Map/waypoint_custom_pin.png");
#endif
        }

        private void SetupMinimapPois()
        {
            Transform parent = poiMarkerContainer != null ? poiMarkerContainer : mapContent;
            if (parent == null) return;

            (string id, Vector2 pos, string iconName, Color col)[] pois = new[]
            {
                ("clinic", new Vector2(-36f, 32f), "poi_hospital", new Color(0.2f, 0.88f, 0.95f)),
                ("shelter", new Vector2(-35f, -42f), "poi_shelter", new Color(0.25f, 0.88f, 0.45f)),
                ("supermarket", new Vector2(32f, 30f), "poi_shop", new Color(0.96f, 0.78f, 0.22f)),
                ("pharmacy", new Vector2(30f, -48f), "poi_hospital", new Color(0.75f, 0.45f, 0.95f)),
                ("gas_station", new Vector2(65f, -65f), "poi_gas", new Color(1.0f, 0.55f, 0.15f)),
                ("police", new Vector2(0f, 15f), "poi_police", new Color(0.28f, 0.62f, 1.0f)),
                ("npc_aras", new Vector2(-28f, 32f), "poi_npc", new Color(0.2f, 0.9f, 0.8f)),
                ("npc_zeynep", new Vector2(-48f, 12f), "poi_npc", new Color(0.85f, 0.4f, 0.9f)),
                ("npc_kerem", new Vector2(16f, -34f), "poi_npc", new Color(1.0f, 0.75f, 0.25f)),
                ("npc_elif", new Vector2(40f, 32f), "poi_npc", new Color(1.0f, 0.45f, 0.2f)),
                // 4 Perimeter Checkpoint Gateways
                ("gate_north", new Vector2(0f, 120f), "poi_police", new Color(1.0f, 0.55f, 0.15f)),
                ("gate_east", new Vector2(120f, 0f), "poi_police", new Color(0.95f, 0.75f, 0.22f)),
                ("gate_west", new Vector2(-120f, 0f), "poi_police", new Color(0.2f, 0.75f, 0.35f)),
                ("gate_south", new Vector2(0f, -120f), "poi_police", new Color(0.75f, 0.25f, 0.9f)),
                // Phase 2 Outer Landmarks
                ("north_fob_camp", new Vector2(40f, 220f), "poi_shelter", new Color(1.0f, 0.65f, 0.2f)),
                ("east_warehouses", new Vector2(305f, 0f), "poi_shop", new Color(0.95f, 0.85f, 0.3f)),
                ("northeast_fuel_depot", new Vector2(175f, 265f), "poi_gas", new Color(0.95f, 0.35f, 0.15f)),
                ("southeast_control_room", new Vector2(265f, -260f), "poi_police", new Color(1.0f, 0.45f, 0.15f)),
                // Phase 3 Outer Landmarks
                ("west_forest_shed", new Vector2(-230f, 25f), "poi_shelter", new Color(0.2f, 0.75f, 0.35f)),
                ("northwest_fire_tower", new Vector2(-260f, 260f), "poi_shelter", new Color(0.2f, 0.85f, 0.9f)),
                ("south_mall_concourse", new Vector2(0f, -280f), "poi_shop", new Color(0.75f, 0.25f, 0.9f)),
                ("southwest_flooded_dock", new Vector2(-240f, -240f), "poi_shelter", new Color(0.35f, 0.55f, 0.85f))
            };

            foreach (var p in pois)
            {
                GameObject obj = new GameObject($"Minimap_POI_{p.id}");
                obj.transform.SetParent(parent, false);

                RectTransform rt = obj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(p.pos.x * metersToUiScale, p.pos.y * metersToUiScale);
                rt.sizeDelta = new Vector2(18f, 18f);

                Image img = obj.AddComponent<Image>();
                img.color = p.col;
                img.raycastTarget = false;

#if UNITY_EDITOR
                Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/UI/Sprites/Map/{p.iconName}.png");
                if (sp != null) img.sprite = sp;
#endif

                minimapPois.Add(new MinimapPoi
                {
                    id = p.id,
                    worldPos = p.pos,
                    markerObj = rt
                });
            }
        }

        private void SetupZombieBlipPool(int count)
        {
            Transform parent = zombieBlipContainer != null ? zombieBlipContainer : mapContent;
            if (parent == null) return;

            for (int i = 0; i < count; i++)
            {
                GameObject bObj = new GameObject($"ZombieBlip_{i}");
                bObj.transform.SetParent(parent, false);

                RectTransform rt = bObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(14f, 14f);

                Image img = bObj.AddComponent<Image>();
                img.color = new Color(1.0f, 0.15f, 0.20f, 0.95f);
                img.raycastTarget = false;
                if (zombieBlipSprite != null) img.sprite = zombieBlipSprite;

                bObj.SetActive(false);
                pooledZombieBlips.Add(rt);
            }
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            Vector3 pPos = playerTransform.position;
            float camYaw = mainCamera.transform.eulerAngles.y;
            float playerYaw = playerTransform.eulerAngles.y;

            // 1. Rotate the map content with camera heading
            if (mapRotator != null)
            {
                mapRotator.localRotation = Quaternion.Euler(0f, 0f, camYaw);
            }

            // 2. Pan map content centered on player
            if (mapContent != null)
            {
                mapContent.anchoredPosition = new Vector2(-pPos.x * metersToUiScale, -pPos.z * metersToUiScale);
            }

            // 3. Update Player Direction Chevron (relative to camera yaw)
            if (playerBlip != null)
            {
                float relativeYaw = playerYaw - camYaw;
                playerBlip.localRotation = Quaternion.Euler(0f, 0f, -relativeYaw);
            }

            // 4. Update Rotating Cardinal Direction Markers on Bezel
            UpdateCardinalMarkers(camYaw);

            // 5. Update POI Visibilities based on Fog of War
            UpdatePoiVisibilities();

            // 6. Update Active Objective Beacon
            UpdateObjectiveBeacon(pPos);

            // 7. Update Custom Waypoint Marker
            UpdateCustomWaypoint(pPos);

            // 8. Update Acoustic Zombie Contacts
            UpdateZombieContacts(pPos);
        }

        private void UpdateCardinalMarkers(float camYaw)
        {
            float radius = 76f;
            float angleNorth = -camYaw;

            void PlaceCardinal(Text t, float baseAngle)
            {
                if (t == null) return;
                float a = (angleNorth + baseAngle) * Mathf.Deg2Rad;
                t.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(a) * radius, Mathf.Cos(a) * radius);
            }

            PlaceCardinal(cardinalNorth, 0f);   // Kuzey (K)
            PlaceCardinal(cardinalEast, 90f);   // Doğu (D)
            PlaceCardinal(cardinalSouth, 180f); // Güney (G)
            PlaceCardinal(cardinalWest, 270f);  // Batı (B)
        }

        private void UpdatePoiVisibilities()
        {
            bool hasFog = FogOfWarManager.Instance != null;

            foreach (var poi in minimapPois)
            {
                if (poi.markerObj == null) continue;

                bool isExplored = !hasFog || FogOfWarManager.Instance.IsWorldPosExplored(poi.worldPos, 0.6f);
                bool isMissionTarget = MissionManager.Instance != null && MissionManager.Instance.HasActiveMission &&
                    MissionManager.Instance.ActiveObjectivePosition.HasValue &&
                    Vector2.Distance(poi.worldPos, new Vector2(MissionManager.Instance.ActiveObjectivePosition.Value.x, MissionManager.Instance.ActiveObjectivePosition.Value.z)) < 15f;

                poi.markerObj.gameObject.SetActive(isExplored || isMissionTarget);
            }
        }

        private void UpdateObjectiveBeacon(Vector3 pPos)
        {
            if (objectiveMarker == null) return;

            Vector3? target = null;
            if (MissionManager.Instance != null && MissionManager.Instance.HasActiveMission)
            {
                target = MissionManager.Instance.ActiveObjectivePosition;
            }

            if (target.HasValue)
            {
                objectiveMarker.gameObject.SetActive(true);
                objectiveMarker.anchoredPosition = new Vector2(target.Value.x * metersToUiScale, target.Value.z * metersToUiScale);

                float pulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 5f);
                objectiveMarker.localScale = new Vector3(pulse, pulse, 1f);
            }
            else
            {
                objectiveMarker.gameObject.SetActive(false);
            }
        }

        private void UpdateCustomWaypoint(Vector3 pPos)
        {
            if (customWaypointMarker == null) return;

            bool hasWp = CustomWaypointManager.Instance != null && CustomWaypointManager.Instance.HasCustomWaypoint;
            if (hasWp)
            {
                customWaypointMarker.gameObject.SetActive(true);
                Vector3 wp = CustomWaypointManager.Instance.WaypointPosition;
                customWaypointMarker.anchoredPosition = new Vector2(wp.x * metersToUiScale, wp.z * metersToUiScale);

                float pulse = 1f + 0.18f * Mathf.Sin(Time.unscaledTime * 6f);
                customWaypointMarker.localScale = new Vector3(pulse, pulse, 1f);
            }
            else
            {
                customWaypointMarker.gameObject.SetActive(false);
            }
        }

        private void UpdateZombieContacts(Vector3 pPos)
        {
            int blipIndex = 0;
            var zombies = ZombieAI.ActiveZombies;

            for (int i = 0; i < zombies.Count && blipIndex < pooledZombieBlips.Count; i++)
            {
                var z = zombies[i];
                if (z == null || z.IsDead) continue;

                // Only show alert/chasing/investigating zombies within hearing radius
                if (z.CurrentState == ZombieState.Chase || z.CurrentState == ZombieState.Attack || z.CurrentState == ZombieState.Investigate)
                {
                    float dist = Vector3.Distance(pPos, z.transform.position);
                    if (dist <= acousticAlertRadius)
                    {
                        var blip = pooledZombieBlips[blipIndex++];
                        blip.gameObject.SetActive(true);
                        blip.anchoredPosition = new Vector2(z.transform.position.x * metersToUiScale, z.transform.position.z * metersToUiScale);

                        // Pulsing crimson contact
                        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 8f);
                        blip.localScale = new Vector3(pulse, pulse, 1f);
                    }
                }
            }

            // Deactivate remaining blips in pool
            for (int j = blipIndex; j < pooledZombieBlips.Count; j++)
            {
                pooledZombieBlips[j].gameObject.SetActive(false);
            }
        }

        public void RegisterVehicleMarker(string id, string name, Vector2 worldPos)
        {
            RegisterDynamicPoi(id, worldPos, new Color(0.22f, 0.75f, 0.95f, 0.95f));
        }

        public void RegisterDynamicPoi(string id, Vector2 worldPos, Color color)
        {
            for (int i = 0; i < minimapPois.Count; i++)
            {
                if (minimapPois[i].id == id) return;
            }

            Transform parent = poiMarkerContainer != null ? poiMarkerContainer : mapContent;
            if (parent == null) return;

            GameObject obj = new GameObject($"DynamicPOI_{id}");
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(worldPos.x * metersToUiScale, worldPos.y * metersToUiScale);
            rt.sizeDelta = new Vector2(16f, 16f);

            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            minimapPois.Add(new MinimapPoi
            {
                id = id,
                worldPos = worldPos,
                markerObj = rt
            });
        }

        public void UnregisterDynamicPoi(string id)
        {
            for (int i = minimapPois.Count - 1; i >= 0; i--)
            {
                if (minimapPois[i].id == id)
                {
                    if (minimapPois[i].markerObj != null)
                    {
                        Destroy(minimapPois[i].markerObj.gameObject);
                    }
                    minimapPois.RemoveAt(i);
                    break;
                }
            }
        }

        private void OnMinimapClicked()
        {
            if (FullTacticalMapUI.Instance != null)
            {
                FullTacticalMapUI.Instance.ToggleMap();
            }
        }

        // =========================================================================
        // SCENEBUILDER BINDING
        // =========================================================================
        public void Configure(RectTransform root, Button btn, RectTransform mask, RectTransform rotator,
            RectTransform content, RawImage baseMap, RawImage fogMap, Image frame, RectTransform blip,
            Text kText, Text dText, Text gText, Text bText, Text badgeTxt,
            RectTransform objMarker, RectTransform wpMarker, Transform poiContainer, Transform zombieContainer)
        {
            minimapRoot = root;
            openMapButton = btn;
            maskContainer = mask;
            mapRotator = rotator;
            mapContent = content;
            baseMapImage = baseMap;
            fogMapImage = fogMap;
            frameImage = frame;
            playerBlip = blip;

            cardinalNorth = kText;
            cardinalEast = dText;
            cardinalSouth = gText;
            cardinalWest = bText;
            fullMapBadgeText = badgeTxt;

            objectiveMarker = objMarker;
            customWaypointMarker = wpMarker;
            poiMarkerContainer = poiContainer;
            zombieBlipContainer = zombieContainer;

            if (openMapButton != null)
            {
                openMapButton.onClick.RemoveAllListeners();
                openMapButton.onClick.AddListener(OnMinimapClicked);
            }
        }
    }
}
