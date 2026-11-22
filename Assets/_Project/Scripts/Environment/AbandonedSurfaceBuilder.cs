using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectSurvival.Environment
{
    public static class AbandonedSurfaceBuilder
    {
        public static void BuildAbandonedCitySurfaces(Transform environmentRoot)
        {
            if (environmentRoot == null) return;

#if UNITY_EDITOR
            Material glassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Weathered_Glass.mat");
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Storefront_Wood.mat");
            Material crackMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Asphalt_Crack_Decal.mat");
            Material mossMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Moss_Decal.mat");
            Material debrisMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Cover_Wood.mat") 
                ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Curb.mat");

            // 1. Apply Weathered Glass to all windows and vehicle glass in environment
            ApplyWeatheredGlass(environmentRoot, glassMat);

            // 2. Create Boarded-Up Storefronts across retail & broken windows
            CreateBoardedStorefronts(environmentRoot, woodMat);

            // 3. Create Street Decals & Sidewalk Debris Clusters
            CreateSidewalkDebrisAndDecals(environmentRoot, crackMat, mossMat, debrisMat);
#endif
            Debug.Log("[AbandonedSurfaceBuilder] Successfully applied weathered glass, storefront barricades, and sidewalk decay.");
        }

#if UNITY_EDITOR
        private static void ApplyWeatheredGlass(Transform root, Material weatheredGlassMat)
        {
            if (weatheredGlassMat == null) return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null) continue;

                string objName = rend.gameObject.name.ToLower();
                bool isWindow = objName.Contains("window") || objName.Contains("glass") || objName.Contains("storefront");

                if (isWindow)
                {
                    rend.sharedMaterial = weatheredGlassMat;
                }
            }
        }

        private static void CreateBoardedStorefronts(Transform root, Material woodMat)
        {
            if (woodMat == null) return;

            GameObject barricadeRoot = new GameObject("--- STOREFRONT BARRICADES ---");
            barricadeRoot.transform.SetParent(root);

            // Locations for boarded-up storefront windows & ground doors
            Vector3[] barricadeLocations = new Vector3[]
            {
                // Supermarket storefront windows
                new Vector3(25f, 1.8f, 24f),
                new Vector3(28f, 1.8f, 24f),
                new Vector3(33f, 1.8f, 24f),
                new Vector3(37f, 1.8f, 24f),

                // Pharmacy front windows
                new Vector3(26f, 1.8f, -40f),
                new Vector3(30f, 1.8f, -40f),
                new Vector3(34f, 1.8f, -40f),

                // Clinic ground side windows
                new Vector3(-25f, 1.8f, 26f),
                new Vector3(-25f, 1.8f, 32f),
                new Vector3(-25f, 1.8f, 38f),

                // Police station ground detention windows
                new Vector3(-18f, 1.8f, 12f),
                new Vector3(-26f, 1.8f, 12f),

                // Residential houses broken ground windows
                new Vector3(-58f, 1.5f, -14f),
                new Vector3(-62f, 1.5f, 14f),
                new Vector3(-58f, 1.5f, 34f),

                // Gas station retail store front
                new Vector3(51f, 1.8f, -38f),
                new Vector3(57f, 1.8f, -38f)
            };

            for (int i = 0; i < barricadeLocations.Length; i++)
            {
                Vector3 center = barricadeLocations[i];
                GameObject setObj = new GameObject($"BoardedWindow_{i + 1}");
                setObj.transform.SetParent(barricadeRoot.transform);
                setObj.transform.position = center;

                // Create 3 diagonally crossed planks
                CreateDiagonalPlank(setObj.transform, new Vector3(0f, 0.35f, 0f), 22f, woodMat);
                CreateDiagonalPlank(setObj.transform, new Vector3(0f, -0.05f, 0f), -18f, woodMat);
                CreateDiagonalPlank(setObj.transform, new Vector3(0f, -0.42f, 0f), 12f, woodMat);

                GameObjectUtility.SetStaticEditorFlags(setObj, StaticEditorFlags.BatchingStatic);
            }
        }

        private static void CreateDiagonalPlank(Transform parent, Vector3 localPos, float zAngle, Material mat)
        {
            GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = "Plank_Board";
            plank.transform.SetParent(parent, false);
            plank.transform.localPosition = localPos;
            plank.transform.localRotation = Quaternion.Euler(0f, 0f, zAngle);
            plank.transform.localScale = new Vector3(1.75f, 0.16f, 0.045f);

            // Destroy collider so it never impedes NavMesh or player movement
            Collider col = plank.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            Renderer rend = plank.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;

            GameObjectUtility.SetStaticEditorFlags(plank, StaticEditorFlags.BatchingStatic);
        }

        private static void CreateSidewalkDebrisAndDecals(Transform root, Material crackMat, Material mossMat, Material debrisMat)
        {
            GameObject decayRoot = new GameObject("--- URBAN DECAY & DECALS ---");
            decayRoot.transform.SetParent(root);

            // 1. Asphalt Cracks along main streets & crossroads
            Vector3[] crackPositions = new Vector3[]
            {
                new Vector3(0f, 0.02f, -12f),
                new Vector3(-4f, 0.02f, 2f),
                new Vector3(3f, 0.02f, 18f),
                new Vector3(0f, 0.02f, 36f),
                new Vector3(-16f, 0.02f, 0f),
                new Vector3(22f, 0.02f, 0f),
                new Vector3(45f, 0.02f, -18f),
                new Vector3(-35f, 0.02f, 22f)
            };

            for (int i = 0; i < crackPositions.Length; i++)
            {
                CreateDecalQuad(decayRoot.transform, $"Decal_AsphaltCrack_{i + 1}", crackPositions[i], new Vector3(3.2f, 3.2f, 1f), (i * 47f) % 360f, crackMat);
            }

            // 2. Curb Moss Strips along sidewalks
            Vector3[] mossPositions = new Vector3[]
            {
                new Vector3(-3.85f, 0.22f, -16f),
                new Vector3(-3.85f, 0.22f, -4f),
                new Vector3(-3.85f, 0.22f, 14f),
                new Vector3(3.85f, 0.22f, -22f),
                new Vector3(3.85f, 0.22f, 6f),
                new Vector3(3.85f, 0.22f, 28f),
                new Vector3(-20f, 0.22f, -3.85f),
                new Vector3(24f, 0.22f, -3.85f)
            };

            for (int i = 0; i < mossPositions.Length; i++)
            {
                CreateDecalQuad(decayRoot.transform, $"Decal_CurbMoss_{i + 1}", mossPositions[i], new Vector3(1.2f, 4.5f, 1f), 0f, mossMat);
            }

            // 3. Scattered Paper & Concrete Rubble clusters (non-colliding, batching static)
            Vector3[] rubbleClusters = new Vector3[]
            {
                new Vector3(-5.2f, 0.22f, -10f),
                new Vector3(5.2f, 0.22f, 4f),
                new Vector3(-5.2f, 0.22f, 22f),
                new Vector3(21f, 0.22f, 21f),
                new Vector3(-22f, 0.22f, 20f),
                new Vector3(48f, 0.22f, -28f)
            };

            for (int i = 0; i < rubbleClusters.Length; i++)
            {
                CreateRubbleCluster(decayRoot.transform, $"Debris_Cluster_{i + 1}", rubbleClusters[i], debrisMat);
            }
        }

        private static void CreateDecalQuad(Transform parent, string name, Vector3 pos, Vector3 scale, float yRot, Material mat)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent);
            quad.transform.position = pos;
            quad.transform.rotation = Quaternion.Euler(90f, yRot, 0f);
            quad.transform.localScale = scale;

            Collider c = quad.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);

            if (mat != null)
            {
                quad.GetComponent<Renderer>().sharedMaterial = mat;
            }

            GameObjectUtility.SetStaticEditorFlags(quad, StaticEditorFlags.BatchingStatic);
        }

        private static void CreateRubbleCluster(Transform parent, string name, Vector3 pos, Material mat)
        {
            GameObject cluster = new GameObject(name);
            cluster.transform.SetParent(parent);
            cluster.transform.position = pos;

            for (int j = 0; j < 5; j++)
            {
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = $"Rubble_{j + 1}";
                piece.transform.SetParent(cluster.transform);
                Vector2 r = Random.insideUnitCircle * 0.7f;
                piece.transform.position = pos + new Vector3(r.x, 0.04f, r.y);
                piece.transform.rotation = Quaternion.Euler(Random.Range(0f, 20f), Random.Range(0f, 360f), Random.Range(0f, 20f));
                piece.transform.localScale = new Vector3(Random.Range(0.12f, 0.28f), Random.Range(0.04f, 0.09f), Random.Range(0.12f, 0.28f));

                Collider col = piece.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);

                if (mat != null) piece.GetComponent<Renderer>().sharedMaterial = mat;
                GameObjectUtility.SetStaticEditorFlags(piece, StaticEditorFlags.BatchingStatic);
            }

            GameObjectUtility.SetStaticEditorFlags(cluster, StaticEditorFlags.BatchingStatic);
        }
#endif
    }
}
