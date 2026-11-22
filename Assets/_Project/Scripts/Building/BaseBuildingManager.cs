using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.OpenWorld;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Building
{
    /// <summary>
    /// Active Phase 3: Modular Base Building & Fortification Manager.
    /// Governs grid-snapped (2m x 2m) placement, 45-degree rotation stepping,
    /// slope & collision validation, authorized courtyard/outpost territory checks,
    /// real-time glowing hologram previews, dismantling, and persistence.
    /// </summary>
    public class BaseBuildingManager : MonoBehaviour
    {
        private static BaseBuildingManager _instance;
        public static BaseBuildingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<BaseBuildingManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("BaseBuildingManager");
                        _instance = go.AddComponent<BaseBuildingManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (_instance == null)
            {
                var inst = Instance;
            }
        }

        [Header("Build Settings")]
        [SerializeField] private float gridSnapSize = 2.0f;
        [SerializeField] private float placementDistance = 3.4f;
        [SerializeField] private float maxAllowedSlopeAngle = 10.0f;
        [SerializeField] private float safeHouseCourtyardRadius = 36.0f;
        [SerializeField] private float outpostBuildRadius = 35.0f;

        [Header("State")]
        [SerializeField] private bool isBuildModeActive = false;
        [SerializeField] private bool isDismantleMode = false;
        [SerializeField] private StructureType selectedStructure = StructureType.DefenseWall;
        [SerializeField] private float currentRotationY = 0f;

        private GameObject ghostObject;
        private Material ghostMaterial;
        private Transform playerTransform;
        private Camera mainCamera;

        private Vector3 currentSnappedPos;
        private Quaternion currentSnappedRot;
        private bool isCurrentPlacementValid = false;
        private string placementInvalidReason = string.Empty;

        private BuildableStructure hoveredDismantleStructure = null;
        private Renderer[] hoveredRenderers = null;
        private Color[] originalRendererColors = null;

        public bool IsBuildModeActive => isBuildModeActive;
        public bool IsDismantleMode => isDismantleMode;
        public StructureType SelectedStructure => selectedStructure;
        public float CurrentRotationY => currentRotationY;
        public bool IsPlacementValid => isCurrentPlacementValid;
        public string PlacementInvalidReason => placementInvalidReason;

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
            LocatePlayerAndCamera();
        }

        private void LocatePlayerAndCamera()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Root");
                if (player != null) playerTransform = player.transform;
            }
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        public bool IsInAuthorizedBuildZone(Vector3 worldPos)
        {
            // 1. Central SafeHouse Courtyard: radius <= 36m or player inside SafeHouse trigger
            float distFromCenter = new Vector2(worldPos.x, worldPos.z).magnitude;
            if (distFromCenter <= safeHouseCourtyardRadius) return true;

            if (SafeHouseManager.Instance != null && SafeHouseManager.Instance.IsPlayerInside)
            {
                return true;
            }

            // 2. Liberated Outpost Safe Zones (35m radius around any liberated outpost)
            if (OutpostLiberationManager.Instance != null && OutpostLiberationManager.Instance.IsPointInSafeZone(worldPos, outpostBuildRadius))
            {
                return true;
            }

            return false;
        }

        public bool HasRequiredResources(StructureType type)
        {
            if (InventoryManager.Instance == null) return false;
            ResourceCost[] costs = BuildableStructure.GetCosts(type);
            for (int i = 0; i < costs.Length; i++)
            {
                if (!InventoryManager.Instance.HasItem(costs[i].itemId, costs[i].amount))
                {
                    return false;
                }
            }
            return true;
        }

        public void EnterBuildMode()
        {
            LocatePlayerAndCamera();
            if (playerTransform == null) return;

            if (!IsInAuthorizedBuildZone(playerTransform.position))
            {
                GameEvents.TriggerObjectiveToastRequested("[İNŞA KISITLAMASI] Sadece Merkez Güvenli Ev avlusunda veya kurtarılmış karakollarda inşa edilebilir!", 4.0f);
                ProceduralAudioManager.Instance?.PlaySoftError();
                return;
            }

            isBuildModeActive = true;
            isDismantleMode = false;
            currentRotationY = Mathf.Round(playerTransform.eulerAngles.y / 45f) * 45f;

            RecreateGhostObject();

            // Disable player shooting/aiming while building
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.SetAiming(false);

            BaseBuildingUI.Instance?.ShowUI(true);
            ProceduralAudioManager.Instance?.PlayWrenchRepair();
            GameEvents.TriggerObjectiveToastRequested("[İNŞA MODU AKTİF] Izgaraya hizalı yapı yerleştirin.", 2.5f);
        }

        public void ExitBuildMode()
        {
            isBuildModeActive = false;
            isDismantleMode = false;

            ClearHoveredDismantle();
            DestroyGhostObject();

            BaseBuildingUI.Instance?.ShowUI(false);
            GameEvents.TriggerObjectiveToastRequested("[İNŞA MODU KAPATILDI]", 2.0f);
        }

        public void ToggleBuildMode()
        {
            if (isBuildModeActive) ExitBuildMode();
            else EnterBuildMode();
        }

        public void SelectStructure(StructureType type)
        {
            if (selectedStructure == type && ghostObject != null) return;

            selectedStructure = type;
            isDismantleMode = false;
            ClearHoveredDismantle();
            RecreateGhostObject();
            ProceduralAudioManager.Instance?.PlayItemPickup();
        }

        public void RotateStructure(float deltaY = 45f)
        {
            currentRotationY = (currentRotationY + deltaY) % 360f;
            if (currentRotationY < 0f) currentRotationY += 360f;

            if (ghostObject != null)
            {
                ghostObject.transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
            }
            ProceduralAudioManager.Instance?.PlayItemPickup();
        }

        public void ToggleDismantleMode()
        {
            isDismantleMode = !isDismantleMode;
            if (isDismantleMode)
            {
                DestroyGhostObject();
                GameEvents.TriggerObjectiveToastRequested("[SÖKÜM MODU] Sökmek istediğiniz yapıya bakın ve onaylayın (%50 İade).", 3.0f);
            }
            else
            {
                ClearHoveredDismantle();
                RecreateGhostObject();
                GameEvents.TriggerObjectiveToastRequested("[İNŞA MODU] Yapı yerleştirme modu devrede.", 2.0f);
            }
            ProceduralAudioManager.Instance?.PlayItemPickup();
        }

        private void Update()
        {
            if (!Application.isPlaying || !isBuildModeActive) return;

            // PC Hotkeys
            if (Input.GetKeyDown(KeyCode.R))
            {
                RotateStructure(45f);
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                ToggleDismantleMode();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitBuildMode();
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (isDismantleMode) TryDismantleLookedAtStructure();
                else TryPlaceStructure();
            }

            if (isDismantleMode)
            {
                UpdateDismantleRaycast();
            }
            else
            {
                UpdateGhostPositionAndValidation();
            }
        }

        private void UpdateGhostPositionAndValidation()
        {
            LocatePlayerAndCamera();
            if (playerTransform == null || ghostObject == null) return;

            // Target location in front of player
            Vector3 rawPos = playerTransform.position + (playerTransform.forward * placementDistance);

            // 2.0m x 2.0m Grid Snapping
            float snapX = Mathf.Round(rawPos.x / gridSnapSize) * gridSnapSize;
            float snapZ = Mathf.Round(rawPos.z / gridSnapSize) * gridSnapSize;
            float sampleY = playerTransform.position.y + 2.5f;

            Vector3 rayOrigin = new Vector3(snapX, sampleY, snapZ);
            float finalY = playerTransform.position.y;
            bool validGround = false;
            float slopeAngle = 0f;

            int groundMask = ~((1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("UI")));
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) groundMask &= ~(1 << playerLayer);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 7.0f, groundMask))
            {
                finalY = hit.point.y;
                slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                validGround = slopeAngle <= maxAllowedSlopeAngle;
            }

            currentSnappedPos = new Vector3(snapX, finalY, snapZ);
            currentSnappedRot = Quaternion.Euler(0f, currentRotationY, 0f);

            ghostObject.transform.position = currentSnappedPos;
            ghostObject.transform.rotation = currentSnappedRot;

            // Check authorized zone
            bool inZone = IsInAuthorizedBuildZone(currentSnappedPos);

            // Check player material resources
            bool hasResources = HasRequiredResources(selectedStructure);

            // Check collision overlap
            Vector3 boxHalfExtents = GetStructureBoxExtents(selectedStructure) * 0.45f;
            Vector3 boxCenter = currentSnappedPos + Vector3.up * boxHalfExtents.y;
            Collider[] colliders = Physics.OverlapBox(boxCenter, boxHalfExtents, currentSnappedRot, groundMask);

            bool hasCollision = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || col.isTrigger) continue;
                if (col.transform.IsChildOf(ghostObject.transform)) continue;
                if (playerTransform != null && col.transform.IsChildOf(playerTransform)) continue;

                // Ignore ground terrain/road planes
                if (col.name.Contains("Terrain") || col.name.Contains("Road") || col.name.Contains("Ground")) continue;

                hasCollision = true;
                break;
            }

            // Synthesize validity
            if (!inZone)
            {
                isCurrentPlacementValid = false;
                placementInvalidReason = "Sadece Güvenli Ev avlusunda veya karakollarda inşa edilebilir!";
            }
            else if (!validGround)
            {
                isCurrentPlacementValid = false;
                placementInvalidReason = $"Zemin çok eğimli! ({slopeAngle:F1}° > {maxAllowedSlopeAngle}°)";
            }
            else if (hasCollision)
            {
                isCurrentPlacementValid = false;
                placementInvalidReason = "Başka bir engel veya yapıyla çakışıyor!";
            }
            else if (!hasResources)
            {
                isCurrentPlacementValid = false;
                placementInvalidReason = "Yetersiz malzeme!";
            }
            else
            {
                isCurrentPlacementValid = true;
                placementInvalidReason = string.Empty;
            }

            // Update hologram color
            UpdateGhostColor(isCurrentPlacementValid);
        }

        private Vector3 GetStructureBoxExtents(StructureType type)
        {
            switch (type)
            {
                case StructureType.DefenseWall: return new Vector3(2.4f, 2.2f, 0.5f);
                case StructureType.RazorWire: return new Vector3(2.0f, 0.9f, 0.9f);
                case StructureType.Floodlight: return new Vector3(0.9f, 2.6f, 0.9f);
                case StructureType.RainCollector: return new Vector3(1.1f, 1.8f, 1.1f);
                case StructureType.HerbPlanter: return new Vector3(1.6f, 0.7f, 0.9f);
                default: return Vector3.one;
            }
        }

        private void UpdateGhostColor(bool valid)
        {
            if (ghostMaterial == null) return;

            // Emerald Green `#06D6A0` when valid, Crimson Red `#EF476F` when invalid
            Color targetColor = valid
                ? new Color(0.02f, 0.84f, 0.63f, 0.55f)
                : new Color(0.94f, 0.28f, 0.44f, 0.55f);

            ghostMaterial.color = targetColor;
        }

        public bool TryPlaceStructure()
        {
            if (!isBuildModeActive || isDismantleMode) return false;

            if (!isCurrentPlacementValid)
            {
                ProceduralAudioManager.Instance?.PlaySoftError();
                if (!string.IsNullOrEmpty(placementInvalidReason))
                {
                    GameEvents.TriggerObjectiveToastRequested($"[İNŞA ENGELLENDİ] {placementInvalidReason}", 3.0f);
                }
                return false;
            }

            // Deduct required materials
            ResourceCost[] costs = BuildableStructure.GetCosts(selectedStructure);
            for (int i = 0; i < costs.Length; i++)
            {
                InventoryManager.RemoveItem(costs[i].itemId, costs[i].amount);
            }

            // Instantiate permanent structure
            GameObject placedGo = BuildableStructure.CreateStructureGameObject(selectedStructure, currentSnappedPos, currentSnappedRot, false);

            ProceduralAudioManager.Instance?.PlayCraftSuccess();
            GameEvents.TriggerObjectiveToastRequested($"[İNŞA TAMAMLANDI] {BuildableStructure.GetStructureDisplayName(selectedStructure)} yerleştirildi!", 3.0f);

            // Re-evaluate validation immediately after placement
            UpdateGhostPositionAndValidation();
            BaseBuildingUI.Instance?.RefreshCostBadges();
            return true;
        }

        private void UpdateDismantleRaycast()
        {
            LocatePlayerAndCamera();
            if (mainCamera == null && playerTransform == null) return;

            Ray ray = (mainCamera != null)
                ? mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(playerTransform.position + Vector3.up * 1.6f, playerTransform.forward);

            BuildableStructure hitStructure = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 5.0f))
            {
                hitStructure = hit.collider.GetComponentInParent<BuildableStructure>();
            }

            if (hitStructure != hoveredDismantleStructure)
            {
                ClearHoveredDismantle();
                hoveredDismantleStructure = hitStructure;

                if (hoveredDismantleStructure != null)
                {
                    HighlightDismantleTarget(hoveredDismantleStructure);
                }
            }
        }

        private void HighlightDismantleTarget(BuildableStructure bs)
        {
            hoveredRenderers = bs.GetComponentsInChildren<Renderer>();
            originalRendererColors = new Color[hoveredRenderers.Length];

            for (int i = 0; i < hoveredRenderers.Length; i++)
            {
                if (hoveredRenderers[i] != null && hoveredRenderers[i].material != null)
                {
                    originalRendererColors[i] = hoveredRenderers[i].material.color;
                    hoveredRenderers[i].material.color = new Color(0.95f, 0.45f, 0.15f, 1f); // Warning orange
                }
            }
        }

        private void ClearHoveredDismantle()
        {
            if (hoveredDismantleStructure != null && hoveredRenderers != null && originalRendererColors != null)
            {
                for (int i = 0; i < hoveredRenderers.Length; i++)
                {
                    if (hoveredRenderers[i] != null && hoveredRenderers[i].material != null && i < originalRendererColors.Length)
                    {
                        hoveredRenderers[i].material.color = originalRendererColors[i];
                    }
                }
            }

            hoveredDismantleStructure = null;
            hoveredRenderers = null;
            originalRendererColors = null;
        }

        public bool TryDismantleLookedAtStructure()
        {
            if (!isBuildModeActive || !isDismantleMode) return false;

            if (hoveredDismantleStructure == null)
            {
                GameEvents.TriggerObjectiveToastRequested("[SÖKÜM] Sökmek için bir yapıya odaklanın.", 2.5f);
                ProceduralAudioManager.Instance?.PlaySoftError();
                return false;
            }

            hoveredDismantleStructure.Dismantle();
            ClearHoveredDismantle();
            BaseBuildingUI.Instance?.RefreshCostBadges();
            return true;
        }

        private void RecreateGhostObject()
        {
            DestroyGhostObject();

            LocatePlayerAndCamera();
            Vector3 spawnPos = (playerTransform != null) ? playerTransform.position : Vector3.zero;
            Quaternion spawnRot = Quaternion.Euler(0f, currentRotationY, 0f);

            ghostObject = BuildableStructure.CreateStructureGameObject(selectedStructure, spawnPos, spawnRot, true);

            Renderer[] rends = ghostObject.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0 && rends[0] != null)
            {
                ghostMaterial = rends[0].material;
            }

            UpdateGhostPositionAndValidation();
        }

        private void DestroyGhostObject()
        {
            if (ghostObject != null)
            {
                Destroy(ghostObject);
                ghostObject = null;
                ghostMaterial = null;
            }
        }

        public void RestoreSavedStructures(List<SavedBaseStructure> records)
        {
            // Destroy all existing active structures to prevent duplicates
            var existing = new List<BuildableStructure>(BuildableStructure.ActiveStructures);
            for (int i = 0; i < existing.Count; i++)
            {
                if (existing[i] != null) Destroy(existing[i].gameObject);
            }
            BuildableStructure.ActiveStructures.Clear();

            if (records == null || records.Count == 0) return;

            for (int i = 0; i < records.Count; i++)
            {
                SavedBaseStructure rec = records[i];
                if (rec == null) continue;

                StructureType st = (StructureType)rec.structureType;
                Vector3 pos = new Vector3(rec.posX, rec.posY, rec.posZ);
                Quaternion rot = Quaternion.Euler(0f, rec.rotY, 0f);

                GameObject go = BuildableStructure.CreateStructureGameObject(st, pos, rot, false);
                BuildableStructure bs = go.GetComponent<BuildableStructure>();
                if (bs != null)
                {
                    bs.Initialize(st, rec.structureId, rec.currentHP, rec.accumulatedResources, rec.resourceTimer);
                }
            }

            Debug.Log($"[BaseBuildingManager] Successfully restored {records.Count} player base fortifications.");
        }
    }
}
