using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;

namespace ProjectSurvival.AI
{
    public enum CityDistrict
    {
        SafeHouseBuffer,
        QuarantineCore,
        IndustrialGasStation,
        ResidentialStealth,
        NeutralCity,
        NorthEvacuationHighway,
        EastRailYard,
        NorthEastHarbor,
        SouthEastSubstation,
        WestUrbanForest,
        NorthWestLakefront,
        SouthSubwayMall,
        SouthWestFloodedSuburbs
    }

    public enum DistrictThreatLevel
    {
        Low,
        Moderate,
        High,
        Extreme
    }

    [Serializable]
    public class DistrictZone
    {
        public CityDistrict districtType;
        public string displayName;
        public string subTitle;
        public DistrictThreatLevel threatLevel;
        public Color themeColor;
        public Vector3 center;
        public Vector3 size;
        public float radius; // Used if size == Vector3.zero
        public bool isSpherical;

        public bool Contains(Vector3 position)
        {
            if (isSpherical)
            {
                Vector3 diff = position - center;
                diff.y = 0;
                return diff.sqrMagnitude <= (radius * radius);
            }
            else
            {
                Bounds b = new Bounds(center, size);
                return b.Contains(position);
            }
        }
    }

    /// <summary>
    /// District-Based Zombie Ecology.
    /// Manages territorial threat zones across the 240x240m city:
    /// - Safe House Buffer Zone: 28m perimeter around (-36, 0, -30), low threat, prevents spawns.
    /// - Quarantine Core: Clinic & Police Station area (-25, 0, 20), bio-hazard, Screamer & Scavengers.
    /// - East Commercial & Gas Station Industrial Zone: (45, 0, -5), armored Tank packs.
    /// - West Residential Stealth Maze: (-58, 0, 0), tight alleys, house interior Lurkers.
    /// Displays subtle, non-blocking 2.5s top-center HUD location toasts on zone entrance.
    /// </summary>
    public class DistrictZombieEcology : MonoBehaviour
    {
        private static DistrictZombieEcology instance;
        public static DistrictZombieEcology Instance
        {
            get
            {
                if (instance == null) instance = FindAnyObjectByType<DistrictZombieEcology>();
                return instance;
            }
            private set => instance = value;
        }

        [Header("Districts Configuration")]
        [SerializeField] private List<DistrictZone> zones = new List<DistrictZone>();

        [Header("Current State")]
        [SerializeField] private CityDistrict currentDistrict = CityDistrict.NeutralCity;
        [SerializeField] private DistrictThreatLevel currentThreatLevel = DistrictThreatLevel.Moderate;

        private Transform playerTransform;
        private CanvasGroup districtToastGroup;
        private Text districtTitleText;
        private Text districtSubtitleText;
        private Image districtAccentBar;
        private Coroutine toastCoroutine;

        public CityDistrict CurrentDistrict => currentDistrict;
        public DistrictThreatLevel CurrentThreatLevel => currentThreatLevel;
        public IReadOnlyList<DistrictZone> Zones
        {
            get
            {
                if (zones == null || zones.Count < 12) InitializeZones();
                return zones;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeZones();
        }

        private void Start()
        {
            FindPlayer();
            SetupDistrictToastUI();
        }

        public void InitializeZones()
        {
            if (zones == null) zones = new List<DistrictZone>();
            if (zones.Count >= 12) return;
            zones.Clear();

            // 1. Safe House Buffer Zone (28m Low Threat Perimeter)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.SafeHouseBuffer,
                displayName = "Sığınak Güvenli Alanı",
                subTitle = "Düşük Tehdit — Zombi Saldırısı Yok",
                threatLevel = DistrictThreatLevel.Low,
                themeColor = new Color(0.25f, 0.85f, 0.45f, 0.95f), // Emerald green
                center = new Vector3(-36f, 0f, -30f),
                radius = 28f,
                isSpherical = true
            });

            // 2. Quarantine Core (Clinic & Police Station High Biohazard)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.QuarantineCore,
                displayName = "Karantina Bölgesi",
                subTitle = "Yüksek Biyolojik Tehlike — Screamer & Scavenger Yoğunluğu",
                threatLevel = DistrictThreatLevel.High,
                themeColor = new Color(0.75f, 0.15f, 0.85f, 0.95f), // Biohazard Violet
                center = new Vector3(-25f, 0f, 20f),
                size = new Vector3(50f, 25f, 60f),
                isSpherical = false
            });

            // 3. East Commercial & Gas Station Industrial Zone (Tank & Armor)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.IndustrialGasStation,
                displayName = "Sanayi & İkmal Hattı",
                subTitle = "Aşırı Tehdit — Zırhlı Tank Zombi Sürüleri & Benzinlik",
                threatLevel = DistrictThreatLevel.Extreme,
                themeColor = new Color(1.0f, 0.22f, 0.22f, 0.95f), // Crimson red
                center = new Vector3(45f, 0f, -5f),
                size = new Vector3(60f, 25f, 85f),
                isSpherical = false
            });

            // 4. West Residential Stealth Maze (Tight Alleys & Lurkers)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.ResidentialStealth,
                displayName = "Konut Mahallesi",
                subTitle = "Gizlilik Bölgesi — Dar Ara Sokaklar & Pusu Tehlikesi",
                threatLevel = DistrictThreatLevel.Moderate,
                themeColor = new Color(1.0f, 0.65f, 0.15f, 0.95f), // Amber warning
                center = new Vector3(-58f, 0f, 0f),
                size = new Vector3(55f, 25f, 70f),
                isSpherical = false
            });

            // 5. North Evacuation Highway & Viaduct FOB (Phase 2 Outer Sector)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.NorthEvacuationHighway,
                displayName = "Kuzey Tahliye Otoyolu",
                subTitle = "Aşırı Tehdit — Zırhlı Tank Zombi Sürüleri & Viyadük Gözcüleri",
                threatLevel = DistrictThreatLevel.Extreme,
                themeColor = new Color(1.0f, 0.55f, 0.15f, 0.95f),
                center = new Vector3(0f, 0f, 240f),
                size = new Vector3(240f, 40f, 240f),
                isSpherical = false
            });

            // 6. East Industrial Rail Yard & Warehouses (Phase 2 Outer Sector)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.EastRailYard,
                displayName = "Doğu Sanayi & Tren Garı",
                subTitle = "Yüksek Tehdit — Vagon İçi Pusu & Depo Scavenger Kümeleri",
                threatLevel = DistrictThreatLevel.High,
                themeColor = new Color(0.95f, 0.75f, 0.22f, 0.95f),
                center = new Vector3(240f, 0f, 0f),
                size = new Vector3(240f, 40f, 240f),
                isSpherical = false
            });

            // 7. NorthEast Container Harbor & Fuel Depot (Phase 2 Outer Sector)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.NorthEastHarbor,
                displayName = "Kuzeydoğu Konteyner Limanı",
                subTitle = "Aşırı Tehdit — Konteyner Labirenti & Yanıcı Yakıt Deposu",
                threatLevel = DistrictThreatLevel.Extreme,
                themeColor = new Color(0.95f, 0.25f, 0.25f, 0.95f),
                center = new Vector3(240f, 0f, 240f),
                size = new Vector3(240f, 40f, 240f),
                isSpherical = false
            });

            // 8. SouthEast Regional Power Substation (Phase 2 Outer Sector)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.SouthEastSubstation,
                displayName = "Güneydoğu Ana Trafo Merkezi",
                subTitle = "Yüksek Tehdit — Yüksek Gerilim Sahası & Şebeke Kontrol Binası",
                threatLevel = DistrictThreatLevel.High,
                themeColor = new Color(1.0f, 0.15f, 0.15f, 0.95f),
                center = new Vector3(240f, 0f, -240f),
                size = new Vector3(240f, 40f, 240f),
                isSpherical = false
            });

            // 9. West Urban Forest Park & Stealth Trails (Phase 3 Outer Sector)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.WestUrbanForest,
                displayName = "Batı Şehir Ormanı & Yaban Parkı",
                subTitle = "Orta Tehdit — Ağaçlar Arasında Lone Roamer Devriyeleri & Gizlilik Çalıları",
                threatLevel = DistrictThreatLevel.Moderate,
                themeColor = new Color(0.2f, 0.75f, 0.35f, 0.95f),
                center = new Vector3(-240f, 0f, 0f),
                size = new Vector3(240f, 40f, 240f),
                isSpherical = false
            });

            // 10. NorthWest Lakefront Cabins & Lookout Outpost (Phase 3 Outer Sector)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.NorthWestLakefront,
                displayName = "Kuzeybatı Göl Kenarı & Kulübeler",
                subTitle = "Yüksek Tehdit — Balıkçı İskelesi & Hızlı Zombi (Runner) Pusuları",
                threatLevel = DistrictThreatLevel.High,
                themeColor = new Color(0.2f, 0.85f, 0.9f, 0.95f),
                center = new Vector3(-240f, 0f, 240f),
                size = new Vector3(240f, 40f, 240f),
                isSpherical = false
            });

            // 11. South Subway Station & Grand Shopping Mall (Phase 3 Outer Sector)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.SouthSubwayMall,
                displayName = "Güney Metro İstasyonu & Büyük AVM",
                subTitle = "Aşırı Tehdit — Yeraltı Karanlığı, AVM İçi Lurker Kümeleri & Tank Nöbetçisi",
                threatLevel = DistrictThreatLevel.Extreme,
                themeColor = new Color(0.75f, 0.25f, 0.9f, 0.95f),
                center = new Vector3(0f, 0f, -240f),
                size = new Vector3(240f, 40f, 240f),
                isSpherical = false
            });

            // 12. SouthWest Flooded Residential Suburbs (Phase 3 Outer Sector)
            zones.Add(new DistrictZone
            {
                districtType = CityDistrict.SouthWestFloodedSuburbs,
                displayName = "Güneybatı Sular Altındaki Banliyö",
                subTitle = "Yüksek Tehdit — Sığ Su Yavaşlatması & Yarı Batık Evlerde Gizli Pusu",
                threatLevel = DistrictThreatLevel.High,
                themeColor = new Color(0.35f, 0.55f, 0.85f, 0.95f),
                center = new Vector3(-240f, 0f, -240f),
                size = new Vector3(240f, 40f, 240f),
                isSpherical = false
            });
        }

        private void FindPlayer()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            Vector3 playerPos = playerTransform.position;
            CityDistrict evaluatedDistrict = CityDistrict.NeutralCity;
            DistrictZone activeZone = null;

            // Safe House buffer takes precedence
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i].districtType == CityDistrict.SafeHouseBuffer && zones[i].Contains(playerPos))
                {
                    evaluatedDistrict = CityDistrict.SafeHouseBuffer;
                    activeZone = zones[i];
                    break;
                }
            }

            if (activeZone == null)
            {
                for (int i = 0; i < zones.Count; i++)
                {
                    if (zones[i].districtType != CityDistrict.SafeHouseBuffer && zones[i].Contains(playerPos))
                    {
                        evaluatedDistrict = zones[i].districtType;
                        activeZone = zones[i];
                        break;
                    }
                }
            }

            if (evaluatedDistrict != currentDistrict)
            {
                currentDistrict = evaluatedDistrict;
                currentThreatLevel = activeZone != null ? activeZone.threatLevel : DistrictThreatLevel.Moderate;

                if (activeZone != null)
                {
                    TriggerDistrictToast(activeZone);
                }
            }
        }

        /// <summary>
        /// Prevents zombie spawning inside the 28m Safe House buffer zone.
        /// </summary>
        public bool CanSpawnZombieAt(Vector3 position)
        {
            if (zones == null || zones.Count == 0) InitializeZones();
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i].districtType == CityDistrict.SafeHouseBuffer && zones[i].Contains(position))
                {
                    return false;
                }
            }
            return true;
        }

        public ZombieArchetype GetPreferredArchetypeForDistrict(CityDistrict district)
        {
            switch (district)
            {
                case CityDistrict.QuarantineCore:
                    return ZombieArchetype.Scavenger;
                case CityDistrict.IndustrialGasStation:
                    return ZombieArchetype.LoneRoamer;
                case CityDistrict.ResidentialStealth:
                    return ZombieArchetype.DormantLurker;
                default:
                    return ZombieArchetype.LoneRoamer;
            }
        }

        private void SetupDistrictToastUI()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject toastRoot = new GameObject("DistrictAlertToast_HUD");
            toastRoot.transform.SetParent(canvas.transform, false);

            RectTransform rt = toastRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -80f);
            rt.sizeDelta = new Vector2(380f, 48f);

            districtToastGroup = toastRoot.AddComponent<CanvasGroup>();
            districtToastGroup.alpha = 0f;
            districtToastGroup.blocksRaycasts = false;
            districtToastGroup.interactable = false;

            // Background pill
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(toastRoot.transform, false);
            RectTransform rtBg = bgObj.AddComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.14f, 0.88f);
            bgImg.raycastTarget = false;

            // Accent Bar
            GameObject barObj = new GameObject("AccentBar");
            barObj.transform.SetParent(toastRoot.transform, false);
            RectTransform rtBar = barObj.AddComponent<RectTransform>();
            rtBar.anchorMin = new Vector2(0f, 0f);
            rtBar.anchorMax = new Vector2(0f, 1f);
            rtBar.pivot = new Vector2(0f, 0.5f);
            rtBar.sizeDelta = new Vector2(4f, 0f);
            districtAccentBar = barObj.AddComponent<Image>();
            districtAccentBar.color = new Color(1f, 0.25f, 0.25f, 1f);
            districtAccentBar.raycastTarget = false;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(toastRoot.transform, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0f, 0.5f);
            rtTitle.anchorMax = new Vector2(1f, 1f);
            rtTitle.pivot = new Vector2(0f, 0.5f);
            rtTitle.anchoredPosition = new Vector2(14f, -2f);
            districtTitleText = titleObj.AddComponent<Text>();
            districtTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            districtTitleText.fontSize = 14;
            districtTitleText.fontStyle = FontStyle.Bold;
            districtTitleText.alignment = TextAnchor.MiddleLeft;
            districtTitleText.color = Color.white;
            districtTitleText.raycastTarget = false;

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(toastRoot.transform, false);
            RectTransform rtSub = subObj.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0f, 0f);
            rtSub.anchorMax = new Vector2(1f, 0.5f);
            rtSub.pivot = new Vector2(0f, 0.5f);
            rtSub.anchoredPosition = new Vector2(14f, 2f);
            districtSubtitleText = subObj.AddComponent<Text>();
            districtSubtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            districtSubtitleText.fontSize = 10;
            districtSubtitleText.fontStyle = FontStyle.Normal;
            districtSubtitleText.alignment = TextAnchor.MiddleLeft;
            districtSubtitleText.color = new Color(0.85f, 0.88f, 0.92f, 0.85f);
            districtSubtitleText.raycastTarget = false;
        }

        public void TriggerDistrictToast(DistrictZone zone)
        {
            if (zone == null) return;

            string toastText = $"{zone.displayName.ToUpper()} — {zone.threatLevel.ToString().ToUpper()}";
            GameEvents.TriggerObjectiveToastRequested(toastText, 2.5f);

            if (districtToastGroup == null) return;

            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(ToastRoutine(zone));
        }

        private IEnumerator ToastRoutine(DistrictZone zone)
        {
            if (districtTitleText != null) districtTitleText.text = zone.displayName.ToUpper();
            if (districtSubtitleText != null) districtSubtitleText.text = zone.subTitle;
            if (districtAccentBar != null) districtAccentBar.color = zone.themeColor;

            // Fade in (0.3s)
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                districtToastGroup.alpha = Mathf.Clamp01(t / 0.3f);
                yield return null;
            }
            districtToastGroup.alpha = 1f;

            // Hold (2.0s)
            yield return new WaitForSeconds(2.0f);

            // Fade out (0.4s)
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                districtToastGroup.alpha = Mathf.Clamp01(1f - (t / 0.4f));
                yield return null;
            }
            districtToastGroup.alpha = 0f;
            toastCoroutine = null;
        }
    }
}
