using UnityEngine;

namespace ProjectSurvival.Core
{
    public class FlickeringLight : MonoBehaviour
    {
        [SerializeField] private float baseIntensity = 3.4f;
        [SerializeField] private float flickerAmount = 0.65f;
        [SerializeField] private float flickerSpeed = 18f;

        private Light targetLight;
        private float noiseOffset;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            noiseOffset = Random.Range(0f, 100f);
            if (targetLight != null) baseIntensity = targetLight.intensity;
        }

        private void Update()
        {
            if (targetLight == null) return;
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);
            targetLight.intensity = Mathf.Max(0.2f, baseIntensity + (noise - 0.5f) * flickerAmount);
        }
    }
}
