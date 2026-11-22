using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Narrative;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;

namespace ProjectSurvival.Missions
{
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Active Mission")]
        [SerializeField] private DynamicObjective activeObjective;

        private float surviveTimer = 0f;
        private string currentZoneInside = string.Empty;

        public DynamicObjective ActiveObjective => activeObjective;
        public bool HasActiveMission => activeObjective != null && !activeObjective.isCompleted && !activeObjective.isFailed;

        public Vector3? ActiveObjectivePosition
        {
            get
            {
                if (!HasActiveMission || activeObjective?.data == null) return null;
                string targetZone = activeObjective.data.targetZoneId;
                if (!string.IsNullOrEmpty(targetZone))
                {
                    var zones = Object.FindObjectsByType<MissionZone>(FindObjectsSortMode.None);
                    foreach (var z in zones)
                    {
                        if (z != null && z.ZoneId == targetZone) return z.transform.position;
                    }
                }
                return null;
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
            GameEvents.OnInventoryUpdated += CheckCollectItemObjective;
            GameEvents.OnPlayerDied += HandlePlayerDiedDuringMission;
            GameEvents.OnCivilianRescued += HandleCivilianRescued;
        }

        private void OnDisable()
        {
            GameEvents.OnInventoryUpdated -= CheckCollectItemObjective;
            GameEvents.OnPlayerDied -= HandlePlayerDiedDuringMission;
            GameEvents.OnCivilianRescued -= HandleCivilianRescued;
        }

        private void HandleCivilianRescued()
        {
            if (HasActiveMission && activeObjective.data.missionType == "RescueNPC")
            {
                CompleteMission();
            }
        }

        private void Update()
        {
            if (!HasActiveMission) return;

            // Handle SurviveTimer mission
            if (activeObjective.data.missionType == "SurviveTimer")
            {
                surviveTimer += Time.deltaTime;
                activeObjective.currentProgress = surviveTimer;

                GameEvents.TriggerMissionProgressUpdated(
                    activeObjective.data.missionId,
                    activeObjective.data.description,
                    surviveTimer,
                    activeObjective.data.duration
                );

                if (surviveTimer >= activeObjective.data.duration)
                {
                    CompleteMission();
                }
            }
        }

        public void StartMission(MissionTriggerData data)
        {
            activeObjective = new DynamicObjective(data);
            surviveTimer = 0f;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.MissionActive);
            }

            Debug.Log($"[MissionManager] Started Mission: {data.title} ({data.missionType})");

            GameEvents.TriggerMissionProgressUpdated(
                data.missionId,
                data.description,
                0f,
                activeObjective.RequiredAmount
            );

            // Immediate check in case player already has the items
            if (data.missionType == "CollectItem")
            {
                CheckCollectItemObjective();
            }
            else if (data.missionType == "RescueNPC")
            {
                CivilianFollower.Instance?.StartFollowing();
            }
        }

        public void OnPlayerEnteredZone(string zoneId)
        {
            currentZoneInside = zoneId;

            if (!HasActiveMission) return;

            if (activeObjective.data.missionType == "ReachZone" || activeObjective.data.missionType == "RescueNPC")
            {
                if (activeObjective.data.targetZoneId == zoneId)
                {
                    CompleteMission();
                }
            }
        }

        public void OnPlayerExitedZone(string zoneId)
        {
            if (currentZoneInside == zoneId)
            {
                currentZoneInside = string.Empty;
            }
        }

        private void CheckCollectItemObjective()
        {
            if (!HasActiveMission) return;
            if (activeObjective.data.missionType != "CollectItem") return;

            if (InventorySystem.Instance != null)
            {
                int count = 0;
                foreach (var slot in InventorySystem.Instance.Slots)
                {
                    if (slot.item != null && slot.item.itemId == activeObjective.data.targetItemId)
                    {
                        count += slot.quantity;
                    }
                }

                activeObjective.currentProgress = count;
                GameEvents.TriggerMissionProgressUpdated(
                    activeObjective.data.missionId,
                    activeObjective.data.description,
                    count,
                    activeObjective.data.targetCount
                );

                if (count >= activeObjective.data.targetCount)
                {
                    CompleteMission();
                }
            }
        }

        public void CompleteMission()
        {
            if (!HasActiveMission) return;

            activeObjective.isCompleted = true;
            string missionId = activeObjective.data.missionId;
            string successNodeId = activeObjective.data.onSuccessNodeId;

            Debug.Log($"[MissionManager] Mission COMPLETED: {activeObjective.data.title}!");
            GameEvents.TriggerMissionCompleted(missionId, successNodeId);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Gameplay);
            }

            // Automatically resume story if a follow-up story node exists
            if (!string.IsNullOrEmpty(successNodeId) && StoryManager.Instance != null)
            {
                StoryManager.Instance.ResumeStoryAtNode(successNodeId);
            }
        }

        public void FailMission()
        {
            if (!HasActiveMission) return;

            activeObjective.isFailed = true;
            string missionId = activeObjective.data.missionId;
            string failNodeId = activeObjective.data.onFailNodeId;

            Debug.Log($"[MissionManager] Mission FAILED: {activeObjective.data.title}!");
            GameEvents.TriggerMissionFailed(missionId, failNodeId);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Gameplay);
            }

            if (!string.IsNullOrEmpty(failNodeId) && StoryManager.Instance != null)
            {
                StoryManager.Instance.ResumeStoryAtNode(failNodeId);
            }
        }

        private void HandlePlayerDiedDuringMission()
        {
            if (HasActiveMission)
            {
                FailMission();
            }
        }
    }
}
