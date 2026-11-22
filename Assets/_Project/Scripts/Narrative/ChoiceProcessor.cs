using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Missions;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Narrative
{
    public class ChoiceProcessor : MonoBehaviour
    {
        public static ChoiceProcessor Instance { get; private set; }

        private readonly HashSet<string> activeWorldFlags = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool IsChoiceAvailable(StoryChoice choice)
        {
            return IsChoiceAvailable(choice, out _);
        }

        public bool IsChoiceAvailable(StoryChoice choice, out string lockReason)
        {
            lockReason = string.Empty;
            if (choice == null) return false;

            // 1. Required Flags
            if (choice.requiredFlags != null && choice.requiredFlags.Count > 0)
            {
                foreach (var flag in choice.requiredFlags)
                {
                    if (string.IsNullOrEmpty(flag)) continue;
                    if (!HasWorldFlag(flag))
                    {
                        lockReason = $"[Gereksinim: {flag}]";
                        return false;
                    }
                }
            }

            // 2. Forbidden Flags
            if (choice.forbiddenFlags != null && choice.forbiddenFlags.Count > 0)
            {
                foreach (var flag in choice.forbiddenFlags)
                {
                    if (string.IsNullOrEmpty(flag)) continue;
                    if (HasWorldFlag(flag))
                    {
                        lockReason = "[Seçenek Engellendi]";
                        return false;
                    }
                }
            }

            // 3. Required Item
            if (!string.IsNullOrEmpty(choice.requiredItemId))
            {
                int count = choice.requiredItemCount > 0 ? choice.requiredItemCount : 1;
                if (InventorySystem.Instance == null || !InventorySystem.Instance.HasItem(choice.requiredItemId, count))
                {
                    string displayName = choice.requiredItemId;
                    ItemData it = Resources.Load<ItemData>($"Items/{choice.requiredItemId}");
                    if (it != null && !string.IsNullOrEmpty(it.itemName)) displayName = it.itemName;
                    lockReason = $"[Gerekli: {count}x {displayName}]";
                    return false;
                }
            }

            // 4. Required Time of Day
            if (!string.IsNullOrEmpty(choice.requiredTimeOfDay) && !choice.requiredTimeOfDay.Equals("Any", System.StringComparison.OrdinalIgnoreCase))
            {
                bool isNight = DayNightManager.Instance != null && DayNightManager.Instance.IsNight;
                if (choice.requiredTimeOfDay.Equals("Night", System.StringComparison.OrdinalIgnoreCase) && !isNight)
                {
                    lockReason = "[Sadece Gece]";
                    return false;
                }
                if (choice.requiredTimeOfDay.Equals("Day", System.StringComparison.OrdinalIgnoreCase) && isNight)
                {
                    lockReason = "[Sadece Gündüz]";
                    return false;
                }
            }

            // 5. Min Trust Required
            if (!string.IsNullOrEmpty(choice.trustNpcId) && choice.minTrustRequired != 0)
            {
                int trust = StoryManager.Instance != null ? StoryManager.Instance.GetTrust(choice.trustNpcId) : 0;
                if (trust < choice.minTrustRequired)
                {
                    lockReason = $"[Yüksek Güven Gerekli ({trust}/{choice.minTrustRequired})]";
                    return false;
                }
            }

            return true;
        }

        public void ProcessChoice(StoryChoice choice)
        {
            if (choice == null) return;

            // 1. Consume required item only if consumeItem is true
            if (!string.IsNullOrEmpty(choice.requiredItemId) && choice.consumeItem)
            {
                int count = choice.requiredItemCount > 0 ? choice.requiredItemCount : 1;
                if (InventorySystem.Instance != null)
                {
                    InventorySystem.Instance.RemoveItem(choice.requiredItemId, count);
                    Debug.Log($"[ChoiceProcessor] Consumed {count}x {choice.requiredItemId}");
                }
            }

            // 2. Apply Stat Effects
            if (choice.statEffects != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    SurvivalStats stats = player.GetComponent<SurvivalStats>();
                    if (stats != null)
                    {
                        if (choice.statEffects.healthChange > 0) stats.Heal(choice.statEffects.healthChange);
                        else if (choice.statEffects.healthChange < 0) stats.TakeDamage(-choice.statEffects.healthChange);

                        if (choice.statEffects.hungerChange != 0) stats.Feed(choice.statEffects.hungerChange);
                        if (choice.statEffects.thirstChange != 0) stats.Hydrate(choice.statEffects.thirstChange);
                        if (choice.statEffects.infectionChange != 0) stats.ModifyInfection(choice.statEffects.infectionChange);
                    }
                }
            }

            // 3. Give Item Reward
            if (!string.IsNullOrEmpty(choice.giveItemId))
            {
                int count = choice.giveItemCount > 0 ? choice.giveItemCount : 1;
                ItemData item = Resources.Load<ItemData>($"Items/{choice.giveItemId}");
                if (item != null && InventorySystem.Instance != null)
                {
                    InventorySystem.Instance.AddItem(item, count);
                    Debug.Log($"[ChoiceProcessor] Granted reward: {count}x {item.itemName}");
                }
            }

            // 4. Save World Flags
            if (choice.worldFlags != null)
            {
                foreach (var flag in choice.worldFlags)
                {
                    if (!string.IsNullOrEmpty(flag))
                    {
                        activeWorldFlags.Add(flag);
                        Debug.Log($"[ChoiceProcessor] World flag set: {flag}");
                        if (flag.Contains("chapter1_complete"))
                        {
                            GameEvents.TriggerChapterCompleted("chapter1_outbreak");
                        }
                    }
                }
            }

            // 5. Apply NPC Trust Changes
            if (choice.trustChanges != null && StoryManager.Instance != null)
            {
                foreach (var tc in choice.trustChanges)
                {
                    if (tc != null && !string.IsNullOrEmpty(tc.npcId))
                    {
                        StoryManager.Instance.ModifyTrust(tc.npcId, tc.amount);
                    }
                }
            }

            // 6. Trigger Mission if present
            if (choice.triggerMission != null && !string.IsNullOrEmpty(choice.triggerMission.missionId))
            {
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.StartMission(choice.triggerMission);
                }
            }

            // 7. Companion Recruitment Trigger
            if (choice.choiceText != null && (choice.choiceText.Contains("Benimle Gel") || choice.choiceText.Contains("Yoldaş Ol")))
            {
                if (ProjectSurvival.AI.CompanionManager.Instance != null)
                {
                    GameObject p = GameObject.FindWithTag("Player");
                    if (p != null)
                    {
                        var npcs = Object.FindObjectsByType<ProjectSurvival.AI.SurvivorNPC_AI>(FindObjectsSortMode.None);
                        ProjectSurvival.AI.SurvivorNPC_AI nearest = null;
                        float minDist = 4.5f;
                        foreach (var n in npcs)
                        {
                            float d = Vector3.Distance(p.transform.position, n.transform.position);
                            if (d < minDist) { minDist = d; nearest = n; }
                        }
                        if (nearest != null)
                        {
                            ProjectSurvival.AI.CompanionManager.Instance.RecruitCompanion(nearest);
                        }
                    }
                }
            }
        }

        public bool HasWorldFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return false;
            return activeWorldFlags.Contains(flag);
        }

        public List<string> GetActiveWorldFlags() => new List<string>(activeWorldFlags);

        public void SetWorldFlags(List<string> flags)
        {
            activeWorldFlags.Clear();
            if (flags != null)
            {
                foreach (var f in flags)
                {
                    if (!string.IsNullOrEmpty(f)) activeWorldFlags.Add(f);
                }
            }
        }
    }
}
