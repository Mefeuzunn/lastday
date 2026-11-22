using System.Collections;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Survival
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(LineRenderer))]
    public class SoundBomb : MonoBehaviour
    {
        [Header("Sound Bomb Settings")]
        [SerializeField] private float noiseRadius = 22f;
        [SerializeField] private float activeDuration = 5.0f;
        [SerializeField] private float beepInterval = 0.8f;

        private Rigidbody rb;
        private LineRenderer lineRenderer;
        private bool hasLanded = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
        }

        private void ConfigureLineRenderer()
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.positionCount = 32;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;

            Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Color c = new Color(1f, 0.8f, 0.1f, 0.7f);
            if (s != null)
            {
                Material m = new Material(s);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                lineRenderer.material = m;
            }
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
            lineRenderer.enabled = false;
        }

        public void Initialize(Vector3 throwForce)
        {
            if (rb != null)
            {
                rb.AddForce(throwForce, ForceMode.Impulse);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasLanded) return;
            hasLanded = true;

            StartCoroutine(BeepRoutine());
        }

        private IEnumerator BeepRoutine()
        {
            float elapsed = 0f;
            lineRenderer.enabled = true;

            while (elapsed < activeDuration)
            {
                // Emit noise pulse
                GameEvents.TriggerNoiseEmitted(transform.position, noiseRadius);
                Debug.Log($"[SoundBomb] *BEEP* Sound bomb emitting {noiseRadius}m noise wave!");

                // Quick visual pulse
                yield return StartCoroutine(PulseVisual(0.35f));

                yield return new WaitForSeconds(beepInterval - 0.35f);
                elapsed += beepInterval;
            }

            lineRenderer.enabled = false;
            Destroy(gameObject, 0.2f);
        }

        private IEnumerator PulseVisual(float duration)
        {
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float r = Mathf.Lerp(0.5f, noiseRadius, timer / duration);
                DrawCircle(r);
                yield return null;
            }
        }

        private void DrawCircle(float radius)
        {
            int segments = 32;
            float step = 360f / segments;
            Vector3 center = transform.position;

            for (int i = 0; i < segments; i++)
            {
                float ang = i * step * Mathf.Deg2Rad;
                float x = center.x + Mathf.Cos(ang) * radius;
                float z = center.z + Mathf.Sin(ang) * radius;
                lineRenderer.SetPosition(i, new Vector3(x, center.y + 0.1f, z));
            }
        }
    }
}
