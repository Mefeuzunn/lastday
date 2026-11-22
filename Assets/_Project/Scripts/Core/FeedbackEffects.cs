using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSurvival.Core
{
    public class FeedbackEffects : MonoBehaviour
    {
        public static FeedbackEffects Instance { get; private set; }

        [Header("Damage Vignette")]
        [SerializeField] private Image damageVignetteImage;
        [SerializeField] private Color damageFlashColor = new Color(0.85f, 0.08f, 0.08f, 0.35f);
        private Coroutine flashRoutine;

        [Header("Camera Reference")]
        [SerializeField] private Transform cameraTransform;
        private Vector3 originalCameraLocalPos;

        [Header("Floating World Markers")]
        [SerializeField] private GameObject floatingTextPrefab;
        private readonly Dictionary<GameObject, GameObject> zombieMarkers = new Dictionary<GameObject, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (cameraTransform != null)
            {
                originalCameraLocalPos = cameraTransform.localPosition;
            }

            EnsureVignetteUI();
        }

        private void EnsureVignetteUI()
        {
            if (damageVignetteImage == null)
            {
                // Find existing DamageVignette if present on Canvas
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform existing = canvas.transform.Find("DamageVignette");
                    if (existing != null)
                    {
                        damageVignetteImage = existing.GetComponent<Image>();
                    }
                    else
                    {
                        GameObject vignetteObj = new GameObject("DamageVignette", typeof(RectTransform), typeof(Image));
                        vignetteObj.transform.SetParent(canvas.transform, false);
                        vignetteObj.transform.SetAsFirstSibling(); // behind HUD elements

                        RectTransform rt = vignetteObj.GetComponent<RectTransform>();
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.sizeDelta = Vector2.zero;

                        damageVignetteImage = vignetteObj.GetComponent<Image>();
                        damageVignetteImage.raycastTarget = false;
                    }
                }
            }

            if (damageVignetteImage != null)
            {
                damageVignetteImage.color = Color.clear;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
            GameEvents.OnZombieAlertStateChanged += HandleZombieAlertChanged;
            GameEvents.OnItemCollected += HandleItemCollected;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
            GameEvents.OnZombieAlertStateChanged -= HandleZombieAlertChanged;
            GameEvents.OnItemCollected -= HandleItemCollected;
        }

        private void Update()
        {
            // Camera shake is solely owned and resolved by ThirdPersonCamera to prevent transform fighting
        }

        public void TriggerShake(float intensity, float duration)
        {
            GameEvents.TriggerCameraShakeRequested(intensity, duration);
        }

        private void HandlePlayerDamaged(float amount)
        {
            // Ignore minor micro-tick damage from hunger/thirst decay so screen doesn't stay red
            if (amount < 2.0f) return;

            if (damageVignetteImage != null)
            {
                if (flashRoutine != null) StopCoroutine(flashRoutine);
                flashRoutine = StartCoroutine(DamageFlashRoutine(Mathf.Clamp(amount / 25f, 0.2f, 0.4f)));
            }

            TriggerShake(0.25f, 0.2f);
        }

        private IEnumerator DamageFlashRoutine(float peakAlpha)
        {
            float duration = 0.30f;
            float elapsed = 0f;
            Color c = damageFlashColor;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                c.a = Mathf.Lerp(peakAlpha, 0f, progress);
                if (damageVignetteImage != null) damageVignetteImage.color = c;
                yield return null;
            }

            if (damageVignetteImage != null) damageVignetteImage.color = Color.clear;
            flashRoutine = null;
        }

        private void HandleZombieAlertChanged(GameObject zombie, string alertState)
        {
            if (zombie == null) return;

            // Clean up any old markers
            if (zombieMarkers.TryGetValue(zombie, out GameObject marker))
            {
                if (marker != null) Destroy(marker);
                zombieMarkers.Remove(zombie);
            }

            // Audio & Camera Tension cues instead of overhead text markers
            if (alertState == "!")
            {
                TriggerShake(0.12f, 0.15f);
                ProceduralAudioManager.Instance?.PlayHeartbeat(0.85f);
            }
            else if (alertState == "?")
            {
                ProceduralAudioManager.Instance?.PlayHeartbeat(0.45f);
            }
        }

        private void HandleItemCollected(string itemName, int count)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;

            Vector3 spawnPos = player.transform.position + new Vector3(0, 2.0f, 0);
            StartCoroutine(SpawnFloatingTextRoutine($"+{count} {itemName}", spawnPos));
        }

        private IEnumerator SpawnFloatingTextRoutine(string text, Vector3 startPos)
        {
            GameObject textObj = new GameObject("FloatingText");
            textObj.transform.position = startPos;

            var tm = textObj.AddComponent<TextMesh>();
            tm.text = text;
            tm.characterSize = 0.15f;
            tm.fontSize = 36;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(0.3f, 1f, 0.5f, 1f); // bright friendly green
            textObj.AddComponent<FaceCameraBillboard>();

            float elapsed = 0f;
            float duration = 1.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                textObj.transform.position = startPos + new Vector3(0, progress * 1.2f, 0);

                Color c = tm.color;
                c.a = 1f - progress;
                tm.color = c;
                yield return null;
            }

            Destroy(textObj);
        }
    }

    public class FaceCameraBillboard : MonoBehaviour
    {
        private Transform mainCamTransform;

        private void Start()
        {
            if (Camera.main != null) mainCamTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (mainCamTransform == null && Camera.main != null)
            {
                mainCamTransform = Camera.main.transform;
            }

            if (mainCamTransform != null)
            {
                transform.rotation = mainCamTransform.rotation;
            }
        }
    }
}
