using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.AI;

namespace ProjectSurvival.Survival
{
    public enum HidingSpotType
    {
        Dumpster,
        Locker,
        FoliageBush
    }

    public class HidingSpot : MonoBehaviour
    {
        [Header("Spot Type")]
        [SerializeField] private HidingSpotType spotType = HidingSpotType.Dumpster;
        [SerializeField] private Transform peekPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private Transform doorOrLid;

        [Header("Zombies Investigation")]
        [SerializeField] private float loseAggroRadius = 25f;

        private bool isPlayerInside = false;
        private bool isPlayerNearby = false;
        private GameObject playerRef;
        private CharacterController playerCC;
        private PlayerController playerPC;
        private GameObject playerVisual;

        public bool IsOccupied => isPlayerInside;
        public HidingSpotType SpotType => spotType;

        public void ConfigureSpotType(HidingSpotType type)
        {
            spotType = type;
        }

        private void Awake()
        {
            if (peekPoint == null)
            {
                GameObject peek = new GameObject("PeekPoint");
                peek.transform.SetParent(transform);
                peek.transform.localPosition = new Vector3(0f, 1.4f, 0.2f);
                peek.transform.localRotation = Quaternion.identity;
                peekPoint = peek.transform;
            }

            if (exitPoint == null)
            {
                GameObject exit = new GameObject("ExitPoint");
                exit.transform.SetParent(transform);
                exit.transform.localPosition = new Vector3(0f, 0f, 1.4f);
                exit.transform.localRotation = Quaternion.identity;
                exitPoint = exit.transform;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnInteractPressed += HandleInteractPressed;
        }

        private void OnDisable()
        {
            GameEvents.OnInteractPressed -= HandleInteractPressed;
        }

        private void HandleInteractPressed()
        {
            if (isPlayerInside)
            {
                ExitHiding();
            }
            else if (isPlayerNearby)
            {
                EnterHiding();
            }
        }

        public void EnterHiding()
        {
            if (isPlayerInside) return;

            if (playerRef == null)
            {
                playerRef = GameObject.FindWithTag("Player");
            }
            if (playerRef == null) return;

            playerCC = playerRef.GetComponent<CharacterController>();
            playerPC = playerRef.GetComponent<PlayerController>();

            Transform visualTrans = playerRef.transform.Find("Visual_Model");
            if (visualTrans != null) playerVisual = visualTrans.gameObject;

            isPlayerInside = true;

            // Disable player control and visuals
            if (playerCC != null) playerCC.enabled = false;
            if (playerPC != null) playerPC.enabled = false;
            if (playerVisual != null) playerVisual.SetActive(false);

            // Move player root inside hiding spot
            playerRef.transform.position = transform.position;

            // Crack open door/lid slightly for peek view
            if (doorOrLid != null)
            {
                doorOrLid.localRotation = Quaternion.Euler(0f, 12f, 0f);
            }

            // Camera transition
            ThirdPersonCamera.Instance?.EnterHidingPeek(peekPoint);
            GameEvents.TriggerPlayerHidingStateChanged(true);
            GameEvents.TriggerInteractionPromptChanged(true, "DIŞARI ÇIK (E)");

            // Make nearby chasing zombies lose aggro and investigate around the spot
            AlertNearbyZombiesToInvestigate();

            Debug.Log($"[HidingSpot] Player hidden inside {spotType}!");
        }

        public void ExitHiding()
        {
            if (!isPlayerInside) return;

            isPlayerInside = false;

            // Restore door/lid
            if (doorOrLid != null)
            {
                doorOrLid.localRotation = Quaternion.identity;
            }

            // Position player at exit point
            Vector3 targetExitPos = exitPoint != null ? exitPoint.position : (transform.position + transform.forward * 1.5f);
            if (playerRef != null)
            {
                playerRef.transform.position = targetExitPos;
            }

            // Re-enable player control and visuals
            if (playerVisual != null) playerVisual.SetActive(true);
            if (playerCC != null) playerCC.enabled = true;
            if (playerPC != null) playerPC.enabled = true;

            ThirdPersonCamera.Instance?.ExitHidingPeek();
            GameEvents.TriggerPlayerHidingStateChanged(false);

            if (isPlayerNearby)
            {
                GameEvents.TriggerInteractionPromptChanged(true, "SAKLAN (E)");
            }
            else
            {
                GameEvents.TriggerInteractionPromptChanged(false, "");
            }

            Debug.Log($"[HidingSpot] Player exited {spotType}.");
        }

        private void AlertNearbyZombiesToInvestigate()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, loseAggroRadius);
            foreach (var hit in hits)
            {
                ZombieAI zombie = hit.GetComponentInParent<ZombieAI>();
                if (zombie != null && !zombie.IsDead && zombie.CurrentState == ZombieState.Chase)
                {
                    // Random point near hiding spot
                    Vector3 investigatePos = transform.position + UnityEngine.Random.insideUnitSphere * 3.5f;
                    investigatePos.y = transform.position.y;
                    zombie.OnNoiseDetected(investigatePos);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isPlayerInside)
            {
                isPlayerNearby = true;
                playerRef = other.gameObject;
                string prompt = spotType == HidingSpotType.Dumpster ? "KONTEYNERE SAKLAN (E)" : "DOLABA SAKLAN (E)";
                GameEvents.TriggerInteractionPromptChanged(true, prompt);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && !isPlayerInside)
            {
                isPlayerNearby = false;
                GameEvents.TriggerInteractionPromptChanged(false, "");
            }
        }
    }
}
