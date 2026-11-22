using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSurvival.UI
{
    public interface IModalWindow
    {
        bool IsModalOpen { get; }
        void CloseModal();
    }

    /// <summary>
    /// Pre-Phase 3 Optimization: Exclusive Modal Window Coordinator.
    /// Manages all UI overlay modals across the game (Vehicle Trunk, Survival Alchemy,
    /// Workbench Crafting, Skill Tree, Inventory / Backpack, Stash, Full Map, and Metro Travel).
    /// Ensures that opening any modal automatically and cleanly closes any previously open modal,
    /// preventing overlapping UI stacks and broken mobile input contexts.
    /// </summary>
    public class ModalWindowManager : MonoBehaviour
    {
        private static ModalWindowManager _instance;
        public static ModalWindowManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ModalWindowManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ModalWindowManager");
                        _instance = go.AddComponent<ModalWindowManager>();
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private readonly List<IModalWindow> registeredModals = new List<IModalWindow>();
        private MonoBehaviour currentActiveModal;

        public MonoBehaviour CurrentActiveModal => currentActiveModal;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public void RegisterModal(IModalWindow modal)
        {
            if (modal != null && !registeredModals.Contains(modal))
            {
                registeredModals.Add(modal);
            }
        }

        public void UnregisterModal(IModalWindow modal)
        {
            if (modal != null)
            {
                registeredModals.Remove(modal);
            }
        }

        /// <summary>
        /// Call whenever any modal is opened. Automatically closes all other open modals.
        /// </summary>
        public void NotifyModalOpened(MonoBehaviour openingModal)
        {
            currentActiveModal = openingModal;
            CloseAllModalsExcept(openingModal);
        }

        /// <summary>
        /// Call whenever a modal is closed.
        /// </summary>
        public void NotifyModalClosed(MonoBehaviour closingModal)
        {
            if (currentActiveModal == closingModal)
            {
                currentActiveModal = null;
            }
        }

        /// <summary>
        /// Closes all active modal windows except the specified instance.
        /// </summary>
        public void CloseAllModalsExcept(MonoBehaviour except = null)
        {
            // 1. Registered IModalWindow instances
            for (int i = 0; i < registeredModals.Count; i++)
            {
                var modal = registeredModals[i];
                if (modal != null && (except == null || (modal as MonoBehaviour) != except))
                {
                    if (modal.IsModalOpen)
                    {
                        modal.CloseModal();
                    }
                }
            }

            // 2. Direct Studio-Grade Fallbacks for all core game systems
            // Vehicle Trunk UI
            if (VehicleTrunkUI.Instance != null && VehicleTrunkUI.Instance != except && VehicleTrunkUI.Instance.IsOpen)
            {
                VehicleTrunkUI.Instance.CloseTrunk();
            }

            // Survival Alchemy & Cooking UI
            if (SurvivalAlchemyUI.Instance != null && SurvivalAlchemyUI.Instance != except && SurvivalAlchemyUI.Instance.IsOpen)
            {
                SurvivalAlchemyUI.Instance.ClosePanel();
            }

            // Workbench Crafting UI
            if (WorkbenchCraftingUI.Instance != null && WorkbenchCraftingUI.Instance != except)
            {
                WorkbenchCraftingUI.Instance.CloseWorkbench();
            }

            // Skill Tree UI
            if (SkillTreeUI.Instance != null && SkillTreeUI.Instance != except)
            {
                SkillTreeUI.Instance.CloseSkillTree();
            }

            // Backpack UI
            if (BackpackUI.Instance != null && BackpackUI.Instance != except && BackpackUI.Instance.gameObject.activeSelf)
            {
                BackpackUI.Instance.ClosePanel();
            }

            // Stash UI
            if (StashUI.Instance != null && StashUI.Instance != except && StashUI.Instance.gameObject.activeSelf)
            {
                StashUI.Instance.ClosePanel();
            }

            // Full Tactical Map UI
            if (FullTacticalMapUI.Instance != null && FullTacticalMapUI.Instance != except && FullTacticalMapUI.Instance.IsMapOpen)
            {
                FullTacticalMapUI.Instance.CloseMap();
            }

            // Full Map UI (legacy)
            if (FullMapUI.Instance != null && FullMapUI.Instance != except && FullMapUI.Instance.IsMapOpen)
            {
                FullMapUI.Instance.CloseMap();
            }

            // Settings UI
            if (SettingsUI.Instance != null && SettingsUI.Instance != except && SettingsUI.Instance.gameObject.activeSelf)
            {
                SettingsUI.Instance.CloseSettings();
            }
        }

        public bool HasAnyModalOpen()
        {
            if (VehicleTrunkUI.Instance != null && VehicleTrunkUI.Instance.IsOpen) return true;
            if (SurvivalAlchemyUI.Instance != null && SurvivalAlchemyUI.Instance.IsOpen) return true;
            if (BackpackUI.Instance != null && BackpackUI.Instance.gameObject.activeSelf) return true;
            if (StashUI.Instance != null && StashUI.Instance.gameObject.activeSelf) return true;
            if (FullTacticalMapUI.Instance != null && FullTacticalMapUI.Instance.IsMapOpen) return true;

            for (int i = 0; i < registeredModals.Count; i++)
            {
                if (registeredModals[i] != null && registeredModals[i].IsModalOpen) return true;
            }
            return false;
        }
    }
}
