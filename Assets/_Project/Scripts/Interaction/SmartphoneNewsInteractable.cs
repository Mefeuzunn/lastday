using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.UI;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Step B: Interactive Nightstand Smartphone.
    /// Sits on top of the bedroom nightstand beside the player's bed in StarterHouse_Safehome.
    /// Emits a bright pulsing cyan screen glow and displays a diegetic floating prompt: "AKILLI TELEFON (Haberlere Bak)".
    /// Supports 3 effortless interaction modalities within 2.6m:
    /// 1. Btn_PrimaryInteract (AL / ETKİLEŞ) via PlayerInteractionScanner & ContextualInteractionHUD,
    /// 2. Keyboard E or F in Editor,
    /// 3. Direct screen tap / click on the 3D smartphone.
    /// </summary>
    public class SmartphoneNewsInteractable : WorldInteractableObject
    {
        [Header("Smartphone Screen Glow")]
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private Light screenGlowLight;
        [SerializeField] private Color glowColor = new Color(0.2f, 0.75f, 1.0f); // Cyan notification glow
        [SerializeField] private float pulseSpeed = 2.4f;

        [Header("Floating Diegetic Badge")]
        [SerializeField] private Canvas floatingBadgeCanvas;
        [SerializeField] private Text floatingBadgeText;
        [SerializeField] private float badgeMaxViewDistance = 6.0f;

        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock localBlock;
        private Camera mainCamera;

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "AKILLI TELEFON";
            objectSubtitle = "Haberlere Bak";

            // Proximity interaction trigger: 2.6m sphere
            var sc = GetComponent<SphereCollider>();
            if (sc == null)
            {
                sc = gameObject.AddComponent<SphereCollider>();
            }
            sc.isTrigger = true;
            sc.radius = 2.6f;

            // Direct tap raycast collider
            var bc = GetComponent<BoxCollider>();
            if (bc == null)
            {
                bc = gameObject.AddComponent<BoxCollider>();
            }
            bc.isTrigger = false;
            bc.size = new Vector3(0.18f, 0.04f, 0.12f);

            if (screenRenderer == null)
            {
                screenRenderer = GetComponentInChildren<Renderer>();
            }

            if (screenGlowLight == null)
            {
                screenGlowLight = GetComponentInChildren<Light>();
                if (screenGlowLight == null)
                {
                    GameObject lightObj = new GameObject("Screen_Glow_Light");
                    lightObj.transform.SetParent(transform, false);
                    lightObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                    screenGlowLight = lightObj.AddComponent<Light>();
                    screenGlowLight.type = LightType.Point;
                    screenGlowLight.shadows = LightShadows.None;
                    screenGlowLight.range = 1.35f;
                    screenGlowLight.color = glowColor;
                }
            }

            EnsureFloatingBadge();
        }

        private void Start()
        {
            mainCamera = Camera.main;
            EnsureFloatingBadge();
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            bool hasChecked = ApartmentWakeUpSequence.Instance != null && ApartmentWakeUpSequence.Instance.HasCheckedPhone;
            float intensity = hasChecked ? 0.25f : (0.65f + 0.35f * Mathf.Sin(Time.time * pulseSpeed));

            // 1. Screen Light Pulse
            if (screenGlowLight != null)
            {
                screenGlowLight.intensity = intensity * 1.0f;
                screenGlowLight.color = hasChecked ? new Color(0.3f, 0.5f, 0.7f) : glowColor;
            }

            // 2. Emissive Screen Material Glow
            if (screenRenderer != null)
            {
                if (localBlock == null) localBlock = new MaterialPropertyBlock();
                try
                {
                    screenRenderer.GetPropertyBlock(localBlock);
                    Color currentGlow = (hasChecked ? Color.gray * 0.15f : glowColor) * intensity;
                    localBlock.SetColor(EmissionColorProp, currentGlow);
                    screenRenderer.SetPropertyBlock(localBlock);
                }
                catch (System.Exception)
                {
                    localBlock = new MaterialPropertyBlock();
                }
            }

            // 3. Floating Diegetic Billboard Badge
            UpdateFloatingBadge(hasChecked);
        }

        private void EnsureFloatingBadge()
        {
            if (floatingBadgeCanvas != null) return;

            Transform existing = transform.Find("FloatingBadgeCanvas");
            if (existing != null)
            {
                floatingBadgeCanvas = existing.GetComponent<Canvas>();
                floatingBadgeText = existing.GetComponentInChildren<Text>();
                if (floatingBadgeCanvas != null) return;
            }

            GameObject canvasObj = new GameObject("FloatingBadgeCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 0.28f, 0f);

            floatingBadgeCanvas = canvasObj.AddComponent<Canvas>();
            floatingBadgeCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform rtCanvas = canvasObj.GetComponent<RectTransform>();
            rtCanvas.sizeDelta = new Vector2(300f, 54f);
            rtCanvas.localScale = new Vector3(0.0035f, 0.0035f, 0.0035f);

            // Background
            GameObject bgObj = new GameObject("Badge_Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtBg = bgObj.AddComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.07f, 0.11f, 0.94f);
            bgImg.raycastTarget = false;

            // Frame Outline
            GameObject frameObj = new GameObject("Badge_Frame");
            frameObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtFrame = frameObj.AddComponent<RectTransform>();
            rtFrame.anchorMin = Vector2.zero;
            rtFrame.anchorMax = Vector2.one;
            rtFrame.sizeDelta = Vector2.zero;
            Outline outline = frameObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.75f, 1.0f, 0.90f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Label Text
            GameObject textObj = new GameObject("Badge_Text");
            textObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtText = textObj.AddComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.sizeDelta = Vector2.zero;

            floatingBadgeText = textObj.AddComponent<Text>();
            floatingBadgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            floatingBadgeText.fontSize = 20;
            floatingBadgeText.fontStyle = FontStyle.Bold;
            floatingBadgeText.alignment = TextAnchor.MiddleCenter;
            floatingBadgeText.color = new Color(0.35f, 0.85f, 1.0f);
            floatingBadgeText.text = "AKILLI TELEFON\n(Haberlere Bak)";
            floatingBadgeText.raycastTarget = false;
        }

        private void UpdateFloatingBadge(bool hasChecked)
        {
            if (floatingBadgeCanvas == null) return;

            if (hasChecked)
            {
                if (floatingBadgeCanvas.gameObject.activeSelf)
                {
                    floatingBadgeCanvas.gameObject.SetActive(false);
                }
                return;
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            float distToCam = Vector3.Distance(transform.position, mainCamera.transform.position);
            bool inRange = distToCam <= badgeMaxViewDistance;

            if (floatingBadgeCanvas.gameObject.activeSelf != inRange)
            {
                floatingBadgeCanvas.gameObject.SetActive(inRange);
            }

            if (inRange)
            {
                Vector3 toCam = floatingBadgeCanvas.transform.position - mainCamera.transform.position;
                if (toCam.sqrMagnitude > 0.001f)
                {
                    floatingBadgeCanvas.transform.rotation = Quaternion.LookRotation(toCam);
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

        /// <summary>
        /// Direct screen tap / mouse click handler on the 3D smartphone.
        /// </summary>
        private void OnMouseDown()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null && Vector3.Distance(player.transform.position, transform.position) <= 3.2f)
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
            Debug.Log("[SmartphoneNewsInteractable] Player accessed nightstand smartphone.");
        }
    }
}
