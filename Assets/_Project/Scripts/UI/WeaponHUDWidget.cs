using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    public class WeaponHUDWidget : MonoBehaviour
    {
        public static WeaponHUDWidget Instance { get; private set; }

        [Header("Weapon Card UI")]
        [SerializeField] private Button weaponCardButton;
        [SerializeField] private Image weaponIconImage;
        [SerializeField] private Text weaponNameText;
        [SerializeField] private Text ammoCounterText;

        [Header("Tactile Mini Buttons")]
        [SerializeField] private Button holsterButton;
        [SerializeField] private Button switchButton;
        [SerializeField] private Button reloadButton;
        [SerializeField] private Button flashlightButton;

        [Header("Weapon Icons")]
        [SerializeField] private Sprite unarmedFistSprite;
        [SerializeField] private Sprite axeSprite;
        [SerializeField] private Sprite pistolSprite;
        [SerializeField] private Sprite shotgunSprite;
        [SerializeField] private Sprite crossbowSprite;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (weaponCardButton != null)
            {
                weaponCardButton.onClick.RemoveAllListeners();
                weaponCardButton.onClick.AddListener(OnWeaponCardClicked);
            }

            if (holsterButton != null)
            {
                holsterButton.onClick.RemoveAllListeners();
                holsterButton.onClick.AddListener(OnHolsterClicked);
            }

            if (switchButton != null)
            {
                switchButton.onClick.RemoveAllListeners();
                switchButton.onClick.AddListener(OnSwitchClicked);
            }

            if (reloadButton != null)
            {
                reloadButton.onClick.RemoveAllListeners();
                reloadButton.onClick.AddListener(OnReloadClicked);
            }

            if (flashlightButton != null)
            {
                flashlightButton.onClick.RemoveAllListeners();
                flashlightButton.onClick.AddListener(OnFlashlightClicked);
            }
        }

        private void Start()
        {
            if (WeaponSystem.Instance != null)
            {
                WeaponSystem.Instance.BroadcastWeaponState();
            }
            else
            {
                HandleWeaponChanged("ELLER BOŞ (SİLAHSIZ)", 0, 0);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnWeaponChanged += HandleWeaponChanged;
            if (WeaponSystem.Instance != null)
            {
                WeaponSystem.Instance.BroadcastWeaponState();
            }
        }

        private void OnDisable()
        {
            GameEvents.OnWeaponChanged -= HandleWeaponChanged;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;

            if (Input.GetKeyDown(KeyCode.H))
            {
                OnHolsterClicked();
            }

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Tab))
            {
                OnSwitchClicked();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                OnReloadClicked();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                OnFlashlightClicked();
            }
        }

        public void Configure(Button cardBtn, Image iconImg, Text nameTxt, Text ammoTxt,
                              Button hBtn, Button sBtn,
                              Sprite fist, Sprite axe, Sprite pistol, Sprite shotgun, Sprite crossbow = null,
                              Button rBtn = null, Button fBtn = null)
        {
            weaponCardButton = cardBtn;
            weaponIconImage = iconImg;
            weaponNameText = nameTxt;
            ammoCounterText = ammoTxt;
            holsterButton = hBtn;
            switchButton = sBtn;
            if (rBtn != null) reloadButton = rBtn;
            if (fBtn != null) flashlightButton = fBtn;

            unarmedFistSprite = fist;
            axeSprite = axe;
            pistolSprite = pistol;
            shotgunSprite = shotgun;
            if (crossbow != null) crossbowSprite = crossbow;

            if (weaponCardButton != null)
            {
                weaponCardButton.onClick.RemoveAllListeners();
                weaponCardButton.onClick.AddListener(OnWeaponCardClicked);
            }

            if (holsterButton != null)
            {
                holsterButton.onClick.RemoveAllListeners();
                holsterButton.onClick.AddListener(OnHolsterClicked);
            }

            if (switchButton != null)
            {
                switchButton.onClick.RemoveAllListeners();
                switchButton.onClick.AddListener(OnSwitchClicked);
            }

            if (reloadButton != null)
            {
                reloadButton.onClick.RemoveAllListeners();
                reloadButton.onClick.AddListener(OnReloadClicked);
            }

            if (flashlightButton != null)
            {
                flashlightButton.onClick.RemoveAllListeners();
                flashlightButton.onClick.AddListener(OnFlashlightClicked);
            }
        }

        private void OnWeaponCardClicked()
        {
            WeaponSystem.Instance?.CycleNextWeapon();
        }

        private void OnHolsterClicked()
        {
            WeaponSystem.Instance?.HolsterWeapon();
        }

        private void OnSwitchClicked()
        {
            WeaponSystem.Instance?.CycleNextWeapon();
        }

        public void OnReloadClicked()
        {
            WeaponSystem.Instance?.Reload();
        }

        public void OnFlashlightClicked()
        {
            FlashlightController.Instance?.ToggleFlashlight();
        }

        private void HandleWeaponChanged(string weaponName, int clip, int reserve)
        {
            if (weaponNameText != null)
            {
                weaponNameText.text = weaponName;
            }

            if (ammoCounterText != null)
            {
                if (weaponName.StartsWith("ELLER BOŞ") || weaponName.StartsWith("YUMRUK") || weaponName.StartsWith("İTFAİYE BALTASI"))
                {
                    ammoCounterText.text = "---";
                }
                else
                {
                    ammoCounterText.text = $"{clip} / {reserve}";
                }
            }

            if (weaponIconImage != null)
            {
                Sprite chosen = null;
                if (weaponName.StartsWith("ELLER BOŞ") || weaponName.StartsWith("YUMRUK")) chosen = unarmedFistSprite;
                else if (weaponName.StartsWith("İTFAİYE BALTASI")) chosen = axeSprite;
                else if (weaponName.StartsWith("9MM TABANCA")) chosen = pistolSprite;
                else if (weaponName.StartsWith("POMPALI TÜFEK")) chosen = shotgunSprite;
                else if (weaponName.StartsWith("TAKTİK TATAR YAYI")) chosen = crossbowSprite != null ? crossbowSprite : shotgunSprite;

                weaponIconImage.sprite = chosen;
                weaponIconImage.enabled = chosen != null;
            }
        }
    }
}
