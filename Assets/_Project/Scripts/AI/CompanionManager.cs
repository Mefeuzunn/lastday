using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.AI
{
    public class CompanionManager : MonoBehaviour
    {
        public static CompanionManager Instance { get; private set; }

        [Header("Active Companion")]
        [SerializeField] private SurvivorNPC_AI activeCompanion;
        [SerializeField] private List<SurvivorNPC_AI> recruitedRoster = new List<SurvivorNPC_AI>();

        [Header("HUD Card References")]
        [SerializeField] private GameObject companionCardRoot;
        [SerializeField] private Text companionNameText;
        [SerializeField] private Slider companionHealthSlider;
        [SerializeField] private Text companionHealthText;
        [SerializeField] private Button orderButton;
        [SerializeField] private Text orderButtonText;
        [SerializeField] private Button healButton;

        public SurvivorNPC_AI ActiveCompanion => activeCompanion;
        public bool HasActiveCompanion => activeCompanion != null && !activeCompanion.IsDead;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureHUDCard();
        }

        private void OnEnable()
        {
            GameEvents.OnCompanionHealthChanged += HandleCompanionHealthChanged;
            GameEvents.OnCompanionOrderChanged += HandleCompanionOrderChanged;
            GameEvents.OnDialogueStarted += HandleDialogueStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnCompanionHealthChanged -= HandleCompanionHealthChanged;
            GameEvents.OnCompanionOrderChanged -= HandleCompanionOrderChanged;
            GameEvents.OnDialogueStarted -= HandleDialogueStarted;
        }

        private void EnsureHUDCard()
        {
            if (companionCardRoot != null)
            {
                companionCardRoot.SetActive(false);
                return;
            }

            // Find dynamically on Canvas if not wired via inspector
            GameObject cardObj = GameObject.Find("CompanionCard_Panel");
            if (cardObj != null)
            {
                companionCardRoot = cardObj;
                companionNameText = cardObj.transform.Find("CompanionName")?.GetComponent<Text>();
                companionHealthSlider = cardObj.transform.Find("HealthBar")?.GetComponent<Slider>();
                companionHealthText = cardObj.transform.Find("HealthBar/HealthText")?.GetComponent<Text>();
                
                Transform btnOrder = cardObj.transform.Find("Btn_Order");
                if (btnOrder != null)
                {
                    orderButton = btnOrder.GetComponent<Button>();
                    orderButtonText = btnOrder.GetComponentInChildren<Text>();
                    orderButton.onClick.RemoveAllListeners();
                    orderButton.onClick.AddListener(ToggleOrder);
                }

                Transform btnHeal = cardObj.transform.Find("Btn_Heal");
                if (btnHeal != null)
                {
                    healButton = btnHeal.GetComponent<Button>();
                    healButton.onClick.RemoveAllListeners();
                    healButton.onClick.AddListener(HealCompanion);
                }

                companionCardRoot.SetActive(false);
            }
        }

        public void RecruitCompanion(SurvivorNPC_AI npc)
        {
            if (npc == null || npc.IsDead) return;

            // Dismiss previous active companion to safe house
            if (activeCompanion != null && activeCompanion != npc)
            {
                activeCompanion.SetAsCompanion(false);
                Debug.Log($"[CompanionManager] Dismissed previous companion {activeCompanion.NpcDisplayName} to SafeHouse.");
            }

            activeCompanion = npc;
            if (!recruitedRoster.Contains(npc)) recruitedRoster.Add(npc);

            npc.SetAsCompanion(true);

            UpdateHUDCardDisplay();
            if (companionCardRoot != null) companionCardRoot.SetActive(true);

            GameEvents.TriggerCompanionRecruited(npc.NpcDisplayName, npc.gameObject);
            GameEvents.TriggerObjectiveToastRequested($"[YOLDAŞ] {npc.NpcDisplayName} ekibinize katıldı! Birlikte hayatta kalacaksınız.");
            ProceduralAudioManager.Instance?.PlayItemPickup();
        }

        public void DismissCompanion()
        {
            if (activeCompanion != null)
            {
                string name = activeCompanion.NpcDisplayName;
                activeCompanion.SetAsCompanion(false);
                activeCompanion = null;
                if (companionCardRoot != null) companionCardRoot.SetActive(false);
                GameEvents.TriggerCompanionDismissed(name);
                GameEvents.TriggerObjectiveToastRequested($"[YOLDAŞ] {name} ekibinizden ayrıldı.");
            }
        }

        public string GetActiveCompanionId()
        {
            return (activeCompanion != null && !activeCompanion.IsDead) ? activeCompanion.NpcId : string.Empty;
        }

        public void RestoreCompanion(string companionId)
        {
            if (string.IsNullOrEmpty(companionId))
            {
                DismissCompanion();
                return;
            }

            EnsureHUDCard();

            SurvivorNPC_AI[] npcs = UnityEngine.Object.FindObjectsByType<SurvivorNPC_AI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            SurvivorNPC_AI match = null;
            for (int i = 0; i < npcs.Length; i++)
            {
                var n = npcs[i];
                if (n != null && (n.NpcId == companionId || n.NpcDisplayName.ToLower().Contains(companionId.ToLower())))
                {
                    match = n;
                    break;
                }
            }

            if (match != null)
            {
                GameObject player = null;
                try { player = GameObject.FindWithTag("Player"); } catch {}
                if (player != null)
                {
                    Vector3 targetPos = player.transform.position - player.transform.forward * 2.5f;
                    if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out UnityEngine.AI.NavMeshHit hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        targetPos = hit.position;
                    }

                    var agent = match.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.Warp(targetPos);
                    }
                    else
                    {
                        match.transform.position = targetPos;
                    }
                }

                RecruitCompanion(match);
                Debug.Log($"[CompanionManager] Restored active companion: {match.NpcDisplayName} ({companionId})");
            }
        }

        public void ToggleOrder()
        {
            if (activeCompanion == null || activeCompanion.IsDead) return;

            if (activeCompanion.CurrentState == NPCState.FollowPlayer)
            {
                activeCompanion.OrderHold();
                if (orderButtonText != null) orderButtonText.text = "TAKİP ET";
                GameEvents.TriggerObjectiveToastRequested($"[EMİR] {activeCompanion.NpcDisplayName}: 'Burada bekliyorum.'");
            }
            else
            {
                activeCompanion.OrderFollow();
                if (orderButtonText != null) orderButtonText.text = "BEKLE";
                GameEvents.TriggerObjectiveToastRequested($"[EMİR] {activeCompanion.NpcDisplayName}: 'Arkanızdayım.'");
            }

            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        public void HealCompanion()
        {
            if (activeCompanion == null || activeCompanion.IsDead) return;

            if (activeCompanion.CurrentHealth >= activeCompanion.MaxHealth - 0.1f)
            {
                GameEvents.TriggerObjectiveToastRequested($"[YOLDAŞ] {activeCompanion.NpcDisplayName} zaten tam sağlıkta!");
                return;
            }

            InventorySystem inv = InventorySystem.Instance;
            if (inv == null) return;

            if (inv.HasItem("item_medkit", 1))
            {
                inv.RemoveItem("item_medkit", 1);
                activeCompanion.Heal(80f);
                GameEvents.TriggerObjectiveToastRequested($"[İYİLEŞTİRME] İlk Yardım Kiti kullanıldı! {activeCompanion.NpcDisplayName} (+80 HP)");
            }
            else if (inv.HasItem("item_bandage", 1))
            {
                inv.RemoveItem("item_bandage", 1);
                activeCompanion.Heal(40f);
                GameEvents.TriggerObjectiveToastRequested($"[İYİLEŞTİRME] Sargı Bezi kullanıldı! {activeCompanion.NpcDisplayName} (+40 HP)");
            }
            else
            {
                GameEvents.TriggerObjectiveToastRequested("[YOLDAŞ] Çantanda Sargı Bezi veya İlk Yardım Kiti yok!");
                ProceduralAudioManager.Instance?.PlayDamageSound();
            }
        }

        private void UpdateHUDCardDisplay()
        {
            if (activeCompanion == null) return;

            if (companionNameText != null)
            {
                string archetypeLabel = activeCompanion.Archetype == NPCArchetype.Armed ? "Silahlı Yoldaş" : "Sıhhiyeci";
                companionNameText.text = $"{activeCompanion.NpcDisplayName} <color=#95A5A6>({archetypeLabel})</color>";
            }

            if (companionHealthSlider != null)
            {
                companionHealthSlider.maxValue = activeCompanion.MaxHealth;
                companionHealthSlider.value = activeCompanion.CurrentHealth;
            }

            if (companionHealthText != null)
            {
                companionHealthText.text = $"{activeCompanion.CurrentHealth:F0} / {activeCompanion.MaxHealth:F0}";
            }

            if (orderButtonText != null)
            {
                orderButtonText.text = activeCompanion.CurrentState == NPCState.FollowPlayer ? "BEKLE" : "TAKİP ET";
            }
        }

        private void HandleCompanionHealthChanged(float current, float max)
        {
            if (companionHealthSlider != null)
            {
                companionHealthSlider.maxValue = max;
                companionHealthSlider.value = current;
            }

            if (companionHealthText != null)
            {
                companionHealthText.text = $"{current:F0} / {max:F0}";
            }
        }

        private void HandleCompanionOrderChanged(bool isFollowing)
        {
            if (orderButtonText != null)
            {
                orderButtonText.text = isFollowing ? "BEKLE" : "TAKİP ET";
            }
        }

        private void HandleDialogueStarted(string storyResource, string startNodeId)
        {
            // Dynamically inject companion recruitment choice if talking to an eligible NPC
            if (StoryManager.Instance != null && StoryManager.Instance.CurrentNode != null)
            {
                InjectRecruitChoiceIfEligible(StoryManager.Instance.CurrentNode);
            }
        }

        public void InjectRecruitChoiceIfEligible(StoryNode node)
        {
            if (node == null) return;

            // Check if player is near any eligible NPC
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;

            SurvivorNPC_AI[] allNpcs = FindObjectsByType<SurvivorNPC_AI>(FindObjectsSortMode.None);
            SurvivorNPC_AI nearbyNpc = null;
            float minDist = 3.5f;

            foreach (var npc in allNpcs)
            {
                float d = Vector3.Distance(player.transform.position, npc.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearbyNpc = npc;
                }
            }

            if (nearbyNpc != null && !nearbyNpc.IsCompanion)
            {
                // Check if choice already injected
                bool alreadyHasRecruit = false;
                foreach (var c in node.choices)
                {
                    if (c.choiceText.Contains("Benimle Gel") || c.choiceText.Contains("Yoldaş Ol"))
                    {
                        alreadyHasRecruit = true;
                        break;
                    }
                }

                if (!alreadyHasRecruit)
                {
                    StoryChoice recruitChoice = new StoryChoice
                    {
                        choiceText = "★ Benimle Gel (Yoldaş Ol)",
                        nextNodeId = "END"
                    };

                    node.choices.Add(recruitChoice);
                }
            }
        }
    }
}
