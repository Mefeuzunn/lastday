using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectSurvival.Environment
{
    public static class OvergrownIvyGenerator
    {
        public static void GenerateUrbanIvy(Transform environmentRoot)
        {
            if (environmentRoot == null) return;

#if UNITY_EDITOR
            Material ivyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Overgrown_Ivy.mat");
            if (ivyMat == null)
            {
                Debug.LogWarning("[OvergrownIvyGenerator] Mat_Overgrown_Ivy not found. Skipping ivy generation.");
                return;
            }

            GameObject ivyParent = new GameObject("--- OVERGROWN URBAN IVY ---");
            ivyParent.transform.SetParent(environmentRoot);

            // Wall Growth Origin points across major city buildings & infrastructure
            // Format: (Origin Base, Wall Search Direction, Max Height, Width)
            var wallTargets = new (Vector3 origin, Vector3 rayDir, float maxHeight, float width)[]
            {
                // Clinic Front & Side Facades
                (new Vector3(-24.5f, 0.2f, 25f), Vector3.left, 8.5f, 5.0f),
                (new Vector3(-28.0f, 0.2f, 21.5f), Vector3.forward, 6.0f, 4.0f),
                (new Vector3(-35.0f, 0.2f, 39.5f), Vector3.back, 7.5f, 4.5f),

                // Police Station Brick Facade & Corner
                (new Vector3(-17.5f, 0.2f, 11.5f), Vector3.left, 7.0f, 6.0f),
                (new Vector3(-32.5f, 0.2f, -0.5f), Vector3.right, 6.5f, 4.0f),

                // SafeHouse Exterior Walls
                (new Vector3(-35.5f, 0.2f, -24.5f), Vector3.left, 5.5f, 5.5f),
                (new Vector3(-42.0f, 0.2f, -32.5f), Vector3.forward, 5.0f, 4.0f),

                // Pharmacy & Supermarket East Facades
                (new Vector3(23.5f, 0.2f, 23.5f), Vector3.right, 8.0f, 6.5f),
                (new Vector3(25.5f, 0.2f, -39.5f), Vector3.right, 6.5f, 5.0f),

                // Skyscraper Podiums up to 14m height
                (new Vector3(45.5f, 0.2f, 42.5f), Vector3.right, 13.5f, 7.0f),
                (new Vector3(-45.5f, 0.2f, 42.5f), Vector3.left, 12.0f, 6.0f),
                (new Vector3(45.5f, 0.2f, -42.5f), Vector3.right, 11.0f, 6.0f),

                // Residential Suburb Brick & Stone Walls
                (new Vector3(-55.5f, 0.2f, -12.5f), Vector3.left, 5.5f, 4.5f),
                (new Vector3(-55.5f, 0.2f, 15.5f), Vector3.left, 5.5f, 4.5f),
                (new Vector3(-55.5f, 0.2f, 32.5f), Vector3.left, 5.0f, 4.0f),

                // Crashed Police Car & Heavy Van Overgrowth
                (new Vector3(2.5f, 0.2f, -13.5f), Vector3.back, 2.2f, 2.5f),
                (new Vector3(-18.5f, 0.2f, -38.5f), Vector3.forward, 2.4f, 2.8f)
            };

            for (int i = 0; i < wallTargets.Length; i++)
            {
                var target = wallTargets[i];
                CreateIvyClimberPatch(ivyParent.transform, $"IvyPatch_{i + 1}", target.origin, target.rayDir, target.maxHeight, target.width, ivyMat);
            }

            GameObjectUtility.SetStaticEditorFlags(ivyParent, StaticEditorFlags.BatchingStatic);
#endif
            Debug.Log("[OvergrownIvyGenerator] Urban Ivy generated with combined instanced meshes & surface-hugging raycasts.");
        }

#if UNITY_EDITOR
        public static void GenerateIvyPatches(Transform parent, string containerName, (Vector3 origin, Vector3 rayDir, float maxHeight, float width)[] targets)
        {
            if (parent == null || targets == null || targets.Length == 0) return;

            Material ivyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Overgrown_Ivy.mat");
            if (ivyMat == null) return;

            GameObject ivyParent = new GameObject(containerName);
            ivyParent.transform.SetParent(parent, false);

            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                CreateIvyClimberPatch(ivyParent.transform, $"IvyPatch_{i + 1}", target.origin, target.rayDir, target.maxHeight, target.width, ivyMat);
            }

            GameObjectUtility.SetStaticEditorFlags(ivyParent, StaticEditorFlags.BatchingStatic);
        }
#endif

#if UNITY_EDITOR
        private static void CreateIvyClimberPatch(Transform parent, string name, Vector3 origin, Vector3 rayDir, float maxHeight, float spreadWidth, Material mat)
        {
            // Cast multi-point surface normal raycast
            Ray ray = new Ray(origin + Vector3.up * 1f, rayDir.normalized);
            Vector3 wallNormal = -rayDir.normalized;
            Vector3 contactPoint = origin;

            if (Physics.Raycast(ray, out RaycastHit hit, 4.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                wallNormal = hit.normal;
                contactPoint = hit.point;
            }

            Vector3 wallTangent = Vector3.Cross(wallNormal, Vector3.up).normalized;
            if (wallTangent == Vector3.zero) wallTangent = Vector3.right;

            List<CombineInstance> combineList = new List<CombineInstance>();

            // Generate 3 to 6 branching vines along this wall section
            int numVines = Mathf.Clamp(Mathf.RoundToInt(spreadWidth / 1.1f), 3, 7);
            float vineSpacing = spreadWidth / (numVines + 1);

            for (int v = 0; v < numVines; v++)
            {
                float xOffset = (-spreadWidth * 0.5f) + (v + 1) * vineSpacing + Random.Range(-0.25f, 0.25f);
                Vector3 vineBase = contactPoint + (wallTangent * xOffset);
                float vineHeight = maxHeight * Random.Range(0.65f, 1.0f);

                // Grow vine upwards node by node
                int nodes = Mathf.RoundToInt(vineHeight / 0.45f);
                Vector3 currentNode = vineBase;

                for (int n = 0; n < nodes; n++)
                {
                    float heightProgress = (float)n / nodes;
                    float lateralJitter = (Mathf.PerlinNoise(v * 4.2f + n * 0.35f, 0.5f) - 0.5f) * 0.35f;
                    currentNode += Vector3.up * 0.45f + (wallTangent * lateralJitter);

                    // Re-cast onto wall to ensure surface-hugging precision
                    Vector3 sampleOrigin = currentNode - wallNormal * 0.6f;
                    Vector3 nodeSurfacePos = currentNode;
                    Vector3 nodeNormal = wallNormal;

                    if (Physics.Raycast(new Ray(sampleOrigin, wallNormal), out RaycastHit nHit, 1.4f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        nodeSurfacePos = nHit.point;
                        nodeNormal = nHit.normal;
                    }

                    // 0.03m normal offset to completely eliminate Z-fighting
                    Vector3 surfaceOffset = nodeSurfacePos + (nodeNormal * 0.032f);

                    // Add dense leaf cluster (2 to 4 leaf quads)
                    int leavesInCluster = Random.Range(2, 4);
                    for (int l = 0; l < leavesInCluster; l++)
                    {
                        float leafSize = Random.Range(0.42f, 0.68f) * (1f - heightProgress * 0.35f);
                        float rotAngle = Random.Range(-45f, 45f);

                        Vector3 leafOffset = (wallTangent * Random.Range(-0.16f, 0.16f)) + (Vector3.up * Random.Range(-0.12f, 0.12f));
                        Vector3 leafPos = surfaceOffset + leafOffset;

                        Quaternion leafRot = Quaternion.LookRotation(nodeNormal, Vector3.up) * Quaternion.Euler(0f, 0f, rotAngle);

                        Mesh quadMesh = CreateLeafQuadMesh(leafSize, heightProgress);
                        CombineInstance ci = new CombineInstance();
                        ci.mesh = quadMesh;
                        ci.transform = Matrix4x4.TRS(leafPos, leafRot, Vector3.one);
                        combineList.Add(ci);
                    }
                }
            }

            if (combineList.Count == 0) return;

            // Combine all leaf cards into one single mesh per building patch!
            Mesh combinedMesh = new Mesh();
            combinedMesh.name = $"{name}_CombinedMesh";
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combineList.ToArray(), true, true);
            combinedMesh.RecalculateBounds();

            GameObject patchObj = new GameObject(name);
            patchObj.transform.SetParent(parent, false);

            MeshFilter mf = patchObj.AddComponent<MeshFilter>();
            mf.sharedMesh = combinedMesh;

            MeshRenderer mr = patchObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Mobile optimization
            mr.receiveShadows = true;

            // No collider attached
            GameObjectUtility.SetStaticEditorFlags(patchObj, StaticEditorFlags.BatchingStatic);
        }

        private static Mesh CreateLeafQuadMesh(float size, float heightProgress)
        {
            Mesh m = new Mesh();
            float half = size * 0.5f;

            m.vertices = new Vector3[]
            {
                new Vector3(-half, -half, 0f),
                new Vector3(half, -half, 0f),
                new Vector3(-half, half, 0f),
                new Vector3(half, half, 0f)
            };

            m.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };

            m.triangles = new int[] { 0, 2, 1, 2, 3, 1 };

            // Subtle vertex color variation: deep green #2D4A22 to autumn olive #5C5327
            Color deepGreen = new Color(0.176f, 0.290f, 0.133f, 1f);
            Color oliveGreen = new Color(0.360f, 0.325f, 0.153f, 1f);
            Color leafColor = Color.Lerp(deepGreen, oliveGreen, heightProgress * 0.7f + Random.Range(0f, 0.3f));

            m.colors = new Color[] { leafColor, leafColor, leafColor, leafColor };

            m.normals = new Vector3[]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            };

            return m;
        }
#endif
    }
}
