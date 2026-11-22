using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Phase 1 (Step 1B): Dynamic Accuracy Crosshair Bloom.
    /// Drives smooth stance-reactive crosshair spread (Crouch: 12px, Walk: 20px, Sprint: 34px),
    /// dynamic weapon fire bloom (+14px Pistol, +24px Shotgun), laser sight bonus (-35%),
    /// and clean collapse into a focused center dot when unarmed/holstered.
    /// All tick bars and dot have raycastTarget = false.
    /// </summary>
    public class DynamicCrosshairUI : MonoBehaviour
    {
        public static DynamicCrosshairUI Instance { get; private set; }

        [Header("Crosshair Elements")]
        [SerializeField] private RectTransform crosshairRoot;
        [SerializeField] private RectTransform tickTop;
        [SerializeField] private RectTransform tickBottom;
        [SerializeField] private RectTransform tickLeft;
        [SerializeField] private RectTransform tickRight;
        [SerializeField] private RectTransform centerDot;

        [Header("Stance Spreads (Pixels)")]
        [SerializeField] private float crouchSpread = 12f;
        [SerializeField] private float idleSpread = 16f;
        [SerializeField] private float walkSpread = 20f;
        [SerializeField] private float sprintSpread = 34f;
        [SerializeField] private float aimSpreadMultiplier = 0.65f;
        [SerializeField] private float laserSightSpreadMultiplier = 0.65f; // -35%

        [Header("Recoil Recovery")]
        [SerializeField] private float bloomDecaySpeed = 65f;
        [SerializeField] private float spreadSmoothTime = 0.06f;

        [Header("Visual Colors")]
        [SerializeField] private Color tickColor = new Color(1f, 1f, 1f, 0.90f);
        [SerializeField] private Color dotColor = new Color(1f, 1f, 1f, 0.75f);

        private float currentSpread = 16f;
        private float spreadVelocity = 0f;
        private float currentBloom = 0f;
        private Image[] tickImages;
        private Image dotImage;

        private TPSPlayerController playerController;
        private WeaponSystem weaponSystem;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeElements();
        }

        private void Start()
        {
            FindReferences();
        }

        private void OnEnable()
        {
            GameEvents.OnWeaponFired += HandleWeaponFired;
        }

        private void OnDisable()
        {
            GameEvents.OnWeaponFired -= HandleWeaponFired;
        }

        private void FindReferences()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<TPSPlayerController>();
            }
            if (weaponSystem == null)
            {
                weaponSystem = WeaponSystem.Instance ?? FindFirstObjectByType<WeaponSystem>();
            }
        }

        private void InitializeElements()
        {
            if (crosshairRoot == null)
            {
                GameObject crossObj = GameObject.Find("Crosshair_Panel");
                if (crossObj != null)
                {
                    crosshairRoot = crossObj.GetComponent<RectTransform>();
                }
            }

            if (crosshairRoot == null)
            {
                Canvas hudCanvas = FindFirstObjectByType<Canvas>();
                if (hudCanvas != null)
                {
                    GameObject crossObj = new GameObject("Crosshair_Panel", typeof(RectTransform));
                    crossObj.transform.SetParent(hudCanvas.transform, false);
                    crosshairRoot = crossObj.GetComponent<RectTransform>();
                    crosshairRoot.anchorMin = new Vector2(0.5f, 0.5f);
                    crosshairRoot.anchorMax = new Vector2(0.5f, 0.5f);
                    crosshairRoot.pivot = new Vector2(0.5f, 0.5f);
                    crosshairRoot.anchoredPosition = Vector2.zero;
                    crosshairRoot.sizeDelta = new Vector2(60f, 60f);
                }
            }

            if (crosshairRoot != null)
            {
                // Find or build 4 ticks
                tickTop = FindOrCreateTick("Tick_Top", new Vector2(2f, 10f), new Vector2(0f, idleSpread));
                tickBottom = FindOrCreateTick("Tick_Bottom", new Vector2(2f, 10f), new Vector2(0f, -idleSpread));
                tickLeft = FindOrCreateTick("Tick_Left", new Vector2(10f, 2f), new Vector2(-idleSpread, 0f));
                tickRight = FindOrCreateTick("Tick_Right", new Vector2(10f, 2f), new Vector2(idleSpread, 0f));

                // Find or build center dot
                Transform existingDot = crosshairRoot.Find("Dot_Center");
                if (existingDot == null)
                {
                    GameObject dotObj = new GameObject("Dot_Center", typeof(RectTransform));
                    dotObj.transform.SetParent(crosshairRoot, false);
                    centerDot = dotObj.GetComponent<RectTransform>();
                    centerDot.anchorMin = new Vector2(0.5f, 0.5f);
                    centerDot.anchorMax = new Vector2(0.5f, 0.5f);
                    centerDot.pivot = new Vector2(0.5f, 0.5f);
                    centerDot.anchoredPosition = Vector2.zero;
                    centerDot.sizeDelta = new Vector2(3f, 3f);

                    dotImage = dotObj.AddComponent<Image>();
                    dotImage.color = dotColor;
                    dotImage.raycastTarget = false;
                }
                else
                {
                    centerDot = existingDot.GetComponent<RectTransform>();
                    dotImage = existingDot.GetComponent<Image>();
                    if (dotImage != null) dotImage.raycastTarget = false;
                }

                tickImages = new Image[]
                {
                    tickTop.GetComponent<Image>(),
                    tickBottom.GetComponent<Image>(),
                    tickLeft.GetComponent<Image>(),
                    tickRight.GetComponent<Image>()
                };

                foreach (var img in tickImages)
                {
                    if (img != null)
                    {
                        img.raycastTarget = false;
                        img.color = tickColor;
                    }
                }
            }
        }

        private RectTransform FindOrCreateTick(string name, Vector2 size, Vector2 initialPos)
        {
            Transform t = crosshairRoot.Find(name);
            if (t != null)
            {
                var rt = t.GetComponent<RectTransform>();
                var img = t.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
                return rt;
            }

            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(crosshairRoot, false);
            RectTransform nrt = obj.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.5f, 0.5f);
            nrt.anchorMax = new Vector2(0.5f, 0.5f);
            nrt.pivot = new Vector2(0.5f, 0.5f);
            nrt.anchoredPosition = initialPos;
            nrt.sizeDelta = size;

            Image nImg = obj.AddComponent<Image>();
            nImg.color = tickColor;
            nImg.raycastTarget = false;
            return nrt;
        }

        private void Update()
        {
            if (crosshairRoot == null) return;
            FindReferences();

            // Decay recoil bloom
            if (currentBloom > 0f)
            {
                currentBloom = Mathf.Max(0f, currentBloom - bloomDecaySpeed * Time.deltaTime);
            }

            // Determine equipped weapon & state
            WeaponType weapon = weaponSystem != null ? weaponSystem.CurrentWeapon : WeaponType.None;
            bool isUnarmed = weapon == WeaponType.None;

            if (isUnarmed)
            {
                // Collapse into focused center dot
                SetTicksVisible(false);
                if (centerDot != null) centerDot.gameObject.SetActive(true);
                return;
            }

            SetTicksVisible(true);

            // Compute stance base spread
            float targetSpread = idleSpread;
            if (playerController != null)
            {
                if (playerController.IsCrouching) targetSpread = crouchSpread;
                else if (playerController.IsSprinting) targetSpread = sprintSpread;
                else if (playerController.IsMoving) targetSpread = walkSpread;
            }

            // Weapon aiming modifier
            if (weaponSystem != null && weaponSystem.IsAiming)
            {
                if (weapon == WeaponType.Crossbow)
                {
                    // Scoped pinpoint accuracy (10px baseline when stationary/crouched)
                    targetSpread = Mathf.Min(targetSpread * 0.4f, 10f);
                }
                else
                {
                    targetSpread *= aimSpreadMultiplier;
                }
            }

            // Laser Sight modifier
            if (WeaponAttachmentManager.Instance != null && WeaponAttachmentManager.Instance.HasLaserSight(weapon))
            {
                targetSpread *= laserSightSpreadMultiplier;
            }

            // Add recoil bloom
            targetSpread += currentBloom;

            // Smooth interpolation
            currentSpread = Mathf.SmoothDamp(currentSpread, targetSpread, ref spreadVelocity, spreadSmoothTime);

            // Apply tick positions
            if (tickTop != null) tickTop.anchoredPosition = new Vector2(0f, currentSpread);
            if (tickBottom != null) tickBottom.anchoredPosition = new Vector2(0f, -currentSpread);
            if (tickLeft != null) tickLeft.anchoredPosition = new Vector2(-currentSpread, 0f);
            if (tickRight != null) tickRight.anchoredPosition = new Vector2(currentSpread, 0f);
        }

        private void SetTicksVisible(bool visible)
        {
            if (tickTop != null && tickTop.gameObject.activeSelf != visible) tickTop.gameObject.SetActive(visible);
            if (tickBottom != null && tickBottom.gameObject.activeSelf != visible) tickBottom.gameObject.SetActive(visible);
            if (tickLeft != null && tickLeft.gameObject.activeSelf != visible) tickLeft.gameObject.SetActive(visible);
            if (tickRight != null && tickRight.gameObject.activeSelf != visible) tickRight.gameObject.SetActive(visible);
        }

        private void HandleWeaponFired()
        {
            WeaponType weapon = weaponSystem != null ? weaponSystem.CurrentWeapon : WeaponType.None;
            switch (weapon)
            {
                case WeaponType.Pistol:
                    AddRecoilBloom(14f);
                    break;
                case WeaponType.Shotgun:
                    AddRecoilBloom(24f);
                    break;
                case WeaponType.Axe:
                    AddRecoilBloom(8f);
                    break;
                case WeaponType.Crossbow:
                    AddRecoilBloom(5f);
                    break;
            }
        }

        /// <summary>
        /// Expands the crosshair with dynamic recoil bloom (in pixels).
        /// </summary>
        public void AddRecoilBloom(float amount)
        {
            currentBloom = Mathf.Clamp(currentBloom + amount, 0f, 60f);
        }
    }
}
