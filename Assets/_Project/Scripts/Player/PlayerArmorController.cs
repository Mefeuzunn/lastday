using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Player
{
    /// <summary>
    /// Improvised Tactical Armor Vest Controller.
    /// - Starts at 100 Armor HP.
    /// - Absorbs 35% of all incoming physical/zombie damage.
    /// - Blocks single-hit infection spikes while active.
    /// - Displays segmented ZIRH bar in VitalStatusHUD.
    /// - Attaches sculpted Kevlar/scrap-plate chest & shoulder armor pads to Player_Root/Visual_Model.
    /// </summary>
    public class PlayerArmorController : MonoBehaviour
    {
        private static PlayerArmorController _instance;
        public static PlayerArmorController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlayerArmorController>();
                    if (_instance == null)
                    {
                        GameObject player = GameObject.FindWithTag("Player");
                        if (player != null)
                        {
                            _instance = player.GetComponent<PlayerArmorController>() ?? player.AddComponent<PlayerArmorController>();
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Armor State")]
        [SerializeField] private float maxArmorHP = 100f;
        [SerializeField] private float currentArmorHP = 0f;
        [SerializeField] private bool hasArmorEquipped = false;

        [Header("Mitigation")]
        [SerializeField] private float damageAbsorptionRatio = 0.35f;

        private GameObject visualArmorRoot;
        private Material armorMaterial;

        public float MaxArmorHP => maxArmorHP;
        public float CurrentArmorHP => currentArmorHP;
        public bool HasArmorEquipped => hasArmorEquipped;
        public bool HasArmor => hasArmorEquipped && currentArmorHP > 0f;

        public void SetMaxCapacity(float cap)
        {
            maxArmorHP = cap;
            if (currentArmorHP > maxArmorHP) currentArmorHP = maxArmorHP;
            UpdateVisualsAndHUD();
        }

        public void UpgradeToCompositeArmor()
        {
            damageAbsorptionRatio = 0.50f;
            Debug.Log("[PlayerArmorController] Upgraded to Composite Plate Armor (50% absorption).");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (Instance == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    Instance = player.GetComponent<PlayerArmorController>() ?? player.AddComponent<PlayerArmorController>();
                }
                else
                {
                    GameObject go = new GameObject("PlayerArmorController");
                    Instance = go.AddComponent<PlayerArmorController>();
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
        }

        private void Start()
        {
            EnsureArmorVisuals();
            UpdateVisualsAndHUD();
        }

        public void EnsureArmorVisuals()
        {
            if (visualArmorRoot != null) return;

            GameObject player = GameObject.FindWithTag("Player") ?? gameObject;
            Transform visualModel = player.transform.Find("Visual_Model") ?? player.transform;

            Transform existing = visualModel.Find("Armor_Vest");
            if (existing != null)
            {
                visualArmorRoot = existing.gameObject;
                return;
            }

            // Create Procedural Armor Mesh
            visualArmorRoot = new GameObject("Armor_Vest");
            visualArmorRoot.transform.SetParent(visualModel, false);
            visualArmorRoot.transform.localPosition = Vector3.zero;
            visualArmorRoot.transform.localRotation = Quaternion.identity;

            // Load or create PBR scrap metal / Kevlar material
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            armorMaterial = new Material(litShader);
            armorMaterial.name = "Mat_Procedural_ArmorVest";
            Color plateColor = new Color(0.18f, 0.20f, 0.23f); // Dark tactical steel/kevlar
            if (armorMaterial.HasProperty("_BaseColor")) armorMaterial.SetColor("_BaseColor", plateColor);
            else armorMaterial.color = plateColor;
            if (armorMaterial.HasProperty("_Metallic")) armorMaterial.SetFloat("_Metallic", 0.70f);
            if (armorMaterial.HasProperty("_Smoothness")) armorMaterial.SetFloat("_Smoothness", 0.55f);

            // 1. Chest Plate (front torso)
            GameObject chestPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chestPlate.name = "Armor_ChestPlate";
            chestPlate.transform.SetParent(visualArmorRoot.transform, false);
            chestPlate.transform.localPosition = new Vector3(0f, 1.15f, 0.12f);
            chestPlate.transform.localScale = new Vector3(0.36f, 0.40f, 0.10f);
            SetupPart(chestPlate, armorMaterial);

            // 2. Back Plate
            GameObject backPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backPlate.name = "Armor_BackPlate";
            backPlate.transform.SetParent(visualArmorRoot.transform, false);
            backPlate.transform.localPosition = new Vector3(0f, 1.15f, -0.12f);
            backPlate.transform.localScale = new Vector3(0.36f, 0.40f, 0.08f);
            SetupPart(backPlate, armorMaterial);

            // 3. Left Shoulder Pad
            GameObject leftShoulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftShoulder.name = "Armor_Shoulder_L";
            leftShoulder.transform.SetParent(visualArmorRoot.transform, false);
            leftShoulder.transform.localPosition = new Vector3(-0.25f, 1.34f, 0f);
            leftShoulder.transform.localRotation = Quaternion.Euler(0f, 0f, -15f);
            leftShoulder.transform.localScale = new Vector3(0.15f, 0.12f, 0.18f);
            SetupPart(leftShoulder, armorMaterial);

            // 4. Right Shoulder Pad
            GameObject rightShoulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightShoulder.name = "Armor_Shoulder_R";
            rightShoulder.transform.SetParent(visualArmorRoot.transform, false);
            rightShoulder.transform.localPosition = new Vector3(0.25f, 1.34f, 0f);
            rightShoulder.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            rightShoulder.transform.localScale = new Vector3(0.15f, 0.12f, 0.18f);
            SetupPart(rightShoulder, armorMaterial);

            visualArmorRoot.SetActive(HasArmor);
        }

        private static void SetupPart(GameObject part, Material mat)
        {
            Collider col = part.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Renderer r = part.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
        }

        public float MitigateDamage(float incomingDamage)
        {
            if (!HasArmor || incomingDamage <= 0f) return incomingDamage;

            float absorbed = incomingDamage * damageAbsorptionRatio;
            currentArmorHP = Mathf.Max(0f, currentArmorHP - absorbed);
            float remaining = incomingDamage - absorbed;

            ProceduralAudioManager.Instance?.PlayArmorHit();
            UpdateVisualsAndHUD();

            if (currentArmorHP <= 0f)
            {
                hasArmorEquipped = false;
                UpdateVisualsAndHUD();
                GameEvents.TriggerObjectiveToastRequested("[ZIRH KIRILDI] Zırhınız parçalandı!", 3.5f);
                ProceduralAudioManager.Instance?.PlayGlassShatter();
            }

            return remaining;
        }

        public void EquipArmor(float initialHP = 100f)
        {
            hasArmorEquipped = true;
            currentArmorHP = Mathf.Clamp(initialHP, 1f, maxArmorHP);

            EnsureArmorVisuals();
            UpdateVisualsAndHUD();

            ProceduralAudioManager.Instance?.PlayButtonClick();
            GameEvents.TriggerObjectiveToastRequested("[ZIRH KUŞANILDI] El Yapımı Zırh Yeleği kuşanıldı!", 3.0f);
            Debug.Log($"[PlayerArmorController] Equipped tactical armor. Current HP: {currentArmorHP}/{maxArmorHP}");
        }

        public void RepairArmor(float repairAmount = 50f)
        {
            hasArmorEquipped = true;
            currentArmorHP = Mathf.Min(maxArmorHP, currentArmorHP + repairAmount);

            EnsureArmorVisuals();
            UpdateVisualsAndHUD();

            ProceduralAudioManager.Instance?.PlayCraftSuccess();
            GameEvents.TriggerObjectiveToastRequested($"[ZIRH ONARILDI] Zırh tamir edildi (+{Mathf.CeilToInt(repairAmount)} HP)", 3.0f);
            Debug.Log($"[PlayerArmorController] Repaired tactical armor. Current HP: {currentArmorHP}/{maxArmorHP}");
        }

        public void RestoreArmor(float savedHP, bool savedEquipped)
        {
            hasArmorEquipped = savedEquipped && savedHP > 0f;
            currentArmorHP = Mathf.Clamp(savedHP, 0f, maxArmorHP);

            EnsureArmorVisuals();
            UpdateVisualsAndHUD();
            Debug.Log($"[PlayerArmorController] Restored armor state: Equipped={hasArmorEquipped}, HP={currentArmorHP}/{maxArmorHP}");
        }

        private void UpdateVisualsAndHUD()
        {
            if (visualArmorRoot != null)
            {
                visualArmorRoot.SetActive(HasArmor);
            }

            GameEvents.TriggerArmorChanged(HasArmor ? currentArmorHP : 0f, maxArmorHP);
        }
    }
}
