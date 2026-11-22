using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Interactive 3D Weapon Pickup Component.
    /// Features:
    /// - 3D weapon mesh mounted on crate, vehicle, or illuminated rack.
    /// - Golden-amber pulsing ground ring (#FFB703) visible from 25m.
    /// - Diegetic floating billboard badge visible within 14m.
    /// - Instant single-tap equip (holdDuration = 0f) with metallic audio and HUD update.
    /// - Live map tracking (Minimap and Full Tactical Map).
    /// </summary>
    public class WeaponPickupInteractable : WorldInteractableObject
    {
        [Header("Weapon Configuration")]
        [SerializeField] private WeaponType weaponType = WeaponType.Axe;
        [SerializeField] private string weaponItemId = "weapon_axe";
        [SerializeField] private string ammoItemId = "";
        [SerializeField] private int ammoCount = 0;
        [SerializeField] private string pickupDisplayName = "YANGIN BALTASI";
        [SerializeField] private string pickupSubLabel = "YAKIN DÖVÜŞ";

        [Header("Visual Feedback & Ring")]
        [SerializeField] private Color amberColor = new Color(1.0f, 0.72f, 0.01f, 0.95f); // #FFB703
        [SerializeField] private Transform groundRingTransform;
        [SerializeField] private Canvas floatingBadgeCanvas;
        [SerializeField] private Text floatingBadgeText;

        private float ringPulseTimer = 0f;
        private Camera mainCamera;
        private string mapMarkerId;

        public WeaponType WeaponType => weaponType;
        public string WeaponItemId => weaponItemId;
        public string PickupDisplayName => pickupDisplayName;

        public override string ObjectSubtitle => hasBeenSearched ? "TOPLANDI" : pickupSubLabel;

        protected override void Awake()
        {
            base.Awake();
            objectTitle = $"SİLAH: {pickupDisplayName.ToUpper()}";
            objectSubtitle = pickupSubLabel;
            mapMarkerId = $"weapon_pickup_{PersistentId}";

            // Guarantee active trigger sphere collider centered on weapon mesh
            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            if (sc.radius < 1.85f) sc.radius = 1.85f;

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

            float radius = 0.75f;
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
            canvasObj.transform.localPosition = new Vector3(0f, 1.35f, 0f);

            floatingBadgeCanvas = canvasObj.AddComponent<Canvas>();
            floatingBadgeCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform rtCanvas = canvasObj.GetComponent<RectTransform>();
            rtCanvas.sizeDelta = new Vector2(260f, 48f);
            rtCanvas.localScale = new Vector3(0.007f, 0.007f, 0.007f);

            // Badge Background
            GameObject bgObj = new GameObject("Badge_Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtBg = bgObj.AddComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
            bgImg.raycastTarget = false;

            // Border Frame
            GameObject frameObj = new GameObject("Badge_Frame");
            frameObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtFrame = frameObj.AddComponent<RectTransform>();
            rtFrame.anchorMin = Vector2.zero;
            rtFrame.anchorMax = Vector2.one;
            rtFrame.sizeDelta = Vector2.zero;
            Outline outline = frameObj.AddComponent<Outline>();
            outline.effectColor = amberColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Text Label
            GameObject textObj = new GameObject("Badge_Text");
            textObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtText = textObj.AddComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.sizeDelta = Vector2.zero;

            floatingBadgeText = textObj.AddComponent<Text>();
            floatingBadgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            floatingBadgeText.fontSize = 20;
            floatingBadgeText.fontStyle = FontStyle.Bold;
            floatingBadgeText.alignment = TextAnchor.MiddleCenter;
            floatingBadgeText.color = amberColor;
            floatingBadgeText.raycastTarget = false;

            string ammoStr = ammoCount > 0 ? $" (+{ammoCount})" : "";
            floatingBadgeText.text = $"SİLAH: {pickupDisplayName.ToUpper()}{ammoStr}";

            floatingBadgeCanvas.gameObject.SetActive(false);
        }

        public void Configure(WeaponType type, string wItemId, string aItemId, int aCount, string dName, string sLabel)
        {
            weaponType = type;
            weaponItemId = wItemId;
            ammoItemId = aItemId;
            ammoCount = aCount;
            pickupDisplayName = dName;
            pickupSubLabel = sLabel;
            objectTitle = $"SİLAH: {pickupDisplayName.ToUpper()}";
            objectSubtitle = pickupSubLabel;

            if (floatingBadgeText != null)
            {
                string ammoStr = ammoCount > 0 ? $" (+{ammoCount})" : "";
                floatingBadgeText.text = $"SİLAH: {pickupDisplayName.ToUpper()}{ammoStr}";
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();
            if (!hasBeenSearched)
            {
                string ammoDesc = ammoCount > 0 ? $" (+{ammoCount} Mermi)" : "";
                verbs.Add(new InteractionVerb(
                    "pickup_equip",
                    $"KUŞAN: {pickupDisplayName}{ammoDesc}",
                    0f, // Instant Single-Tap Equip!
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
            return verbId == "pickup_equip";
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "pickup_equip" && !hasBeenSearched)
            {
                ExecutePickupAndEquip();
            }
        }

        private void ExecutePickupAndEquip()
        {
            hasBeenSearched = true;

            // 1. Add weapon to inventory
            if (InventoryManager.Instance != null)
            {
                ItemData wData = InventorySystem.FindItemData(weaponItemId);
                if (wData != null && !InventorySystem.Instance.HasItem(weaponItemId, 1))
                {
                    InventorySystem.Instance.AddItem(wData, 1);
                }
                else
                {
                    InventoryManager.AddItem(weaponItemId, 1);
                }

                // 2. Add starting ammo if applicable
                if (ammoCount > 0 && !string.IsNullOrEmpty(ammoItemId))
                {
                    ItemData aData = InventorySystem.FindItemData(ammoItemId);
                    if (aData != null)
                    {
                        InventorySystem.Instance.AddItem(aData, ammoCount);
                    }
                    else
                    {
                        InventoryManager.AddItem(ammoItemId, ammoCount);
                    }
                }
            }

            // 3. Auto-equip immediately into player's hands
            if (WeaponSystem.Instance != null)
            {
                WeaponSystem.Instance.EquipWeapon(weaponType);
            }

            // Notify Starter Prologue Director if active
            ProjectSurvival.Narrative.StarterPrologueDirector.Instance?.OnWeaponPickedUp(weaponType, weaponItemId);

            // 4. Audio Feedback (metallic equip cue + item pickup) & Haptics
            ProceduralAudioManager.Instance?.PlayWeaponEquip();
            ProceduralAudioManager.Instance?.PlayItemPickup();
            MobileHapticManager.TriggerLight();

            // 5. Toast
            string ammoMsg = ammoCount > 0 ? $" ve {ammoCount} mermi" : "";
            GameEvents.TriggerObjectiveToastRequested($"[SİLAH KUŞANILDI] {pickupDisplayName}{ammoMsg} kuşanıldı!", 3.5f);

            // 6. Map unregistration
            UnregisterFromMaps();

            // 7. Visual disable
            gameObject.SetActive(false);
            Debug.Log($"[WeaponPickupInteractable] Player collected & auto-equipped: {pickupDisplayName} ({weaponType})");
        }

        private void RegisterOnMaps()
        {
            Vector2 mapPos = new Vector2(transform.position.x, transform.position.z);
            MinimapHUDWidget.Instance?.RegisterDynamicPoi(mapMarkerId, mapPos, amberColor);
            FullTacticalMapUI.Instance?.RegisterDynamicWeaponMarker(mapMarkerId, $"Silah: {pickupDisplayName}", mapPos);
        }

        private void UnregisterFromMaps()
        {
            MinimapHUDWidget.Instance?.UnregisterDynamicPoi(mapMarkerId);
            FullTacticalMapUI.Instance?.UnregisterDynamicWeaponMarker(mapMarkerId);
        }

        public override string GetSerializedState()
        {
            return hasBeenSearched ? "1" : "0";
        }

        public override void LoadSerializedState(string state)
        {
            hasBeenSearched = (state == "1");
            if (hasBeenSearched)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
