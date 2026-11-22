using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.AI;
using ProjectSurvival.Core;

namespace ProjectSurvival.UI
{
    public class StealthIndicatorUI : MonoBehaviour
    {
        public static StealthIndicatorUI Instance { get; private set; }

        [Header("Arc Display")]
        [SerializeField] private Image primaryThreatArc;
        [SerializeField] private Image secondaryThreatArc;
        [SerializeField] private float maxDetectionDistance = 24f;

        [Header("Colors")]
        [SerializeField] private Color suspiciousColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color alertedColor = new Color(0.95f, 0.2f, 0.2f, 0.85f);

        private Transform playerTransform;
        private Camera mainCamera;
        private readonly List<ZombieAI> cachedZombies = new List<ZombieAI>();
        private float scanTimer = 0f;

        private float primaryAlpha = 0f;
        private float secondaryAlpha = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FindPlayerAndCamera();
            RefreshZombieCache();

            if (primaryThreatArc != null) primaryThreatArc.color = new Color(1, 1, 1, 0);
            if (secondaryThreatArc != null) secondaryThreatArc.color = new Color(1, 1, 1, 0);
        }

        private void FindPlayerAndCamera()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void RefreshZombieCache()
        {
            cachedZombies.Clear();
            var allZombies = Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
            for (int i = 0; i < allZombies.Length; i++)
            {
                if (allZombies[i] != null && !allZombies[i].IsDead)
                {
                    cachedZombies.Add(allZombies[i]);
                }
            }
        }

        private void Update()
        {
            FindPlayerAndCamera();

            scanTimer += Time.deltaTime;
            if (scanTimer > 1.2f)
            {
                scanTimer = 0f;
                RefreshZombieCache();
            }

            if (playerTransform == null || mainCamera == null)
            {
                FadeOutAll();
                return;
            }

            // Find top two highest threat zombies
            ZombieAI topThreat1 = null;
            ZombieAI topThreat2 = null;
            float maxScore1 = 0f;
            float maxScore2 = 0f;

            for (int i = 0; i < cachedZombies.Count; i++)
            {
                var z = cachedZombies[i];
                if (z == null || z.IsDead || !z.gameObject.activeInHierarchy) continue;

                float dist = Vector3.Distance(playerTransform.position, z.transform.position);
                if (dist > maxDetectionDistance) continue;

                float threatMultiplier = 0f;
                if (z.CurrentState == ZombieState.Chase || z.CurrentState == ZombieState.Attack)
                {
                    threatMultiplier = 2.0f;
                }
                else if (z.CurrentState == ZombieState.Investigate)
                {
                    threatMultiplier = 1.0f;
                }
                else if (dist < 8f) // Ambient close proximity
                {
                    threatMultiplier = 0.35f;
                }

                if (threatMultiplier <= 0f) continue;

                float score = (maxDetectionDistance - dist) * threatMultiplier;
                if (score > maxScore1)
                {
                    maxScore2 = maxScore1;
                    topThreat2 = topThreat1;
                    maxScore1 = score;
                    topThreat1 = z;
                }
                else if (score > maxScore2)
                {
                    maxScore2 = score;
                    topThreat2 = z;
                }
            }

            UpdateArc(primaryThreatArc, topThreat1, ref primaryAlpha);
            UpdateArc(secondaryThreatArc, topThreat2, ref secondaryAlpha);
        }

        private void UpdateArc(Image arc, ZombieAI zombie, ref float currentAlpha)
        {
            if (arc == null) return;

            if (zombie == null)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, 0f, Time.deltaTime * 3.5f);
                Color c = arc.color;
                c.a = currentAlpha;
                arc.color = c;
                return;
            }

            Vector3 toZombie = zombie.transform.position - playerTransform.position;
            toZombie.y = 0f;
            if (toZombie.sqrMagnitude < 0.01f) return;
            toZombie.Normalize();

            Vector3 camFwd = mainCamera.transform.forward;
            camFwd.y = 0f;
            camFwd.Normalize();

            float angle = Vector3.SignedAngle(camFwd, toZombie, Vector3.up);
            arc.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);

            bool isAlert = (zombie.CurrentState == ZombieState.Chase || zombie.CurrentState == ZombieState.Attack);
            Color targetBaseColor = isAlert ? alertedColor : suspiciousColor;

            currentAlpha = Mathf.MoveTowards(currentAlpha, targetBaseColor.a, Time.deltaTime * 4f);
            Color finalCol = targetBaseColor;
            finalCol.a = currentAlpha;
            arc.color = finalCol;
        }

        private void FadeOutAll()
        {
            primaryAlpha = Mathf.MoveTowards(primaryAlpha, 0f, Time.deltaTime * 4f);
            secondaryAlpha = Mathf.MoveTowards(secondaryAlpha, 0f, Time.deltaTime * 4f);

            if (primaryThreatArc != null)
            {
                Color c = primaryThreatArc.color;
                c.a = primaryAlpha;
                primaryThreatArc.color = c;
            }
            if (secondaryThreatArc != null)
            {
                Color c = secondaryThreatArc.color;
                c.a = secondaryAlpha;
                secondaryThreatArc.color = c;
            }
        }
    }
}
