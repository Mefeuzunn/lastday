using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Active Phase 5: Auto-Fading Radio & Audio Log Transcript Banner.
    /// Positioned at bottom-center cleanly above QuickSlots_Bar, displaying real-time speaker name
    /// and Turkish dialogue transcripts with smooth canvas fade-in / fade-out animations.
    /// </summary>
    public class RadioTranscriptBannerUI : MonoBehaviour
    {
        public static RadioTranscriptBannerUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject bannerRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text speakerLabelText;
        [SerializeField] private Text transcriptBodyText;

        private Coroutine activeFadeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (bannerRoot != null) bannerRoot.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        public void DisplayTranscript(string speaker, string transcript, float duration = 8.0f)
        {
            if (bannerRoot != null) bannerRoot.SetActive(true);

            if (speakerLabelText != null)
            {
                speakerLabelText.text = speaker.ToUpper();
            }

            if (transcriptBodyText != null)
            {
                transcriptBodyText.text = $"\"{transcript}\"";
            }

            if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
            activeFadeRoutine = StartCoroutine(AnimateBanner(duration));
        }

        private IEnumerator AnimateBanner(float duration)
        {
            if (canvasGroup == null) yield break;

            // Fade in (0.3s)
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / 0.3f);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // Hold display
            yield return new WaitForSeconds(duration);

            // Fade out (0.6s)
            elapsed = 0f;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / 0.6f));
                yield return null;
            }
            canvasGroup.alpha = 0f;
            if (bannerRoot != null) bannerRoot.SetActive(false);
        }

        public void BindUIReferences(GameObject root, CanvasGroup cg, Text speaker, Text body)
        {
            bannerRoot = root;
            canvasGroup = cg;
            speakerLabelText = speaker;
            transcriptBodyText = body;
        }
    }
}
