using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Progression;
using ProjectSurvival.Managers;
using ProjectSurvival.Environment;
using ProjectSurvival.UI;

namespace ProjectSurvival.Narrative
{
    [Serializable]
    public class StoryObjective
    {
        public string id;
        public int chapter;
        public string chapterTitle;
        public string title;
        public string description;
        public Vector3 targetPosition;
        public float requiredFrequency = 0f;
        public bool isTerminalInteraction = false;
        public int xpReward = 0;
    }

    /// <summary>
    /// Active Phase 5: 4-Chapter Branching Main Story Director.
    /// Drives narrative progression, objective tracker states, radio frequency triggers,
    /// terminal interactions, XP progression rewards, and the endgame moral finale.
    /// </summary>
    public class MainStoryDirector : MonoBehaviour
    {
        public static MainStoryDirector Instance { get; set; }

        [Header("Story Progression State")]
        [SerializeField] private int currentChapter = 1;
        [SerializeField] private string currentObjectiveId = "obj_1a_tune_radio";
        [SerializeField] private bool isStoryCompleted = false;
        [SerializeField] private string endgameMoralChoice = string.Empty;

        [Header("Audio Logs Tracking")]
        [SerializeField] private List<string> collectedAudioLogIds = new List<string>();

        public int CurrentChapter => currentChapter;
        public string CurrentObjectiveId => currentObjectiveId;
        public bool IsStoryCompleted => isStoryCompleted;
        public string EndgameMoralChoice => endgameMoralChoice;
        public IReadOnlyList<string> CollectedAudioLogIds => collectedAudioLogIds;

        public event Action<StoryObjective> OnObjectiveChanged;
        public event Action<int, string> OnChapterUnlocked;
        public event Action<string> OnStoryCompletedEvent;
        public event Action<string> OnAudioLogCollected;

        private readonly Dictionary<string, StoryObjective> allObjectives = new Dictionary<string, StoryObjective>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildObjectiveCatalog();
        }

        private void OnEnable()
        {
            if (allObjectives.Count == 0) BuildObjectiveCatalog();
        }

        private void Start()
        {
            if (StarterPrologueDirector.Instance != null && !StarterPrologueDirector.Instance.IsPrologueCompleted)
            {
                // Defer to StarterPrologueDirector until prologue onboarding is completed
                return;
            }
            DispatchCurrentObjective();
        }

        public void BuildObjectiveCatalog()
        {
            allObjectives.Clear();

            // --- BÖLÜM 1: SESSİZLİĞİN KAYNAĞI ---
            allObjectives["obj_1a_tune_radio"] = new StoryObjective
            {
                id = "obj_1a_tune_radio",
                chapter = 1,
                chapterTitle = "BÖLÜM 1: SESSİZLİĞİN KAYNAĞI",
                title = "Telsizi 92.4 MHz Frekansına Ayarla",
                description = "Telsiz tarayıcısını aç ve Dr. Selin Kaya'nın acil yardım sinyalini çöz.",
                targetPosition = new Vector3(24f, 0.5f, 15f),
                requiredFrequency = 92.4f,
                isTerminalInteraction = false,
                xpReward = 0
            };

            allObjectives["obj_1b_inspect_clinic"] = new StoryObjective
            {
                id = "obj_1b_inspect_clinic",
                chapter = 1,
                chapterTitle = "BÖLÜM 1: SESSİZLİĞİN KAYNAĞI",
                title = "Zone Klinik Terminalini İncele",
                description = "Merkez klinikteki Dr. Kaya'nın şifreli araştırma terminaline ulaş ve verileri indir.",
                targetPosition = new Vector3(24f, 0.5f, 15f),
                requiredFrequency = 0f,
                isTerminalInteraction = true,
                xpReward = 150
            };

            // --- BÖLÜM 2: KARANTİNA ŞİFRESİ ---
            allObjectives["obj_2a_tune_radio"] = new StoryObjective
            {
                id = "obj_2a_tune_radio",
                chapter = 2,
                chapterTitle = "BÖLÜM 2: KARANTİNA ŞİFRESİ",
                title = "Telsizi 98.6 MHz Frekansına Ayarla",
                description = "Askeri tren enkazı kara kutu sinyalini çözerek Doğu Tren Garı'nı konumlandır.",
                targetPosition = new Vector3(280f, 0.5f, 30f),
                requiredFrequency = 98.6f,
                isTerminalInteraction = false,
                xpReward = 0
            };

            allObjectives["obj_2b_recover_disk"] = new StoryObjective
            {
                id = "obj_2b_recover_disk",
                chapter = 2,
                chapterTitle = "BÖLÜM 2: KARANTİNA ŞİFRESİ",
                title = "Karantina Şifre Diski'ni Ele Geçir",
                description = "Doğu Tren Garı'ndaki zırhlı kargo vagonundan Biyolojik Karantina Şifre Diski'ni al.",
                targetPosition = new Vector3(280f, 0.5f, 30f),
                requiredFrequency = 0f,
                isTerminalInteraction = true,
                xpReward = 200
            };

            // --- BÖLÜM 3: GÖLGE PROTOKOLÜ ---
            allObjectives["obj_3a_tune_radio"] = new StoryObjective
            {
                id = "obj_3a_tune_radio",
                chapter = 3,
                chapterTitle = "BÖLÜM 3: GÖLGE PROTOKOLÜ",
                title = "Telsizi 104.2 MHz Frekansına Ayarla",
                description = "104.2 MHz frekansını çöz ve Kuzeybatı göl kıyısındaki gizli araştırma sığınağını açığa çıkar.",
                targetPosition = new Vector3(-240f, 0.5f, 270f),
                requiredFrequency = 104.2f,
                isTerminalInteraction = false,
                xpReward = 0
            };

            allObjectives["obj_3b_retrieve_key"] = new StoryObjective
            {
                id = "obj_3b_retrieve_key",
                chapter = 3,
                chapterTitle = "BÖLÜM 3: GÖLGE PROTOKOLÜ",
                title = "Antiviral Sentez Anahtarı'nı Kurtar",
                description = "Kuzeybatı sığınak terminaline eriş ve Dr. Kaya'nın Antiviral Sentez Anahtarı'nı kurtar.",
                targetPosition = new Vector3(-240f, 0.5f, 270f),
                requiredFrequency = 0f,
                isTerminalInteraction = true,
                xpReward = 250
            };

            // --- BÖLÜM 4: SON ŞAFAK KARARI ---
            allObjectives["obj_4a_tune_radio"] = new StoryObjective
            {
                id = "obj_4a_tune_radio",
                chapter = 4,
                chapterTitle = "BÖLÜM 4: SON ŞAFAK KARARI",
                title = "107.8 MHz ve Kuzey Yayın Kulesine Ulaş",
                description = "107.8 MHz frekansını ayarla ve Kuzey Otoyolu Viyadüğü üzerindeki ana verici kulesine çık.",
                targetPosition = new Vector3(0f, 7.5f, 220f),
                requiredFrequency = 107.8f,
                isTerminalInteraction = false,
                xpReward = 0
            };

            allObjectives["obj_4b_moral_choice"] = new StoryObjective
            {
                id = "obj_4b_moral_choice",
                chapter = 4,
                chapterTitle = "BÖLÜM 4: SON ŞAFAK KARARI",
                title = "Nihai Yayını Seç: Tahliye mi, Antiviral mi?",
                description = "Yayın konsolunu etkinleştir ve insanlığın kaderini belirleyecek yayını yap.",
                targetPosition = new Vector3(0f, 7.5f, 220f),
                requiredFrequency = 0f,
                isTerminalInteraction = true,
                xpReward = 500
            };
        }

        public StoryObjective GetActiveObjective()
        {
            if (allObjectives.Count == 0) BuildObjectiveCatalog();
            if (allObjectives.TryGetValue(currentObjectiveId, out StoryObjective obj))
            {
                return obj;
            }
            return null;
        }

        public void OnFrequencyDecoded(float freq)
        {
            StoryObjective current = GetActiveObjective();
            if (current == null || isStoryCompleted) return;

            if (current.requiredFrequency > 0f && Mathf.Abs(current.requiredFrequency - freq) < 0.05f)
            {
                ProceduralAudioManager.Instance?.PlayObjectiveSuccess();
                MobileHapticManager.TriggerMedium();

                if (current.id == "obj_1a_tune_radio")
                {
                    AdvanceToObjective("obj_1b_inspect_clinic");
                }
                else if (current.id == "obj_2a_tune_radio")
                {
                    AdvanceToObjective("obj_2b_recover_disk");
                }
                else if (current.id == "obj_3a_tune_radio")
                {
                    AdvanceToObjective("obj_3b_retrieve_key");
                }
                else if (current.id == "obj_4a_tune_radio")
                {
                    AdvanceToObjective("obj_4b_moral_choice");
                }
            }
        }

        public bool TryInteractWithStoryTerminal(string terminalId)
        {
            StoryObjective current = GetActiveObjective();
            if (current == null || !current.isTerminalInteraction || isStoryCompleted) return false;

            if (terminalId == "terminal_clinic" && current.id == "obj_1b_inspect_clinic")
            {
                CompleteCurrentObjectiveWithReward(150, "Bölüm 1: Araştırma Terminali Kurtarıldı");
                UnlockChapter(2, "obj_2a_tune_radio");
                return true;
            }

            if (terminalId == "terminal_railcar" && current.id == "obj_2b_recover_disk")
            {
                CompleteCurrentObjectiveWithReward(200, "Bölüm 2: Karantina Şifre Diski Alındı");
                UnlockChapter(3, "obj_3a_tune_radio");
                return true;
            }

            if (terminalId == "terminal_bunker" && current.id == "obj_3b_retrieve_key")
            {
                CompleteCurrentObjectiveWithReward(250, "Bölüm 3: Antiviral Sentez Anahtarı Ele Geçirildi");
                UnlockChapter(4, "obj_4a_tune_radio");
                return true;
            }

            if (terminalId == "terminal_relay_tower" && current.id == "obj_4b_moral_choice")
            {
                // Open Moral Choice Modal
                StoryChoiceUI.Instance?.OpenModal();
                return true;
            }

            return false;
        }

        private void CompleteCurrentObjectiveWithReward(int xp, string reason)
        {
            ProceduralAudioManager.Instance?.PlayObjectiveSuccess();
            MobileHapticManager.TriggerHeavy();

            if (SurvivorProgressionManager.Instance != null && xp > 0)
            {
                SurvivorProgressionManager.Instance.AddXP(xp, reason);
            }
        }

        private void UnlockChapter(int chapterNum, string firstObjectiveId)
        {
            currentChapter = chapterNum;
            currentObjectiveId = firstObjectiveId;

            StoryObjective nextObj = GetActiveObjective();
            string chapTitle = nextObj != null ? nextObj.chapterTitle : $"BÖLÜM {chapterNum}";

            OnChapterUnlocked?.Invoke(chapterNum, chapTitle);
            DispatchCurrentObjective();

            Debug.Log($"[MainStoryDirector] Chapter {chapterNum} unlocked: {chapTitle}. Next obj: {firstObjectiveId}");
        }

        public void AdvanceToObjective(string nextObjId)
        {
            currentObjectiveId = nextObjId;
            DispatchCurrentObjective();
        }

        public void DispatchCurrentObjective()
        {
            if (StarterPrologueDirector.Instance != null && !StarterPrologueDirector.Instance.IsPrologueCompleted)
            {
                // Defer strictly to StarterPrologueDirector until Chapter 0 is completed
                return;
            }

            StoryObjective obj = GetActiveObjective();
            if (obj != null)
            {
                OnObjectiveChanged?.Invoke(obj);
                QuestTrackerHUD.Instance?.RefreshObjectiveDisplay(obj);
            }
        }

        public void ExecuteEndgameMoralChoice(string choice)
        {
            if (isStoryCompleted) return;

            isStoryCompleted = true;
            endgameMoralChoice = choice;

            if (choice == "Evacuation")
            {
                // Seçenek A: Tahliye Koordinatlarını Yayınla
                CommunityMoraleManager.Instance?.ModifyMorale(40f);
                SurvivorProgressionManager.Instance?.AddXP(500, "Tahliye Koordinatları Yayını");
                DynamicWeatherManager.Instance?.TransitionToHopefulDawn();

                StartCoroutine(PlayCelebratoryRadioBroadcasts());
            }
            else if (choice == "GlobalCure")
            {
                // Seçenek B: Antiviral Formülü Küresel Ağa Gönder
                CommunityMoraleManager.Instance?.ModifyMorale(35f);
                if (SurvivorProgressionManager.Instance != null)
                {
                    SurvivorProgressionManager.Instance.HasGlobalCurePerk = true;
                    SurvivorProgressionManager.Instance.AddXP(500, "Küresel Antiviral Formülü");
                }
            }

            ProceduralAudioManager.Instance?.PlayLevelUp();
            MobileHapticManager.TriggerHeavy();

            OnStoryCompletedEvent?.Invoke(choice);
            QuestTrackerHUD.Instance?.OnStoryCompleted(choice);
            Debug.Log($"[MainStoryDirector] Main Story Completed! Final moral choice: {choice}");
        }

        private IEnumerator PlayCelebratoryRadioBroadcasts()
        {
            yield return new WaitForSeconds(1.5f);
            ProceduralAudioManager.Instance?.PlayRadioTransmissionBeep();
            Debug.Log("[TELSİZ YAYINI - Dr. Aras]: 'Bölgedeki tüm tahliye ekipleri haraket geçti... Koordinatlar ulaştı!'");

            yield return new WaitForSeconds(3.0f);
            ProceduralAudioManager.Instance?.PlayRadioTransmissionBeep();
            Debug.Log("[TELSİZ YAYINI - Kerem & Elif]: 'Kuzey koridoru açık! Başardın dostum, şafak söküyor!'");

            yield return new WaitForSeconds(3.0f);
            ProceduralAudioManager.Instance?.PlayRadioTransmissionBeep();
            Debug.Log("[TELSİZ YAYINI - Zeynep]: 'Sığınak halkı kurtuldu... Artık bir umudumuz var.'");
        }

        public void RegisterAudioLogCollected(string logId)
        {
            if (string.IsNullOrEmpty(logId)) return;

            if (!collectedAudioLogIds.Contains(logId))
            {
                collectedAudioLogIds.Add(logId);
                ProceduralAudioManager.Instance?.PlayAudioLogPlaybackCue();
                MobileHapticManager.TriggerLight();

                if (SurvivorProgressionManager.Instance != null)
                {
                    SurvivorProgressionManager.Instance.AddXP(35, $"Ses Kaydı: {logId}");
                }

                OnAudioLogCollected?.Invoke(logId);
            }
        }

        public bool IsAudioLogCollected(string logId)
        {
            return collectedAudioLogIds.Contains(logId);
        }

        public void RestoreStoryState(int chapter, string objectiveId, bool isCompleted, string moralChoice,
            List<string> audioLogs, bool hasGlobalCure)
        {
            currentChapter = Mathf.Clamp(chapter, 1, 4);
            currentObjectiveId = string.IsNullOrEmpty(objectiveId) ? "obj_1a_tune_radio" : objectiveId;
            isStoryCompleted = isCompleted;
            endgameMoralChoice = moralChoice ?? string.Empty;

            collectedAudioLogIds.Clear();
            if (audioLogs != null) collectedAudioLogIds.AddRange(audioLogs);

            if (SurvivorProgressionManager.Instance != null)
            {
                SurvivorProgressionManager.Instance.HasGlobalCurePerk = hasGlobalCure;
            }

            if (isStoryCompleted && endgameMoralChoice == "Evacuation")
            {
                DynamicWeatherManager.Instance?.TransitionToHopefulDawn();
            }

            DispatchCurrentObjective();
        }
    }
}
