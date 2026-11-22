using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.AI;

namespace ProjectSurvival.Environment
{
    /// <summary>
    /// Shallow Floodwater Movement Modifier.
    /// Covers flooded streets in Sector_-1_-1_SouthWest:
    /// - Reduces Player and Zombie movement speed by 18% (speed multiplier = 0.82f).
    /// - Plays periodic procedural water-splash footstep audio while walking through water.
    /// - Traversing elevated wooden boardwalks keeps characters out of the water trigger, granting dry full speed.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FloodwaterMovementModifier : MonoBehaviour
    {
        [Header("Modifier Configuration")]
        [Range(0.05f, 0.50f)]
        [SerializeField] private float speedReduction = 0.18f; // 18% speed cut
        [SerializeField] private float splashInterval = 0.42f;

        private Collider triggerCollider;
        private readonly HashSet<ZombieBrain> submergedZombies = new HashSet<ZombieBrain>();
        private PlayerController activePlayer;
        private float nextSplashTime = 0f;

        public float SpeedMultiplier => Mathf.Clamp01(1.0f - speedReduction);

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Player detection
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                PlayerController pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
                if (pc != null)
                {
                    activePlayer = pc;
                    activePlayer.EnvironmentalSpeedMultiplier = SpeedMultiplier;
                    ProceduralAudioManager.Instance?.PlayWaterSplashFootstep(activePlayer.transform.position, activePlayer.IsSprinting);
                }
                return;
            }

            // Zombie detection
            ZombieBrain zb = other.GetComponent<ZombieBrain>() ?? other.GetComponentInParent<ZombieBrain>();
            if (zb != null && !submergedZombies.Contains(zb))
            {
                submergedZombies.Add(zb);
                zb.ApplyEnvironmentalSpeedModifier(SpeedMultiplier);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (activePlayer != null && other.transform.root == activePlayer.transform.root)
            {
                if (activePlayer.IsMoving && Time.time >= nextSplashTime)
                {
                    nextSplashTime = Time.time + (activePlayer.IsSprinting ? splashInterval * 0.7f : splashInterval);
                    ProceduralAudioManager.Instance?.PlayWaterSplashFootstep(activePlayer.transform.position, activePlayer.IsSprinting);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Player exit
            if (activePlayer != null && (other.CompareTag("Player") || other.transform.root == activePlayer.transform.root))
            {
                activePlayer.EnvironmentalSpeedMultiplier = 1.0f;
                activePlayer = null;
                return;
            }

            // Zombie exit
            ZombieBrain zb = other.GetComponent<ZombieBrain>() ?? other.GetComponentInParent<ZombieBrain>();
            if (zb != null && submergedZombies.Contains(zb))
            {
                submergedZombies.Remove(zb);
                zb.ApplyEnvironmentalSpeedModifier(1.0f);
            }
        }

        private void OnDisable()
        {
            // Clean up any remaining modifications when sector unloads or object disabled
            if (activePlayer != null)
            {
                activePlayer.EnvironmentalSpeedMultiplier = 1.0f;
                activePlayer = null;
            }

            foreach (var zb in submergedZombies)
            {
                if (zb != null)
                {
                    zb.ApplyEnvironmentalSpeedModifier(1.0f);
                }
            }
            submergedZombies.Clear();
        }
    }
}
