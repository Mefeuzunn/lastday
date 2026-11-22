using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace ProjectSurvival.EditorTools
{
    public static class AAA_PBRMaterialLibrary
    {
        public const string MaterialsFolder = "Assets/_Project/Materials/AAA_PBR";
        public const string TexturesFolder = "Assets/_Project/Materials/AAA_PBR/Textures";

        [MenuItem("ProjectSurvival/Generate AAA PBR Material Library", false, 20)]
        public static void GenerateAllMenuItem()
        {
            GenerateAll(true);
        }

        public static void GenerateAll(bool refreshAssetDatabase = true)
        {
            Debug.Log("==================================================================");
            Debug.Log(">>> GENERATING AAA PBR PROCEDURAL TEXTURES & MATERIAL PIPELINE <<<");
            Debug.Log("==================================================================");

            if (!Directory.Exists(TexturesFolder))
            {
                Directory.CreateDirectory(TexturesFolder);
            }

            // 1. Generate Procedural Textures (1024x1024)
            GenerateAsphaltTextures();
            GenerateBrickTextures();
            GenerateConcreteTextures();
            GenerateAutomotiveTextures();
            GenerateWoodPlankTextures();
            GenerateTacticalFabricTextures();
            GenerateContactShadowTexture();

            if (refreshAssetDatabase)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            // Configure Texture Importers (NormalMaps, Linear MaskMaps, Mipmaps, Aniso)
            ConfigureTextureImporters();

            // 2. Build or Update Materials (URP Lit, SRP Batcher, GPU Instancing)
            BuildPBRMaterials();

            AssetDatabase.SaveAssets();
            Debug.Log("[AAA_PBRMaterialLibrary] High-Definition PBR Library Generated Successfully!");
        }

        #region Texture Generation

        private static void GenerateAsphaltTextures()
        {
            int size = 1024;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, false);
            float[,] height = new float[size, size];
            Texture2D mask = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color colDark = new Color(0.11f, 0.12f, 0.15f);
            Color colLight = new Color(0.18f, 0.20f, 0.24f);
            Color colPuddleTint = new Color(0.07f, 0.08f, 0.10f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    // Multi-octave fBm asphalt noise
                    float n1 = Mathf.PerlinNoise(u * 28f, v * 28f);
                    float n2 = Mathf.PerlinNoise(u * 75f, v * 75f) * 0.5f;
                    float n3 = Mathf.PerlinNoise(u * 180f, v * 180f) * 0.25f;
                    float aggregate = (n1 + n2 + n3) / 1.75f;

                    // Crack patterns via high frequency domain warp
                    float crackNoise = Mathf.PerlinNoise(u * 12f + aggregate * 0.15f, v * 12f + aggregate * 0.15f);
                    float crackDist = Mathf.Abs(crackNoise - 0.5f) * 2f;
                    bool isCrack = crackDist < 0.045f;
                    float crackDepth = isCrack ? Mathf.Clamp01(1f - (crackDist / 0.045f)) : 0f;

                    // Puddles (organic patches where rainwater pools)
                    float puddleNoise = Mathf.PerlinNoise(u * 4.5f, v * 4.5f);
                    bool isPuddle = puddleNoise > 0.62f;
                    float puddleFactor = isPuddle ? Mathf.Clamp01((puddleNoise - 0.62f) / 0.15f) : 0f;

                    // Compute height for normal map
                    float hVal = aggregate * 0.6f - crackDepth * 0.45f - puddleFactor * 0.15f;
                    height[x, y] = hVal;

                    // Albedo
                    Color c = Color.Lerp(colDark, colLight, aggregate);
                    if (isCrack) c *= (1f - crackDepth * 0.6f);
                    if (puddleFactor > 0f) c = Color.Lerp(c, colPuddleTint, puddleFactor * 0.75f);
                    albedo.SetPixel(x, y, c);

                    // MaskMap: R: Metallic, G: AO, B: Detail, A: Smoothness
                    float metallic = Mathf.Lerp(0.05f, 0.14f, puddleFactor);
                    float ao = isCrack ? (1f - crackDepth * 0.65f) : 1f;
                    float smoothness = Mathf.Lerp(0.14f, 0.88f, puddleFactor); // Dry 0.14 vs Puddle specular 0.88

                    mask.SetPixel(x, y, new Color(metallic, ao, 0f, smoothness));
                }
            }

            albedo.Apply();
            mask.Apply();

            SaveTextureToFile(albedo, Path.Combine(TexturesFolder, "T_Asphalt_Albedo.png"));
            SaveTextureToFile(mask, Path.Combine(TexturesFolder, "T_Asphalt_MaskMap.png"));

            // Generate Sobel Normal Map
            Texture2D normalMap = GenerateSobelNormalMap(height, size, 2.5f);
            SaveTextureToFile(normalMap, Path.Combine(TexturesFolder, "T_Asphalt_Normal.png"));
        }

        private static void GenerateBrickTextures()
        {
            int size = 1024;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, false);
            float[,] height = new float[size, size];
            Texture2D mask = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color brickRed = new Color(0.55f, 0.24f, 0.18f);
            Color brickBurnt = new Color(0.38f, 0.18f, 0.14f);
            Color mortarCol = new Color(0.45f, 0.44f, 0.42f);

            int rows = 32;
            int cols = 16;
            float rowHeight = 1f / rows;
            float colWidth = 1f / cols;

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                int rowIndex = Mathf.FloorToInt(v / rowHeight);
                float rowFrac = (v - rowIndex * rowHeight) / rowHeight;
                bool isMortarH = rowFrac < 0.12f || rowFrac > 0.88f;

                float xOffset = (rowIndex % 2 == 0) ? 0f : colWidth * 0.5f;

                for (int x = 0; x < size; x++)
                {
                    float u = ((float)x / size + xOffset) % 1f;
                    int colIndex = Mathf.FloorToInt(u / colWidth);
                    float colFrac = (u - colIndex * colWidth) / colWidth;
                    bool isMortarV = colFrac < 0.08f || colFrac > 0.92f;

                    bool isMortar = isMortarH || isMortarV;

                    float fbm = Mathf.PerlinNoise((float)x / size * 45f, (float)y / size * 45f) * 0.25f;
                    float runoffGrime = Mathf.PerlinNoise((float)x / size * 8f, (float)y / size * 2f) * 0.2f;

                    float h = isMortar ? 0.15f : (0.85f + fbm);
                    height[x, y] = h;

                    Color c;
                    if (isMortar)
                    {
                        c = mortarCol * (1f - fbm);
                    }
                    else
                    {
                        float brickVar = Mathf.PerlinNoise(colIndex * 1.5f, rowIndex * 1.5f);
                        c = Color.Lerp(brickRed, brickBurnt, brickVar) + new Color(fbm, fbm, fbm) * 0.3f;
                        c -= new Color(runoffGrime, runoffGrime, runoffGrime);
                    }

                    albedo.SetPixel(x, y, c);

                    float ao = isMortar ? 0.45f : 0.95f;
                    float smoothness = isMortar ? 0.12f : 0.26f;
                    mask.SetPixel(x, y, new Color(0f, ao, 0f, smoothness));
                }
            }

            albedo.Apply();
            mask.Apply();

            SaveTextureToFile(albedo, Path.Combine(TexturesFolder, "T_Brick_Albedo.png"));
            SaveTextureToFile(mask, Path.Combine(TexturesFolder, "T_Brick_MaskMap.png"));

            Texture2D normalMap = GenerateSobelNormalMap(height, size, 3.2f);
            SaveTextureToFile(normalMap, Path.Combine(TexturesFolder, "T_Brick_Normal.png"));
        }

        private static void GenerateConcreteTextures()
        {
            int size = 1024;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, false);
            float[,] height = new float[size, size];
            Texture2D mask = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color baseConcrete = new Color(0.42f, 0.43f, 0.44f);
            Color grimeConcrete = new Color(0.28f, 0.29f, 0.30f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    float fbm1 = Mathf.PerlinNoise(u * 16f, v * 16f);
                    float fbm2 = Mathf.PerlinNoise(u * 64f, v * 64f) * 0.4f;
                    float fbm3 = Mathf.PerlinNoise(u * 180f, v * 180f) * 0.15f;
                    float surface = (fbm1 + fbm2 + fbm3) / 1.55f;

                    // Formwork horizontal line every 128 pixels
                    bool isSeam = (y % 128) < 3;
                    float seamDepth = isSeam ? 0.3f : 0f;

                    // Water runoff streaks
                    float runoff = Mathf.PerlinNoise(u * 10f, v * 2f);
                    float grime = Mathf.Clamp01(runoff * 0.45f);

                    height[x, y] = surface - seamDepth;

                    Color c = Color.Lerp(baseConcrete, grimeConcrete, grime) + new Color(surface * 0.15f, surface * 0.15f, surface * 0.15f);
                    if (isSeam) c *= 0.75f;
                    albedo.SetPixel(x, y, c);

                    float ao = isSeam ? 0.5f : 0.90f;
                    float smoothness = 0.18f;
                    mask.SetPixel(x, y, new Color(0f, ao, 0f, smoothness));
                }
            }

            albedo.Apply();
            mask.Apply();

            SaveTextureToFile(albedo, Path.Combine(TexturesFolder, "T_Concrete_Albedo.png"));
            SaveTextureToFile(mask, Path.Combine(TexturesFolder, "T_Concrete_MaskMap.png"));

            Texture2D normalMap = GenerateSobelNormalMap(height, size, 2.0f);
            SaveTextureToFile(normalMap, Path.Combine(TexturesFolder, "T_Concrete_Normal.png"));
        }

        private static void GenerateAutomotiveTextures()
        {
            int size = 512;
            Texture2D mask = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    // Edge wear / rust noise
                    float edgeNoise = Mathf.PerlinNoise(u * 32f, v * 32f);
                    bool isRust = edgeNoise > 0.78f;

                    float metallic = isRust ? 0.25f : 0.85f;
                    float ao = 0.92f;
                    float smoothness = isRust ? 0.22f : 0.78f; // Glossy automotive clearcoat vs worn rust

                    mask.SetPixel(x, y, new Color(metallic, ao, 0f, smoothness));
                }
            }

            mask.Apply();
            SaveTextureToFile(mask, Path.Combine(TexturesFolder, "T_Auto_MaskMap.png"));
        }

        private static void GenerateWoodPlankTextures()
        {
            int size = 1024;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, false);
            float[,] height = new float[size, size];
            Texture2D mask = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Color woodLight = new Color(0.48f, 0.35f, 0.22f);
            Color woodDark = new Color(0.32f, 0.21f, 0.12f);

            int plankCount = 12;
            float plankHeight = 1f / plankCount;

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                int pIndex = Mathf.FloorToInt(v / plankHeight);
                float pFrac = (v - pIndex * plankHeight) / plankHeight;
                bool isPlankGap = pFrac < 0.05f || pFrac > 0.95f;

                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;

                    // Elongated wood grain
                    float grain = Mathf.PerlinNoise(u * 85f, v * 8f);
                    float knot = Mathf.PerlinNoise(u * 12f, v * 12f);
                    float woodPattern = (grain * 0.7f + knot * 0.3f);

                    float h = isPlankGap ? 0.15f : (0.75f + woodPattern * 0.25f);
                    height[x, y] = h;

                    Color c = Color.Lerp(woodDark, woodLight, woodPattern);
                    if (isPlankGap) c *= 0.5f;
                    albedo.SetPixel(x, y, c);

                    float ao = isPlankGap ? 0.4f : 0.92f;
                    float smoothness = 0.22f;
                    mask.SetPixel(x, y, new Color(0f, ao, 0f, smoothness));
                }
            }

            albedo.Apply();
            mask.Apply();

            SaveTextureToFile(albedo, Path.Combine(TexturesFolder, "T_Wood_Planks_Albedo.png"));
            SaveTextureToFile(mask, Path.Combine(TexturesFolder, "T_Wood_Planks_MaskMap.png"));

            Texture2D normalMap = GenerateSobelNormalMap(height, size, 2.8f);
            SaveTextureToFile(normalMap, Path.Combine(TexturesFolder, "T_Wood_Planks_Normal.png"));
        }

        private static void GenerateTacticalFabricTextures()
        {
            int size = 512;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, false);
            float[,] height = new float[size, size];

            Color fabricBase = new Color(0.22f, 0.25f, 0.28f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // High frequency basket weave
                    int px = (x / 4) % 2;
                    int py = (y / 4) % 2;
                    bool weave = (px ^ py) == 1;

                    float h = weave ? 0.65f : 0.35f;
                    height[x, y] = h;

                    Color c = weave ? fabricBase * 1.15f : fabricBase * 0.88f;
                    albedo.SetPixel(x, y, c);
                }
            }

            albedo.Apply();
            SaveTextureToFile(albedo, Path.Combine(TexturesFolder, "T_Tactical_Fabric_Albedo.png"));

            Texture2D normalMap = GenerateSobelNormalMap(height, size, 1.8f);
            SaveTextureToFile(normalMap, Path.Combine(TexturesFolder, "T_Tactical_Fabric_Normal.png"));
        }

        private static void GenerateContactShadowTexture()
        {
            int size = 256;
            Texture2D shadowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxRadius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normDist = Mathf.Clamp01(dist / maxRadius);

                    // Smooth radial Gaussian falloff
                    float alpha = Mathf.Exp(-normDist * normDist * 3.8f);
                    if (normDist >= 1.0f) alpha = 0f;

                    shadowTex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha * 0.88f));
                }
            }

            shadowTex.Apply();
            SaveTextureToFile(shadowTex, Path.Combine(TexturesFolder, "T_ContactShadow.png"));
        }

        private static Texture2D GenerateSobelNormalMap(float[,] height, int size, float strength)
        {
            Texture2D normalMap = new Texture2D(size, size, TextureFormat.RGB24, false);

            for (int y = 0; y < size; y++)
            {
                int yPrev = (y - 1 + size) % size;
                int yNext = (y + 1) % size;

                for (int x = 0; x < size; x++)
                {
                    int xPrev = (x - 1 + size) % size;
                    int xNext = (x + 1) % size;

                    // Sobel filter kernels
                    float dX = (height[xNext, yPrev] + 2f * height[xNext, y] + height[xNext, yNext]) -
                               (height[xPrev, yPrev] + 2f * height[xPrev, y] + height[xPrev, yNext]);

                    float dY = (height[xPrev, yNext] + 2f * height[x, yNext] + height[xNext, yNext]) -
                               (height[xPrev, yPrev] + 2f * height[x, yPrev] + height[xNext, yPrev]);

                    Vector3 normal = new Vector3(-dX * strength, -dY * strength, 1.0f).normalized;

                    // Tangent space normal to RGB: [-1, 1] -> [0, 1]
                    Color c = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f
                    );

                    normalMap.SetPixel(x, y, c);
                }
            }

            normalMap.Apply();
            return normalMap;
        }

        private static void SaveTextureToFile(Texture2D tex, string filePath)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
        }

        #endregion

        #region Importer Configuration

        private static void ConfigureTextureImporters()
        {
            string[] files = Directory.GetFiles(TexturesFolder, "*.png");
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;

                ti.mipmapEnabled = true;
                ti.anisoLevel = 4;
                ti.filterMode = FilterMode.Bilinear;

                if (path.Contains("_Normal"))
                {
                    ti.textureType = TextureImporterType.NormalMap;
                }
                else if (path.Contains("_MaskMap"))
                {
                    ti.sRGBTexture = false; // MaskMap must be linear
                }

                ti.SaveAndReimport();
            }
        }

        #endregion

        #region Material Building

        private static void BuildPBRMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");

            if (litShader == null)
            {
                Debug.LogError("[AAA_PBRMaterialLibrary] 'Universal Render Pipeline/Lit' shader not found!");
                return;
            }

            // 1. Wet Cracked Asphalt
            CreateOrUpdatePBRMat("MAT_Asphalt_WetPuddles", litShader,
                albedoPath: Path.Combine(TexturesFolder, "T_Asphalt_Albedo.png"),
                normalPath: Path.Combine(TexturesFolder, "T_Asphalt_Normal.png"),
                maskPath: Path.Combine(TexturesFolder, "T_Asphalt_MaskMap.png"),
                tiling: new Vector2(16f, 16f));

            // 2. Weathered Mortar Brick
            CreateOrUpdatePBRMat("MAT_Brick_Weathered", litShader,
                albedoPath: Path.Combine(TexturesFolder, "T_Brick_Albedo.png"),
                normalPath: Path.Combine(TexturesFolder, "T_Brick_Normal.png"),
                maskPath: Path.Combine(TexturesFolder, "T_Brick_MaskMap.png"),
                tiling: new Vector2(6f, 4f));

            // 3. Spalling Concrete
            CreateOrUpdatePBRMat("MAT_Concrete_Spalling", litShader,
                albedoPath: Path.Combine(TexturesFolder, "T_Concrete_Albedo.png"),
                normalPath: Path.Combine(TexturesFolder, "T_Concrete_Normal.png"),
                maskPath: Path.Combine(TexturesFolder, "T_Concrete_MaskMap.png"),
                tiling: new Vector2(8f, 8f));

            // 4. Wood Planks
            CreateOrUpdatePBRMat("MAT_Wood_Planks", litShader,
                albedoPath: Path.Combine(TexturesFolder, "T_Wood_Planks_Albedo.png"),
                normalPath: Path.Combine(TexturesFolder, "T_Wood_Planks_Normal.png"),
                maskPath: Path.Combine(TexturesFolder, "T_Wood_Planks_MaskMap.png"),
                tiling: new Vector2(3f, 3f));

            // 5. Tactical Fabric
            CreateOrUpdatePBRMat("MAT_Tactical_Fabric", litShader,
                albedoPath: Path.Combine(TexturesFolder, "T_Tactical_Fabric_Albedo.png"),
                normalPath: Path.Combine(TexturesFolder, "T_Tactical_Fabric_Normal.png"),
                maskPath: null,
                tiling: new Vector2(6f, 6f));

            // 6. Automotive Paint Materials
            Texture2D autoMask = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TexturesFolder, "T_Auto_MaskMap.png"));

            CreateAutoMaterial("MAT_Auto_Police", litShader, new Color(0.12f, 0.13f, 0.15f), autoMask);
            CreateAutoMaterial("MAT_Auto_Ambulance", litShader, new Color(0.92f, 0.92f, 0.95f), autoMask);
            CreateAutoMaterial("MAT_Auto_Taxi", litShader, new Color(0.95f, 0.72f, 0.10f), autoMask);
            CreateAutoMaterial("MAT_Auto_RustMetal", litShader, new Color(0.38f, 0.22f, 0.16f), autoMask);

            // 7. Dusty Reflective Glass
            Material glassMat = GetOrCreateMaterial("MAT_Glass_DustyReflective", litShader);
            glassMat.SetColor("_BaseColor", new Color(0.72f, 0.85f, 0.95f, 0.55f));
            glassMat.SetFloat("_Metallic", 0.25f);
            glassMat.SetFloat("_Smoothness", 0.68f);
            glassMat.enableInstancing = true;

            // 8. Contact Shadow Material (Unlit Transparent)
            Material contactMat = GetOrCreateMaterial("MAT_ContactShadow", unlitShader);
            Texture2D shadowTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(TexturesFolder, "T_ContactShadow.png"));
            if (shadowTex != null) contactMat.mainTexture = shadowTex;
            contactMat.SetFloat("_Surface", 1); // Transparent
            contactMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            contactMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            contactMat.SetInt("_ZWrite", 0);
            contactMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            contactMat.enableInstancing = true;
        }

        private static Material CreateOrUpdatePBRMat(string matName, Shader shader, string albedoPath, string normalPath, string maskPath, Vector2 tiling)
        {
            Material mat = GetOrCreateMaterial(matName, shader);
            mat.enableInstancing = true;

            if (!string.IsNullOrEmpty(albedoPath))
            {
                Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                if (albedo != null)
                {
                    mat.SetTexture("_BaseMap", albedo);
                    mat.SetTextureScale("_BaseMap", tiling);
                }
            }

            if (!string.IsNullOrEmpty(normalPath))
            {
                Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.SetTextureScale("_BumpMap", tiling);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            if (!string.IsNullOrEmpty(maskPath))
            {
                Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
                if (mask != null)
                {
                    mat.SetTexture("_MaskMap", mask);
                    mat.SetTextureScale("_MaskMap", tiling);
                    mat.EnableKeyword("_MASKMAP");
                    mat.SetFloat("_Metallic", 1.0f);
                    mat.SetFloat("_Smoothness", 1.0f);
                }
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void CreateAutoMaterial(string matName, Shader shader, Color baseColor, Texture2D mask)
        {
            Material mat = GetOrCreateMaterial(matName, shader);
            mat.SetColor("_BaseColor", baseColor);
            if (mask != null)
            {
                mat.SetTexture("_MaskMap", mask);
                mat.EnableKeyword("_MASKMAP");
                mat.SetFloat("_Metallic", 1.0f);
                mat.SetFloat("_Smoothness", 1.0f);
            }
            else
            {
                mat.SetFloat("_Metallic", 0.85f);
                mat.SetFloat("_Smoothness", 0.78f);
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
        }

        private static Material GetOrCreateMaterial(string name, Shader shader)
        {
            string path = Path.Combine(MaterialsFolder, $"{name}.mat");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            return mat;
        }

        #endregion
    }
}
