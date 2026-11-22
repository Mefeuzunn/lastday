using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.UI;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Interactive 3D World Pickup for Rare Weapon Attachment Kits.
    /// Unlocks and automatically equips the modification upon single-tap pickup.
    /// Features pulsing golden ring, diegetic floating billboard badge, and map tracking.
    /// </summary>
    public class AttachmentKitPickupInteractable : WorldInteractableObject
    {
        [Header("Kit Configuration")]
        [SerializeField] private string modId = WeaponAttachmentManager.MOD_SUPPRESSOR;
        [SerializeField] private string kitDisplayName = "El Yapımı Susturucu Kiti";
        [SerializeField] private string kitSubtitle = "SES GİZLİLİĞİ (-%75 GÜRÜLTÜ)";

        [Header("Visual Feedback")]
        [SerializeField] private Color amberColor = new Color(1.0f, 0.72f, 0.01f, 0.95f);
        [SerializeField] private Transform groundRingTransform;
        [SerializeField] private Canvas floatingBadgeCanvas;
        [SerializeField] private Text floatingBadgeText;

        private float ringPulseTimer = 0f;
        private Camera mainCamera;
        private string mapMarkerId;

        public string ModId => modId;
        public string KitDisplayName => kitDisplayName;

        protected override void Awake()
        {
            base.Awake();
            objectTitle = $"SİLAH KİTİ: {kitDisplayName.ToUpper()}";
            objectSubtitle = kitSubtitle;
            mapMarkerId = $"kit_pickup_{PersistentId}";

            SetupPulsingRing();
            SetupFloatingBadge();
        }

        private void Start()
        {
            mainCamera = Camera.main;

            if (hasBeenSearched)
            {
                gameObject.SetActive(false);
                return;
            }

            RegisterOnMaps();
        }

        private void OnDestroy()
        {
            UnregisterFromMaps();
        }

        private void Update()
        {
            if (hasBeenSearched) return;

            // Pulse amber ground ring
            if (groundRingTransform != null)
            {
                ringPulseTimer += Time.deltaTime * 2.8f;
                float scaleMod = 1.0f + Mathf.Sin(ringPulseTimer) * 0.12f;
                groundRingTransform.localScale = new Vector3(scaleMod, scaleMod, 1f);
            }

            // Billboard floating badge toward camera within 14m
            if (floatingBadgeCanvas != null)
            {
                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    float dist = Vector3.Distance(mainCamera.transform.position, transform.position);
                    bool inRange = dist <= 14.0f;
                    floatingBadgeCanvas.gameObject.SetActive(inRange);

                    if (inRange)
                    {
                        floatingBadgeCanvas.transform.rotation = Quaternion.LookRotation(floatingBadgeCanvas.transform.position - mainCamera.transform.position);
                    }
                }
            }
        }

        public void Configure(string mId, string name, string subtitle)
        {
            modId = mId;
            kitDisplayName = name;
            kitSubtitle = subtitle;
            objectTitle = $"SİLAH KİTİ: {kitDisplayName.ToUpper()}";
            objectSubtitle = kitSubtitle;

            if (floatingBadgeText != null)
            {
                floatingBadgeText.text = $"SİLAH KİTİ: {kitDisplayName.ToUpper()}";
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();
            if (!hasBeenSearched)
            {
                verbs.Add(new InteractionVerb(
                    "pickup_kit",
                    $"KİTİ AL & KUŞAN: {kitDisplayName}",
                    0f, // Instant Single-Tap
                    null,
                    null,
                    0f
                ));
            }
            return verbs;
        }

        public override bool CanInteract(string verbId)
        {
            if (hasBeenSearched) return false;
            return verbId == "pickup_kit";
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "pickup_kit" && !hasBeenSearched)
            {
                hasBeenSearched = true;

                // Unlock & Equip modification
                if (WeaponAttachmentManager.Instance != null)
                {
                    WeaponAttachmentManager.Instance.UnlockAttachment(modId);
                    WeaponAttachmentManager.Instance.EquipAttachment(modId);
                }

                ProceduralAudioManager.Instance?.PlayCraftSuccess();
                ProceduralAudioManager.Instance?.PlayItemPickup();

                GameEvents.TriggerObjectiveToastRequested($"[EKLENTİ BULUNDU] {kitDisplayName} kilidi açıldı ve kuşanıldı!", 3.5f);
                GameEvents.TriggerCameraShakeRequested(0.2f, 0.2f);

                UnregisterFromMaps();
                gameObject.SetActive(false);
                Debug.Log($"[AttachmentKitPickup] Player picked up and equipped {kitDisplayName} ({modId})");
            }
        }

        private void RegisterOnMaps()
        {
            Vector2 mapPos = new Vector2(transform.position.x, transform.position.z);
            MinimapHUDWidget.Instance?.RegisterDynamicPoi(mapMarkerId, mapPos, amberColor);
            FullTacticalMapUI.Instance?.RegisterDynamicWeaponMarker(mapMarkerId, $"Kit: {kitDisplayName}", mapPos);
        }

        private void UnregisterFromMaps()
        {
            MinimapHUDWidget.Instance?.UnregisterDynamicPoi(mapMarkerId);
            FullTacticalMapUI.Instance?.UnregisterDynamicWeaponMarker(mapMarkerId);
        }

        private void SetupPulsingRing()
        {
            if (groundRingTransform != null) return;

            GameObject ringObj = new GameObject("PulsingGroundRing");
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ringObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            groundRingTransform = ringObj.transform;

            LineRenderer lr = ringObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 36;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;

            Shader ringShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (ringShader != null)
            {
                Material mat = new Material(ringShader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", amberColor);
                else mat.color = amberColor;
                lr.material = mat;
            }

            lr.startColor = amberColor;
            lr.endColor = amberColor;

            float radius = 0.70f;
            for (int i = 0; i < 36; i++)
            {
                float angle = i * (Mathf.PI * 2f / 36);
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void SetupFloatingBadge()
        {
            if (floatingBadgeCanvas != null) return;

            GameObject canvasObj = new GameObject("FloatingBadgeCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 1.25f, 0f);

            floatingBadgeCanvas = canvasObj.AddComponent<Canvas>();
            floatingBadgeCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform rtCanvas = canvasObj.GetComponent<RectTransform>();
            rtCanvas.sizeDelta = new Vector2(280f, 48f);
            rtCanvas.localScale = new Vector3(0.007f, 0.007f, 0.007f);

            GameObject bgObj = new GameObject("Badge_Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtBg = bgObj.AddComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
            bgImg.raycastTarget = false;

            GameObject textObj = new GameObject("Badge_Text");
            textObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtText = textObj.AddComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.sizeDelta = Vector2.zero;

            floatingBadgeText = textObj.AddComponent<Text>();
            floatingBadgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            floatingBadgeText.fontSize = 19;
            floatingBadgeText.fontStyle = FontStyle.Bold;
            floatingBadgeText.alignment = TextAnchor.MiddleCenter;
            floatingBadgeText.color = amberColor;
            floatingBadgeText.text = $"SİLAH KİTİ: {kitDisplayName.ToUpper()}";
            floatingBadgeText.raycastTarget = false;

            floatingBadgeCanvas.gameObject.SetActive(false);
        }

        public override string GetSerializedState() => hasBeenSearched ? "1" : "0";
        public override void LoadSerializedState(string state)
        {
            hasBeenSearched = (state == "1");
            if (hasBeenSearched) gameObject.SetActive(false);
        }
    }
}
