using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.AI
{
    /// <summary>
    /// Coordinates city-wide horde alerts, Screamer telegraph wind-ups, 45m sonic shockwaves,
    /// and chain reaction alerts across nearby packs, roamers, and scavengers.
    /// </summary>
    public class HordeAlertSystem : MonoBehaviour
    {
        public static HordeAlertSystem Instance { get; private set; }

        [Header("Screamer Shriek Parameters")]
        [SerializeField] private float shriekRadius = 45f;
        [SerializeField] private float telegraphDuration = 1.0f;
        [SerializeField] private Color shockwaveColor = new Color(0.85f, 0.15f, 0.95f, 0.65f);

        public float TelegraphDuration => telegraphDuration;

        private LineRenderer activeShockwaveRenderer;
        private Coroutine shockwaveCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupShockwaveRenderer();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void SetupShockwaveRenderer()
        {
            GameObject shockObj = new GameObject("HordeSonicShockwave");
            shockObj.transform.SetParent(transform, false);
            activeShockwaveRenderer = shockObj.AddComponent<LineRenderer>();
            activeShockwaveRenderer.useWorldSpace = true;
            activeShockwaveRenderer.loop = true;
            activeShockwaveRenderer.positionCount = 48;
            activeShockwaveRenderer.startWidth = 0.25f;
            activeShockwaveRenderer.endWidth = 0.25f;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (unlitShader != null)
            {
                Material mat = new Material(unlitShader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", shockwaveColor);
                activeShockwaveRenderer.material = mat;
            }

            activeShockwaveRenderer.startColor = shockwaveColor;
            activeShockwaveRenderer.endColor = shockwaveColor;
            activeShockwaveRenderer.enabled = false;
        }

        /// <summary>
        /// Executes a full Screamer sonic shriek: plays piercing shriek audio, triggers camera shake,
        /// launches an expanding 45m URP shockwave ring, and summons all zombies within 45m to the player's position.
        /// </summary>
        public void ExecuteScreamerShriek(Vector3 screamerPos, Transform target)
        {
            // 1. Procedural Audio
            ProceduralAudioManager.Instance?.PlayScreamerShriek();

            // 2. Tactile Camera Shake
            GameEvents.TriggerCameraShakeRequested(0.4f, 0.25f);

            // 3. Visual URP Shockwave Ring
            if (shockwaveCoroutine != null) StopCoroutine(shockwaveCoroutine);
            shockwaveCoroutine = StartCoroutine(AnimateShockwaveRing(screamerPos, shriekRadius, 0.65f));

            // 4. Minimap Radar Notification
            GameEvents.TriggerHordeAlertTriggered(screamerPos, shriekRadius);

            // 5. Chain Reaction: Alert all active zombies within 45m
            Vector3 targetPos = target != null ? target.position : screamerPos;
            var allZombies = ZombieAI.ActiveZombies;
            int alertedCount = 0;

            for (int i = 0; i < allZombies.Count; i++)
            {
                ZombieAI z = allZombies[i];
                if (z == null || z.IsDead || !z.gameObject.activeInHierarchy) continue;

                float dist = Vector3.Distance(screamerPos, z.transform.position);
                if (dist <= shriekRadius)
                {
                    z.HearNoise(targetPos);
                    if (target != null)
                    {
                        z.SetTarget(target);
                        z.SetState(ZombieState.Chase);
                    }
                    alertedCount++;
                }
            }

            Debug.Log($"[HordeAlertSystem] Screamer sonic shriek triggered at {screamerPos}! Alerted {alertedCount} zombies within {shriekRadius}m!");
        }

        /// <summary>
        /// Broadcasts an omnidirectional acoustic lure (e.g. from an active Sound Bomb)
        /// that pulls surrounding zombies to the target location.
        /// </summary>
        public void BroadcastAcousticLure(Vector3 lurePos, float radius)
        {
            var allZombies = ZombieAI.ActiveZombies;
            if (allZombies == null) return;

            for (int i = 0; i < allZombies.Count; i++)
            {
                ZombieAI z = allZombies[i];
                if (z == null || z.IsDead || !z.gameObject.activeInHierarchy) continue;

                float dist = Vector3.Distance(lurePos, z.transform.position);
                if (dist <= radius)
                {
                    z.HearNoise(lurePos);
                }
            }
        }

        private IEnumerator AnimateShockwaveRing(Vector3 center, float maxRadius, float duration)
        {
            if (activeShockwaveRenderer == null) yield break;

            activeShockwaveRenderer.enabled = true;
            float elapsed = 0f;
            int segments = 48;
            float step = 360f / segments;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
                float radius = Mathf.Lerp(1.5f, maxRadius, easeOut);

                Color fadeColor = shockwaveColor;
                fadeColor.a = Mathf.Lerp(shockwaveColor.a, 0f, t);
                activeShockwaveRenderer.startColor = fadeColor;
                activeShockwaveRenderer.endColor = fadeColor;

                for (int i = 0; i < segments; i++)
                {
                    float rad = i * step * Mathf.Deg2Rad;
                    float x = center.x + Mathf.Cos(rad) * radius;
                    float z = center.z + Mathf.Sin(rad) * radius;
                    activeShockwaveRenderer.SetPosition(i, new Vector3(x, center.y + 0.2f, z));
                }

                yield return null;
            }

            activeShockwaveRenderer.enabled = false;
            shockwaveCoroutine = null;
        }
    }
}
