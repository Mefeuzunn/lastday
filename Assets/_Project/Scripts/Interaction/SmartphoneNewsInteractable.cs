using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.UI;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Step B: Interactive Nightstand Smartphone.
    /// Sits on the wooden nightstand beside the player's bed in StarterHouse_Safehome.
    /// Emits a soft pulsing glow and opens the Emergency Broadcast UI when interacted with.
    /// </summary>
    public class SmartphoneNewsInteractable : WorldInteractableObject
    {
        [Header("Smartphone Screen Glow")]
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private Light screenGlowLight;
        [SerializeField] private Color glowColor = new Color(0.2f, 0.75f, 1.0f); // Cyan notification glow
        [SerializeField] private float pulseSpeed = 2.4f;

        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock localBlock;

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "AKILLI TELEFON";
            objectSubtitle = "Haberlere Bak";

            // Active SphereCollider for proximity interaction (radius = 1.65f, isTrigger = true)
            var sc = GetComponent<SphereCollider>();
            if (sc == null)
            {
                var bc = GetComponent<BoxCollider>();
                if (bc != null && bc.isTrigger)
                {
                    Destroy(bc);
                }
                sc = gameObject.AddComponent<SphereCollider>();
            }
            sc.isTrigger = true;
            sc.radius = 1.65f;

            if (screenRenderer == null)
            {
                screenRenderer = GetComponentInChildren<Renderer>();
            }

            if (screenGlowLight == null)
            {
                screenGlowLight = GetComponentInChildren<Light>();
            }
        }

        private void Update()
        {
            // Subtle pulse effect while phone has not been checked
            bool hasChecked = ApartmentWakeUpSequence.Instance != null && ApartmentWakeUpSequence.Instance.HasCheckedPhone;
            float intensity = hasChecked ? 0.35f : (0.5f + 0.45f * Mathf.Sin(Time.time * pulseSpeed));

            if (screenGlowLight != null)
            {
                screenGlowLight.intensity = intensity * 0.8f;
                screenGlowLight.color = hasChecked ? new Color(0.3f, 0.5f, 0.7f) : glowColor;
            }

            if (screenRenderer != null)
            {
                if (localBlock == null) localBlock = new MaterialPropertyBlock();
                try
                {
                    screenRenderer.GetPropertyBlock(localBlock);
                    Color currentGlow = (hasChecked ? Color.gray * 0.2f : glowColor) * intensity;
                    localBlock.SetColor(EmissionColorProp, currentGlow);
                    screenRenderer.SetPropertyBlock(localBlock);
                }
                catch (System.Exception)
                {
                    localBlock = new MaterialPropertyBlock();
                }
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();
            verbs.Add(new InteractionVerb("check_phone", "HABERLERE BAK", 0f, null, null, 0f, true));
            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "check_phone")
            {
                OpenSmartphone();
            }
        }

        public void OpenSmartphone()
        {
            ProceduralAudioManager.Instance?.PlayButtonClick();
            MobileHapticManager.TriggerLight();

            if (SmartphoneScreenUI.Instance != null)
            {
                SmartphoneScreenUI.Instance.ShowSmartphone();
            }

            hasBeenSearched = true;
            ApartmentWakeUpSequence.Instance?.MarkPhoneChecked();
            Debug.Log("[SmartphoneNewsInteractable] Player accessed nightstand smartphone.");
        }
    }
}
