using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Player
{
    /// <summary>
    /// Modular 3D Weapon Attachment & Gunsmith Modification System.
    /// Manages 4 physical upgrades rendered directly on player's 3D weapon models:
    /// 1. Suppressor (mod_suppressor) -> Pistol & Shotgun: -75% noise radius, suppressed gunfire sound, smoke puff.
    /// 2. TacticalLaserSight (mod_laser_sight) -> Pistol & Shotgun: -35% spread, crimson (#FF2A5F) laser beam & dot up to 45m.
    /// 3. ExtendedMagazine (mod_extended_mag) -> Pistol (18 rounds), Shotgun (8 shells), +15% faster reload.
    /// 4. ReinforcedAxeHead (mod_reinforced_axe) -> Fire Axe: +35% damage, heavy stagger knockback, -20% stamina cost.
    /// </summary>
    public class WeaponAttachmentManager : MonoBehaviour
    {
        private static WeaponAttachmentManager _instance;
        public static WeaponAttachmentManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<WeaponAttachmentManager>();
                    if (_instance == null)
                    {
                        GameObject player = GameObject.FindWithTag("Player");
                        if (player != null)
                        {
                            _instance = player.GetComponent<WeaponAttachmentManager>() ?? player.AddComponent<WeaponAttachmentManager>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public const string MOD_SUPPRESSOR = "mod_suppressor";
        public const string MOD_LASER_SIGHT = "mod_laser_sight";
        public const string MOD_EXTENDED_MAG = "mod_extended_mag";
        public const string MOD_REINFORCED_AXE = "mod_reinforced_axe";

        private readonly HashSet<string> unlockedMods = new HashSet<string>();
        private readonly HashSet<string> equippedMods = new HashSet<string>();

        [Header("3D Visual Attachment Meshes")]
        [SerializeField] private GameObject pistolSuppressorMesh;
        [SerializeField] private GameObject shotgunSuppressorMesh;
        [SerializeField] private GameObject pistolLaserModuleMesh;
        [SerializeField] private GameObject shotgunLaserModuleMesh;
        [SerializeField] private GameObject pistolExtendedMagMesh;
        [SerializeField] private GameObject shotgunExtendedTubeMesh;
        [SerializeField] private GameObject axeReinforcedPlatesMesh;
        [SerializeField] private GameObject axeParacordWrapMesh;

        [Header("Laser Sight Beam & Dot")]
        [SerializeField] private LineRenderer laserBeamRenderer;
        [SerializeField] private GameObject laserDotObject;
        [SerializeField] private Color laserColor = new Color(1.0f, 0.165f, 0.373f, 0.90f); // #FF2A5F Tactical Crimson
        [SerializeField] private float maxLaserRange = 45f;

        private Material gunMetalMaterial;
        private Material steelMaterial;
        private Material paracordMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    Instance = player.GetComponent<WeaponAttachmentManager>() ?? player.AddComponent<WeaponAttachmentManager>();
                }
                else
                {
                    GameObject go = new GameObject("WeaponAttachmentManager");
                    Instance = go.AddComponent<WeaponAttachmentManager>();
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeMaterials();
        }

        private void Start()
        {
            EnsureAttachmentVisuals();
            UpdateAllAttachmentVisuals();
        }

        private void InitializeMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            gunMetalMaterial = new Material(litShader);
            gunMetalMaterial.name = "Mat_Attachment_GunMetal";
            Color gmCol = new Color(0.12f, 0.13f, 0.15f);
            if (gunMetalMaterial.HasProperty("_BaseColor")) gunMetalMaterial.SetColor("_BaseColor", gmCol);
            else gunMetalMaterial.color = gmCol;
            if (gunMetalMaterial.HasProperty("_Metallic")) gunMetalMaterial.SetFloat("_Metallic", 0.80f);
            if (gunMetalMaterial.HasProperty("_Smoothness")) gunMetalMaterial.SetFloat("_Smoothness", 0.60f);

            steelMaterial = new Material(litShader);
            steelMaterial.name = "Mat_Attachment_SteelPlates";
            Color stCol = new Color(0.75f, 0.78f, 0.82f);
            if (steelMaterial.HasProperty("_BaseColor")) steelMaterial.SetColor("_BaseColor", stCol);
            else steelMaterial.color = stCol;
            if (steelMaterial.HasProperty("_Metallic")) steelMaterial.SetFloat("_Metallic", 0.90f);
            if (steelMaterial.HasProperty("_Smoothness")) steelMaterial.SetFloat("_Smoothness", 0.85f);

            paracordMaterial = new Material(litShader);
            paracordMaterial.name = "Mat_Attachment_Paracord";
            Color pcCol = new Color(0.24f, 0.28f, 0.20f); // Tactical olive drab
            if (paracordMaterial.HasProperty("_BaseColor")) paracordMaterial.SetColor("_BaseColor", pcCol);
            else paracordMaterial.color = pcCol;
            if (paracordMaterial.HasProperty("_Smoothness")) paracordMaterial.SetFloat("_Smoothness", 0.20f);
        }

        public void EnsureAttachmentVisuals()
        {
            WeaponSystem ws = WeaponSystem.Instance;
            if (ws == null) return;

            Transform pVisual = ws.transform.Find("Visual_Weapon_Pistol") ?? ws.transform.Find("Weapon_Socket/Visual_Weapon_Pistol");
            Transform sVisual = ws.transform.Find("Visual_Weapon_Shotgun") ?? ws.transform.Find("Weapon_Socket/Visual_Weapon_Shotgun");
            Transform aVisual = ws.transform.Find("Visual_Weapon_Axe") ?? ws.transform.Find("Weapon_Socket/Visual_Weapon_Axe");

            // 1. Pistol Suppressor
            if (pVisual != null && pistolSuppressorMesh == null)
            {
                pistolSuppressorMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pistolSuppressorMesh.name = "Attach_Pistol_Suppressor";
                pistolSuppressorMesh.transform.SetParent(pVisual, false);
                pistolSuppressorMesh.transform.localPosition = new Vector3(0f, 0.02f, 0.25f);
                pistolSuppressorMesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                pistolSuppressorMesh.transform.localScale = new Vector3(0.045f, 0.12f, 0.045f);
                SetupPart(pistolSuppressorMesh, gunMetalMaterial);
            }

            // 2. Shotgun Suppressor
            if (sVisual != null && shotgunSuppressorMesh == null)
            {
                shotgunSuppressorMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shotgunSuppressorMesh.name = "Attach_Shotgun_Suppressor";
                shotgunSuppressorMesh.transform.SetParent(sVisual, false);
                shotgunSuppressorMesh.transform.localPosition = new Vector3(0f, 0.035f, 0.48f);
                shotgunSuppressorMesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                shotgunSuppressorMesh.transform.localScale = new Vector3(0.065f, 0.15f, 0.065f);
                SetupPart(shotgunSuppressorMesh, gunMetalMaterial);
            }

            // 3. Pistol Laser Module
            if (pVisual != null && pistolLaserModuleMesh == null)
            {
                pistolLaserModuleMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pistolLaserModuleMesh.name = "Attach_Pistol_LaserModule";
                pistolLaserModuleMesh.transform.SetParent(pVisual, false);
                pistolLaserModuleMesh.transform.localPosition = new Vector3(0f, -0.015f, 0.09f);
                pistolLaserModuleMesh.transform.localScale = new Vector3(0.028f, 0.025f, 0.06f);
                SetupPart(pistolLaserModuleMesh, gunMetalMaterial);
            }

            // 4. Shotgun Laser Module
            if (sVisual != null && shotgunLaserModuleMesh == null)
            {
                shotgunLaserModuleMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shotgunLaserModuleMesh.name = "Attach_Shotgun_LaserModule";
                shotgunLaserModuleMesh.transform.SetParent(sVisual, false);
                shotgunLaserModuleMesh.transform.localPosition = new Vector3(0f, -0.012f, 0.24f);
                shotgunLaserModuleMesh.transform.localScale = new Vector3(0.034f, 0.030f, 0.07f);
                SetupPart(shotgunLaserModuleMesh, gunMetalMaterial);
            }

            // 5. Pistol Extended Magazine
            if (pVisual != null && pistolExtendedMagMesh == null)
            {
                pistolExtendedMagMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pistolExtendedMagMesh.name = "Attach_Pistol_ExtendedMag";
                pistolExtendedMagMesh.transform.SetParent(pVisual, false);
                pistolExtendedMagMesh.transform.localPosition = new Vector3(0f, -0.13f, -0.05f);
                pistolExtendedMagMesh.transform.localScale = new Vector3(0.036f, 0.065f, 0.055f);
                SetupPart(pistolExtendedMagMesh, gunMetalMaterial);
            }

            // 6. Shotgun Extended Tube
            if (sVisual != null && shotgunExtendedTubeMesh == null)
            {
                shotgunExtendedTubeMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shotgunExtendedTubeMesh.name = "Attach_Shotgun_ExtendedTube";
                shotgunExtendedTubeMesh.transform.SetParent(sVisual, false);
                shotgunExtendedTubeMesh.transform.localPosition = new Vector3(0f, 0.005f, 0.28f);
                shotgunExtendedTubeMesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                shotgunExtendedTubeMesh.transform.localScale = new Vector3(0.035f, 0.12f, 0.035f);
                SetupPart(shotgunExtendedTubeMesh, gunMetalMaterial);
            }

            // 7. Axe Reinforced Steel Plates & Paracord Wrap
            if (aVisual != null && axeReinforcedPlatesMesh == null)
            {
                axeReinforcedPlatesMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                axeReinforcedPlatesMesh.name = "Attach_Axe_ReinforcedPlates";
                axeReinforcedPlatesMesh.transform.SetParent(aVisual, false);
                axeReinforcedPlatesMesh.transform.localPosition = new Vector3(0f, 0.50f, 0.08f);
                axeReinforcedPlatesMesh.transform.localScale = new Vector3(0.062f, 0.08f, 0.16f);
                SetupPart(axeReinforcedPlatesMesh, steelMaterial);

                axeParacordWrapMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                axeParacordWrapMesh.name = "Attach_Axe_ParacordWrap";
                axeParacordWrapMesh.transform.SetParent(aVisual, false);
                axeParacordWrapMesh.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                axeParacordWrapMesh.transform.localScale = new Vector3(0.052f, 0.18f, 0.052f);
                SetupPart(axeParacordWrapMesh, paracordMaterial);
            }

            // 8. Laser Beam LineRenderer & Dot
            EnsureLaserBeam();
        }

        private void EnsureLaserBeam()
        {
            if (laserBeamRenderer != null) return;

            GameObject laserObj = new GameObject("TacticalLaser_Beam");
            laserObj.transform.SetParent(transform, false);

            laserBeamRenderer = laserObj.AddComponent<LineRenderer>();
            laserBeamRenderer.useWorldSpace = true;
            laserBeamRenderer.positionCount = 2;
            laserBeamRenderer.startWidth = 0.008f;
            laserBeamRenderer.endWidth = 0.008f;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material laserMat = new Material(unlitShader);
            if (laserMat.HasProperty("_BaseColor")) laserMat.SetColor("_BaseColor", laserColor);
            else laserMat.color = laserColor;

            laserBeamRenderer.material = laserMat;
            laserBeamRenderer.startColor = laserColor;
            laserBeamRenderer.endColor = laserColor;
            laserBeamRenderer.enabled = false;

            // Crimson Dot
            laserDotObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            laserDotObject.name = "TacticalLaser_Dot";
            laserDotObject.transform.SetParent(laserObj.transform, false);
            laserDotObject.transform.localScale = new Vector3(0.035f, 0.035f, 0.035f);
            SetupPart(laserDotObject, laserMat);
            laserDotObject.SetActive(false);
        }

        private static void SetupPart(GameObject part, Material mat)
        {
            Collider col = part.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Renderer r = part.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
        }

        private void Update()
        {
            UpdateLaserSightBeam();
        }

        private void UpdateLaserSightBeam()
        {
            WeaponSystem ws = WeaponSystem.Instance;
            if (ws == null || laserBeamRenderer == null) return;

            bool isAimingOrEquipped = ws.CurrentWeapon == WeaponType.Pistol || ws.CurrentWeapon == WeaponType.Shotgun;
            bool hasLaser = HasLaserSight(ws.CurrentWeapon);

            if (!isAimingOrEquipped || !hasLaser)
            {
                if (laserBeamRenderer.enabled) laserBeamRenderer.enabled = false;
                if (laserDotObject != null && laserDotObject.activeSelf) laserDotObject.SetActive(false);
                return;
            }

            Transform laserOrigin = ws.CurrentWeapon == WeaponType.Pistol && pistolLaserModuleMesh != null
                ? pistolLaserModuleMesh.transform
                : (shotgunLaserModuleMesh != null ? shotgunLaserModuleMesh.transform : ws.transform);

            Vector3 startPos = laserOrigin.position;
            Vector3 aimDir = laserOrigin.forward;

            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray camRay = cam.ViewportPointToRay(new Vector2(0.5f, 0.5f));
                Vector3 targetPoint = camRay.origin + camRay.direction * maxLaserRange;
                if (Physics.Raycast(camRay, out RaycastHit camHit, maxLaserRange, ~0, QueryTriggerInteraction.Ignore))
                {
                    targetPoint = camHit.point;
                }
                aimDir = (targetPoint - startPos).normalized;
            }

            Vector3 endPos = startPos + aimDir * maxLaserRange;
            bool hitSurface = false;

            if (Physics.Raycast(startPos, aimDir, out RaycastHit hit, maxLaserRange, ~0, QueryTriggerInteraction.Ignore))
            {
                endPos = hit.point;
                hitSurface = true;
            }

            laserBeamRenderer.enabled = true;
            laserBeamRenderer.SetPosition(0, startPos);
            laserBeamRenderer.SetPosition(1, endPos);

            if (laserDotObject != null)
            {
                laserDotObject.SetActive(hitSurface);
                if (hitSurface)
                {
                    laserDotObject.transform.position = endPos - aimDir * 0.015f;
                }
            }
        }

        // =========================================================================
        // ATTACHMENT QUERIES & STAT MULTIPLIERS
        // =========================================================================
        public bool IsUnlocked(string modId) => unlockedMods.Contains(modId);
        public bool IsEquipped(string modId) => equippedMods.Contains(modId);

        public void UnlockAttachment(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return;
            if (!unlockedMods.Contains(modId))
            {
                unlockedMods.Add(modId);
                EquipAttachment(modId); // Auto-equip upon unlock
                Debug.Log($"[WeaponAttachmentManager] Unlocked weapon modification: {modId}");
            }
        }

        public void EquipAttachment(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return;
            unlockedMods.Add(modId);
            equippedMods.Add(modId);
            UpdateAllAttachmentVisuals();
            WeaponSystem.Instance?.BroadcastWeaponState();
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        public void UnequipAttachment(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return;
            equippedMods.Remove(modId);
            UpdateAllAttachmentVisuals();
            WeaponSystem.Instance?.BroadcastWeaponState();
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        public void ToggleAttachment(string modId)
        {
            if (IsEquipped(modId)) UnequipAttachment(modId);
            else EquipAttachment(modId);
        }

        public bool HasSuppressor(WeaponType weapon) =>
            (weapon == WeaponType.Pistol || weapon == WeaponType.Shotgun) && equippedMods.Contains(MOD_SUPPRESSOR);

        public bool HasLaserSight(WeaponType weapon) =>
            (weapon == WeaponType.Pistol || weapon == WeaponType.Shotgun) && equippedMods.Contains(MOD_LASER_SIGHT);

        public bool HasExtendedMag(WeaponType weapon) =>
            (weapon == WeaponType.Pistol || weapon == WeaponType.Shotgun) && equippedMods.Contains(MOD_EXTENDED_MAG);

        public bool HasReinforcedAxe() => equippedMods.Contains(MOD_REINFORCED_AXE);

        public float GetNoiseRadiusMultiplier(WeaponType weapon) => HasSuppressor(weapon) ? 0.25f : 1.0f;
        public float GetSpreadMultiplier(WeaponType weapon) => HasLaserSight(weapon) ? 0.65f : 1.0f;
        public float GetReloadSpeedMultiplier(WeaponType weapon) => HasExtendedMag(weapon) ? 0.85f : 1.0f;

        public int GetMaxClip(WeaponType weapon, int defaultMax)
        {
            if (!HasExtendedMag(weapon)) return defaultMax;
            if (weapon == WeaponType.Pistol) return 18;
            if (weapon == WeaponType.Shotgun) return 8;
            return defaultMax;
        }

        public float GetMeleeDamage(float defaultDmg) => HasReinforcedAxe() ? defaultDmg * 1.35f : defaultDmg;
        public float GetMeleeStaminaCost(float defaultCost) => HasReinforcedAxe() ? defaultCost * 0.80f : defaultCost;

        public string GetActiveBadgesForWeapon(WeaponType weapon)
        {
            List<string> badges = new List<string>();
            if (weapon == WeaponType.Pistol || weapon == WeaponType.Shotgun)
            {
                if (HasSuppressor(weapon)) badges.Add("[S]");
                if (HasLaserSight(weapon)) badges.Add("[L]");
                if (HasExtendedMag(weapon)) badges.Add("[M]");
            }
            else if (weapon == WeaponType.Axe)
            {
                if (HasReinforcedAxe()) badges.Add("[R]");
            }
            return badges.Count > 0 ? string.Join(" ", badges) : string.Empty;
        }

        public void UpdateAllAttachmentVisuals()
        {
            EnsureAttachmentVisuals();

            if (pistolSuppressorMesh != null) pistolSuppressorMesh.SetActive(equippedMods.Contains(MOD_SUPPRESSOR));
            if (shotgunSuppressorMesh != null) shotgunSuppressorMesh.SetActive(equippedMods.Contains(MOD_SUPPRESSOR));

            if (pistolLaserModuleMesh != null) pistolLaserModuleMesh.SetActive(equippedMods.Contains(MOD_LASER_SIGHT));
            if (shotgunLaserModuleMesh != null) shotgunLaserModuleMesh.SetActive(equippedMods.Contains(MOD_LASER_SIGHT));

            if (pistolExtendedMagMesh != null) pistolExtendedMagMesh.SetActive(equippedMods.Contains(MOD_EXTENDED_MAG));
            if (shotgunExtendedTubeMesh != null) shotgunExtendedTubeMesh.SetActive(equippedMods.Contains(MOD_EXTENDED_MAG));

            if (axeReinforcedPlatesMesh != null) axeReinforcedPlatesMesh.SetActive(equippedMods.Contains(MOD_REINFORCED_AXE));
            if (axeParacordWrapMesh != null) axeParacordWrapMesh.SetActive(equippedMods.Contains(MOD_REINFORCED_AXE));
        }

        // =========================================================================
        // SAVESYSTEM INTEGRATION
        // =========================================================================
        public List<string> GetUnlockedAttachmentIds() => new List<string>(unlockedMods);
        public List<string> GetEquippedAttachmentIds() => new List<string>(equippedMods);

        public void RestoreAttachments(List<string> unlocked, List<string> equipped)
        {
            unlockedMods.Clear();
            equippedMods.Clear();

            if (unlocked != null)
            {
                for (int i = 0; i < unlocked.Count; i++) unlockedMods.Add(unlocked[i]);
            }
            if (equipped != null)
            {
                for (int i = 0; i < equipped.Count; i++) equippedMods.Add(equipped[i]);
            }

            UpdateAllAttachmentVisuals();
            WeaponSystem.Instance?.BroadcastWeaponState();
            Debug.Log($"[WeaponAttachmentManager] Restored {unlockedMods.Count} unlocked and {equippedMods.Count} equipped attachments.");
        }
    }
}
