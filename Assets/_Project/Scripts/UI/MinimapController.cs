using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.AI;
using ProjectSurvival.Missions;
using ProjectSurvival.Survival;

namespace ProjectSurvival.UI
{
    public class MinimapController : MonoBehaviour
    {
        public static MinimapController Instance { get; private set; }

        [Header("Minimap Dimensions")]
        [SerializeField] private RectTransform blipContainer;
        [SerializeField] private float mapUIRadius = 75f;
        [SerializeField] private float worldRadarRange = 45f;

        [Header("Colors")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.7f, 1f);
        [SerializeField] private Color lootColor = new Color(0.2f, 0.9f, 0.3f);
        [SerializeField] private Color zombieColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color missionColor = new Color(1f, 0.85f, 0.1f);

        private Transform playerTransform;
        private readonly List<LootContainer> lootContainers = new List<LootContainer>();
        private readonly List<ZombieAI> zombies = new List<ZombieAI>();
        private readonly List<MissionZone> missionZones = new List<MissionZone>();

        // Cached UI blips
        private readonly List<Image> lootBlips = new List<Image>();
        private readonly List<Image> zombieBlips = new List<Image>();
        private Image missionBlip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FindWorldTargets();
            CreateBlipPools();
        }

        public void FindWorldTargets()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;

            lootContainers.Clear();
            lootContainers.AddRange(FindObjectsByType<LootContainer>(FindObjectsSortMode.None));

            zombies.Clear();
            zombies.AddRange(FindObjectsByType<ZombieAI>(FindObjectsSortMode.None));

            missionZones.Clear();
            missionZones.AddRange(FindObjectsByType<MissionZone>(FindObjectsSortMode.None));
        }

        private void CreateBlipPools()
        {
            if (blipContainer == null) blipContainer = transform as RectTransform;

            // Mission Blip (Gold Diamond)
            GameObject mBlipObj = CreateRawBlip("Mission_Blip", missionColor, 14f);
            missionBlip = mBlipObj.GetComponent<Image>();
            mBlipObj.SetActive(false);

            // Loot Blips
            for (int i = 0; i < lootContainers.Count; i++)
            {
                GameObject lb = CreateRawBlip($"Loot_Blip_{i}", lootColor, 7f);
                lootBlips.Add(lb.GetComponent<Image>());
            }

            // Zombie Blips
            for (int i = 0; i < zombies.Count; i++)
            {
                GameObject zb = CreateRawBlip($"Zombie_Blip_{i}", zombieColor, 8f);
                zombieBlips.Add(zb.GetComponent<Image>());
            }
        }

        private GameObject CreateRawBlip(string name, Color color, float size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(blipContainer, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
                return;
            }

            Vector3 playerPos = playerTransform.position;

            // 1. Update Loot Blips
            for (int i = 0; i < lootContainers.Count; i++)
            {
                if (i >= lootBlips.Count) break;

                LootContainer loot = lootContainers[i];
                Image blip = lootBlips[i];

                if (loot == null || loot.IsLooted)
                {
                    blip.gameObject.SetActive(false);
                    continue;
                }

                Vector2 uiPos = CalculateUIPosition(playerPos, loot.transform.position);
                if (uiPos.magnitude <= mapUIRadius)
                {
                    blip.gameObject.SetActive(true);
                    blip.rectTransform.anchoredPosition = uiPos;
                }
                else
                {
                    blip.gameObject.SetActive(false);
                }
            }

            // 2. Update Zombie Blips (Stealth Fog of War)
            for (int i = 0; i < zombies.Count; i++)
            {
                if (i >= zombieBlips.Count) break;

                ZombieAI z = zombies[i];
                Image blip = zombieBlips[i];

                if (z == null || z.IsDead || !z.gameObject.activeInHierarchy)
                {
                    blip.gameObject.SetActive(false);
                    continue;
                }

                float dist = Vector3.Distance(playerPos, z.transform.position);

                // Reveal condition: active aggression OR close proximity (<10m)
                bool isAlerted = (z.CurrentState == ZombieState.Chase || 
                                  z.CurrentState == ZombieState.Investigate || 
                                  z.CurrentState == ZombieState.Attack || 
                                  z.CurrentState == ZombieState.Scream);

                bool isRevealed = isAlerted || (dist <= 10f);

                if (isRevealed && dist <= worldRadarRange)
                {
                    Vector2 uiPos = CalculateUIPosition(playerPos, z.transform.position);
                    blip.gameObject.SetActive(true);
                    blip.rectTransform.anchoredPosition = uiPos;
                }
                else
                {
                    blip.gameObject.SetActive(false);
                }
            }

            // 3. Update Active Mission Blip
            UpdateMissionBlip(playerPos);
        }

        private void UpdateMissionBlip(Vector3 playerPos)
        {
            if (missionBlip == null) return;

            if (MissionManager.Instance != null && MissionManager.Instance.HasActiveMission)
            {
                string targetZone = MissionManager.Instance.ActiveObjective.data.targetZoneId;
                MissionZone zone = missionZones.Find(mz => mz != null && mz.ZoneId == targetZone);

                if (zone != null)
                {
                    Vector2 uiPos = CalculateUIPosition(playerPos, zone.transform.position);

                    // Clamp to edge of map if outside radar range so player knows which way to go!
                    if (uiPos.magnitude > mapUIRadius)
                    {
                        uiPos = uiPos.normalized * (mapUIRadius - 4f);
                    }

                    missionBlip.gameObject.SetActive(true);
                    missionBlip.rectTransform.anchoredPosition = uiPos;
                    return;
                }
            }

            missionBlip.gameObject.SetActive(false);
        }

        private Vector2 CalculateUIPosition(Vector3 center, Vector3 target)
        {
            Vector3 diff = target - center;
            float ratio = mapUIRadius / worldRadarRange;

            // World X -> UI X, World Z -> UI Y
            return new Vector2(diff.x * ratio, diff.z * ratio);
        }
    }
}
