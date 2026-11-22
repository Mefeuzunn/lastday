using System;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Active Phase 5: Top-Left Story Campaign & Objective Tracker HUD.
    /// Aligned with SafeAreaHandler, dynamically updates Chapter titles, active narrative objectives,
    /// target waypoints, distance meters, and completion states.
    /// </summary>
    public class QuestTrackerHUD : MonoBehaviour
    {
        public static QuestTrackerHUD Instance { get; private set; }

        [Header("UI Component Bindings")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text chapterHeaderLabel;
        [SerializeField] private Text objectiveTitleText;
        [SerializeField] private Text objectiveDescText;
        [SerializeField] private Text objectiveDistanceText;
        [SerializeField] private Image statusIndicatorDot;

        private Transform playerTransform;
        private StoryObjective cachedObjective;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void FindPlayer()
        {
            if (playerTransform != null) return;
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        private void Start()
        {
            FindPlayer();

            if (StarterPrologueDirector.Instance != null && !StarterPrologueDirector.Instance.IsPrologueCompleted)
            {
                StarterPrologueDirector.Instance.OnStageChanged += HandlePrologueStageChanged;
                RefreshObjectiveDisplay(StarterPrologueDirector.Instance.GetCurrentPrologueObjective());
            }
            else if (MainStoryDirector.Instance != null)
            {
                RefreshObjectiveDisplay(MainStoryDirector.Instance.GetActiveObjective());
            }

            if (MainStoryDirector.Instance != null)
            {
                MainStoryDirector.Instance.OnObjectiveChanged += HandleObjectiveChanged;
            }
        }

        private void OnDestroy()
        {
            if (StarterPrologueDirector.Instance != null)
            {
                StarterPrologueDirector.Instance.OnStageChanged -= HandlePrologueStageChanged;
            }
            if (MainStoryDirector.Instance != null)
            {
                MainStoryDirector.Instance.OnObjectiveChanged -= HandleObjectiveChanged;
            }
        }

        private void HandlePrologueStageChanged(PrologueStage stage)
        {
            if (StarterPrologueDirector.Instance != null && !StarterPrologueDirector.Instance.IsPrologueCompleted)
            {
                RefreshObjectiveDisplay(StarterPrologueDirector.Instance.GetCurrentPrologueObjective());
            }
        }

        private void HandleObjectiveChanged(StoryObjective obj)
        {
            if (StarterPrologueDirector.Instance != null && !StarterPrologueDirector.Instance.IsPrologueCompleted)
            {
                // Suppress Chapter 1 objectives while prologue is active
                return;
            }
            RefreshObjectiveDisplay(obj);
        }

        public void RefreshObjectiveDisplay(StoryObjective obj)
        {
            cachedObjective = obj;
            if (obj == null) return;

            if (panelRoot != null) panelRoot.SetActive(true);

            if (chapterHeaderLabel != null)
            {
                chapterHeaderLabel.text = obj.chapterTitle;
                chapterHeaderLabel.color = new Color(1.0f, 0.82f, 0.28f); // High-contrast amber/gold
            }

            if (objectiveTitleText != null)
            {
                objectiveTitleText.text = obj.title;
            }

            if (objectiveDescText != null)
            {
                objectiveDescText.text = obj.description;
            }

            if (statusIndicatorDot != null)
            {
                statusIndicatorDot.color = new Color(0.2f, 0.95f, 0.45f); // Vibrant tactical green
            }

            UpdateDistanceDisplay();
        }

        private void UpdateDistanceDisplay()
        {
            if (objectiveDistanceText == null) return;

            if (MainStoryDirector.Instance != null && MainStoryDirector.Instance.IsStoryCompleted)
            {
                objectiveDistanceText.text = "ANA HİKAYE TAMAMLANDI";
                objectiveDistanceText.color = new Color(0.3f, 0.95f, 0.5f);
                return;
            }

            if (cachedObjective == null) return;

            FindPlayer();

            if (cachedObjective.requiredFrequency > 0f)
            {
                objectiveDistanceText.text = $"Gerekli Frekans: {cachedObjective.requiredFrequency:F1} MHz (T tuşu)";
                objectiveDistanceText.color = new Color(1f, 0.85f, 0.35f);
            }
            else if (cachedObjective.targetPosition != Vector3.zero && playerTransform != null)
            {
                float dist = Vector3.Distance(playerTransform.position, cachedObjective.targetPosition);
                objectiveDistanceText.text = $"Hedef Mesafesi: {dist:F0}m";
                objectiveDistanceText.color = dist < 12f ? new Color(0.2f, 0.98f, 0.45f) : new Color(0.35f, 0.82f, 1f);
            }
            else
            {
                objectiveDistanceText.text = "Hedefe Doğru İlerle";
                objectiveDistanceText.color = new Color(0.35f, 0.82f, 1f);
            }
        }

        public void OnStoryCompleted(string moralChoice)
        {
            if (chapterHeaderLabel != null)
            {
                chapterHeaderLabel.text = "ANA HİKAYE SONA ERDİ";
                chapterHeaderLabel.color = new Color(0.2f, 0.95f, 0.45f);
            }

            if (objectiveTitleText != null)
            {
                objectiveTitleText.text = moralChoice == "Evacuation"
                    ? "Bölgesel Tahliye Yayını Başarılı"
                    : "Küresel Antiviral Formülü Aktarıldı";
            }

            if (objectiveDescText != null)
            {
                objectiveDescText.text = "Karantina bölgesindeki görevin tamamlandı. Hayatta kalmaya devam et.";
            }

            if (objectiveDistanceText != null)
            {
                objectiveDistanceText.text = "Serbest Keşif & Üs İnşası Modu";
                objectiveDistanceText.color = new Color(0.3f, 0.95f, 0.5f);
            }

            if (statusIndicatorDot != null)
            {
                statusIndicatorDot.color = new Color(0.2f, 0.95f, 0.45f);
            }
        }

        public void BindUIReferences(GameObject root, Text header, Text title, Text desc, Text dist, Image dot)
        {
            panelRoot = root;
            chapterHeaderLabel = header;
            objectiveTitleText = title;
            objectiveDescText = desc;
            objectiveDistanceText = dist;
            statusIndicatorDot = dot;
        }
    }
}
