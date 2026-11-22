using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSurvival.Core
{
    [Serializable]
    public struct SectorData
    {
        public string sectorId;
        public Vector2Int gridCoord; // (-1..1, -1..1)
        public Vector3 worldCenter;  // Center in world coordinates
        public string turkishDisplayName;
        public string subTitle;
        public string biomeTheme;
        public int threatLevel;      // 1 to 5
        public Color themeColor;

        public SectorData(string id, Vector2Int coord, Vector3 center, string name, string sub, string biome, int threat, Color col)
        {
            sectorId = id;
            gridCoord = coord;
            worldCenter = center;
            turkishDisplayName = name;
            subTitle = sub;
            biomeTheme = biome;
            threatLevel = threat;
            themeColor = col;
        }
    }

    /// <summary>
    /// Centralized Open-World Coordinate & Sector Registry for the 720x720m (3x3 Sector Grid) World.
    /// Eliminates hardcoded world-size constants across gameplay, AI, and UI systems.
    /// </summary>
    public static class WorldMapConfig
    {
        public const float SectorSize = 240f;
        public const int GridDimension = 3; // 3x3 sectors: -1, 0, +1
        public const float TotalWorldSize = 720f;
        public const float WorldMin = -360f;
        public const float WorldMax = 360f;
        public const float HalfWorldSize = 360f;

        private static readonly Dictionary<Vector2Int, SectorData> SectorRegistry = new Dictionary<Vector2Int, SectorData>();
        private static readonly List<SectorData> AllSectorsList = new List<SectorData>();

        static WorldMapConfig()
        {
            RegisterSectors();
        }

        private static void RegisterSectors()
        {
            SectorRegistry.Clear();
            AllSectorsList.Clear();

            // (0, 0) — Merkez Karantina Bölgesi (Existing Core City)
            Register(new SectorData(
                "Sector_0_0_Center",
                new Vector2Int(0, 0),
                new Vector3(0f, 0f, 0f),
                "Merkez Karantina Bölgesi",
                "Ana Karantina & Sığınak Çevresi",
                "Urban_Ruins",
                2,
                new Color(0.25f, 0.85f, 0.45f, 0.95f)
            ));

            // (0, +1) — Kuzey Tahliye Otoyolu
            Register(new SectorData(
                "Sector_0_1_North",
                new Vector2Int(0, 1),
                new Vector3(0f, 0f, 240f),
                "Kuzey Tahliye Otoyolu",
                "Askeri Kontrol Noktaları & Viyadük",
                "Highway_Overpass",
                3,
                new Color(1.0f, 0.55f, 0.15f, 0.95f)
            ));

            // (+1, 0) — Doğu Sanayi ve Tren Garı
            Register(new SectorData(
                "Sector_1_0_East",
                new Vector2Int(1, 0),
                new Vector3(240f, 0f, 0f),
                "Doğu Sanayi ve Tren Garı",
                "Lojistik Ambarları & Vagon Hatları",
                "Industrial_Rail",
                3,
                new Color(0.95f, 0.75f, 0.22f, 0.95f)
            ));

            // (+1, +1) — Kuzeydoğu Konteyner Limanı
            Register(new SectorData(
                "Sector_1_1_NorthEast",
                new Vector2Int(1, 1),
                new Vector3(240f, 0f, 240f),
                "Kuzeydoğu Konteyner Limanı",
                "Yakıt Tankeri İskelesi & Vinç Sahası",
                "Harbor_Depot",
                4,
                new Color(0.95f, 0.25f, 0.25f, 0.95f)
            ));

            // (-1, 0) — Batı Şehir Ormanı
            Register(new SectorData(
                "Sector_-1_0_West",
                new Vector2Int(-1, 0),
                new Vector3(-240f, 0f, 0f),
                "Batı Şehir Ormanı",
                "Yaban Parkı & Terk Edilmiş Yürüyüş Yolları",
                "Overgrown_Park",
                2,
                new Color(0.2f, 0.75f, 0.35f, 0.95f)
            ));

            // (-1, +1) — Kuzeybatı Göl Kenarı
            Register(new SectorData(
                "Sector_-1_1_NorthWest",
                new Vector2Int(-1, 1),
                new Vector3(-240f, 0f, 240f),
                "Kuzeybatı Göl Kenarı",
                "Orman Kulübeleri & Balıkçı İskelesi",
                "Lakeside_Woodlands",
                3,
                new Color(0.2f, 0.85f, 0.9f, 0.95f)
            ));

            // (0, -1) — Güney Metro ve AVM Meydanı
            Register(new SectorData(
                "Sector_0_-1_South",
                new Vector2Int(0, -1),
                new Vector3(0f, 0f, -240f),
                "Güney Metro ve AVM Meydanı",
                "Yeraltı İstasyonu & Ticaret Pasajları",
                "Subway_Mall",
                4,
                new Color(0.75f, 0.25f, 0.9f, 0.95f)
            ));

            // (-1, -1) — Güneybatı Sular Altındaki Banliyö
            Register(new SectorData(
                "Sector_-1_-1_SouthWest",
                new Vector2Int(-1, -1),
                new Vector3(-240f, 0f, -240f),
                "Güneybatı Sular Altındaki Banliyö",
                "Batık Sokaklar & Çatıda İkmal Noktaları",
                "Flooded_Suburbs",
                3,
                new Color(0.35f, 0.55f, 0.85f, 0.95f)
            ));

            // (+1, -1) — Güneydoğu Ana Trafo Merkezi
            Register(new SectorData(
                "Sector_1_-1_SouthEast",
                new Vector2Int(1, -1),
                new Vector3(240f, 0f, -240f),
                "Güneydoğu Ana Trafo Merkezi",
                "Yüksek Gerilim Hatları & Şalt Sahası",
                "Power_Substation",
                5,
                new Color(1.0f, 0.15f, 0.15f, 0.95f)
            ));
        }

        private static void Register(SectorData data)
        {
            SectorRegistry[data.gridCoord] = data;
            AllSectorsList.Add(data);
        }

        public static IReadOnlyList<SectorData> GetAllSectors() => AllSectorsList;

        public static bool TryGetSector(Vector2Int coord, out SectorData sector)
        {
            return SectorRegistry.TryGetValue(coord, out sector);
        }

        public static SectorData GetSectorData(Vector2Int coord)
        {
            if (SectorRegistry.TryGetValue(coord, out SectorData s))
            {
                return s;
            }
            return SectorRegistry[Vector2Int.zero];
        }

        public static Vector2Int GetSectorCoordAtPosition(Vector3 worldPos)
        {
            // Each sector spans [-120..+120] relative to its center (-240, 0, +240)
            int gx = Mathf.Clamp(Mathf.RoundToInt(worldPos.x / SectorSize), -1, 1);
            int gz = Mathf.Clamp(Mathf.RoundToInt(worldPos.z / SectorSize), -1, 1);
            return new Vector2Int(gx, gz);
        }

        public static SectorData GetSectorAtPosition(Vector3 worldPos)
        {
            Vector2Int coord = GetSectorCoordAtPosition(worldPos);
            return GetSectorData(coord);
        }

        /// <summary>
        /// Converts a world coordinate (-360m to +360m) to normalized UV coordinates (0.0 to 1.0).
        /// </summary>
        public static Vector2 WorldToNormalizedUV(Vector3 worldPos)
        {
            float u = Mathf.Clamp01((worldPos.x - WorldMin) / TotalWorldSize);
            float v = Mathf.Clamp01((worldPos.z - WorldMin) / TotalWorldSize);
            return new Vector2(u, v);
        }

        /// <summary>
        /// Converts normalized UV coordinates (0.0 to 1.0) back into world coordinate (-360m to +360m).
        /// </summary>
        public static Vector3 NormalizedUVToWorld(Vector2 uv)
        {
            float x = Mathf.Lerp(WorldMin, WorldMax, Mathf.Clamp01(uv.x));
            float z = Mathf.Lerp(WorldMin, WorldMax, Mathf.Clamp01(uv.y));
            return new Vector3(x, 0f, z);
        }

        /// <summary>
        /// Clamps a world position inside the outer border of the 720x720m world.
        /// </summary>
        public static Vector3 ClampToWorldBounds(Vector3 pos, float margin = 5f)
        {
            float limit = HalfWorldSize - margin;
            pos.x = Mathf.Clamp(pos.x, -limit, limit);
            pos.z = Mathf.Clamp(pos.z, -limit, limit);
            return pos;
        }

        /// <summary>
        /// Checks if a position is within the playable 720x720m world bounds.
        /// </summary>
        public static bool IsInsideWorld(Vector3 worldPos)
        {
            return worldPos.x >= WorldMin && worldPos.x <= WorldMax &&
                   worldPos.z >= WorldMin && worldPos.z <= WorldMax;
        }
    }
}
