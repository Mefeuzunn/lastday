using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.UI;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Phase 3 (Step 3B): Northwest Lake Pier Fishing Spot.
    /// Positioned at the weathered wooden dock on the lake surface (Sector_-1_1_NorthWest).
    /// Interacting casts a visible 3D fishing line & URP bobber onto the water surface,
    /// emits a subtle 6m splash sound, and opens FishingMiniGameUI.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LakeFishingSpotInteractable : WorldInteractableObject
    {
        [Header("Fishing Spot Settings")]
        [SerializeField] private Transform bobberSpawnPoint;
        [SerializeField] private GameObject activeBobberVisual;
        [SerializeField] private LineRenderer fishingLine;

        private NoiseEmitter noiseEmitter;
        private bool isFishingActive = false;

        public bool IsFishingActive => isFishingActive;

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "BALIKÇI İSKELESİ";
            objectSubtitle = "Tatlı Su Avı";
            noiseEmitter = GetComponent<NoiseEmitter>();
            EnsureFishingVisuals();
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            return new List<InteractionVerb>
            {
                new InteractionVerb(
                    "fish_lake",
                    "BALIK TUT (OLTA AT)",
                    0f, // Instant Tap
                    null,
                    null,
                    0f
                )
            };
        }

        public override bool CanInteract(string verbId)
        {
            return verbId == "fish_lake" && !isFishingActive;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "fish_lake")
            {
                StartFishingSession();
            }
        }

        public void StartFishingSession()
        {
            isFishingActive = true;

            // 1. Cast Bobber & Line
            Vector3 castTarget = transform.position + transform.forward * 4.5f + Vector3.down * 0.4f;
            if (bobberSpawnPoint != null) castTarget = bobberSpawnPoint.position;

            if (activeBobberVisual != null)
            {
                activeBobberVisual.transform.position = castTarget;
                activeBobberVisual.SetActive(true);
            }

            if (fishingLine != null)
            {
                fishingLine.SetPosition(0, transform.position + Vector3.up * 1.2f);
                fishingLine.SetPosition(1, castTarget);
                fishingLine.enabled = true;
            }

            // 2. Audio & Noise (Subtle 6m ripple splash)
            ProceduralAudioManager.Instance?.PlayWaterSplash(castTarget, 0.75f);
            if (noiseEmitter != null) noiseEmitter.EmitCustomImpulse(6.0f);

            // 3. Open UI
            if (FishingMiniGameUI.Instance != null)
            {
                FishingMiniGameUI.Instance.StartMiniGame(this, castTarget);
            }
            else
            {
                Debug.LogWarning("[LakeFishingSpotInteractable] FishingMiniGameUI.Instance not found!");
            }
        }

        public void EndFishingSession()
        {
            isFishingActive = false;
            if (activeBobberVisual != null) activeBobberVisual.SetActive(false);
            if (fishingLine != null) fishingLine.enabled = false;
        }

        public void TriggerBobberPlunge(Vector3 waterPos)
        {
            ProceduralAudioManager.Instance?.PlayWaterSplash(waterPos, 0.95f);
            if (activeBobberVisual != null)
            {
                StartCoroutine(BobberPlungeRoutine());
            }
        }

        private IEnumerator BobberPlungeRoutine()
        {
            if (activeBobberVisual == null) yield break;
            Vector3 orig = activeBobberVisual.transform.position;
            activeBobberVisual.transform.position = orig + Vector3.down * 0.28f;
            yield return new WaitForSeconds(0.4f);
            if (activeBobberVisual != null && isFishingActive)
            {
                activeBobberVisual.transform.position = orig;
            }
        }

        private void EnsureFishingVisuals()
        {
            if (activeBobberVisual == null)
            {
                Transform found = transform.Find("Visual_Fishing_Bobber");
                if (found != null) activeBobberVisual = found.gameObject;
            }

            if (activeBobberVisual == null)
            {
                // Create red & white sphere bobber
                GameObject bobber = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bobber.name = "Visual_Fishing_Bobber";
                bobber.transform.SetParent(transform, false);
                bobber.transform.localPosition = new Vector3(0f, -0.4f, 4.5f);
                bobber.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
                Collider col = bobber.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);

                Material bobberMat = GetOrCreateMaterial("Mat_Fishing_Bobber", new Color(0.95f, 0.18f, 0.15f), 0.6f);
                bobber.GetComponent<Renderer>().sharedMaterial = bobberMat;

                activeBobberVisual = bobber;
                activeBobberVisual.SetActive(false);
            }

            if (fishingLine == null)
            {
                fishingLine = GetComponent<LineRenderer>();
                if (fishingLine == null)
                {
                    fishingLine = gameObject.AddComponent<LineRenderer>();
                    fishingLine.useWorldSpace = true;
                    fishingLine.startWidth = 0.012f;
                    fishingLine.endWidth = 0.012f;
                    fishingLine.positionCount = 2;

                    Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    lineMat.color = new Color(0.9f, 0.95f, 1.0f, 0.75f);
                    fishingLine.sharedMaterial = lineMat;
                    fishingLine.enabled = false;
                }
            }
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness = 0.5f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader)
            {
                name = name,
                enableInstancing = true
            };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }
    }
}
