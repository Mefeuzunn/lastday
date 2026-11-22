using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.AI
{
    public class NPCDailyScheduleManager : MonoBehaviour
    {
        public static NPCDailyScheduleManager Instance { get; private set; }

        [Header("Evaluation Settings")]
        [Tooltip("Evaluation interval per NPC slice (staggered for mobile 60 FPS)")]
        [SerializeField] private float evaluationInterval = 0.25f;
        [SerializeField] private bool enableSchedule = true;

        private float nextSliceTime = 0f;
        private int currentNpcIndex = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!enableSchedule) return;

            if (Time.time >= nextSliceTime)
            {
                nextSliceTime = Time.time + evaluationInterval;
                EvaluateNextNPCSlice();
            }
        }

        private void EvaluateNextNPCSlice()
        {
            var survivors = SurvivorNPC_AI.ActiveSurvivors;
            if (survivors == null || survivors.Count == 0) return;

            if (currentNpcIndex >= survivors.Count)
            {
                currentNpcIndex = 0;
            }

            SurvivorNPC_AI npc = survivors[currentNpcIndex];
            currentNpcIndex = (currentNpcIndex + 1) % survivors.Count;

            if (npc == null || npc.IsDead || npc.IsCompanion) return;
            if (npc.CurrentState == NPCState.Combat || npc.CurrentState == NPCState.Dialogue || npc.CurrentState == NPCState.FleeToSafeZone) return;

            float hour = DayNightManager.Instance != null ? DayNightManager.Instance.CurrentHour : 12f;
            NPCTaskType desiredType = GetCurrentTaskForNpc(npc.NpcId, hour);

            // If NPC is already executing a valid anchor matching the desired type, keep executing
            if (npc.CurrentTaskAnchor != null && npc.CurrentTaskAnchor.TaskType == desiredType)
            {
                return;
            }

            // Find best matching anchor
            NPCTaskAnchor bestAnchor = FindBestAnchor(npc, desiredType);
            if (bestAnchor != null)
            {
                npc.AssignTaskAnchor(bestAnchor);
            }
        }

        public NPCTaskType GetCurrentTaskForNpc(string npcId, float hour)
        {
            string id = npcId?.ToLowerInvariant() ?? "";

            if (id.Contains("aras") || id.Contains("doc"))
            {
                // Dr. Aras: Clinic rounds during daytime, Safe House medical rest at evening/night
                if (hour >= 7.5f && hour < 18.5f) return NPCTaskType.MedicalDuty;
                return NPCTaskType.RestAndSocial;
            }

            if (id.Contains("kerem"))
            {
                // Kerem: Perimeter guard during day/night shifts, workshop maintenance afternoon
                if (hour >= 6f && hour < 14f) return NPCTaskType.GuardPost;
                if (hour >= 14f && hour < 18.5f) return NPCTaskType.WorkshopRepair;
                if (hour >= 18.5f && hour < 21.5f) return NPCTaskType.RestAndSocial;
                return NPCTaskType.GuardPost; // Night guard
            }

            if (id.Contains("zeynep"))
            {
                // Zeynep: Generator / Workshop maintenance and barricading
                if (hour >= 7f && hour < 18f) return NPCTaskType.WorkshopRepair;
                return NPCTaskType.RestAndSocial;
            }

            if (id.Contains("elif"))
            {
                // Elif: Scout / Intel mapping during day, rest at night
                if (hour >= 8f && hour < 17.5f) return NPCTaskType.ScavengeRoute;
                return NPCTaskType.RestAndSocial;
            }

            // Scavengers / Civilians (Mert, etc.)
            if (hour >= 7.5f && hour < 18f)
            {
                return NPCTaskType.ScavengeRoute;
            }
            return NPCTaskType.RestAndSocial;
        }

        private NPCTaskAnchor FindBestAnchor(SurvivorNPC_AI npc, NPCTaskType desiredType)
        {
            var anchors = NPCTaskAnchor.AllAnchors;
            if (anchors == null || anchors.Count == 0) return null;

            NPCTaskAnchor best = null;
            float bestDist = float.MaxValue;
            Vector3 npcPos = npc.transform.position;

            for (int i = 0; i < anchors.Count; i++)
            {
                NPCTaskAnchor a = anchors[i];
                if (a == null || !a.gameObject.activeInHierarchy) continue;
                if (a.TaskType != desiredType) continue;
                if (!a.CanAccept(npc.NpcId)) continue;

                // Priority to specifically assigned anchors
                bool isSpecificallyAssigned = string.Equals(a.AssignedNpcId, npc.NpcId, StringComparison.OrdinalIgnoreCase);
                float dist = Vector3.Distance(npcPos, a.transform.position);

                if (isSpecificallyAssigned)
                {
                    return a; // Exact match found immediately
                }

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = a;
                }
            }

            return best;
        }

        public void ForceEvaluateSchedule()
        {
            var survivors = SurvivorNPC_AI.ActiveSurvivors;
            if (survivors == null) return;

            float hour = DayNightManager.Instance != null ? DayNightManager.Instance.CurrentHour : 12f;
            for (int i = 0; i < survivors.Count; i++)
            {
                SurvivorNPC_AI npc = survivors[i];
                if (npc == null || npc.IsDead || npc.IsCompanion) continue;
                if (npc.CurrentState == NPCState.Combat || npc.CurrentState == NPCState.Dialogue) continue;

                NPCTaskType desiredType = GetCurrentTaskForNpc(npc.NpcId, hour);
                NPCTaskAnchor anchor = FindBestAnchor(npc, desiredType);
                if (anchor != null)
                {
                    npc.AssignTaskAnchor(anchor);
                }
            }
        }
    }
}
