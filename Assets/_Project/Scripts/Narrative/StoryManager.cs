using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Narrative
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private StoryContainer currentStory;
        [SerializeField] private StoryNode currentNode;

        private readonly Dictionary<string, StoryContainer> loadedStories = new Dictionary<string, StoryContainer>();
        private readonly Dictionary<string, int> npcTrust = new Dictionary<string, int>();

        public StoryNode CurrentNode => currentNode;

        public int GetTrust(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return 0;
            return npcTrust.TryGetValue(npcId.ToLowerInvariant(), out int val) ? val : 0;
        }

        public void ModifyTrust(string npcId, int delta)
        {
            if (string.IsNullOrEmpty(npcId)) return;
            string key = npcId.ToLowerInvariant();
            int current = GetTrust(key);
            int updated = current + delta;
            npcTrust[key] = updated;
            Debug.Log($"[StoryManager] Trust for '{key}' updated: {current} -> {updated} (delta: {delta:+0;-#;0})");
            GameEvents.TriggerNPCTrustChanged(key, updated, delta);
        }

        public void SetTrust(string npcId, int value)
        {
            if (string.IsNullOrEmpty(npcId)) return;
            npcTrust[npcId.ToLowerInvariant()] = value;
        }

        public Dictionary<string, int> GetAllTrust() => new Dictionary<string, int>(npcTrust);

        public void LoadTrustData(List<string> keys, List<int> values)
        {
            npcTrust.Clear();
            if (keys == null || values == null) return;
            for (int i = 0; i < keys.Count && i < values.Count; i++)
            {
                if (!string.IsNullOrEmpty(keys[i]))
                {
                    npcTrust[keys[i].ToLowerInvariant()] = values[i];
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

        private void OnEnable()
        {
            GameEvents.OnStoryRequested += StartStory;
        }

        private void OnDisable()
        {
            GameEvents.OnStoryRequested -= StartStory;
        }

        public void StartStory(string storyResourceName, string startNodeId)
        {
            StoryContainer story = LoadStory(storyResourceName);
            if (story == null)
            {
                Debug.LogError($"[StoryManager] Could not load story: Stories/{storyResourceName}");
                return;
            }

            currentStory = story;
            string nodeToLoad = !string.IsNullOrEmpty(startNodeId) ? startNodeId : story.startNodeId;
            GameEvents.TriggerDialogueStarted(storyResourceName, nodeToLoad);
            GoToNode(nodeToLoad);
        }

        public void GoToNode(string nodeId)
        {
            if (currentStory == null) return;

            if (string.IsNullOrEmpty(nodeId) || nodeId.Equals("END", System.StringComparison.OrdinalIgnoreCase))
            {
                EndDialogue();
                return;
            }

            StoryNode node = currentStory.FindNode(nodeId);
            if (node == null)
            {
                Debug.LogWarning($"[StoryManager] Node '{nodeId}' not found in story '{currentStory.storyId}'");
                EndDialogue();
                return;
            }

            currentNode = node;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Dialogue);
            }

            if (DialogueUI.Instance != null)
            {
                DialogueUI.Instance.DisplayNode(node);
            }
        }

        public void SelectChoice(StoryChoice choice)
        {
            if (choice == null) return;

            // Process effects, items, flags, missions
            if (ChoiceProcessor.Instance != null)
            {
                ChoiceProcessor.Instance.ProcessChoice(choice);
            }

            // If a mission was triggered, close dialogue so the gameplay mission can be played
            if (choice.triggerMission != null && !string.IsNullOrEmpty(choice.triggerMission.missionId))
            {
                EndDialogue();
                return;
            }

            // Otherwise advance dialogue node
            GoToNode(choice.nextNodeId);
        }

        public void ResumeStoryAtNode(string nodeId)
        {
            if (currentStory == null)
            {
                // Fallback to default outbreak story if none cached
                StartStory("chapter1_outbreak", nodeId);
                return;
            }

            GoToNode(nodeId);
        }

        public void EndDialogue()
        {
            currentNode = null;
            if (DialogueUI.Instance != null)
            {
                DialogueUI.Instance.Hide();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Gameplay);
            }

            GameEvents.TriggerDialogueClosed();
        }

        private StoryContainer LoadStory(string resourceName)
        {
            if (loadedStories.TryGetValue(resourceName, out StoryContainer cached))
            {
                return cached;
            }

            TextAsset jsonAsset = Resources.Load<TextAsset>($"Stories/{resourceName}");
            if (jsonAsset == null)
            {
                Debug.LogError($"[StoryManager] Story JSON file not found at Resources/Stories/{resourceName}");
                return null;
            }

            try
            {
                StoryContainer story = JsonUtility.FromJson<StoryContainer>(jsonAsset.text);
                loadedStories[resourceName] = story;
                return story;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[StoryManager] Failed to parse JSON for {resourceName}: {ex.Message}");
                return null;
            }
        }
    }
}
