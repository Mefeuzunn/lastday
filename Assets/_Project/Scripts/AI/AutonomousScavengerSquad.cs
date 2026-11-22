using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Survival;

namespace ProjectSurvival.AI
{
    public enum SquadState
    {
        Patrolling,
        Scavenging,
        Combat,
        DistressPinned,
        RescuedRetreating
    }

    public class AutonomousScavengerSquad : MonoBehaviour
    {
        [Header("Squad Members")]
        [SerializeField] private SurvivorNPC_AI pointMan;
        [SerializeField] private SurvivorNPC_AI packCarrier;

        [Header("Patrol & Scavenge Anchors")]
        [SerializeField] private List<Transform> scavengeWaypoints = new List<Transform>();
        [SerializeField] private float scavengeDuration = 3.5f;

        [Header("Distress & Ambush Event")]
        [SerializeField] private float distressDuration = 75f;
        [SerializeField] private Transform ambushLocation;
        [SerializeField] private bool triggerDistressOnStart = false;

        [Header("Runtime State")]
        [SerializeField] private SquadState currentState = SquadState.Patrolling;

        private int currentWaypointIndex = 0;
        private float stateTimer = 0f;
        private float distressRemaining = 0f;
        private bool isAmbushTriggered = false;
        private bool isRescued = false;

        public SquadState CurrentState => currentState;
        public bool IsPinned => currentState == SquadState.DistressPinned;
        public float DistressRemaining => distressRemaining;

        private void Start()
        {
            if (scavengeWaypoints == null || scavengeWaypoints.Count == 0)
            {
                GenerateFallbackWaypoints();
            }

            if (triggerDistressOnStart)
            {
                TriggerAmbushDistress();
            }
            else
            {
                StartPatrol();
            }
        }

        private void GenerateFallbackWaypoints()
        {
            Vector3 center = transform.position;
            scavengeWaypoints = new List<Transform>();

            Vector3[] offsets = new Vector3[]
            {
                new Vector3(15f, 0f, 10f),
                new Vector3(-20f, 0f, 25f),
                new Vector3(5f, 0f, -20f),
                new Vector3(-15f, 0f, -15f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject wp = new GameObject($"ScavengeWP_{i}");
                wp.transform.position = center + offsets[i];
                scavengeWaypoints.Add(wp.transform);
            }
        }

        private void Update()
        {
            stateTimer += Time.deltaTime;

            switch (currentState)
            {
                case SquadState.Patrolling:
                    UpdatePatrolling();
                    break;
                case SquadState.Scavenging:
                    UpdateScavenging();
                    break;
                case SquadState.DistressPinned:
                    UpdateDistressPinned();
                    break;
                case SquadState.RescuedRetreating:
                    UpdateRescuedRetreating();
                    break;
            }
        }

        private void StartPatrol()
        {
            currentState = SquadState.Patrolling;
            stateTimer = 0f;
            SendSquadToNextWaypoint();
        }

        private void SendSquadToNextWaypoint()
        {
            if (scavengeWaypoints == null || scavengeWaypoints.Count == 0) return;

            Transform targetWP = scavengeWaypoints[currentWaypointIndex];
            currentWaypointIndex = (currentWaypointIndex + 1) % scavengeWaypoints.Count;

            if (pointMan != null && !pointMan.IsDead)
            {
                NavMeshAgent a = pointMan.GetComponent<NavMeshAgent>();
                if (a != null) { a.isStopped = false; a.SetDestination(targetWP.position); }
            }

            if (packCarrier != null && !packCarrier.IsDead)
            {
                NavMeshAgent a = packCarrier.GetComponent<NavMeshAgent>();
                if (a != null) { a.isStopped = false; a.SetDestination(targetWP.position + Vector3.right * 1.5f); }
            }
        }

        private void UpdatePatrolling()
        {
            if (pointMan == null || pointMan.IsDead) return;

            Transform currentWP = scavengeWaypoints[currentWaypointIndex];
            if (currentWP == null) return;

            float dist = Vector3.Distance(pointMan.transform.position, currentWP.position);
            if (dist <= 2.5f)
            {
                // Arrived at scavenge node!
                currentState = SquadState.Scavenging;
                stateTimer = 0f;

                if (packCarrier != null && !packCarrier.IsDead)
                {
                    NPCProceduralMotion pm = packCarrier.GetComponent<NPCProceduralMotion>();
                    if (pm != null) pm.SetCrouched(true);
                }
            }
        }

        private void UpdateScavenging()
        {
            if (stateTimer >= scavengeDuration)
            {
                if (packCarrier != null && !packCarrier.IsDead)
                {
                    NPCProceduralMotion pm = packCarrier.GetComponent<NPCProceduralMotion>();
                    if (pm != null) pm.SetCrouched(false);
                }

                // Check for nearby interactable prop to search
                Collider[] hits = Physics.OverlapSphere(transform.position, 6f);
                for (int i = 0; i < hits.Length; i++)
                {
                    WorldInteractableObject obj = hits[i].GetComponentInParent<WorldInteractableObject>();
                    if (obj != null && !obj.HasBeenSearched)
                    {
                        obj.ExecuteVerb("search", gameObject);
                        break;
                    }
                }

                StartPatrol();
            }
        }

        public void TriggerAmbushDistress()
        {
            if (isAmbushTriggered || isRescued) return;
            isAmbushTriggered = true;

            currentState = SquadState.DistressPinned;
            distressRemaining = distressDuration;
            stateTimer = 0f;

            Vector3 distressPos = ambushLocation != null ? ambushLocation.position : transform.position;

            // Stop squad and put pointman in combat
            if (pointMan != null && !pointMan.IsDead)
            {
                pointMan.transform.position = distressPos;
                pointMan.TransitionToState(NPCState.Combat);
            }

            if (packCarrier != null && !packCarrier.IsDead)
            {
                packCarrier.transform.position = distressPos + Vector3.back * 1.8f;
                NPCProceduralMotion pm = packCarrier.GetComponent<NPCProceduralMotion>();
                if (pm != null) pm.SetCrouched(true);
            }

            // Radio broadcast + Minimap waypoint
            GameEvents.TriggerScavengerSquadDistress(distressPos, "Kuzey Sanayi Bölgesinde keşif ekibimiz sıkıştı!", distressDuration);
            GameEvents.TriggerObjectiveToastRequested("[TELSİZ İMDAT] Keşif ekibimiz zombilerce kıstırıldı! (Kalan: 75sn)", 6f);
            GameEvents.TriggerWaypointChanged(distressPos, "İMDAT: Keşif Ekibi");

            Debug.Log($"[AutonomousScavengerSquad] Distress call emitted at {distressPos}!");
        }

        private void UpdateDistressPinned()
        {
            distressRemaining -= Time.deltaTime;

            // Check if player is near and zombies are cleared
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
                if (distToPlayer <= 16f)
                {
                    // Check if nearby zombies are dead/cleared
                    Collider[] nearby = Physics.OverlapSphere(transform.position, 18f);
                    int liveZombies = 0;
                    for (int i = 0; i < nearby.Length; i++)
                    {
                        ZombieAI z = nearby[i].GetComponentInParent<ZombieAI>();
                        if (z != null && !z.IsDead) liveZombies++;
                    }

                    if (liveZombies == 0)
                    {
                        CompleteRescue();
                        return;
                    }
                }
            }

            if (distressRemaining <= 0f)
            {
                // Timeout failure
                currentState = SquadState.RescuedRetreating;
                CommunityOutpostManager.Instance?.ModifyMorale(-10f);
                GameEvents.TriggerObjectiveToastRequested("[BAŞARISIZ] Keşif ekibiyle telsiz irtibatı koptu...", 5f);
            }
        }

        private void CompleteRescue()
        {
            if (isRescued) return;
            isRescued = true;
            currentState = SquadState.RescuedRetreating;

            // Morale boost & rewards
            CommunityOutpostManager.Instance?.ModifyMorale(15f);
            CommunityOutpostManager.Instance?.AddResident("Kurtarılan Keşifçi");

            InventorySystem.Instance?.AddItemById("ammo_9mm", 12);
            InventorySystem.Instance?.AddItemById("item_medkit", 1);

            ProceduralAudioManager.Instance?.PlayObjectiveSuccess();
            GameEvents.TriggerObjectiveToastRequested("[BAŞARILI] Keşif ekibi kurtarıldı! (+15 Moral, +12 Mermi, +1 Medkit)", 6f);
            GameEvents.TriggerScavengerRescued("Murat & Can", "Ekip sığınağa sağ salim ulaştı.");

            Debug.Log("[AutonomousScavengerSquad] Rescue completed successfully!");
        }

        private void UpdateRescuedRetreating()
        {
            // Head towards Safe House
            GameObject safeHouse = GameObject.Find("Zone_SafeHouse") ?? GameObject.FindGameObjectWithTag("SafeHouse");
            Vector3 target = safeHouse != null ? safeHouse.transform.position : Vector3.zero;

            if (pointMan != null && !pointMan.IsDead)
            {
                NavMeshAgent a = pointMan.GetComponent<NavMeshAgent>();
                if (a != null && a.enabled) { a.isStopped = false; a.speed = 3.5f; a.SetDestination(target); }
            }

            if (packCarrier != null && !packCarrier.IsDead)
            {
                NavMeshAgent a = packCarrier.GetComponent<NavMeshAgent>();
                if (a != null && a.enabled) { a.isStopped = false; a.speed = 3.5f; a.SetDestination(target); }
            }
        }

        public void ConfigureSquad(SurvivorNPC_AI point, SurvivorNPC_AI carrier)
        {
            pointMan = point;
            packCarrier = carrier;
        }
    }
}
