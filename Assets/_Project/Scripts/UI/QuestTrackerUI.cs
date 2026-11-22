using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Missions;

namespace ProjectSurvival.UI
{
    public class QuestTrackerUI : MonoBehaviour
    {
        public static QuestTrackerUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text missionHeaderLabel;
        [SerializeField] private Text missionTitleText;
        [SerializeField] private Text missionDescText;
        [SerializeField] private Text missionDistanceText;
        [SerializeField] private Image statusIndicatorDot;

        private Transform playerTransform;
        private Transform cachedTargetTransform;
        private string cachedTargetZoneId = string.Empty;

        private void Awake()
        {
            if (GetComponent<QuestTrackerHUD>() != null)
            {
                // Defer to QuestTrackerHUD on the same GameObject to avoid text fighting
                enabled = false;
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
            RefreshDisplay();
        }

        private void OnEnable()
        {
            GameEvents.OnMissionProgressUpdated += HandleMissionUpdated;
            GameEvents.OnMissionCompleted += HandleMissionCompleted;
            GameEvents.OnMissionFailed += HandleMissionFailed;
        }

        private void OnDisable()
        {
            GameEvents.OnMissionProgressUpdated -= HandleMissionUpdated;
            GameEvents.OnMissionCompleted -= HandleMissionCompleted;
            GameEvents.OnMissionFailed -= HandleMissionFailed;
        }

        private void Update()
        {
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (QuestTrackerHUD.Instance != null && QuestTrackerHUD.Instance.gameObject.activeInHierarchy)
            {
                // Defer to QuestTrackerHUD for main narrative/prologue display unless a dynamic side-mission is actively running
                if (MissionManager.Instance == null || !MissionManager.Instance.HasActiveMission)
                {
                    return;
                }
            }

            if (playerTransform == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (MissionManager.Instance != null && MissionManager.Instance.HasActiveMission)
            {
                var objective = MissionManager.Instance.ActiveObjective;
                if (panelRoot != null) panelRoot.SetActive(true);

                if (missionHeaderLabel != null)
                {
                    missionHeaderLabel.text = "AKTİF GÖREV";
                    missionHeaderLabel.color = new Color(1f, 0.85f, 0.3f); // Amber / Gold
                }

                if (missionTitleText != null)
                {
                    missionTitleText.text = objective.data.title ?? "Bilinmeyen Görev";
                }

                if (missionDescText != null)
                {
                    missionDescText.text = objective.data.description ?? "";
                }

                if (statusIndicatorDot != null)
                {
                    statusIndicatorDot.color = new Color(0.2f, 0.85f, 0.45f); // Green
                }

                UpdateObjectiveDistanceAndProgress(objective);
            }
            else
            {
                // Free exploration subtle state
                if (missionHeaderLabel != null)
                {
                    missionHeaderLabel.text = "GÖREV TAKİP";
                    missionHeaderLabel.color = new Color(0.6f, 0.65f, 0.72f);
                }

                if (missionTitleText != null)
                {
                    missionTitleText.text = "Serbest Keşif";
                }

                if (missionDescText != null)
                {
                    missionDescText.text = "Çevredeki hayatta kalanları ve erzakları araştır.";
                }

                if (missionDistanceText != null)
                {
                    missionDistanceText.text = "Yeni Görevler Bekleniyor";
                    missionDistanceText.color = new Color(0.7f, 0.75f, 0.8f);
                }

                if (statusIndicatorDot != null)
                {
                    statusIndicatorDot.color = new Color(0.4f, 0.5f, 0.6f);
                }
            }
        }

        private void UpdateObjectiveDistanceAndProgress(DynamicObjective objective)
        {
            if (missionDistanceText == null) return;

            string mType = objective.data.missionType;

            if (mType == "ReachZone" || mType == "RescueNPC")
            {
                Transform target = GetTargetTransform(objective.data.targetZoneId);
                if (target != null && playerTransform != null)
                {
                    float dist = Vector3.Distance(playerTransform.position, target.position);
                    missionDistanceText.text = $"Hedef Mesafesi: {dist:F0}m";
                    missionDistanceText.color = dist < 8f ? new Color(0.2f, 0.95f, 0.4f) : new Color(0.35f, 0.75f, 1f);
                }
                else
                {
                    missionDistanceText.text = $"Bölge: {objective.data.targetZoneId}";
                    missionDistanceText.color = new Color(0.35f, 0.75f, 1f);
                }
            }
            else if (mType == "CollectItem")
            {
                missionDistanceText.text = $"Toplanan: {objective.currentProgress:F0} / {objective.data.targetCount}";
                missionDistanceText.color = new Color(1f, 0.85f, 0.25f);
            }
            else if (mType == "SurviveTimer")
            {
                float remain = Mathf.Max(0f, objective.data.duration - objective.currentProgress);
                missionDistanceText.text = $"Kalan Süre: {remain:F0} sn";
                missionDistanceText.color = remain < 10f ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.75f, 0.2f);
            }
            else
            {
                missionDistanceText.text = "Hedefe Doğru İlerle";
                missionDistanceText.color = new Color(0.35f, 0.75f, 1f);
            }
        }

        private Transform GetTargetTransform(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId)) return null;

            if (cachedTargetTransform != null && cachedTargetZoneId == zoneId)
            {
                return cachedTargetTransform;
            }

            MissionZone[] zones = Object.FindObjectsByType<MissionZone>(FindObjectsSortMode.None);
            foreach (var z in zones)
            {
                if (z.ZoneId == zoneId)
                {
                    cachedTargetZoneId = zoneId;
                    cachedTargetTransform = z.transform;
                    return cachedTargetTransform;
                }
            }

            // Fallback: search GameObject by name
            GameObject obj = GameObject.Find(zoneId);
            if (obj != null)
            {
                cachedTargetZoneId = zoneId;
                cachedTargetTransform = obj.transform;
                return cachedTargetTransform;
            }

            return null;
        }

        private void HandleMissionUpdated(string missionId, string desc, float cur, float req)
        {
            RefreshDisplay();
        }

        private void HandleMissionCompleted(string missionId, string nextNode)
        {
            if (missionHeaderLabel != null)
            {
                missionHeaderLabel.text = "GÖREV TAMAMLANDI!";
                missionHeaderLabel.color = new Color(0.2f, 0.95f, 0.4f);
            }
            RefreshDisplay();
        }

        private void HandleMissionFailed(string missionId, string failNode)
        {
            if (missionHeaderLabel != null)
            {
                missionHeaderLabel.text = "GÖREV BAŞARISIZ!";
                missionHeaderLabel.color = new Color(0.95f, 0.2f, 0.2f);
            }
            RefreshDisplay();
        }
    }
}
