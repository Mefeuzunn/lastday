using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.UI;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Phase 2 Micro-Step 2B: Camp Stove & Kitchen Range Cooking Station.
    /// Enables boiling water purification, herbal immune tea brewing, and hot stew cooking.
    /// Equipped with animated URP burner flame particles, rising steam, and simmering pot SFX.
    /// </summary>
    public class CookingStoveInteractable : WorldInteractableObject
    {
        [Header("Stove Visual FX")]
        [SerializeField] private ParticleSystem burnerFlameParticles;
        [SerializeField] private ParticleSystem risingSteamParticles;
        [SerializeField] private Light burnerPointLight;

        public static readonly List<CookingStoveInteractable> AllStoves = new List<CookingStoveInteractable>();

        public static bool IsPlayerNearLitStove(Vector3 pos, float radius = 4.5f)
        {
            for (int i = 0; i < AllStoves.Count; i++)
            {
                if (AllStoves[i] != null && Vector3.Distance(pos, AllStoves[i].transform.position) <= radius)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnEnable()
        {
            if (!AllStoves.Contains(this)) AllStoves.Add(this);
        }

        private void OnDisable()
        {
            AllStoves.Remove(this);
        }

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "KAMP OCAĞI & MUTFAK TEZGÂHI";
            objectSubtitle = "YEMEK PİŞİRME VE SU ARITMA";

            EnsureStoveVisualModel();
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            return new List<InteractionVerb>
            {
                new InteractionVerb(
                    "cook_stove",
                    "PİŞİR & ARIT (KAMP OCAĞI)",
                    0f, // Instant Tap
                    null,
                    null,
                    0f
                )
            };
        }

        public override bool CanInteract(string verbId)
        {
            return verbId == "cook_stove";
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "cook_stove")
            {
                if (SurvivalAlchemyUI.Instance != null)
                {
                    SurvivalAlchemyUI.Instance.OpenCookingStation();
                    ProceduralAudioManager.Instance?.PlayBoilingSteam();
                    MobileHapticManager.Instance?.TriggerLight();
                }
                else
                {
                    Debug.LogWarning("[CookingStoveInteractable] SurvivalAlchemyUI.Instance not found!");
                }
            }
        }

        public void SetCookingActive(bool active)
        {
            if (burnerFlameParticles != null)
            {
                if (active && !burnerFlameParticles.isPlaying) burnerFlameParticles.Play();
                else if (!active && burnerFlameParticles.isPlaying) burnerFlameParticles.Stop();
            }

            if (risingSteamParticles != null)
            {
                if (active && !risingSteamParticles.isPlaying) risingSteamParticles.Play();
                else if (!active && risingSteamParticles.isPlaying) risingSteamParticles.Stop();
            }

            if (burnerPointLight != null)
            {
                burnerPointLight.enabled = active;
            }
        }

        private void EnsureStoveVisualModel()
        {
            Transform existing = transform.Find("Visual_Model");
            if (existing != null)
            {
                cachedRenderers = GetComponentsInChildren<Renderer>(true);
                return;
            }

            GameObject visualRoot = new GameObject("Visual_Model");
            visualRoot.transform.SetParent(transform, false);

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material stoveMetalMat = new Material(urpShader)
            {
                name = "Mat_Proc_StoveMetal",
                color = new Color(0.18f, 0.20f, 0.22f)
            };

            Material potMat = new Material(urpShader)
            {
                name = "Mat_Proc_StockPot",
                color = new Color(0.40f, 0.42f, 0.45f)
            };

            // 1. Stove Counter Base Box (1.1m x 0.85m x 0.75m)
            GameObject baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "Stove_Base";
            baseBox.transform.SetParent(visualRoot.transform, false);
            baseBox.transform.localPosition = new Vector3(0f, 0.425f, 0f);
            baseBox.transform.localScale = new Vector3(1.1f, 0.85f, 0.75f);
            baseBox.GetComponent<Renderer>().sharedMaterial = stoveMetalMat;
            DestroyImmediate(baseBox.GetComponent<Collider>());

            // 2. Left Burner Ring
            GameObject burnerL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            burnerL.name = "Burner_Left";
            burnerL.transform.SetParent(visualRoot.transform, false);
            burnerL.transform.localPosition = new Vector3(-0.28f, 0.86f, 0f);
            burnerL.transform.localScale = new Vector3(0.32f, 0.02f, 0.32f);
            burnerL.GetComponent<Renderer>().sharedMaterial = stoveMetalMat;
            DestroyImmediate(burnerL.GetComponent<Collider>());

            // 3. Right Burner Ring
            GameObject burnerR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            burnerR.name = "Burner_Right";
            burnerR.transform.SetParent(visualRoot.transform, false);
            burnerR.transform.localPosition = new Vector3(0.28f, 0.86f, 0f);
            burnerR.transform.localScale = new Vector3(0.28f, 0.02f, 0.28f);
            burnerR.GetComponent<Renderer>().sharedMaterial = stoveMetalMat;
            DestroyImmediate(burnerR.GetComponent<Collider>());

            // 4. Simmering Stockpot on Left Burner
            GameObject pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.name = "StockPot";
            pot.transform.SetParent(visualRoot.transform, false);
            pot.transform.localPosition = new Vector3(-0.28f, 1.02f, 0f);
            pot.transform.localScale = new Vector3(0.30f, 0.16f, 0.30f);
            pot.GetComponent<Renderer>().sharedMaterial = potMat;
            DestroyImmediate(pot.GetComponent<Collider>());

            // 5. Pot Handles
            GameObject handleL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handleL.name = "Handle_L";
            handleL.transform.SetParent(pot.transform, false);
            handleL.transform.localPosition = new Vector3(-0.62f, 0.1f, 0f);
            handleL.transform.localScale = new Vector3(0.25f, 0.12f, 0.15f);
            handleL.GetComponent<Renderer>().sharedMaterial = stoveMetalMat;
            DestroyImmediate(handleL.GetComponent<Collider>());

            // 6. Burner Flame Emitter
            GameObject flameObj = new GameObject("Burner_Flame_Particles");
            flameObj.transform.SetParent(visualRoot.transform, false);
            flameObj.transform.localPosition = new Vector3(-0.28f, 0.87f, 0f);
            burnerFlameParticles = flameObj.AddComponent<ParticleSystem>();

            var fMain = burnerFlameParticles.main;
            fMain.startSize = 0.06f;
            fMain.startLifetime = 0.35f;
            fMain.startSpeed = 0.22f;
            fMain.startColor = new Color(1.0f, 0.62f, 0.12f, 0.85f); // Amber burner flame
            fMain.maxParticles = 24;

            var fEmission = burnerFlameParticles.emission;
            fEmission.rateOverTime = 16f;

            var fShape = burnerFlameParticles.shape;
            fShape.shapeType = ParticleSystemShapeType.Circle;
            fShape.radius = 0.10f;

            ParticleSystemRenderer fRend = flameObj.GetComponent<ParticleSystemRenderer>();
            Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            fRend.sharedMaterial = new Material(pShader);

            // 7. Rising Steam Emitter
            GameObject steamObj = new GameObject("Rising_Steam_Particles");
            steamObj.transform.SetParent(visualRoot.transform, false);
            steamObj.transform.localPosition = new Vector3(-0.28f, 1.20f, 0f);
            risingSteamParticles = steamObj.AddComponent<ParticleSystem>();

            var sMain = risingSteamParticles.main;
            sMain.startSize = 0.18f;
            sMain.startLifetime = 1.25f;
            sMain.startSpeed = 0.35f;
            sMain.startColor = new Color(0.92f, 0.94f, 0.98f, 0.28f); // Soft rising vapor
            sMain.maxParticles = 30;

            var sEmission = risingSteamParticles.emission;
            sEmission.rateOverTime = 10f;

            var sShape = risingSteamParticles.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 12f;
            sShape.radius = 0.08f;

            ParticleSystemRenderer sRend = steamObj.GetComponent<ParticleSystemRenderer>();
            sRend.sharedMaterial = new Material(pShader);

            // 8. Proximity Amber Point Light
            GameObject lightObj = new GameObject("Stove_Warmth_Light");
            lightObj.transform.SetParent(visualRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(-0.28f, 1.05f, 0f);
            burnerPointLight = lightObj.AddComponent<Light>();
            burnerPointLight.type = LightType.Point;
            burnerPointLight.color = new Color(1.0f, 0.72f, 0.32f);
            burnerPointLight.range = 3.2f;
            burnerPointLight.intensity = 1.3f;
            burnerPointLight.shadows = LightShadows.None;

            // Ensure main collider covers the station
            if (mainCollider is BoxCollider box)
            {
                box.center = new Vector3(0f, 0.55f, 0f);
                box.size = new Vector3(1.3f, 1.1f, 0.95f);
            }

            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }
}
