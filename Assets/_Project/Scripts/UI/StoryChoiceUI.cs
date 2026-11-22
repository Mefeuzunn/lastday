using System;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Active Phase 5: Chapter 4 Moral Endgame Decision Modal.
    /// Presents player with the defining choice between Regional Evacuation Broadcast
    /// and Global Antiviral Synthesis Transmission.
    /// </summary>
    public class StoryChoiceUI : MonoBehaviour, IModalWindow
    {
        public static StoryChoiceUI Instance { get; private set; }

        [Header("UI Root & Controls")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;

        [Header("Choice A: Evacuation Broadcast")]
        [SerializeField] private Button btnChoiceA;
        [SerializeField] private Text choiceATitleText;
        [SerializeField] private Text choiceADescText;

        [Header("Choice B: Global Antiviral Formula")]
        [SerializeField] private Button btnChoiceB;
        [SerializeField] private Text choiceBTitleText;
        [SerializeField] private Text choiceBDescText;

        public bool IsModalOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            WireButtons();
        }

        private void Start()
        {
            ModalWindowManager.Instance?.RegisterModal(this);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            ModalWindowManager.Instance?.UnregisterModal(this);
        }

        private void WireButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseModal);
            }

            if (btnChoiceA != null)
            {
                if (btnChoiceA.targetGraphic != null) btnChoiceA.targetGraphic.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);
                btnChoiceA.onClick.RemoveAllListeners();
                btnChoiceA.onClick.AddListener(SelectChoiceA);
            }

            if (btnChoiceB != null)
            {
                if (btnChoiceB.targetGraphic != null) btnChoiceB.targetGraphic.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);
                btnChoiceB.onClick.RemoveAllListeners();
                btnChoiceB.onClick.AddListener(SelectChoiceB);
            }
        }

        public void OpenModal()
        {
            ModalWindowManager.Instance?.NotifyModalOpened(this);
            if (panelRoot != null) panelRoot.SetActive(true);

            ProceduralAudioManager.Instance?.PlayVaultUnlock();
            MobileHapticManager.TriggerHeavy();
        }

        public void CloseModal()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalWindowManager.Instance?.NotifyModalClosed(this);
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        private void SelectChoiceA()
        {
            MainStoryDirector.Instance?.ExecuteEndgameMoralChoice("Evacuation");
            CloseModal();
        }

        private void SelectChoiceB()
        {
            MainStoryDirector.Instance?.ExecuteEndgameMoralChoice("GlobalCure");
            CloseModal();
        }

        public void BindUIReferences(GameObject root, Button closeBtn, Button choiceABtn, Text titleA, Text descA,
            Button choiceBBtn, Text titleB, Text descB)
        {
            panelRoot = root;
            closeButton = closeBtn;
            btnChoiceA = choiceABtn;
            choiceATitleText = titleA;
            choiceADescText = descA;
            btnChoiceB = choiceBBtn;
            choiceBTitleText = titleB;
            choiceBDescText = descB;

            WireButtons();
        }
    }
}
