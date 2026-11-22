using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Player
{
    public class NoiseEmitter : MonoBehaviour
    {
        [Header("Noise Radii")]
        [SerializeField] private float crouchRadius = 2f;
        [SerializeField] private float walkRadius = 5.5f;
        [SerializeField] private float sprintRadius = 11f;

        [Header("Visualization Settings (Debug Disabled)")]
        [SerializeField] private bool showDebugVisuals = false;
        [SerializeField] private int circleSegments = 48;
        [SerializeField] private float visualHeightOffset = 0.05f;
        [SerializeField] private Color visualColor = new Color(0.35f, 0.8f, 1f, 0.22f);
        [SerializeField] private float lineWidth = 0.04f;

        private LineRenderer lineRenderer;
        private float currentRadius;
        private float targetRadius;

        public float CurrentRadius => currentRadius;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                if (!showDebugVisuals)
                {
                    lineRenderer.enabled = false;
                }
                else
                {
                    ConfigureLineRenderer();
                }
            }
        }

        private void ConfigureLineRenderer()
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.positionCount = circleSegments;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            // Simple unlit material if none exists
            if (lineRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    Material m = new Material(shader);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", visualColor);
                    lineRenderer.material = m;
                }
            }

            lineRenderer.startColor = visualColor;
            lineRenderer.endColor = visualColor;
        }

        private void Update()
        {
            currentRadius = Mathf.Lerp(currentRadius, targetRadius, Time.deltaTime * 10f);
            if (showDebugVisuals && lineRenderer != null)
            {
                DrawCircle(currentRadius);
            }
        }

        public void EmitNoise(NoiseState state)
        {
            float noiseMult = 1.0f;
            if (ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null)
            {
                noiseMult *= ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetFootstepNoiseMultiplier();
            }
            if (ProjectSurvival.Environment.DynamicWeatherManager.Instance != null)
            {
                noiseMult *= ProjectSurvival.Environment.DynamicWeatherManager.Instance.GetWeatherAcousticMultiplier();
            }

            switch (state)
            {
                case NoiseState.Silent:
                    targetRadius = 0f;
                    break;
                case NoiseState.Crouch:
                    targetRadius = crouchRadius * noiseMult;
                    break;
                case NoiseState.Walk:
                    targetRadius = walkRadius * noiseMult;
                    break;
                case NoiseState.Sprint:
                    targetRadius = sprintRadius;
                    break;
            }

            if (targetRadius > 0.1f)
            {
                GameEvents.TriggerNoiseEmitted(transform.position, targetRadius);
            }
        }

        public void EmitCustomImpulse(float radius)
        {
            targetRadius = radius;
            currentRadius = radius;
            GameEvents.TriggerNoiseEmitted(transform.position, radius);
        }

        private void DrawCircle(float radius)
        {
            if (radius < 0.2f)
            {
                lineRenderer.enabled = false;
                return;
            }

            lineRenderer.enabled = true;
            float alpha = Mathf.Clamp01(radius / walkRadius) * 0.22f;
            Color c = new Color(visualColor.r, visualColor.g, visualColor.b, alpha);
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;

            float angleStep = 360f / circleSegments;
            Vector3 center = transform.position;

            for (int i = 0; i < circleSegments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;
                lineRenderer.SetPosition(i, new Vector3(x, center.y + visualHeightOffset, z));
            }
        }
    }

    public enum NoiseState
    {
        Silent,
        Crouch,
        Walk,
        Sprint
    }
}
