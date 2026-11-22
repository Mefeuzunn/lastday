using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.Missions;

namespace ProjectSurvival.UI
{
    public class FullTacticalMapUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IScrollHandler, IPointerClickHandler
    {
        public static FullTacticalMapUI Instance { get; private set; }

        [Header("Root & Panels")]
        [SerializeField] private GameObject mapPanelRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform viewportArea;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private RawImage baseMapImage;
        [SerializeField] private RawImage fogMapImage;

        [Header("Header Elements")]
        [SerializeField] private Text headerTitleText;
        [SerializeField] private Text explorationPercentText;

        [Header("Interactive Zoom & Navigation")]
        [SerializeField] private Button zoomInButton;
        [SerializeField] private Button zoomOutButton;
        [SerializeField] private Button recenterButton;
        [SerializeField] private Button clearWaypointButton;

        [Header("Location Info Card")]
        [SerializeField] private GameObject infoCardRoot;
        [SerializeField] private Text infoCardTitleText;
        [SerializeField] private Text infoCardDistanceText;
        [SerializeField] private Text infoCardStatusText;
        [SerializeField] private Button infoCardNavigateButton;

        [Header("Markers")]
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform objectiveMarker;
        [SerializeField] private RectTransform customWaypointMarker;
        [SerializeField] private Transform poiContainer;

        [Header("Map Scale Settings")]
        [SerializeField] private float worldSize = 720f; // -360m to +360m (720x720m)
        [SerializeField] private float baseContentSize = 720f;
        [SerializeField] private float minZoom = 1.0f;
        [SerializeField] private float maxZoom = 6.0f;

        private float currentZoom = 1.0f;
        private Transform playerTransform;
        private bool isMapOpen = false;
        private bool isDragging = false;
        private Vector2 dragStartPos;
        private LandmarkData selectedLandmark;
        private readonly List<(RectTransform rect, Vector2 worldPos)> sectorOverlayItems = new List<(RectTransform, Vector2)>();
        private RectTransform sectorGridOverlayRoot;

        public bool IsMapOpen => isMapOpen;

        public class LandmarkData
        {
            public string id;
            public string name;
            public Vector2 worldPos;
            public string statusTag;
            public Color color;
            public Sprite iconSprite;
            public RectTransform markerObj;
            public Image markerImage;
        }

        private readonly List<LandmarkData> allLandmarks = new List<LandmarkData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            worldSize = WorldMapConfig.TotalWorldSize;

            InitializeButtons();

            if (mapPanelRoot != null)
            {
                mapPanelRoot.SetActive(false);
            }
            if (infoCardRoot != null)
            {
                infoCardRoot.SetActive(false);
            }
        }

        private void InitializeButtons()
        {
            if (closeButton != null) closeButton.onClick.AddListener(CloseMap);
            if (zoomInButton != null) zoomInButton.onClick.AddListener(() => ChangeZoom(0.4f));
            if (zoomOutButton != null) zoomOutButton.onClick.AddListener(() => ChangeZoom(-0.4f));
            if (recenterButton != null) recenterButton.onClick.AddListener(RecenterOnPlayer);
            if (clearWaypointButton != null) clearWaypointButton.onClick.AddListener(OnClearWaypointClicked);
            if (infoCardNavigateButton != null) infoCardNavigateButton.onClick.AddListener(OnNavigateToSelectedClicked);
        }

        private void OnEnable()
        {
            GameEvents.OnMapToggleRequested += ToggleMap;
        }

        private void OnDisable()
        {
            GameEvents.OnMapToggleRequested -= ToggleMap;
        }

        private void Start()
        {
            FindPlayer();
            SetupSectorGridOverlay();
            SetupLandmarkDatabase();
            if (clearWaypointButton != null) clearWaypointButton.gameObject.SetActive(false);
        }

        private void SetupSectorGridOverlay()
        {
            if (mapContent == null || sectorGridOverlayRoot != null) return;

            GameObject rootObj = new GameObject("SectorGridOverlay", typeof(RectTransform));
            rootObj.transform.SetParent(mapContent, false);
            rootObj.transform.SetAsFirstSibling();
            sectorGridOverlayRoot = rootObj.GetComponent<RectTransform>();
            sectorGridOverlayRoot.anchorMin = new Vector2(0.5f, 0.5f);
            sectorGridOverlayRoot.anchorMax = new Vector2(0.5f, 0.5f);
            sectorGridOverlayRoot.pivot = new Vector2(0.5f, 0.5f);
            sectorGridOverlayRoot.anchoredPosition = Vector2.zero;
            sectorGridOverlayRoot.sizeDelta = mapContent.sizeDelta;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;

            var sectors = WorldMapConfig.GetAllSectors();
            for (int i = 0; i < sectors.Count; i++)
            {
                var s = sectors[i];
                GameObject sObj = new GameObject($"SectorBanner_{s.sectorId}", typeof(RectTransform));
                sObj.transform.SetParent(sectorGridOverlayRoot, false);

                RectTransform sRt = sObj.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.5f, 0.5f);
                sRt.anchorMax = new Vector2(0.5f, 0.5f);
                sRt.pivot = new Vector2(0.5f, 0.5f);
                sRt.sizeDelta = new Vector2(210f, 50f);

                Text sText = sObj.AddComponent<Text>();
                sText.font = defaultFont;
                sText.fontSize = 11;
                sText.fontStyle = FontStyle.Bold;
                sText.alignment = TextAnchor.MiddleCenter;
                sText.color = new Color(s.themeColor.r, s.themeColor.g, s.themeColor.b, 0.55f);
                sText.text = $"{s.turkishDisplayName.ToUpper()}\n<size=9>TEHLİKE: {new string('★', s.threatLevel)}{new string('☆', 5 - s.threatLevel)}</size>";
                sText.raycastTarget = false;

                Vector2 wPos = new Vector2(s.worldCenter.x, s.worldCenter.z);
                sRt.anchoredPosition = WorldToContentPos(wPos);
                sectorOverlayItems.Add((sRt, wPos));
            }
        }

        private void UpdateSectorGridOverlayPositions()
        {
            if (sectorGridOverlayRoot != null && mapContent != null)
            {
                sectorGridOverlayRoot.sizeDelta = mapContent.sizeDelta;
            }

            for (int i = 0; i < sectorOverlayItems.Count; i++)
            {
                var item = sectorOverlayItems[i];
                if (item.rect != null)
                {
                    item.rect.anchoredPosition = WorldToContentPos(item.worldPos);
                }
            }
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void SetupLandmarkDatabase()
        {
            allLandmarks.Clear();

            // Key Story & Survival Facilities (Central Sector)
            AddLandmark("clinic", "Eski Klinik (Hastane)", new Vector2(-36f, 32f), "GÜVENLİ BÖLGE", new Color(0.2f, 0.88f, 0.95f), "poi_hospital");
            AddLandmark("shelter", "Güvenli Sığınak", new Vector2(-35f, -42f), "GÜVENLİ BÖLGE", new Color(0.25f, 0.88f, 0.45f), "poi_shelter");
            AddLandmark("supermarket", "Süpermarket (Erzak Deposu)", new Vector2(32f, 30f), "KEŞFEDİLDİ", new Color(0.96f, 0.78f, 0.22f), "poi_shop");
            AddLandmark("pharmacy", "Eczane (İlaç Deposu)", new Vector2(30f, -48f), "KEŞFEDİLDİ", new Color(0.75f, 0.45f, 0.95f), "poi_hospital");
            AddLandmark("gas_station", "Benzin İstasyonu", new Vector2(65f, -65f), "KEŞFEDİLDİ", new Color(1.0f, 0.55f, 0.15f), "poi_gas");
            AddLandmark("police", "Polis Karantina Kontrol Noktası", new Vector2(0f, 15f), "KEŞFEDİLDİ", new Color(0.28f, 0.62f, 1.0f), "poi_police");

            // Story NPC Locations
            AddLandmark("npc_aras", "Dr. Aras", new Vector2(-28f, 32f), "HİKAYE KARAKTERİ", new Color(0.2f, 0.9f, 0.8f), "poi_npc");
            AddLandmark("npc_zeynep", "Zeynep", new Vector2(-48f, 12f), "HİKAYE KARAKTERİ", new Color(0.85f, 0.4f, 0.9f), "poi_npc");
            AddLandmark("npc_kerem", "Kerem", new Vector2(16f, -34f), "HİKAYE KARAKTERİ", new Color(1.0f, 0.75f, 0.25f), "poi_npc");
            AddLandmark("npc_elif", "Elif", new Vector2(40f, 32f), "HİKAYE KARAKTERİ", new Color(1.0f, 0.45f, 0.2f), "poi_npc");

            // Outer Sector 4 Cardinal Gateways
            AddLandmark("gate_north", "Kuzey Askeri Tahliye Kapısı", new Vector2(0f, 120f), "GEÇİT", new Color(1.0f, 0.55f, 0.15f), "poi_police");
            AddLandmark("gate_east", "Doğu Tren Garı Hemzemin Geçidi", new Vector2(120f, 0f), "GEÇİT", new Color(0.95f, 0.75f, 0.22f), "poi_police");
            AddLandmark("gate_west", "Batı Şehir Parkı Giriş Kapısı", new Vector2(-120f, 0f), "GEÇİT", new Color(0.2f, 0.75f, 0.35f), "poi_police");
            AddLandmark("gate_south", "Güney Metro & Meydan Bulvarı", new Vector2(0f, -120f), "GEÇİT", new Color(0.75f, 0.25f, 0.9f), "poi_police");

            // Outer Sector Starter Landmark POIs
            AddLandmark("north_overpass", "Kuzey Otoyol Viyadüğü", new Vector2(0f, 240f), "BİLİNMİYOR", new Color(1.0f, 0.55f, 0.15f), "poi_shelter");
            AddLandmark("north_fob_camp", "Askeri İleri Harekat Kampı (FOB)", new Vector2(40f, 220f), "ASKERİ BÖLGE", new Color(1.0f, 0.65f, 0.2f), "poi_shelter");
            AddLandmark("east_rail_depot", "Doğu Lojistik Tren Garı", new Vector2(215f, 0f), "BİLİNMİYOR", new Color(0.95f, 0.75f, 0.22f), "poi_shop");
            AddLandmark("east_warehouses", "Çelik Depolar & Atölye Kompleksi", new Vector2(305f, 0f), "SANAYİ", new Color(0.95f, 0.85f, 0.3f), "poi_shop");
            AddLandmark("west_lake_park", "Batı Şehir Ormanı & Gölet", new Vector2(-240f, 0f), "BİLİNMİYOR", new Color(0.2f, 0.75f, 0.35f), "poi_shelter");
            AddLandmark("south_metro_hub", "Güney Yeraltı Metro Terminali", new Vector2(0f, -240f), "BİLİNMİYOR", new Color(0.75f, 0.25f, 0.9f), "poi_shop");
            AddLandmark("northeast_harbor", "Kuzeydoğu Konteyner Limanı", new Vector2(240f, 240f), "BİLİNMİYOR", new Color(0.95f, 0.25f, 0.25f), "poi_gas");
            AddLandmark("northeast_fuel_depot", "Liman Yakıt Tankları Deposu", new Vector2(175f, 265f), "TEHLİKELİ", new Color(0.95f, 0.35f, 0.15f), "poi_gas");
            AddLandmark("northwest_lake", "Kuzeybatı Balıkçı İskelesi", new Vector2(-240f, 240f), "BİLİNMİYOR", new Color(0.2f, 0.85f, 0.9f), "poi_shelter");
            AddLandmark("southwest_suburbs", "Güneybatı Batık Banliyö Çatı İkmali", new Vector2(-240f, -240f), "BİLİNMİYOR", new Color(0.35f, 0.55f, 0.85f), "poi_shop");
            AddLandmark("southeast_power", "Güneydoğu Ana Trafo Merkezi", new Vector2(240f, -240f), "BİLİNMİYOR", new Color(1.0f, 0.15f, 0.15f), "poi_gas");
            AddLandmark("southeast_control_room", "Trafo Şebeke Kontrol Binası", new Vector2(265f, -260f), "ELEKTRİK ŞEBEKESİ", new Color(1.0f, 0.45f, 0.15f), "poi_police");

            SpawnAllLandmarkMarkers();
        }

        private void AddLandmark(string id, string name, Vector2 pos, string status, Color col, string spriteName)
        {
            Sprite sp = Resources.Load<Sprite>(spriteName) ??
                        AssetDatabaseLoadSprite($"Assets/_Project/UI/Sprites/Map/{spriteName}.png");

            allLandmarks.Add(new LandmarkData
            {
                id = id,
                name = name,
                worldPos = pos,
                statusTag = status,
                color = col,
                iconSprite = sp
            });
        }

        private Sprite AssetDatabaseLoadSprite(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }

        private void SpawnAllLandmarkMarkers()
        {
            Transform parent = poiContainer != null ? poiContainer : mapContent;
            if (parent == null) return;

            foreach (var lm in allLandmarks)
            {
                if (lm.markerObj != null) continue;

                GameObject mObj = new GameObject($"Marker_{lm.id}");
                mObj.transform.SetParent(parent, false);

                RectTransform rt = mObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = WorldToContentPos(lm.worldPos);
                rt.sizeDelta = new Vector2(28f, 28f);

                Image img = mObj.AddComponent<Image>();
                img.color = lm.color;
                if (lm.iconSprite != null) img.sprite = lm.iconSprite;
                img.raycastTarget = true;

                // Tapping marker triggers info card inspection
                Button btn = mObj.AddComponent<Button>();
                LandmarkData captured = lm;
                btn.onClick.AddListener(() => OnLandmarkClicked(captured));

                lm.markerObj = rt;
                lm.markerImage = img;
            }
        }

        public void RegisterDynamicWeaponMarker(string id, string name, Vector2 worldPos)
        {
            for (int i = 0; i < allLandmarks.Count; i++)
            {
                if (allLandmarks[i].id == id) return;
            }

            LandmarkData lm = new LandmarkData
            {
                id = id,
                name = name,
                worldPos = worldPos,
                statusTag = "SİLAH DEPOSU / CEPHANE",
                color = new Color(1.0f, 0.72f, 0.01f, 0.95f),
                iconSprite = null
            };

            Transform parent = poiContainer != null ? poiContainer : mapContent;
            if (parent != null)
            {
                GameObject mObj = new GameObject($"Marker_{lm.id}");
                mObj.transform.SetParent(parent, false);

                RectTransform rt = mObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = WorldToContentPos(lm.worldPos);
                rt.sizeDelta = new Vector2(24f, 24f);

                Image img = mObj.AddComponent<Image>();
                img.color = lm.color;
                img.raycastTarget = true;

                Button btn = mObj.AddComponent<Button>();
                LandmarkData captured = lm;
                btn.onClick.AddListener(() => OnLandmarkClicked(captured));

                lm.markerObj = rt;
                lm.markerImage = img;
            }

            allLandmarks.Add(lm);
        }

        public void UnregisterDynamicWeaponMarker(string id)
        {
            for (int i = allLandmarks.Count - 1; i >= 0; i--)
            {
                if (allLandmarks[i].id == id)
                {
                    if (allLandmarks[i].markerObj != null)
                    {
                        Destroy(allLandmarks[i].markerObj.gameObject);
                    }
                    allLandmarks.RemoveAt(i);
                    break;
                }
            }
        }

        public void RegisterVehicleMarker(string id, string name, Vector2 worldPos)
        {
            for (int i = 0; i < allLandmarks.Count; i++)
            {
                if (allLandmarks[i].id == id) return;
            }

            LandmarkData lm = new LandmarkData
            {
                id = id,
                name = name,
                worldPos = worldPos,
                statusTag = "SÜRÜLEBİLİR ARAÇ",
                color = new Color(0.0f, 0.94f, 1.0f, 0.95f),
                iconSprite = null
            };

            Transform parent = poiContainer != null ? poiContainer : mapContent;
            if (parent != null)
            {
                GameObject mObj = new GameObject($"Marker_{lm.id}");
                mObj.transform.SetParent(parent, false);

                RectTransform rt = mObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = WorldToContentPos(lm.worldPos);
                rt.sizeDelta = new Vector2(26f, 26f);

                Image img = mObj.AddComponent<Image>();
                img.color = lm.color;
                img.raycastTarget = true;

                Button btn = mObj.AddComponent<Button>();
                LandmarkData captured = lm;
                btn.onClick.AddListener(() => OnLandmarkClicked(captured));

                lm.markerObj = rt;
                lm.markerImage = img;
            }

            allLandmarks.Add(lm);
        }

        public void RegisterAirdropMarker(string id, string name, Vector2 worldPos)
        {
            for (int i = 0; i < allLandmarks.Count; i++)
            {
                if (allLandmarks[i].id == id) return;
            }

            LandmarkData lm = new LandmarkData
            {
                id = id,
                name = name,
                worldPos = worldPos,
                statusTag = "HAVA İKMALİ (PARAŞÜT)",
                color = new Color(0.18f, 0.85f, 0.44f, 0.95f),
                iconSprite = null
            };

            Transform parent = poiContainer != null ? poiContainer : mapContent;
            if (parent != null)
            {
                GameObject mObj = new GameObject($"Marker_{lm.id}");
                mObj.transform.SetParent(parent, false);

                RectTransform rt = mObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = WorldToContentPos(lm.worldPos);
                rt.sizeDelta = new Vector2(28f, 28f);

                Image img = mObj.AddComponent<Image>();
                img.color = lm.color;
                img.raycastTarget = true;

                Button btn = mObj.AddComponent<Button>();
                LandmarkData captured = lm;
                btn.onClick.AddListener(() => OnLandmarkClicked(captured));

                lm.markerObj = rt;
                lm.markerImage = img;
            }

            allLandmarks.Add(lm);
        }

        public void RegisterSOSMarker(string id, string name, Vector2 worldPos)
        {
            for (int i = 0; i < allLandmarks.Count; i++)
            {
                if (allLandmarks[i].id == id) return;
            }

            LandmarkData lm = new LandmarkData
            {
                id = id,
                name = name,
                worldPos = worldPos,
                statusTag = "ACİL SOS (SİVİL KUŞATILDI)",
                color = new Color(0.2f, 0.9f, 0.95f, 0.95f),
                iconSprite = null
            };

            Transform parent = poiContainer != null ? poiContainer : mapContent;
            if (parent != null)
            {
                GameObject mObj = new GameObject($"Marker_{lm.id}");
                mObj.transform.SetParent(parent, false);

                RectTransform rt = mObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = WorldToContentPos(lm.worldPos);
                rt.sizeDelta = new Vector2(28f, 28f);

                Image img = mObj.AddComponent<Image>();
                img.color = lm.color;
                img.raycastTarget = true;

                Button btn = mObj.AddComponent<Button>();
                LandmarkData captured = lm;
                btn.onClick.AddListener(() => OnLandmarkClicked(captured));

                lm.markerObj = rt;
                lm.markerImage = img;
            }

            allLandmarks.Add(lm);
        }

        public void UnregisterLandmarkMarker(string id)
        {
            for (int i = allLandmarks.Count - 1; i >= 0; i--)
            {
                if (allLandmarks[i].id == id)
                {
                    if (allLandmarks[i].markerObj != null)
                    {
                        Destroy(allLandmarks[i].markerObj.gameObject);
                    }
                    allLandmarks.RemoveAt(i);
                    break;
                }
            }
        }

        public void RegisterSecretCacheMarker(string id, string name, Vector2 worldPos)
        {
            for (int i = 0; i < allLandmarks.Count; i++)
            {
                if (allLandmarks[i].id == id) return;
            }

            LandmarkData lm = new LandmarkData
            {
                id = id,
                name = name,
                worldPos = worldPos,
                statusTag = "GİZLİ TELSİZ ZULASI",
                color = new Color(0.2f, 0.98f, 0.5f, 0.95f), // Emerald green
                iconSprite = null
            };

            Transform parent = poiContainer != null ? poiContainer : mapContent;
            if (parent != null)
            {
                GameObject mObj = new GameObject($"Marker_{lm.id}");
                mObj.transform.SetParent(parent, false);

                RectTransform rt = mObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = WorldToContentPos(lm.worldPos);
                rt.sizeDelta = new Vector2(24f, 24f);

                Image img = mObj.AddComponent<Image>();
                img.color = lm.color;
                img.raycastTarget = true;

                Button btn = mObj.AddComponent<Button>();
                LandmarkData captured = lm;
                btn.onClick.AddListener(() => OnLandmarkClicked(captured));

                lm.markerObj = rt;
                lm.markerImage = img;
            }

            allLandmarks.Add(lm);
        }

        public void UnregisterSecretCacheMarker(string id)
        {
            for (int i = allLandmarks.Count - 1; i >= 0; i--)
            {
                if (allLandmarks[i].id == id)
                {
                    if (allLandmarks[i].markerObj != null)
                    {
                        Destroy(allLandmarks[i].markerObj.gameObject);
                    }
                    allLandmarks.RemoveAt(i);
                    break;
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleMap();
            }
            if (isMapOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseMap();
            }

            if (isMapOpen)
            {
                UpdatePlayerMarker();
                UpdateObjectiveMarker();
                UpdateCustomWaypointMarker();
                UpdateFogAndLandmarkVisibilities();
                UpdateHeaderStats();
            }
        }

        public void ToggleMap()
        {
            if (isMapOpen) CloseMap();
            else OpenMap();
        }

        public void OpenMap()
        {
            ModalWindowManager.Instance?.NotifyModalOpened(this);
            FindPlayer();
            isMapOpen = true;

            if (mapPanelRoot != null)
            {
                mapPanelRoot.SetActive(true);
            }

            // Sync live Fog of War texture
            if (FogOfWarManager.Instance != null && fogMapImage != null)
            {
                fogMapImage.texture = FogOfWarManager.Instance.FogTexture;
            }

            RecenterOnPlayer();
            UpdatePlayerMarker();
            UpdateObjectiveMarker();
            UpdateCustomWaypointMarker();
            UpdateFogAndLandmarkVisibilities();
            UpdateHeaderStats();
        }

        public void CloseMap()
        {
            ModalWindowManager.Instance?.NotifyModalClosed(this);
            isMapOpen = false;
            if (mapPanelRoot != null)
            {
                mapPanelRoot.SetActive(false);
            }
            if (infoCardRoot != null)
            {
                infoCardRoot.SetActive(false);
            }
        }

        private void UpdateHeaderStats()
        {
            if (explorationPercentText != null && FogOfWarManager.Instance != null)
            {
                float pct = FogOfWarManager.Instance.ExplorationPercent;
                explorationPercentText.text = $"KEŞİF: %{Mathf.RoundToInt(pct)}";
            }
        }

        private void UpdatePlayerMarker()
        {
            if (playerMarker == null || playerTransform == null) return;

            Vector2 cPos = WorldToContentPos(new Vector2(playerTransform.position.x, playerTransform.position.z));
            playerMarker.anchoredPosition = cPos;

            float yaw = playerTransform.eulerAngles.y;
            playerMarker.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        }

        private void UpdateObjectiveMarker()
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
                objectiveMarker.anchoredPosition = WorldToContentPos(new Vector2(target.Value.x, target.Value.z));

                float pulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 5f);
                objectiveMarker.localScale = new Vector3(pulse, pulse, 1f);
            }
            else
            {
                objectiveMarker.gameObject.SetActive(false);
            }
        }

        private void UpdateCustomWaypointMarker()
        {
            if (customWaypointMarker == null) return;

            bool hasWp = CustomWaypointManager.Instance != null && CustomWaypointManager.Instance.HasCustomWaypoint;
            if (hasWp)
            {
                customWaypointMarker.gameObject.SetActive(true);
                Vector3 wp = CustomWaypointManager.Instance.WaypointPosition;
                customWaypointMarker.anchoredPosition = WorldToContentPos(new Vector2(wp.x, wp.z));

                float pulse = 1f + 0.18f * Mathf.Sin(Time.unscaledTime * 6f);
                customWaypointMarker.localScale = new Vector3(pulse, pulse, 1f);

                if (clearWaypointButton != null) clearWaypointButton.gameObject.SetActive(true);
            }
            else
            {
                customWaypointMarker.gameObject.SetActive(false);
                if (clearWaypointButton != null) clearWaypointButton.gameObject.SetActive(false);
            }
        }

        private void UpdateFogAndLandmarkVisibilities()
        {
            bool hasFog = FogOfWarManager.Instance != null;

            foreach (var lm in allLandmarks)
            {
                if (lm.markerObj == null) continue;

                // Position update based on content scale
                lm.markerObj.anchoredPosition = WorldToContentPos(lm.worldPos);

                // Check active mission target
                bool isActiveMission = MissionManager.Instance != null && MissionManager.Instance.HasActiveMission &&
                    MissionManager.Instance.ActiveObjectivePosition.HasValue &&
                    Vector2.Distance(lm.worldPos, new Vector2(MissionManager.Instance.ActiveObjectivePosition.Value.x, MissionManager.Instance.ActiveObjectivePosition.Value.z)) < 15f;

                bool isExplored = !hasFog || FogOfWarManager.Instance.IsWorldPosExplored(lm.worldPos, 0.6f);
                bool shouldShow = isExplored || isActiveMission;

                lm.markerObj.gameObject.SetActive(shouldShow);
            }
        }

        public Vector2 WorldToContentPos(Vector2 worldPos)
        {
            float halfWorld = worldSize * 0.5f; // 120m
            float currentContentSize = baseContentSize * currentZoom;
            float halfContent = currentContentSize * 0.5f;

            float nx = Mathf.Clamp(worldPos.x, -halfWorld, halfWorld) / halfWorld;
            float ny = Mathf.Clamp(worldPos.y, -halfWorld, halfWorld) / halfWorld;

            return new Vector2(nx * halfContent, ny * halfContent);
        }

        public Vector2 ContentToWorldPos(Vector2 contentPos)
        {
            float halfWorld = worldSize * 0.5f;
            float currentContentSize = baseContentSize * currentZoom;
            float halfContent = currentContentSize * 0.5f;

            float nx = Mathf.Clamp(contentPos.x, -halfContent, halfContent) / halfContent;
            float ny = Mathf.Clamp(contentPos.y, -halfContent, halfContent) / halfContent;

            return new Vector2(nx * halfWorld, ny * halfWorld);
        }

        // =========================================================================
        // INTERACTIVE PAN, ZOOM & WAYPOINT PLACEMENT
        // =========================================================================
        public void OnScroll(PointerEventData eventData)
        {
            float scroll = eventData.scrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                ChangeZoom(scroll * 0.25f);
            }
        }

        public void ChangeZoom(float delta)
        {
            float prevZoom = currentZoom;
            currentZoom = Mathf.Clamp(currentZoom + delta, minZoom, maxZoom);

            if (mapContent != null)
            {
                mapContent.sizeDelta = new Vector2(baseContentSize * currentZoom, baseContentSize * currentZoom);
            }

            ClampContentPosition();
            UpdateFogAndLandmarkVisibilities();
            UpdateSectorGridOverlayPositions();
        }

        public void RecenterOnPlayer()
        {
            if (playerTransform == null || mapContent == null) return;

            Vector2 pWorld = new Vector2(playerTransform.position.x, playerTransform.position.z);
            Vector2 pContent = WorldToContentPos(pWorld);

            mapContent.anchoredPosition = -pContent;
            ClampContentPosition();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            dragStartPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (mapContent == null) return;

            mapContent.anchoredPosition += eventData.delta;
            ClampContentPosition();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        private void ClampContentPosition()
        {
            if (mapContent == null || viewportArea == null) return;

            float contentHalfW = (baseContentSize * currentZoom) * 0.5f;
            float viewHalfW = viewportArea.rect.width * 0.5f;

            float maxOffsetX = Mathf.Max(0f, contentHalfW - viewHalfW);
            float maxOffsetY = Mathf.Max(0f, contentHalfW - viewHalfW);

            Vector2 pos = mapContent.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, -maxOffsetX, maxOffsetX);
            pos.y = Mathf.Clamp(pos.y, -maxOffsetY, maxOffsetY);
            mapContent.anchoredPosition = pos;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // If user was dragging, do not treat as a waypoint click
            if (Vector2.Distance(eventData.position, dragStartPos) > 10f && isDragging)
            {
                return;
            }

            // Convert click position to content space
            if (mapContent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(mapContent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                Vector2 worldCoords = ContentToWorldPos(localPoint);

                if (CustomWaypointManager.Instance != null)
                {
                    CustomWaypointManager.Instance.SetWaypoint(new Vector3(worldCoords.x, 0f, worldCoords.y));
                    UpdateCustomWaypointMarker();
                }
            }
        }

        private void OnClearWaypointClicked()
        {
            if (CustomWaypointManager.Instance != null)
            {
                CustomWaypointManager.Instance.ClearWaypoint();
            }
            UpdateCustomWaypointMarker();
        }

        // =========================================================================
        // POI INSPECTION INFO CARD
        // =========================================================================
        private void OnLandmarkClicked(LandmarkData lm)
        {
            selectedLandmark = lm;
            if (infoCardRoot != null)
            {
                infoCardRoot.SetActive(true);
            }

            if (infoCardTitleText != null) infoCardTitleText.text = lm.name;
            if (infoCardStatusText != null) infoCardStatusText.text = $"[{lm.statusTag}]";

            if (infoCardDistanceText != null && playerTransform != null)
            {
                float dist = Vector2.Distance(new Vector2(playerTransform.position.x, playerTransform.position.z), lm.worldPos);
                infoCardDistanceText.text = $"{Mathf.RoundToInt(dist)}m uzaklıkta";
            }
        }

        private void OnNavigateToSelectedClicked()
        {
            if (selectedLandmark == null) return;

            if (CustomWaypointManager.Instance != null)
            {
                CustomWaypointManager.Instance.SetWaypoint(new Vector3(selectedLandmark.worldPos.x, 0f, selectedLandmark.worldPos.y));
                UpdateCustomWaypointMarker();
            }

            if (infoCardRoot != null)
            {
                infoCardRoot.SetActive(false);
            }
        }

        // =========================================================================
        // SCENEBUILDER CONFIGURATION BINDING
        // =========================================================================
        public void Configure(GameObject root, Button closeBtn, RectTransform viewport, RectTransform content,
            RawImage baseMap, RawImage fogMap, RectTransform pMarker, RectTransform objMarker, RectTransform wpMarker,
            Text titleTxt, Text expTxt, Button zInBtn, Button zOutBtn, Button recenterBtn, Button clearWpBtn,
            GameObject cardRoot, Text cardTitle, Text cardDist, Text cardStatus, Button cardNavBtn)
        {
            mapPanelRoot = root;
            closeButton = closeBtn;
            viewportArea = viewport;
            mapContent = content;
            baseMapImage = baseMap;
            fogMapImage = fogMap;
            playerMarker = pMarker;
            objectiveMarker = objMarker;
            customWaypointMarker = wpMarker;

            headerTitleText = titleTxt;
            explorationPercentText = expTxt;
            zoomInButton = zInBtn;
            zoomOutButton = zOutBtn;
            recenterButton = recenterBtn;
            clearWaypointButton = clearWpBtn;

            infoCardRoot = cardRoot;
            infoCardTitleText = cardTitle;
            infoCardDistanceText = cardDist;
            infoCardStatusText = cardStatus;
            infoCardNavigateButton = cardNavBtn;

            InitializeButtons();
        }
    }
}
