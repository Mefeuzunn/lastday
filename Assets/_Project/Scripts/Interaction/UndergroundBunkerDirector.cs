using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Master Director for the Multi-Room Subterranean Research Bunker Dungeon beneath Northwest Lake (Sector_-1_1_NorthWest).
    /// Features:
    /// - Proximity-culled activation of the subterranean interior (y = -12.0m) ensuring 0 exterior CPU/GPU overhead.
    /// - Manages the progression loop: Emergency Airlock -> Auxiliary Generator -> Dark Archive -> Keycard Door -> Antiviral Vault.
    /// - Controls fluorescent corridor lighting (Shadows: None for mobile 60 FPS compliance).
    /// - Synchronizes seamlessly with MainStoryDirector (Chapter 3) and SaveSystem.
    /// </summary>
    public class UndergroundBunkerDirector : MonoBehaviour
    {
        public static UndergroundBunkerDirector Instance { get; private set; }

        [Header("Bunker Dungeon Identity & Location")]
        [SerializeField] private Vector3 surfaceEntrancePos = new Vector3(-235f, 0.5f, 255f);
        [SerializeField] private float activationDistance = 60f;
        [SerializeField] private float subterraneanDepthThreshold = -4.0f;

        [Header("Subterranean Hierarchy Roots")]
        [SerializeField] private GameObject subterraneanChambersRoot;
        [SerializeField] private GameObject emergencyAirlockLight;
        [SerializeField] private List<Light> fluorescentBunkerLights = new List<Light>();

        [Header("Interactive Core Systems")]
        [SerializeField] private BunkerGeneratorInteractable backupGenerator;
        [SerializeField] private KeycardSecurityDoor bioLabSecurityDoor;
        [SerializeField] private BunkerVaultChestInteractable antiviralVaultChest;
        [SerializeField] private StoryTerminalInteractable synthesisTerminal;

        [Header("Bunker Progression State")]
        [SerializeField] private bool isGeneratorPowered = false;
        [SerializeField] private bool isSecurityDoorUnlocked = false;
        [SerializeField] private bool isVaultLooted = false;

        private Transform playerTransform;
        private bool isSubterraneanActive = true;
        private float cullingCheckTimer = 0f;

        public bool IsGeneratorPowered => isGeneratorPowered;
        public bool IsSecurityDoorUnlocked => isSecurityDoorUnlocked;
        public bool IsVaultLooted => isVaultLooted;
        public Vector3 SurfaceEntrancePos => surfaceEntrancePos;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Ensure all interior bunker lights have Shadows: None (mobile 60 FPS rule)
            if (fluorescentBunkerLights != null)
            {
                for (int i = 0; i < fluorescentBunkerLights.Count; i++)
                {
                    if (fluorescentBunkerLights[i] != null)
                    {
                        fluorescentBunkerLights[i].shadows = LightShadows.None;
                    }
                }
            }

            if (emergencyAirlockLight != null)
            {
                Light el = emergencyAirlockLight.GetComponent<Light>();
                if (el != null) el.shadows = LightShadows.None;
            }
        }

        private void Start()
        {
            FindPlayerReference();
            ApplyLightingState();
        }

        private void Update()
        {
            cullingCheckTimer += Time.deltaTime;
            if (cullingCheckTimer >= 0.4f)
            {
                cullingCheckTimer = 0f;
                UpdateProximityCulling();
            }
        }

        private void FindPlayerReference()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void UpdateProximityCulling()
        {
            if (playerTransform == null)
            {
                FindPlayerReference();
                if (playerTransform == null) return;
            }

            Vector3 pPos = playerTransform.position;
            float distToHatch = Vector3.Distance(pPos, surfaceEntrancePos);
            bool shouldBeActive = (distToHatch <= activationDistance) || (pPos.y <= subterraneanDepthThreshold);

            if (shouldBeActive != isSubterraneanActive)
            {
                isSubterraneanActive = shouldBeActive;
                if (subterraneanChambersRoot != null)
                {
                    subterraneanChambersRoot.SetActive(isSubterraneanActive);
                }
            }
        }

        public void OnGeneratorPowered()
        {
            isGeneratorPowered = true;
            ApplyLightingState();

            ProceduralAudioManager.Instance?.PlayFluorescentHum(surfaceEntrancePos + Vector3.down * 12f);
            Debug.Log("[UndergroundBunkerDirector] Generator powered on. Bunker lights ignited and door magnetics active.");
        }

        public void OnSecurityDoorUnlocked()
        {
            isSecurityDoorUnlocked = true;
            Debug.Log("[UndergroundBunkerDirector] Bio-Containment Lab security door unlocked.");
        }

        public void OnVaultLooted()
        {
            isVaultLooted = true;
            Debug.Log("[UndergroundBunkerDirector] Antiviral Synthesis Vault chest looted.");
        }

        public void ApplyLightingState()
        {
            if (fluorescentBunkerLights != null)
            {
                for (int i = 0; i < fluorescentBunkerLights.Count; i++)
                {
                    if (fluorescentBunkerLights[i] != null)
                    {
                        fluorescentBunkerLights[i].shadows = LightShadows.None;
                        fluorescentBunkerLights[i].enabled = isGeneratorPowered;
                    }
                }
            }

            if (emergencyAirlockLight != null)
            {
                emergencyAirlockLight.SetActive(true); // Emergency red light always active
            }
        }

        public void RestoreBunkerState(bool generatorPowered, bool doorUnlocked, bool vaultLooted)
        {
            isGeneratorPowered = generatorPowered;
            isSecurityDoorUnlocked = doorUnlocked;
            isVaultLooted = vaultLooted;

            if (backupGenerator != null)
            {
                backupGenerator.SetPoweredSilent(isGeneratorPowered);
            }

            if (bioLabSecurityDoor != null)
            {
                bioLabSecurityDoor.SetUnlockedSilent(isSecurityDoorUnlocked);
            }

            if (antiviralVaultChest != null)
            {
                antiviralVaultChest.SetLootedSilent(isVaultLooted);
            }

            ApplyLightingState();
            Debug.Log($"[UndergroundBunkerDirector] Restored bunker state: Power={isGeneratorPowered}, DoorUnlocked={isSecurityDoorUnlocked}, VaultLooted={isVaultLooted}");
        }

        public void ConfigureReferences(
            GameObject chambersRoot,
            GameObject airlockLight,
            List<Light> fluorescentLights,
            BunkerGeneratorInteractable generator,
            KeycardSecurityDoor door,
            BunkerVaultChestInteractable chest,
            StoryTerminalInteractable terminal)
        {
            subterraneanChambersRoot = chambersRoot;
            emergencyAirlockLight = airlockLight;
            fluorescentBunkerLights = fluorescentLights ?? new List<Light>();
            backupGenerator = generator;
            bioLabSecurityDoor = door;
            antiviralVaultChest = chest;
            synthesisTerminal = terminal;

            ApplyLightingState();
        }
    }
}
