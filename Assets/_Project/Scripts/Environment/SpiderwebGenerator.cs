using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectSurvival.Environment
{
    public static class SpiderwebGenerator
    {
        public static void GenerateCobwebsAndDustMotes(Transform environmentRoot)
        {
            if (environmentRoot == null) return;

#if UNITY_EDITOR
            Material webMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Cobweb.mat");
            Texture2D softCircleTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/tex_particle_soft_circle.png");

            GameObject webParent = new GameObject("--- INTERIOR COBWEBS ---");
            webParent.transform.SetParent(environmentRoot);

            // Anchor locations for tensile corner cobwebs
            // (Center, Size, Euler Rotation)
            var cobwebSpawns = new (Vector3 pos, Vector2 size, Vector3 rot)[]
            {
                // Zone_Clinic Ceiling corners & Exam stretchers
                (new Vector3(-35.2f, 2.85f, 38.2f), new Vector2(1.2f, 1.2f), new Vector3(0f, 0f, 0f)),
                (new Vector3(-25.2f, 2.85f, 38.2f), new Vector2(1.1f, 1.1f), new Vector3(0f, 90f, 0f)),
                (new Vector3(-35.2f, 2.85f, 24.2f), new Vector2(1.3f, 1.3f), new Vector3(0f, -90f, 0f)),
                (new Vector3(-33.0f, 1.1f, 34.5f), new Vector2(0.8f, 0.8f), new Vector3(45f, 0f, 0f)), // Stretcher corner

                // Police Station Ceiling corners & Cell bars
                (new Vector3(-27.2f, 2.85f, 11.2f), new Vector2(1.3f, 1.3f), new Vector3(0f, 0f, 0f)),
                (new Vector3(-17.2f, 2.85f, 11.2f), new Vector2(1.2f, 1.2f), new Vector3(0f, 90f, 0f)),
                (new Vector3(-23.8f, 1.8f, 5.2f), new Vector2(0.9f, 0.9f), new Vector3(15f, 45f, 0f)), // Cell door

                // SafeHouse Ceiling corners & Storage
                (new Vector3(-42.2f, 2.85f, -24.2f), new Vector2(1.3f, 1.3f), new Vector3(0f, 0f, 0f)),
                (new Vector3(-35.2f, 2.85f, -34.2f), new Vector2(1.2f, 1.2f), new Vector3(0f, 180f, 0f)),
                (new Vector3(-37.5f, 1.4f, -31.5f), new Vector2(0.75f, 0.75f), new Vector3(30f, 60f, 0f)), // Stash locker

                // Supermarket & Pharmacy Ceiling corners
                (new Vector3(24.2f, 3.2f, 38.2f), new Vector2(1.4f, 1.4f), new Vector3(0f, -90f, 0f)),
                (new Vector3(37.2f, 3.2f, 23.2f), new Vector2(1.3f, 1.3f), new Vector3(0f, 90f, 0f)),
                (new Vector3(34.2f, 2.85f, -44.2f), new Vector2(1.1f, 1.1f), new Vector3(0f, 0f, 0f)),

                // Residential Houses
                (new Vector3(-61.5f, 2.6f, -11.5f), new Vector2(1.2f, 1.2f), new Vector3(0f, 0f, 0f)),
                (new Vector3(-63.5f, 2.6f, 16.5f), new Vector2(1.1f, 1.1f), new Vector3(0f, 90f, 0f)),

                // Exterior Alley Dumpsters
                (new Vector3(-12.8f, 1.3f, 24.2f), new Vector2(0.9f, 0.9f), new Vector3(0f, 45f, 0f)),
                (new Vector3(25.4f, 1.3f, 7.2f), new Vector2(0.85f, 0.85f), new Vector3(0f, -45f, 0f))
            };

            for (int i = 0; i < cobwebSpawns.Length; i++)
            {
                var s = cobwebSpawns[i];
                CreateCornerCobweb(webParent.transform, $"Cobweb_{i + 1}", s.pos, s.size, s.rot, webMat);
            }

            GameObjectUtility.SetStaticEditorFlags(webParent, StaticEditorFlags.BatchingStatic);

            // 2. Interior Volumetric Light-Beam Dust Motes
            GameObject dustParent = new GameObject("--- INTERIOR DUST MOTES ---");
            dustParent.transform.SetParent(environmentRoot);

            Vector3[] dustRooms = new Vector3[]
            {
                new Vector3(-30f, 1.6f, 32f),  // Clinic Interior
                new Vector3(-22f, 1.6f, 2f),   // Police Station Interior
                new Vector3(-38f, 1.6f, -29f), // SafeHouse Interior
                new Vector3(30f, 1.8f, 30f)    // Supermarket Interior
            };

            for (int i = 0; i < dustRooms.Length; i++)
            {
                CreateDustMoteEmitter(dustParent.transform, $"DustMotes_Room_{i + 1}", dustRooms[i], softCircleTex);
            }
#endif
            Debug.Log("[SpiderwebGenerator] Tensile cobwebs and interior dust motes created successfully.");
        }

#if UNITY_EDITOR
        public static void GenerateCobwebPatches(Transform parent, string containerName, (Vector3 pos, Vector2 size, Vector3 rot)[] targets)
        {
            if (parent == null || targets == null || targets.Length == 0) return;

            Material webMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Interior_Cobweb.mat");
            if (webMat == null) return;

            GameObject webParent = new GameObject(containerName);
            webParent.transform.SetParent(parent, false);

            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                CreateCornerCobweb(webParent.transform, $"Cobweb_{i + 1}", target.pos, target.size, target.rot, webMat);
            }

            GameObjectUtility.SetStaticEditorFlags(webParent, StaticEditorFlags.BatchingStatic);
        }
#endif

#if UNITY_EDITOR
        private static void CreateCornerCobweb(Transform parent, string name, Vector3 pos, Vector2 size, Vector3 rot, Material mat)
        {
            GameObject webObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            webObj.name = name;
            webObj.transform.SetParent(parent);
            webObj.transform.position = pos;
            webObj.transform.rotation = Quaternion.Euler(rot);
            webObj.transform.localScale = new Vector3(size.x, size.y, 1f);

            // Destroy collider so it never impedes player or NavMesh
            Collider c = webObj.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);

            Renderer r = webObj.GetComponent<Renderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            GameObjectUtility.SetStaticEditorFlags(webObj, StaticEditorFlags.BatchingStatic);
        }

        private static void CreateDustMoteEmitter(Transform parent, string name, Vector3 pos, Texture2D circleTex)
        {
            GameObject emitterObj = new GameObject(name);
            emitterObj.transform.SetParent(parent);
            emitterObj.transform.position = pos;

            ParticleSystem ps = emitterObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.maxParticles = 35;
            main.startLifetime = 6.0f;
            main.startSpeed = 0.06f;
            main.startSize = 0.08f;
            main.startColor = new Color(1.0f, 0.98f, 0.88f, 0.42f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.loop = true;

            var emission = ps.emission;
            emission.rateOverTime = 5.5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(5.0f, 2.4f, 5.0f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 0.4f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.45f, 0.35f), new GradientAlphaKey(0.45f, 0.65f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            ParticleSystemRenderer psr = emitterObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                Shader partShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
                Material partMat = new Material(partShader);
                partMat.name = "Mat_DustMotes_Unlit";
                if (circleTex != null)
                {
                    partMat.mainTexture = circleTex;
                    if (partMat.HasProperty("_BaseMap")) partMat.SetTexture("_BaseMap", circleTex);
                }
                partMat.SetColor("_BaseColor", new Color(1f, 0.95f, 0.85f, 0.45f));
                partMat.SetFloat("_Surface", 1); // Transparent
                partMat.SetInt("_Blend", 0); // Alpha
                partMat.SetInt("_ZWrite", 0);

                psr.sharedMaterial = partMat;
            }
        }
#endif
    }
}
