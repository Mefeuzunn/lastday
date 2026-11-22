using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.UI;

namespace ProjectSurvival.Narrative
{
    /// <summary>
    /// Active Phase 5: Collectible Scientist Physical Voice Recorder.
    /// Emits a subtle pulsing cyan LED beacon (#00F0FF), plays analog cassette playback cues,
    /// triggers auto-fading Turkish transcript subtitle banners, and awards +35 XP.
    /// </summary>
    public class AudioLogInteractable : WorldInteractableObject
    {
        [Header("Audio Log Data")]
        [SerializeField] private string logId = "log_safehouse_origin";
        [SerializeField] private string speakerName = "Dr. Selin Kaya";
        [TextArea(3, 8)]
        [SerializeField] private string transcriptText = "Karantina protokolü ihlal edildi...";

        [Header("Pulsing Cyan LED Visuals")]
        [SerializeField] private Light cyanLedLight;
        [SerializeField] private Renderer ledMeshRenderer;
        [SerializeField] private Color cyanGlowColor = new Color(0f, 0.94f, 1f, 1f);

        public string LogId => logId;
        public string SpeakerName => speakerName;
        public string TranscriptText => transcriptText;

        protected override void Awake()
        {
            objectTitle = "DİJİTAL SES KAYDEDİCİ";
            objectSubtitle = speakerName;
            base.Awake();

            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if (cyanLedLight == null)
            {
                cyanLedLight = GetComponentInChildren<Light>();
                if (cyanLedLight == null)
                {
                    GameObject lObj = new GameObject("Cyan_LED");
                    lObj.transform.SetParent(transform, false);
                    lObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                    cyanLedLight = lObj.AddComponent<Light>();
                    cyanLedLight.type = LightType.Point;
                    cyanLedLight.range = 2.5f;
                    cyanLedLight.color = cyanGlowColor;
                    cyanLedLight.intensity = 0.8f;
                }
            }

            if (ledMeshRenderer == null)
            {
                ledMeshRenderer = GetComponentInChildren<Renderer>();
            }
        }

        private void Update()
        {
            // Subtle cyan pulsing emission
            float pulse = Mathf.Sin(Time.time * 2.8f) * 0.35f + 0.65f;
            if (cyanLedLight != null)
            {
                cyanLedLight.intensity = pulse * 0.9f;
            }

            if (ledMeshRenderer != null && propBlock != null)
            {
                ledMeshRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_EmissionColor", cyanGlowColor * (pulse * 0.7f));
                ledMeshRenderer.SetPropertyBlock(propBlock);
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();
            bool alreadyListened = MainStoryDirector.Instance != null && MainStoryDirector.Instance.IsAudioLogCollected(logId);
            string label = alreadyListened ? "SES KAYDINI TEKRAR DİNLE" : "SES KAYDINI DİNLE (+35 XP)";

            verbs.Add(new InteractionVerb("listen", label, 0.4f, null, null, 8f, true));
            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "listen")
            {
                PlayAudioLog();
            }
        }

        public void PlayAudioLog()
        {
            ProceduralAudioManager.Instance?.PlayAudioLogPlaybackCue();
            MobileHapticManager.TriggerLight();

            RadioTranscriptBannerUI.Instance?.DisplayTranscript(speakerName, transcriptText, 9.0f);
            MainStoryDirector.Instance?.RegisterAudioLogCollected(logId);

            hasBeenSearched = true;
        }

        public void ConfigureLog(string id, string title, string speaker, string transcript)
        {
            logId = id;
            objectTitle = title;
            speakerName = speaker;
            transcriptText = transcript;
            objectSubtitle = speaker;
        }
    }
}
