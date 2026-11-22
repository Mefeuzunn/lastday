using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSurvival.UI
{
    public class WorldSpeechBubbleUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private Image bubbleBackground;
        [SerializeField] private Text speechText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Timing & Motion")]
        [SerializeField] private float defaultDuration = 3.2f;
        [SerializeField] private float floatSpeed = 0.08f;
        [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 2.15f, 0f);

        public float FloatSpeed => floatSpeed;

        private Transform targetFollow;
        private Transform cameraTransform;
        private Coroutine activeRoutine;

        private void Awake()
        {
            EnsureComponents();
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void EnsureComponents()
        {
            if (worldCanvas == null)
            {
                worldCanvas = GetComponent<Canvas>();
                if (worldCanvas == null) worldCanvas = gameObject.AddComponent<Canvas>();
                worldCanvas.renderMode = RenderMode.WorldSpace;
                worldCanvas.sortingOrder = 30;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Ensure no raycast blocking
            GraphicRaycaster gr = GetComponent<GraphicRaycaster>();
            if (gr != null) gr.enabled = false;

            if (bubbleBackground == null)
            {
                Transform bg = transform.Find("Bubble_BG");
                if (bg != null) bubbleBackground = bg.GetComponent<Image>();
                else
                {
                    GameObject bgObj = new GameObject("Bubble_BG");
                    bgObj.transform.SetParent(transform, false);
                    bubbleBackground = bgObj.AddComponent<Image>();
                    bubbleBackground.color = new Color(0.08f, 0.11f, 0.16f, 0.92f); // Tactical dark slate
                    bubbleBackground.raycastTarget = false;
                }
            }

            if (speechText == null)
            {
                Transform t = transform.Find("Speech_Text");
                if (t != null) speechText = t.GetComponent<Text>();
                else
                {
                    GameObject tObj = new GameObject("Speech_Text");
                    tObj.transform.SetParent(bubbleBackground.transform, false);
                    speechText = tObj.AddComponent<Text>();
                    speechText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    speechText.fontSize = 14;
                    speechText.alignment = TextAnchor.MiddleCenter;
                    speechText.color = new Color(0.95f, 0.95f, 0.96f, 1f);
                    speechText.raycastTarget = false;
                }
            }

            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(3.2f, 1.1f);
                rect.localScale = new Vector3(0.012f, 0.012f, 0.012f);
            }

            if (bubbleBackground != null)
            {
                RectTransform bgRect = bubbleBackground.rectTransform;
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
            }

            if (speechText != null)
            {
                RectTransform tRect = speechText.rectTransform;
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.offsetMin = new Vector2(12f, 8f);
                tRect.offsetMax = new Vector2(-12f, -8f);
            }

            gameObject.SetActive(false);
        }

        public void BindTarget(Transform followTarget)
        {
            targetFollow = followTarget;
        }

        public void ShowSpeech(string text, float duration = -1f)
        {
            if (duration <= 0f) duration = defaultDuration;

            EnsureComponents();
            if (activeRoutine != null) StopCoroutine(activeRoutine);

            gameObject.SetActive(true);
            speechText.text = text;

            activeRoutine = StartCoroutine(DisplayRoutine(duration));
        }

        private IEnumerator DisplayRoutine(float duration)
        {
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;

            Vector3 baseScale = new Vector3(0.013f, 0.013f, 0.013f);
            float elapsed = 0f;

            // Pop in
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.2f);
                float spring = Mathf.Sin(t * Mathf.PI * 0.5f);
                transform.localScale = baseScale * spring;
                canvasGroup.alpha = t;
                yield return null;
            }

            transform.localScale = baseScale;
            canvasGroup.alpha = 1f;

            // Wait visible
            yield return new WaitForSeconds(duration - 0.45f);

            // Fade out
            elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.35f);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            gameObject.SetActive(false);
            activeRoutine = null;
        }

        private void LateUpdate()
        {
            if (targetFollow != null)
            {
                Vector3 bobbing = Vector3.up * (Mathf.Sin(Time.time * 2.5f) * floatSpeed);
                transform.position = targetFollow.position + defaultOffset + bobbing;
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (cameraTransform != null)
            {
                transform.rotation = cameraTransform.rotation;
            }
        }
    }
}
