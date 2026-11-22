using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;

namespace ProjectSurvival.AI
{
    /// <summary>
    /// Nocturnal Frenzy & Night Aggression Escalator.
    /// Connected directly to DayNightManager (active from 20:00 to 05:30):
    /// - 60% of interior DormantLurkers awaken automatically and initiate active street patrols.
    /// - Zombie acoustic hearing radius increased by +35% (hearingMultiplier = 1.35f).
    /// - Zombie chase speed boosted by +15% (EffectiveChaseSpeed = chaseSpeed * 1.15f).
    /// - Pack alert delay halved (alertDelayMultiplier = 0.5f).
    /// - Smoothly fades in bioluminescent eye-glow VFX on all zombies.
    /// - Displays the HUD Night Threat Indicator beside CompassBarUI: ☾ GECE TEHDİDİ (+%15 HIZ).
    /// </summary>
    public class NocturnalZombieEscalator : MonoBehaviour
    {
        private static NocturnalZombieEscalator instance;
        public static NocturnalZombieEscalator Instance
        {
            get
            {
                if (instance == null) instance = FindAnyObjectByType<NocturnalZombieEscalator>();
                return instance;
            }
            private set => instance = value;
        }

        [Header("Escalation Multipliers")]
        [SerializeField] private float hearingMultiplier = 1.35f; // +35%
        [SerializeField] private float chaseSpeedMultiplier = 1.15f; // +15%
        [SerializeField] private float packAlertDelayMultiplier = 0.5f; // Halved
        [SerializeField] private float lurkerAwakenPercentage = 0.60f; // 60%

        [Header("Runtime State")]
        [SerializeField] private bool isNightActive = false;

        private GameObject nightThreatBadgeObj;
        private CanvasGroup badgeCanvasGroup;
        private Coroutine badgePulseRoutine;

        public bool IsNightActive => isNightActive;
        public float HearingMultiplier => isNightActive ? hearingMultiplier : 1.0f;
        public float ChaseSpeedMultiplier => isNightActive ? chaseSpeedMultiplier : 1.0f;

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
            GameEvents.OnDayNightChanged += HandleDayNightChanged;

            if (DayNightManager.Instance != null)
            {
                ApplyNightEscalation(DayNightManager.Instance.IsNight, DayNightManager.Instance.CurrentHour);
            }

            SetupNightThreatBadgeUI();
        }

        private void OnDestroy()
        {
            GameEvents.OnDayNightChanged -= HandleDayNightChanged;
            if (Instance == this) Instance = null;
        }

        private void HandleDayNightChanged(bool isNight, float hour)
        {
            ApplyNightEscalation(isNight, hour);
        }

        public void ApplyNightEscalation(bool isNight, float hour = 0f)
        {
            isNightActive = isNight;
            Debug.Log($"[NocturnalZombieEscalator] Night escalation toggled: {isNight} (Hour: {hour:F1})");

            // 1. Update Pack Alert Delay Multiplier
            if (ZombiePackManager.Instance != null)
            {
                ZombiePackManager.Instance.SetAllPacksAlertDelayMultiplier(isNight ? packAlertDelayMultiplier : 1.0f);
            }

            // 2. Query all active zombies in scene
            List<ZombieAI> allZombies = ZombieAI.ActiveZombies;
            if (allZombies == null || allZombies.Count == 0)
            {
                allZombies = new List<ZombieAI>(FindObjectsByType<ZombieAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            }
            List<ZombieAI> dormantLurkers = new List<ZombieAI>();

            for (int i = 0; i < allZombies.Count; i++)
            {
                ZombieAI z = allZombies[i];
                if (z == null || z.IsDead) continue;

                // Update Hearing Multiplier
                ZombieSensorySystem sensory = z.GetComponent<ZombieSensorySystem>();
                if (sensory != null)
                {
                    sensory.HearingMultiplier = isNight ? hearingMultiplier : 1.0f;
                }

                // Update Eye Glow
                ZombieEyeGlow eyeGlow = z.GetComponent<ZombieEyeGlow>();
                if (eyeGlow != null)
                {
                    eyeGlow.SetNightState(isNight);
                }

                // If currently in Chase state, immediately update agent speed
                if (z.CurrentState == ZombieState.Chase && z.NavAgent != null && z.NavAgent.isOnNavMesh)
                {
                    z.NavAgent.speed = z.EffectiveChaseSpeed;
                }

                if (z.Archetype == ZombieArchetype.DormantLurker)
                {
                    dormantLurkers.Add(z);
                }
            }

            // 3. Awaken 60% of interior Dormant Lurkers into active street patrols
            if (isNight && dormantLurkers.Count > 0)
            {
                int countToAwaken = Mathf.CeilToInt(dormantLurkers.Count * lurkerAwakenPercentage);
                for (int i = 0; i < countToAwaken && i < dormantLurkers.Count; i++)
                {
                    dormantLurkers[i].AwakenToStreetPatrol();
                }
                Debug.Log($"[NocturnalZombieEscalator] Awakened {countToAwaken}/{dormantLurkers.Count} interior lurkers into active nocturnal street patrols!");
            }

            // 4. Update HUD Night Threat Indicator Badge
            UpdateNightThreatBadge(isNight);
        }

        private void SetupNightThreatBadgeUI()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Search if already created
            Transform existing = canvas.transform.Find("NightThreat_Badge");
            if (existing != null)
            {
                nightThreatBadgeObj = existing.gameObject;
                badgeCanvasGroup = nightThreatBadgeObj.GetComponent<CanvasGroup>();
                return;
            }

            nightThreatBadgeObj = new GameObject("NightThreat_Badge");
            nightThreatBadgeObj.transform.SetParent(canvas.transform, false);

            RectTransform rt = nightThreatBadgeObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            // Positioned cleanly beside top compass bar (offset +255px X)
            rt.anchoredPosition = new Vector2(255f, -18f);
            rt.sizeDelta = new Vector2(175f, 26f);

            badgeCanvasGroup = nightThreatBadgeObj.AddComponent<CanvasGroup>();
            badgeCanvasGroup.alpha = 0f;
            badgeCanvasGroup.blocksRaycasts = false;
            badgeCanvasGroup.interactable = false;

            // Background pill
            GameObject bgObj = new GameObject("BG");
            bgObj.transform.SetParent(nightThreatBadgeObj.transform, false);
            RectTransform rtBg = bgObj.AddComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.14f, 0.04f, 0.05f, 0.92f); // Deep dark red
            bgImg.raycastTarget = false;

            // Crimson Border Line
            GameObject lineObj = new GameObject("AccentLine");
            lineObj.transform.SetParent(nightThreatBadgeObj.transform, false);
            RectTransform rtLine = lineObj.AddComponent<RectTransform>();
            rtLine.anchorMin = new Vector2(0f, 0f);
            rtLine.anchorMax = new Vector2(0f, 1f);
            rtLine.pivot = new Vector2(0f, 0.5f);
            rtLine.sizeDelta = new Vector2(3f, 0f);
            Image lineImg = lineObj.AddComponent<Image>();
            lineImg.color = new Color(1.0f, 0.23f, 0.19f, 0.95f);
            lineImg.raycastTarget = false;

            // Badge Text
            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(nightThreatBadgeObj.transform, false);
            RectTransform rtTxt = txtObj.AddComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.anchoredPosition = new Vector2(5f, 0f);
            rtTxt.sizeDelta = new Vector2(-10f, 0f);

            Text txt = txtObj.AddComponent<Text>();
            txt.text = "\u263D GECE TEHD\u0130D\u0130 (+%15 HIZ)"; // ☾ GECE TEHDİDİ (+%15 HIZ)
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            txt.fontSize = 11;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1.0f, 0.35f, 0.35f, 0.95f);
            txt.raycastTarget = false;

            UpdateNightThreatBadge(isNightActive);
        }

        private void UpdateNightThreatBadge(bool isNight)
        {
            if (badgeCanvasGroup == null) return;

            if (badgePulseRoutine != null) StopCoroutine(badgePulseRoutine);
            badgePulseRoutine = StartCoroutine(BadgePulseAnimation(isNight));
        }

        private IEnumerator BadgePulseAnimation(bool isNight)
        {
            float targetA = isNight ? 1f : 0f;
            float startA = badgeCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                badgeCanvasGroup.alpha = Mathf.Lerp(startA, targetA, elapsed / 0.4f);
                yield return null;
            }
            badgeCanvasGroup.alpha = targetA;

            // Subtle continuous pulse while night is active
            while (isNight && isNightActive)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 3.5f);
                badgeCanvasGroup.alpha = pulse;
                yield return null;
            }

            badgeCanvasGroup.alpha = isNightActive ? 1f : 0f;
            badgePulseRoutine = null;
        }
    }
}
