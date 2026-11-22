using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.UI;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Phase 2 Micro-Step 2B: Survival Chemistry & Medical Lab Station.
    /// Enables synthesizing splints, painkillers, antiviral vaccines, and adrenaline combat injectors.
    /// Equipped with glassware beakers, Bunsen burner, cool cyan illumination, and chemistry glassware SFX.
    /// </summary>
    public class ChemistryLabInteractable : WorldInteractableObject
    {
        [Header("Lab Visual FX")]
        [SerializeField] private ParticleSystem bunsenFlameParticles;
        [SerializeField] private ParticleSystem chemicalVaporParticles;
        [SerializeField] private Light labBenchLight;

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "KİMYA VE TIBBİ LABORATUVAR";
            objectSubtitle = "İLAÇ VE UYARICI SENTEZİ";

            EnsureLabVisualModel();
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            return new List<InteractionVerb>
            {
                new InteractionVerb(
                    "open_chemlab",
                    "LABORATUVARI AÇ (KİMYA & İLAÇ)",
                    0f, // Instant Tap
                    null,
                    null,
                    0f
                )
            };
        }

        public override bool CanInteract(string verbId)
        {
            return verbId == "open_chemlab";
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "open_chemlab")
            {
                if (SurvivalAlchemyUI.Instance != null)
                {
                    SurvivalAlchemyUI.Instance.OpenChemistryStation();
                    ProceduralAudioManager.Instance?.PlayChemistryGlassware();
                    MobileHapticManager.Instance?.TriggerLight();
                }
                else
                {
                    Debug.LogWarning("[ChemistryLabInteractable] SurvivalAlchemyUI.Instance not found!");
                }
            }
        }

        public void SetBrewingActive(bool active)
        {
            if (bunsenFlameParticles != null)
            {
                if (active && !bunsenFlameParticles.isPlaying) bunsenFlameParticles.Play();
                else if (!active && bunsenFlameParticles.isPlaying) bunsenFlameParticles.Stop();
            }

            if (chemicalVaporParticles != null)
            {
                if (active && !chemicalVaporParticles.isPlaying) chemicalVaporParticles.Play();
                else if (!active && chemicalVaporParticles.isPlaying) chemicalVaporParticles.Stop();
            }
        }

        private void EnsureLabVisualModel()
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

            Material benchMat = new Material(urpShader)
            {
                name = "Mat_Proc_LabBench",
                color = new Color(0.88f, 0.90f, 0.92f)
            };

            Material metalTrayMat = new Material(urpShader)
            {
                name = "Mat_Proc_MetalTray",
                color = new Color(0.35f, 0.38f, 0.42f)
            };

            Material glassFlaskMat = new Material(urpShader)
            {
                name = "Mat_Proc_GlassFlask",
                color = new Color(0.35f, 0.75f, 0.88f, 0.65f)
            };

            // 1. Lab Table Base (1.4m x 0.90m x 0.80m)
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Lab_Table";
            table.transform.SetParent(visualRoot.transform, false);
            table.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            table.transform.localScale = new Vector3(1.4f, 0.90f, 0.80f);
            table.GetComponent<Renderer>().sharedMaterial = benchMat;
            DestroyImmediate(table.GetComponent<Collider>());

            // 2. Stainless Steel Instrument Tray
            GameObject tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tray.name = "Sterile_Tray";
            tray.transform.SetParent(visualRoot.transform, false);
            tray.transform.localPosition = new Vector3(0.35f, 0.92f, 0f);
            tray.transform.localScale = new Vector3(0.50f, 0.04f, 0.35f);
            tray.GetComponent<Renderer>().sharedMaterial = metalTrayMat;
            DestroyImmediate(tray.GetComponent<Collider>());

            // 3. Chemical Beaker / Flask
            GameObject flask = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flask.name = "Glass_Flask";
            flask.transform.SetParent(visualRoot.transform, false);
            flask.transform.localPosition = new Vector3(-0.35f, 1.05f, 0f);
            flask.transform.localScale = new Vector3(0.24f, 0.22f, 0.24f);
            flask.GetComponent<Renderer>().sharedMaterial = glassFlaskMat;
            DestroyImmediate(flask.GetComponent<Collider>());

            // 4. Test Tube Rack
            GameObject rack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rack.name = "TestTube_Rack";
            rack.transform.SetParent(visualRoot.transform, false);
            rack.transform.localPosition = new Vector3(0f, 0.98f, 0.20f);
            rack.transform.localScale = new Vector3(0.35f, 0.14f, 0.12f);
            rack.GetComponent<Renderer>().sharedMaterial = metalTrayMat;
            DestroyImmediate(rack.GetComponent<Collider>());

            // 5. Bunsen Burner Emitter (Sharp blue flame)
            GameObject flameObj = new GameObject("Bunsen_Flame_Particles");
            flameObj.transform.SetParent(visualRoot.transform, false);
            flameObj.transform.localPosition = new Vector3(-0.35f, 0.93f, 0f);
            bunsenFlameParticles = flameObj.AddComponent<ParticleSystem>();

            var fMain = bunsenFlameParticles.main;
            fMain.startSize = 0.045f;
            fMain.startLifetime = 0.28f;
            fMain.startSpeed = 0.35f;
            fMain.startColor = new Color(0.15f, 0.55f, 1.0f, 0.85f); // High-temp cobalt flame
            fMain.maxParticles = 20;

            var fEmission = bunsenFlameParticles.emission;
            fEmission.rateOverTime = 14f;

            var fShape = bunsenFlameParticles.shape;
            fShape.shapeType = ParticleSystemShapeType.Cone;
            fShape.angle = 8f;
            fShape.radius = 0.04f;

            ParticleSystemRenderer fRend = flameObj.GetComponent<ParticleSystemRenderer>();
            Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            fRend.sharedMaterial = new Material(pShader);

            // 6. Chemical Vapor Emitter
            GameObject vaporObj = new GameObject("Chemical_Vapor_Particles");
            vaporObj.transform.SetParent(visualRoot.transform, false);
            vaporObj.transform.localPosition = new Vector3(-0.35f, 1.25f, 0f);
            chemicalVaporParticles = vaporObj.AddComponent<ParticleSystem>();

            var vMain = chemicalVaporParticles.main;
            vMain.startSize = 0.16f;
            vMain.startLifetime = 1.10f;
            vMain.startSpeed = 0.30f;
            vMain.startColor = new Color(0.30f, 0.85f, 0.90f, 0.22f); // Faint cyan-tinted vapor
            vMain.maxParticles = 24;

            var vEmission = chemicalVaporParticles.emission;
            vEmission.rateOverTime = 8f;

            var vShape = chemicalVaporParticles.shape;
            vShape.shapeType = ParticleSystemShapeType.Cone;
            vShape.angle = 14f;
            vShape.radius = 0.06f;

            ParticleSystemRenderer vRend = vaporObj.GetComponent<ParticleSystemRenderer>();
            vRend.sharedMaterial = new Material(pShader);

            // 7. Cool Cyan Medical Point Light
            GameObject lightObj = new GameObject("Lab_Cyan_Light");
            lightObj.transform.SetParent(visualRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            labBenchLight = lightObj.AddComponent<Light>();
            labBenchLight.type = LightType.Point;
            labBenchLight.color = new Color(0.65f, 0.92f, 0.95f);
            labBenchLight.range = 3.6f;
            labBenchLight.intensity = 1.4f;
            labBenchLight.shadows = LightShadows.None;

            // Ensure main collider covers the station
            if (mainCollider is BoxCollider box)
            {
                box.center = new Vector3(0f, 0.55f, 0f);
                box.size = new Vector3(1.6f, 1.1f, 1.0f);
            }

            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }
}
