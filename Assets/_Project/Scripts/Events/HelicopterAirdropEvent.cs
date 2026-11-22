using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using ProjectSurvival.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.Player;

namespace ProjectSurvival.Events
{
    /// <summary>
    /// Phase 2 (Step 2B): Helicopter Supply Airdrop Sequence Controller.
    /// Manages the full aerial flyover, parachute descent from 38m,
    /// landing touchdown, flare ignition, acoustic noise pull (35m),
    /// and opportunistic raider scavenger convergence.
    /// </summary>
    public class HelicopterAirdropEvent : MonoBehaviour
    {
        [Header("Descent Parameters")]
        [SerializeField] private float descentDuration = 4.5f;
        [SerializeField] private float startAltitude = 38.0f;
        [SerializeField] private float acousticAlertRadius = 35.0f;

        private SupplyAirdropCrate activeCrate;
        private Vector3 targetGroundPos;
        private Coroutine descentCoroutine;
        private bool isEventActive = false;

        public bool IsEventActive => isEventActive;
        public SupplyAirdropCrate ActiveCrate => activeCrate;
        public Vector3 TargetGroundPos => targetGroundPos;

        /// <summary>
        /// Initiates the supply airdrop sequence at the designated ground landing coordinate.
        /// </summary>
        public void LaunchAirdrop(Vector3 groundPos, string airdropId = "Airdrop_Alpha")
        {
            if (isEventActive) return;
            isEventActive = true;

            // Ensure ground position sits on NavMesh
            targetGroundPos = groundPos;
            if (NavMesh.SamplePosition(groundPos, out NavMeshHit navHit, 6.0f, NavMesh.AllAreas))
            {
                targetGroundPos = navHit.position;
            }

            // 1. Play 3D positional helicopter flyover audio
            Vector3 flyoverPos = targetGroundPos + Vector3.up * 45f;
            if (ProceduralAudioManager.Instance != null)
            {
                ProceduralAudioManager.Instance.PlayHelicopterFlyover(flyoverPos);
            }

            // 2. Display tactical radio banner / toast
            GameEvents.TriggerObjectiveToastRequested("HAVA İKMALİ: Bölgeye askeri yardım sandığı bırakıldı — Yeşil işaret fişeğini takip et", 4.5f);

            // 3. Create or instantiate SupplyAirdropCrate
            activeCrate = CreateAirdropCrateObject(targetGroundPos + Vector3.up * startAltitude, airdropId);

            // 4. Start descent coroutine
            if (descentCoroutine != null) StopCoroutine(descentCoroutine);
            descentCoroutine = StartCoroutine(AnimateDescent());
        }

        private IEnumerator AnimateDescent()
        {
            Vector3 startPos = targetGroundPos + Vector3.up * startAltitude;
            float elapsed = 0f;

            while (elapsed < descentDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / descentDuration);

                // Smooth linear-to-soft touchdown curve
                float y = Mathf.Lerp(startPos.y, targetGroundPos.y, Mathf.Sin(t * Mathf.PI * 0.5f));
                float sway = Mathf.Sin(elapsed * 2.8f) * 0.45f * (1f - t);

                if (activeCrate != null)
                {
                    activeCrate.transform.position = new Vector3(targetGroundPos.x + sway, y, targetGroundPos.z);
                    activeCrate.transform.rotation = Quaternion.Euler(Mathf.Sin(elapsed * 2.5f) * 4f, elapsed * 15f, 0f);
                }

                yield return null;
            }

            // Touchdown complete
            if (activeCrate != null)
            {
                activeCrate.transform.position = targetGroundPos;
                activeCrate.transform.rotation = Quaternion.identity;
                activeCrate.OnTouchdown();
            }

            // 5. Contested Three-Way Skirmish Convergence
            TriggerContestedConvergence(targetGroundPos);

            isEventActive = false;
            descentCoroutine = null;
        }

        private void TriggerContestedConvergence(Vector3 landingPos)
        {
            // A. Acoustic pull on nearby zombies within 35m
            GameEvents.TriggerNoiseEmitted(landingPos, acousticAlertRadius);
            HordeAlertSystem.Instance?.BroadcastAcousticLure(landingPos, acousticAlertRadius);

            // B. Direct 2 nearest living HumanRaiders to scavenge the drop zone
            var allRaiders = HumanRaiderAI.ActiveRaiders;
            int alertedRaiders = 0;

            if (allRaiders != null && allRaiders.Count > 0)
            {
                // Sort by distance to landing position
                List<HumanRaiderAI> sorted = new List<HumanRaiderAI>(allRaiders);
                sorted.Sort((a, b) =>
                {
                    float distA = Vector3.Distance(a.transform.position, landingPos);
                    float distB = Vector3.Distance(b.transform.position, landingPos);
                    return distA.CompareTo(distB);
                });

                for (int i = 0; i < sorted.Count && alertedRaiders < 2; i++)
                {
                    var r = sorted[i];
                    if (r != null && !r.IsDead)
                    {
                        r.AlertRaider(null, "Hava ikmali düştü! Koşun, ele geçirin!");
                        NavMeshAgent agent = r.GetComponent<NavMeshAgent>();
                        if (agent != null && agent.isOnNavMesh)
                        {
                            agent.speed = 4.6f;
                            agent.SetDestination(landingPos);
                        }
                        alertedRaiders++;
                    }
                }
            }

            Debug.Log($"[HelicopterAirdropEvent] Airdrop touched down at {landingPos}! Attracted zombies within {acousticAlertRadius}m and {alertedRaiders} raider scavengers.");
        }

        private SupplyAirdropCrate CreateAirdropCrateObject(Vector3 spawnPos, string id)
        {
            GameObject crateObj = new GameObject($"SupplyAirdrop_{id}");
            crateObj.transform.position = spawnPos;

            // BoxCollider
            BoxCollider col = crateObj.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 0.9f, 1.2f);
            col.center = new Vector3(0f, 0.45f, 0f);

            // Material Caches (URP Lit olive military)
            Material oliveMat = GetOrCreateMaterial("Mat_Military_OliveDrab", new Color(0.22f, 0.28f, 0.20f));
            Material steelTrimMat = GetOrCreateMaterial("Mat_Military_SteelTrim", new Color(0.12f, 0.14f, 0.15f));
            Material canopyMat = GetOrCreateMaterial("Mat_Parachute_Canopy", new Color(0.92f, 0.88f, 0.82f));

            oliveMat.enableInstancing = true;
            steelTrimMat.enableInstancing = true;
            canopyMat.enableInstancing = true;

            // Crate Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Crate_Body";
            body.transform.SetParent(crateObj.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            body.transform.localScale = new Vector3(1.15f, 0.85f, 1.15f);
            DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = oliveMat;

            // Metal corner reinforcements
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "Crate_Steel_Trim";
            trim.transform.SetParent(crateObj.transform, false);
            trim.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            trim.transform.localScale = new Vector3(1.18f, 0.25f, 1.18f);
            DestroyImmediate(trim.GetComponent<Collider>());
            trim.GetComponent<Renderer>().sharedMaterial = steelTrimMat;

            // Parachute Canopy
            GameObject canopyObj = new GameObject("Parachute_Canopy");
            canopyObj.transform.SetParent(crateObj.transform, false);
            canopyObj.transform.localPosition = new Vector3(0f, 2.4f, 0f);

            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "Canopy_Dome";
            dome.transform.SetParent(canopyObj.transform, false);
            dome.transform.localScale = new Vector3(2.4f, 0.85f, 2.4f);
            DestroyImmediate(dome.GetComponent<Collider>());
            dome.GetComponent<Renderer>().sharedMaterial = canopyMat;

            // Signal Flare Particle Emitter (URP Unlit smoke pillar)
            GameObject flareObj = new GameObject("Flare_Smoke_Emitter");
            flareObj.transform.SetParent(crateObj.transform, false);
            flareObj.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            ParticleSystem ps = flareObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 3.5f;
            main.startSpeed = 4.2f;
            main.startSize = 0.65f;
            main.startColor = new Color(0.18f, 0.85f, 0.44f, 0.85f); // Emerald green
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            var emission = ps.emission;
            emission.rateOverTime = 18f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6.0f;
            shape.radius = 0.12f;

            var psRenderer = flareObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material particleMat = GetOrCreateMaterial("Mat_Airdrop_Flare_Green", new Color(0.18f, 0.85f, 0.44f, 0.85f));
            particleMat.enableInstancing = true;
            psRenderer.sharedMaterial = particleMat;

            // SupplyAirdropCrate Component
            SupplyAirdropCrate crateComp = crateObj.AddComponent<SupplyAirdropCrate>();
            crateComp.SetAirdropId(id);
            crateComp.InitializeComponents(canopyObj, ps);

            return crateComp;
        }

        private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        private static Material GetOrCreateMaterial(string name, Color color)
        {
            if (materialCache.TryGetValue(name, out Material mat) && mat != null) return mat;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = name;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.enableInstancing = true;
            materialCache[name] = mat;
            return mat;
        }
    }
}
