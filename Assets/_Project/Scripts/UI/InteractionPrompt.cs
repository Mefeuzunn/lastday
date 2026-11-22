using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;

namespace ProjectSurvival.UI
{
    public class InteractionPrompt : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private Text promptText;

        private void Awake()
        {
            if (promptPanel == null) promptPanel = gameObject;
            if (promptText == null) promptText = GetComponentInChildren<Text>();

            SetVisible(false, string.Empty);
        }

        private void OnEnable()
        {
            GameEvents.OnInteractionPromptChanged += HandlePromptChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnInteractionPromptChanged -= HandlePromptChanged;
        }

        private void HandlePromptChanged(bool isVisible, string message)
        {
            SetVisible(isVisible, message);
        }

        public void SetVisible(bool isVisible, string message)
        {
            if (promptPanel != null) promptPanel.SetActive(isVisible);
            if (promptText != null) promptText.text = message;
        }

        public void OnPromptButtonClicked()
        {
            GameEvents.TriggerInteractPressed();
        }
    }
}
