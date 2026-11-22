using System;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Player;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Phase 2 (Step 2B): Ergonomic Action Cluster HUD Controller.
    /// Manages real-time visual feedback for the bottom-right thumb arc:
    /// - Active highlight on Btn_Crouch when crouching
    /// - Active glowing ring on Btn_Sprint when sprinting
    /// - Strict raycast target hygiene (root Image = true, children = false)
    /// </summary>
    public class ActionClusterHUD : MonoBehaviour
    {
        public static ActionClusterHUD Instance { get; private set; }

        [Header("Buttons")]
        [SerializeField] private Button fireButton;
        [SerializeField] private Button aimButton;
        [SerializeField] private Button jumpVaultButton;
        [SerializeField] private Button crouchButton;
        [SerializeField] private Button sprintButton;

        [Header("Active Outlines / Highlights")]
        [SerializeField] private Outline crouchOutline;
        [SerializeField] private Outline sprintOutline;

        private PlayerController playerController;

        private readonly Color crouchActiveColor = new Color(0.2f, 0.85f, 1.0f, 0.95f);
        private readonly Color crouchInactiveColor = new Color(0.22f, 0.50f, 0.75f, 0.65f);

        private readonly Color sprintActiveColor = new Color(1.0f, 0.78f, 0.15f, 0.95f);
        private readonly Color sprintInactiveColor = new Color(0.95f, 0.65f, 0.15f, 0.65f);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            AutoWireComponents();
        }

        private void AutoWireComponents()
        {
            if (fireButton == null) fireButton = transform.Find("Btn_Fire")?.GetComponent<Button>();
            if (aimButton == null) aimButton = transform.Find("Btn_Aim")?.GetComponent<Button>();
            if (crouchButton == null) crouchButton = transform.Find("Btn_Crouch")?.GetComponent<Button>();
            if (sprintButton == null) sprintButton = transform.Find("Btn_Sprint")?.GetComponent<Button>();

            if (crouchButton != null && crouchOutline == null)
            {
                crouchOutline = crouchButton.GetComponent<Outline>();
                if (crouchOutline == null) crouchOutline = crouchButton.gameObject.AddComponent<Outline>();
            }

            if (sprintButton != null && sprintOutline == null)
            {
                sprintOutline = sprintButton.GetComponent<Outline>();
                if (sprintOutline == null) sprintOutline = sprintButton.gameObject.AddComponent<Outline>();
            }
        }

        private void Update()
        {
            if (playerController == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerController = player.GetComponent<PlayerController>();
            }

            if (playerController != null)
            {
                // Update Crouch Visual Highlight
                if (crouchOutline != null)
                {
                    crouchOutline.effectColor = playerController.IsCrouching ? crouchActiveColor : crouchInactiveColor;
                    crouchOutline.effectDistance = playerController.IsCrouching ? new Vector2(2.5f, -2.5f) : new Vector2(1.5f, -1.5f);
                }

                // Update Sprint Visual Ring
                if (sprintOutline != null)
                {
                    sprintOutline.effectColor = playerController.IsSprinting ? sprintActiveColor : sprintInactiveColor;
                    sprintOutline.effectDistance = playerController.IsSprinting ? new Vector2(2.5f, -2.5f) : new Vector2(1.5f, -1.5f);
                }
            }
        }
    }
}
