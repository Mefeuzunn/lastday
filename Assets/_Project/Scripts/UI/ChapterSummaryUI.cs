using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.UI
{
    public class ChapterSummaryUI : MonoBehaviour
    {
        public static ChapterSummaryUI Instance { get; private set; }

        [Header("UI Roots & Panels")]
        [SerializeField] private GameObject summaryPanelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;

        [Header("Stat Labels")]
        [SerializeField] private Text survivedTimeText;
        [SerializeField] private Text zombiesKilledText;
        [SerializeField] private Text civiliansRescuedText;
        [SerializeField] private Text decisionsMadeText;

        [Header("Buttons")]
        [SerializeField] private Button continueFreeRoamButton;

        // Tracked Stats
        private int zombiesKilledCount = 0;
        private int civiliansRescuedCount = 0;
        private readonly List<string> majorDecisions = new List<string>();

        public int ZombiesKilledCount => zombiesKilledCount;
        public int CiviliansRescuedCount => civiliansRescuedCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (continueFreeRoamButton != null)
            {
                continueFreeRoamButton.onClick.AddListener(HideSummary);
            }

            if (summaryPanelRoot != null)
            {
                summaryPanelRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnZombieKilled += HandleZombieKilled;
            GameEvents.OnCivilianRescued += HandleCivilianRescued;
            GameEvents.OnScavengerRescued += HandleScavengerRescued;
            GameEvents.OnChapterCompleted += HandleChapterCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnZombieKilled -= HandleZombieKilled;
            GameEvents.OnCivilianRescued -= HandleCivilianRescued;
            GameEvents.OnScavengerRescued -= HandleScavengerRescued;
            GameEvents.OnChapterCompleted -= HandleChapterCompleted;
        }

        private void Update()
        {
            // Debug Shortcut to test summary: F8
            if (Input.GetKeyDown(KeyCode.F8))
            {
                if (summaryPanelRoot != null && summaryPanelRoot.activeSelf)
                    HideSummary();
                else
                    ShowSummary();
            }
        }

        private void HandleZombieKilled(GameObject z)
        {
            zombiesKilledCount++;
        }

        private void HandleCivilianRescued()
        {
            civiliansRescuedCount++;
        }

        private void HandleScavengerRescued(string scavName, string reward)
        {
            civiliansRescuedCount++;
            RecordDecision($"{scavName} kurtarıldı");
        }

        private void HandleChapterCompleted(string chapterId)
        {
            ShowSummary();
        }

        public void RecordDecision(string decision)
        {
            if (!majorDecisions.Contains(decision))
            {
                majorDecisions.Add(decision);
            }
        }

        public void ShowSummary()
        {
            if (summaryPanelRoot == null) return;

            // Formulate Time
            string timeStr = "Gün 1, 14:30";
            if (DayNightManager.Instance != null)
            {
                timeStr = $"Gün {DayNightManager.Instance.DayCount}, {DayNightManager.Instance.FormattedTime}";
            }

            if (survivedTimeText != null)
                survivedTimeText.text = timeStr;

            if (zombiesKilledText != null)
                zombiesKilledText.text = $"{zombiesKilledCount} Zombi";

            if (civiliansRescuedText != null)
                civiliansRescuedText.text = $"{civiliansRescuedCount} Hayatta Kalan";

            // Decisions
            string decisionsStr = majorDecisions.Count > 0 ? string.Join(", ", majorDecisions) : "Dr. Aras Tedavi Edildi, Güvenli Bölge Keşfedildi";
            if (decisionsMadeText != null)
                decisionsMadeText.text = decisionsStr;

            summaryPanelRoot.SetActive(true);
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            ProceduralAudioManager.Instance?.PlayObjectiveSuccess();
            Debug.Log("[ChapterSummaryUI] Chapter 1 Summary Screen Displayed!");
        }

        public void HideSummary()
        {
            if (summaryPanelRoot != null)
            {
                summaryPanelRoot.SetActive(false);
            }

            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            GameEvents.TriggerObjectiveToastRequested("[SERBEST MOD] Şehri özgürce keşfetmeye devam edebilirsiniz!", 5f);
        }
    }
}
