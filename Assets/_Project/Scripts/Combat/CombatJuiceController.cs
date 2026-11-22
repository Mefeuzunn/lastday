using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.AI;
using ProjectSurvival.Player;

namespace ProjectSurvival.Combat
{
    /// <summary>
    /// Phase 1 (Step 1B): Tactile Combat Juice & Physical Hit Feedback.
    /// Drives micro-hitstop impact freeze (0.045s @ 0.08 timescale), 
    /// crisp 4-tick X hitmarker with dynamic coloration (White vs Crimson-Gold),
    /// camera micro-punch, and haptic impact pulses across melee, firearms, and vehicle rams.
    /// </summary>
    public class CombatJuiceController : MonoBehaviour
    {
        public static CombatJuiceController Instance { get; private set; }

        [Header("Hitstop Configuration")]
        [SerializeField] private float defaultHitstopDuration = 0.045f;
        [SerializeField] private float defaultHitstopTimeScale = 0.08f;
        [SerializeField] private float criticalHitstopDuration = 0.055f;
        [SerializeField] private float criticalHitstopTimeScale = 0.06f;

        [Header("Hitmarker UI Elements")]
        [SerializeField] private RectTransform hitmarkerRoot;
        [SerializeField] private Image hitmarkerImage;
        [SerializeField] private Image[] hitmarkerTicks;

        [Header("Hitmarker Colors")]
        [SerializeField] private Color standardHitColor = new Color(1.0f, 1.0f, 1.0f, 0.95f); // Crisp White #FFFFFF
        [SerializeField] private Color criticalHitColor = new Color(1.0f, 0.23f, 0.19f, 1.0f); // Crimson #FF3B30
        [SerializeField] private Color lethalHitColor = new Color(1.0f, 0.72f, 0.01f, 1.0f); // Gold #FFB703

        [Header("Camera Kick")]
        [SerializeField] private float standardPitchKick = -0.25f;
        [SerializeField] private float criticalPitchKick = -0.45f;

        private Coroutine hitstopCoroutine;
        private Coroutine hitmarkerCoroutine;
        private bool isHitstopActive = false;

        private void Awake()
        {
            Time.timeScale = 1.0f;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeHitmarkerUI();
        }

        private void OnEnable()
        {
            GameEvents.OnHitConfirmed += HandleHitConfirmed;
        }

        private void OnDisable()
        {
            GameEvents.OnHitConfirmed -= HandleHitConfirmed;
            if (isHitstopActive)
            {
                Time.timeScale = 1.0f;
                isHitstopActive = false;
            }
        }

        private void InitializeHitmarkerUI()
        {
            if (hitmarkerRoot == null)
            {
                // Search for existing HitMarker_Panel in scene HUD
                GameObject existing = GameObject.Find("HitMarker_Panel");
                if (existing != null)
                {
                    hitmarkerRoot = existing.GetComponent<RectTransform>();
                    hitmarkerImage = existing.GetComponent<Image>();
                }
            }

            // Create dedicated 4-tick X hitmarker if not present or missing ticks
            if (hitmarkerRoot == null)
            {
                Canvas hudCanvas = FindFirstObjectByType<Canvas>();
                if (hudCanvas != null)
                {
                    GameObject hmObj = new GameObject("HitMarker_Panel");
                    hmObj.transform.SetParent(hudCanvas.transform, false);
                    hitmarkerRoot = hmObj.AddComponent<RectTransform>();
                    hitmarkerRoot.anchorMin = new Vector2(0.5f, 0.5f);
                    hitmarkerRoot.anchorMax = new Vector2(0.5f, 0.5f);
                    hitmarkerRoot.pivot = new Vector2(0.5f, 0.5f);
                    hitmarkerRoot.anchoredPosition = Vector2.zero;
                    hitmarkerRoot.sizeDelta = new Vector2(32f, 32f);

                    hitmarkerImage = hmObj.AddComponent<Image>();
                    hitmarkerImage.raycastTarget = false;
                    hitmarkerImage.color = new Color(1f, 1f, 1f, 0f);
                }
            }

            if (hitmarkerRoot != null)
            {
                EnsureFourTickXLayout();
                hitmarkerRoot.gameObject.SetActive(false);
            }
        }

        private void EnsureFourTickXLayout()
        {
            if (hitmarkerTicks == null || hitmarkerTicks.Length < 4)
            {
                // Check if 4 tick children exist or create them
                hitmarkerTicks = new Image[4];
                string[] tickNames = new string[] { "Tick_NW", "Tick_NE", "Tick_SE", "Tick_SW" };
                Vector2[] angles = new Vector2[]
                {
                    new Vector2(-8f, 8f),   // NW
                    new Vector2(8f, 8f),    // NE
                    new Vector2(8f, -8f),   // SE
                    new Vector2(-8f, -8f)   // SW
                };
                float[] rotations = new float[] { 45f, -45f, 45f, -45f };

                for (int i = 0; i < 4; i++)
                {
                    Transform existingTick = hitmarkerRoot.Find(tickNames[i]);
                    RectTransform rt;
                    GameObject tickObj;
                    if (existingTick != null && existingTick is RectTransform rect)
                    {
                        tickObj = existingTick.gameObject;
                        rt = rect;
                    }
                    else
                    {
                        if (existingTick != null) Destroy(existingTick.gameObject);
                        tickObj = new GameObject(tickNames[i], typeof(RectTransform));
                        tickObj.transform.SetParent(hitmarkerRoot, false);
                        rt = tickObj.GetComponent<RectTransform>();
                    }

                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = angles[i];
                    rt.sizeDelta = new Vector2(2.5f, 8.5f);
                    rt.localRotation = Quaternion.Euler(0f, 0f, rotations[i]);

                    Image img = tickObj.GetComponent<Image>() ?? tickObj.AddComponent<Image>();
                    img.color = standardHitColor;
                    img.raycastTarget = false;
                    hitmarkerTicks[i] = img;
                }
            }
        }

        private void HandleHitConfirmed(bool isCritical, Vector3 hitPos)
        {
            TriggerHitmarker(isCritical, false, hitPos);
        }

        /// <summary>
        /// Visceral micro-hitstop impact freeze.
        /// Temporarily scales time down to timeScale (0.08) for unscaled duration (0.045s),
        /// preserving paused state if game is paused.
        /// </summary>
        public void TriggerHitstop(float duration = 0.045f, float targetTimeScale = 0.08f)
        {
            if (GameManager.Instance != null && (GameManager.Instance.CurrentState == GameState.Paused || GameManager.Instance.CurrentState == GameState.GameOver))
            {
                return;
            }

            if (hitstopCoroutine != null)
            {
                StopCoroutine(hitstopCoroutine);
            }
            hitstopCoroutine = StartCoroutine(HitstopRoutine(duration, targetTimeScale));
        }

        private IEnumerator HitstopRoutine(float duration, float targetScale)
        {
            isHitstopActive = true;
            Time.timeScale = targetScale;
            yield return new WaitForSecondsRealtime(duration);

            if (GameManager.Instance == null || (GameManager.Instance.CurrentState != GameState.Paused && GameManager.Instance.CurrentState != GameState.GameOver))
            {
                Time.timeScale = 1.0f;
            }
            isHitstopActive = false;
            hitstopCoroutine = null;
        }

        /// <summary>
        /// Flashes the 4-tick X hitmarker with sound, camera kick, and haptics.
        /// </summary>
        public void TriggerHitmarker(bool isCritical, bool isLethal = false, Vector3 hitPoint = default)
        {
            if (hitmarkerRoot == null) return;

            Color targetColor = isLethal ? lethalHitColor : (isCritical ? criticalHitColor : standardHitColor);
            float startScale = isLethal ? 1.55f : (isCritical ? 1.45f : 1.15f);
            float duration = isCritical ? 0.22f : 0.18f;

            if (hitmarkerCoroutine != null)
            {
                StopCoroutine(hitmarkerCoroutine);
            }
            hitmarkerCoroutine = StartCoroutine(HitmarkerFlashRoutine(targetColor, startScale, duration));

            // Audio confirmation tick
            ProceduralAudioManager.Instance?.PlayHitMarker(isCritical || isLethal);

            // Camera micro-punch
            float pitch = isCritical ? criticalPitchKick : standardPitchKick;
            float yaw = UnityEngine.Random.Range(-0.15f, 0.15f);
            ThirdPersonCamera.Instance?.AddRecoil(pitch, yaw);

            // Tactile Haptics
            if (isCritical || isLethal)
            {
                MobileHapticManager.Instance?.TriggerHeavy();
            }
            else
            {
                MobileHapticManager.Instance?.TriggerLight();
            }
        }

        private IEnumerator HitmarkerFlashRoutine(Color color, float startScale, float duration)
        {
            hitmarkerRoot.gameObject.SetActive(true);
            hitmarkerRoot.localScale = Vector3.one * startScale;

            if (hitmarkerImage != null)
            {
                hitmarkerImage.color = color;
            }

            if (hitmarkerTicks != null)
            {
                for (int i = 0; i < hitmarkerTicks.Length; i++)
                {
                    if (hitmarkerTicks[i] != null) hitmarkerTicks[i].color = color;
                }
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth scale-in recovery
                hitmarkerRoot.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one, t);

                // Alpha fade-out
                Color c = color;
                c.a = Mathf.Lerp(1.0f, 0.0f, t);

                if (hitmarkerImage != null) hitmarkerImage.color = c;
                if (hitmarkerTicks != null)
                {
                    for (int i = 0; i < hitmarkerTicks.Length; i++)
                    {
                        if (hitmarkerTicks[i] != null) hitmarkerTicks[i].color = c;
                    }
                }

                yield return null;
            }

            hitmarkerRoot.gameObject.SetActive(false);
            hitmarkerCoroutine = null;
        }

        /// <summary>
        /// Dedicated trigger for Fire Axe & heavy melee impact.
        /// </summary>
        public void TriggerMeleeImpactJuice(bool isCritical, Vector3 hitPoint, Vector3 hitNormal)
        {
            TriggerHitstop(defaultHitstopDuration, defaultHitstopTimeScale);
            TriggerHitmarker(isCritical, false, hitPoint);
            ImpactDecalPoolManager.Instance?.SpawnImpact(hitPoint, hitNormal, true);
        }

        /// <summary>
        /// Dedicated trigger for lethal or high-multiplier firearm headshots (2.5x - 3.25x).
        /// </summary>
        public void TriggerCriticalHeadshotJuice(Vector3 hitPoint, Vector3 hitNormal, bool isLethal = false)
        {
            TriggerHitstop(criticalHitstopDuration, criticalHitstopTimeScale);
            TriggerHitmarker(true, isLethal, hitPoint);
            ImpactDecalPoolManager.Instance?.SpawnImpact(hitPoint, hitNormal, true);
        }

        /// <summary>
        /// Dedicated trigger for vehicle kinetic zombie rams.
        /// </summary>
        public void TriggerVehicleRamJuice(Vector3 hitPoint)
        {
            TriggerHitstop(0.050f, 0.08f);
            TriggerHitmarker(true, true, hitPoint);
            ImpactDecalPoolManager.Instance?.SpawnImpact(hitPoint, Vector3.up, true);
        }
    }
}
