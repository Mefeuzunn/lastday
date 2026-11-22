using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectSurvival.Core
{
    public class MobilePerformanceOptimizer : MonoBehaviour
    {
        public static MobilePerformanceOptimizer Instance { get; private set; }

        [Header("Frame Rate & Power Settings")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool disableVSync = true;
        [SerializeField] private bool preventScreenSleep = true;

        [Header("Distance-Based Culling")]
        [SerializeField] private float lightCullDistance = 42f;
        [SerializeField] private float particleCullDistance = 42f;
        [SerializeField] private float propCullDistance = 38f;
        [SerializeField] private float checkInterval = 0.35f;

        private Transform playerTransform;
        private readonly List<Light> managedLights = new List<Light>();
        private readonly List<ParticleSystem> managedParticles = new List<ParticleSystem>();
        private readonly List<Renderer> managedSmallProps = new List<Renderer>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ApplyMobileQualitySettings();
        }

        private void Start()
        {
            FindPlayer();
            EnsureSRPBatcher();
            CollectManagedObjects();
            ConfigureCameraCulling();
            StartCoroutine(DistanceCullingRoutine());
        }

        private void ApplyMobileQualitySettings()
        {
            Application.targetFrameRate = targetFrameRate;
            if (disableVSync)
            {
                QualitySettings.vSyncCount = 0;
            }
            if (preventScreenSleep)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            Debug.Log($"[MobilePerformanceOptimizer] Applied mobile settings: targetFrameRate={targetFrameRate}, vSync={QualitySettings.vSyncCount}");
        }

        private void EnsureSRPBatcher()
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
        }

        private void FindPlayer()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        public void CollectManagedObjects()
        {
            // 1. Lights
            managedLights.Clear();
            Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < allLights.Length; i++)
            {
                Light l = allLights[i];
                if (l != null && l.type != LightType.Directional)
                {
                    managedLights.Add(l);
                }
            }

            // 2. Interior & Ambient Particles
            managedParticles.Clear();
            ParticleSystem[] allParticles = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            for (int i = 0; i < allParticles.Length; i++)
            {
                ParticleSystem ps = allParticles[i];
                if (ps != null && ps.gameObject.name.Contains("DustMotes"))
                {
                    managedParticles.Add(ps);
                }
            }

            // 3. Small Environmental Props (Cobwebs, sidewalk debris)
            managedSmallProps.Clear();
            Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer r = allRenderers[i];
                if (r == null) continue;
                string n = r.gameObject.name.ToLower();
                if (n.Contains("cobweb") || n.Contains("rubble") || n.Contains("debris") || n.Contains("decal_curbmoss"))
                {
                    managedSmallProps.Add(r);
                }
            }
        }

        private void ConfigureCameraCulling()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float[] distances = new float[32];
            distances[0] = 160f; // Default layer max distance
            cam.layerCullDistances = distances;
        }

        private IEnumerator DistanceCullingRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(checkInterval);
            float sqrLightDist = lightCullDistance * lightCullDistance;
            float sqrParticleDist = particleCullDistance * particleCullDistance;
            float sqrPropDist = propCullDistance * propCullDistance;

            while (true)
            {
                yield return wait;

                if (playerTransform == null)
                {
                    FindPlayer();
                    if (playerTransform == null) continue;
                }

                Vector3 playerPos = playerTransform.position;

                // 1. Cull Lights
                for (int i = 0; i < managedLights.Count; i++)
                {
                    Light l = managedLights[i];
                    if (l == null) continue;

                    float sqrDist = (l.transform.position - playerPos).sqrMagnitude;
                    bool active = sqrDist <= sqrLightDist;
                    if (l.enabled != active) l.enabled = active;
                }

                // 2. Cull Particles
                for (int i = 0; i < managedParticles.Count; i++)
                {
                    ParticleSystem ps = managedParticles[i];
                    if (ps == null) continue;

                    float sqrDist = (ps.transform.position - playerPos).sqrMagnitude;
                    bool active = sqrDist <= sqrParticleDist;
                    var emission = ps.emission;
                    if (emission.enabled != active) emission.enabled = active;
                }

                // 3. Cull Small Props / Cobwebs / Debris
                for (int i = 0; i < managedSmallProps.Count; i++)
                {
                    Renderer r = managedSmallProps[i];
                    if (r == null) continue;

                    float sqrDist = (r.transform.position - playerPos).sqrMagnitude;
                    bool active = sqrDist <= sqrPropDist;
                    if (r.enabled != active) r.enabled = active;
                }
            }
        }
    }
}
