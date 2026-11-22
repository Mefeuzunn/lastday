using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;

namespace ProjectSurvival.Narrative
{
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Text speakerNameText;
        [SerializeField] private Text dialogueBodyText;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Barter Button (Optional)")]
        [SerializeField] private Button barterButton;
        [SerializeField] private Text barterButtonText;

        [Header("Pre-allocated Choice Buttons (Optional)")]
        [SerializeField] private List<Button> choiceButtons = new List<Button>();

        [Header("Typewriter Settings")]
        [SerializeField] private float typewriterSpeed = 0.025f;

        private Coroutine typingCoroutine;
        private string currentFullBodyText = "";
        private bool isTyping = false;

        public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (dialoguePanel == null) dialoguePanel = gameObject;
            dialoguePanel.SetActive(false);

            // Add click-to-complete on panel background if Button exists or add button component
            Button panelBtn = dialoguePanel.GetComponent<Button>();
            if (panelBtn == null)
            {
                panelBtn = dialoguePanel.AddComponent<Button>();
                panelBtn.transition = Selectable.Transition.None;
            }
            panelBtn.onClick.RemoveAllListeners();
            panelBtn.onClick.AddListener(OnPanelClicked);
        }

        public void DisplayNode(StoryNode node)
        {
            if (node == null)
            {
                Hide();
                return;
            }

            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (speakerNameText != null) speakerNameText.text = node.speakerName;

            currentFullBodyText = node.dialogueText ?? "";

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypewriterRoutine(currentFullBodyText));

            PopulateChoices(node.choices);
            UpdateBarterButton(node.speakerName);
        }

        private System.Collections.IEnumerator TypewriterRoutine(string fullText)
        {
            isTyping = true;
            if (dialogueBodyText != null)
            {
                dialogueBodyText.text = "";
                for (int i = 0; i < fullText.Length; i++)
                {
                    dialogueBodyText.text += fullText[i];
                    // Skip delay on whitespace for slightly smoother reading rhythm
                    if (fullText[i] != ' ')
                    {
                        yield return new WaitForSecondsRealtime(typewriterSpeed);
                    }
                }
            }
            isTyping = false;
            typingCoroutine = null;
        }

        public void SkipTypewriter()
        {
            if (isTyping)
            {
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = null;
                isTyping = false;
                if (dialogueBodyText != null)
                {
                    dialogueBodyText.text = currentFullBodyText;
                }
            }
        }

        private void OnPanelClicked()
        {
            if (isTyping)
            {
                SkipTypewriter();
            }
        }

        private void PopulateChoices(List<StoryChoice> choices)
        {
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                Button btn = choiceButtons[i];
                if (i < choices.Count)
                {
                    StoryChoice choice = choices[i];
                    btn.gameObject.SetActive(true);

                    string lockReason = string.Empty;
                    bool isAvailable = ChoiceProcessor.Instance == null || ChoiceProcessor.Instance.IsChoiceAvailable(choice, out lockReason);
                    btn.interactable = isAvailable;

                    Text btnText = btn.GetComponentInChildren<Text>();
                    if (btnText != null)
                    {
                        if (!isAvailable && !string.IsNullOrEmpty(lockReason))
                        {
                            btnText.text = $"{choice.choiceText}  <color=#FF7675>{lockReason}</color>";
                        }
                        else
                        {
                            btnText.text = choice.choiceText;
                        }
                    }

                    Image btnImg = btn.GetComponent<Image>();
                    if (btnImg != null)
                    {
                        btnImg.color = isAvailable ? new Color(0.18f, 0.28f, 0.42f, 0.95f) : new Color(0.12f, 0.14f, 0.18f, 0.65f);
                    }

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        if (isTyping)
                        {
                            SkipTypewriter();
                            return;
                        }

                        if (StoryManager.Instance != null)
                        {
                            StoryManager.Instance.SelectChoice(choice);
                        }
                    });
                }
                else
                {
                    btn.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateBarterButton(string speakerName)
        {
            if (barterButton == null)
            {
                CreateBarterButtonIfMissing();
            }

            if (barterButton != null)
            {
                bool isMerchant = !string.IsNullOrEmpty(speakerName) &&
                    (speakerName.Contains("Aras") || speakerName.Contains("Kerem") || speakerName.Contains("Elif") || speakerName.Contains("Zeynep"));

                barterButton.gameObject.SetActive(isMerchant);
                if (isMerchant)
                {
                    barterButton.onClick.RemoveAllListeners();
                    barterButton.onClick.AddListener(() =>
                    {
                        Hide();
                        string npcId = speakerName.ToLowerInvariant();
                        GameEvents.TriggerBarterRequested(npcId, speakerName);
                    });
                }
            }
        }

        private void CreateBarterButtonIfMissing()
        {
            if (choicesContainer == null) return;

            GameObject bObj = new GameObject("Btn_BarterTrade");
            bObj.transform.SetParent(choicesContainer, false);

            Image img = bObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.45f, 0.35f, 0.95f); // Soft tactical emerald green

            barterButton = bObj.AddComponent<Button>();
            barterButton.targetGraphic = img;

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(bObj.transform, false);
            RectTransform tr = tObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            barterButtonText = tObj.AddComponent<Text>();
            barterButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            barterButtonText.fontSize = 15;
            barterButtonText.alignment = TextAnchor.MiddleCenter;
            barterButtonText.color = Color.white;
            barterButtonText.text = "TAKAS YAP (Erzak & İkmal)";
            barterButtonText.raycastTarget = false;

            RectTransform br = bObj.GetComponent<RectTransform>();
            if (br != null) br.sizeDelta = new Vector2(320f, 44f);
        }

        public void Hide()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }
    }
}
