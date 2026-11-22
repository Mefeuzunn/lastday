using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;
using ProjectSurvival.Narrative;
using ProjectSurvival.Missions;
using ProjectSurvival.UI;
using ProjectSurvival.Environment;

namespace ProjectSurvival.EditorTools
{
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player_Root.prefab";
        private const string ZombieStandardPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Standard_Root.prefab";
        private const string ZombieRunnerPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Runner_Root.prefab";
        private const string ZombieScreamerPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Screamer_Root.prefab";
        private const string ZombieTankPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Tank_Root.prefab";
        private const string LootPrefabPath = "Assets/_Project/Prefabs/Environment/LootContainer_Root.prefab";
        private const string DistractionPrefabPath = "Assets/_Project/Prefabs/Environment/DistractionBottle_Root.prefab";
        private const string SoundBombPrefabPath = "Assets/_Project/Prefabs/Environment/SoundBomb_Root.prefab";
        private const string NPCDrArasPrefabPath = "Assets/_Project/Prefabs/NPCs/NPC_DrAras_Root.prefab";
        private const string NPCZeynepPrefabPath = "Assets/_Project/Prefabs/NPCs/NPC_Zeynep_Root.prefab";
        private const string NPCKeremPrefabPath = "Assets/_Project/Prefabs/NPCs/NPC_Kerem_Root.prefab";
        private const string NPCElifPrefabPath = "Assets/_Project/Prefabs/NPCs/NPC_Elif_Root.prefab";
        private const string NPCScavengerPrefabPath = "Assets/_Project/Prefabs/NPCs/Survivor_Scavenger_Root.prefab";

        [MenuItem("ProjectSurvival/Build Complete Prototype (All-In-One)", false, 1)]
        public static void BuildAll()
        {
            Debug.Log("[SceneBuilder] Starting Complete Prototype Build - Real 3D Characters, Animations & Full Post-Apocalyptic City (Phase 6)...");

            EnsureDirectories();
            EnsureUniversalRenderPipelineAsset();

            // Generate procedural UI Sprites & PBR Textures
            AssetGenerator.GenerateAllAssets();

            // Create all URP Materials
            CreateAllMaterials();

            GameObject soundBombPrefab = CreateSoundBombPrefab();
            GameObject distractionPrefab = CreateDistractionPrefab();

            CreateItemDataAssets(distractionPrefab, soundBombPrefab);
            CreateCraftingRecipes();

            GameObject playerPrefab = CreatePlayerPrefab(distractionPrefab);
            GameObject zombieStandardPrefab = CreateStandardZombiePrefab();
            GameObject zombieRunnerPrefab = CreateRunnerZombiePrefab();
            GameObject zombieScreamerPrefab = CreateScreamerZombiePrefab();
            GameObject zombieTankPrefab = CreateTankZombiePrefab();
            GameObject lootPrefab = CreateLootContainerPrefab();

            GameObject npcDrArasPrefab = CreateNPCDrArasPrefab();
            GameObject npcZeynepPrefab = CreateNPCZeynepPrefab();
            GameObject npcKeremPrefab = CreateNPCKeremPrefab();
            GameObject npcElifPrefab = CreateNPCElifPrefab();
            GameObject npcScavengerPrefab = CreateNPCScavengerPrefab();

            BuildScene(
                playerPrefab,
                zombieStandardPrefab,
                zombieRunnerPrefab,
                zombieScreamerPrefab,
                zombieTankPrefab,
                lootPrefab,
                soundBombPrefab,
                distractionPrefab,
                npcDrArasPrefab,
                npcZeynepPrefab,
                npcKeremPrefab,
                npcElifPrefab,
                npcScavengerPrefab
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SceneBuilder] SUCCESS: Phase 6 Complete! Real 3D Characters, Animations & Full Post-Apocalyptic City Built!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = {
                "Assets/_Project/Scenes",
                "Assets/_Project/Prefabs/Player",
                "Assets/_Project/Prefabs/Zombies",
                "Assets/_Project/Prefabs/NPCs",
                "Assets/_Project/Prefabs/Environment",
                "Assets/_Project/Prefabs/UI",
                "Assets/_Project/Materials",
                "Assets/_Project/Settings",
                "Assets/_Project/Data/Items",
                "Assets/_Project/Resources/Items",
                "Assets/_Project/Data/Recipes",
                "Assets/_Project/Resources/Recipes",
                "Assets/_Project/Resources/Stories",
                "Assets/_Project/UI/Sprites",
                "Assets/_Project/Textures"
            };

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        public static UniversalRenderPipelineAsset EnsureUniversalRenderPipelineAsset()
        {
            string assetPath = "Assets/_Project/Settings/UniversalRenderPipelineAsset.asset";
            string rendererPath = "Assets/_Project/Settings/UniversalRendererData.asset";

            UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);

            if (urpAsset == null)
            {
                UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (rendererData == null)
                {
                    rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(rendererData, rendererPath);
                }

                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urpAsset, assetPath);
                Debug.Log("[SceneBuilder] Created new UniversalRenderPipelineAsset at " + assetPath);
            }

            urpAsset.shadowDistance = 45f;
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;
            EditorUtility.SetDirty(urpAsset);

            return urpAsset;
        }

        public static Material GetOrCreateMaterial(string matName, Color color, string albedoPath = null, string normalPath = null, Vector2? tiling = null, bool isTransparent = false, float? smoothness = null)
        {
            string path = $"Assets/_Project/Materials/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader targetShader = isTransparent
                ? (Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"))
                : (Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

            if (mat == null)
            {
                mat = new Material(targetShader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = targetShader;
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (!string.IsNullOrEmpty(albedoPath))
            {
                Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                if (albedo != null)
                {
                    mat.mainTexture = albedo;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
                }
            }

            if (!string.IsNullOrEmpty(normalPath))
            {
                Texture2D norm = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (norm != null)
                {
                    if (mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", norm);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                }
            }

            if (tiling.HasValue)
            {
                mat.mainTextureScale = tiling.Value;
                if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", tiling.Value);
                if (mat.HasProperty("_BumpMap")) mat.SetTextureScale("_BumpMap", tiling.Value);
            }

            if (isTransparent)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }

            if (smoothness.HasValue)
            {
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness.Value);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness.Value);
            }

            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material GetOrCreateAlphaClipMaterial(string matName, Color baseColor, string albedoPath, float cutoff = 0.45f, bool doubleSided = true)
        {
            string path = $"Assets/_Project/Materials/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);

            if (!string.IsNullOrEmpty(albedoPath))
            {
                Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                if (albedo != null)
                {
                    mat.mainTexture = albedo;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
                }
            }

            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", cutoff);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.AlphaTest;

            if (doubleSided)
            {
                mat.SetFloat("_Cull", 0f);
            }

            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material GetOrCreateWeatheredGlassMaterial(string matName)
        {
            string path = $"Assets/_Project/Materials/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            Color glassColor = new Color(0.12f, 0.16f, 0.20f, 0.75f);
            mat.color = glassColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", glassColor);

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/Tex_WeatheredGlass_Albedo.png");
            if (albedo != null)
            {
                mat.mainTexture = albedo;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
            }

            Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/Tex_WeatheredGlass_MaskMap.png");
            if (mask != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", mask);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            Texture2D norm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/Tex_WeatheredGlass_Normal.png");
            if (norm != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", norm);
                mat.EnableKeyword("_NORMALMAP");
            }

            mat.SetFloat("_Surface", 1);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1.0f);
            mat.enableInstancing = true;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material GetOrCreateParticleMaterial(string matName, Color baseColor, bool isAdditive)
        {
            string path = $"Assets/_Project/Materials/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (mat == null)
            {
                if (shader == null) shader = Shader.Find("Hidden/Universal Render Pipeline/FallbackError");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null && mat.shader != shader)
            {
                mat.shader = shader;
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetColor("_Color", baseColor);

            Texture2D pTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/tex_particle_soft_circle.png");
            if (pTex != null)
            {
                mat.SetTexture("_BaseMap", pTex);
                mat.SetTexture("_MainTex", pTex);
            }

            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_ZWrite", 0);

            if (isAdditive)
            {
                mat.SetFloat("_Blend", 1);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
            }
            else
            {
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static void CreateAllMaterials()
        {
            // Hex color mappings with PBR Textures:
            // Ground (#1E2126) with Asphalt PBR
            GetOrCreateMaterial("Mat_Ground", new Color(0.118f, 0.13f, 0.15f),
                "Assets/_Project/Textures/Tex_Asphalt_Albedo.png", "Assets/_Project/Textures/Tex_Asphalt_Normal.png", new Vector2(18f, 18f));

            // Street (#23272E) with Asphalt PBR
            GetOrCreateMaterial("Mat_Street", new Color(0.137f, 0.153f, 0.180f),
                "Assets/_Project/Textures/Tex_Asphalt_Albedo.png", "Assets/_Project/Textures/Tex_Asphalt_Normal.png", new Vector2(10f, 10f));

            // Clinic Building with Brick PBR
            GetOrCreateMaterial("Mat_Building_Clinic", new Color(0.95f, 0.95f, 1f),
                "Assets/_Project/Textures/Tex_Brick_Albedo.png", "Assets/_Project/Textures/Tex_Brick_Normal.png", new Vector2(6f, 4f));

            // General Walls with Brick PBR
            GetOrCreateMaterial("Mat_Building_Wall", new Color(0.85f, 0.85f, 0.85f),
                "Assets/_Project/Textures/Tex_Brick_Albedo.png", "Assets/_Project/Textures/Tex_Brick_Normal.png", new Vector2(6f, 4f));

            // Wood Covers/Chests with Wood PBR
            GetOrCreateMaterial("Mat_Cover_Wood", new Color(1f, 1f, 1f),
                "Assets/_Project/Textures/Tex_Wood_Albedo.png", "Assets/_Project/Textures/Tex_Wood_Normal.png", new Vector2(2f, 2f));

            // Metal Lock (#D4AF37): 212, 175, 55
            GetOrCreateMaterial("Mat_Chest_Metal", new Color(212f / 255f, 175f / 255f, 55f / 255f));

            // Road Infrastructure & Details
            GetOrCreateMaterial("Mat_Curb", new Color(0.38f, 0.40f, 0.44f), "Assets/_Project/Textures/Tex_Concrete_Albedo.png", "Assets/_Project/Textures/Tex_Concrete_Normal.png", new Vector2(4f, 4f));
            GetOrCreateMaterial("Mat_RoadMarking", new Color(0.92f, 0.90f, 0.85f));
            GetOrCreateMaterial("Mat_RoadMarking_Yellow", new Color(0.95f, 0.78f, 0.22f));
            GetOrCreateMaterial("Mat_StreetLamp", new Color(0.18f, 0.20f, 0.24f));
            GetOrCreateMaterial("Mat_LampBulb", new Color(1.0f, 0.718f, 0.012f)); // Warm amber #FFB703
            GetOrCreateMaterial("Mat_Vehicle_Body", new Color(0.28f, 0.35f, 0.40f));
            GetOrCreateMaterial("Mat_Vehicle_Rust", new Color(0.42f, 0.25f, 0.18f));
            GetOrCreateWeatheredGlassMaterial("Mat_Vehicle_Glass");
            GetOrCreateMaterial("Mat_Metal_Barrier", new Color(0.55f, 0.58f, 0.62f));
            GetOrCreateMaterial("Mat_Sign_Clinic", new Color(0.12f, 0.55f, 0.58f));

            // Urban Atmosphere & Building Facade Materials
            GetOrCreateMaterial("Mat_Skyline_Block", new Color(0.045f, 0.055f, 0.075f));
            GetOrCreateMaterial("Mat_Window_Frame", new Color(0.12f, 0.14f, 0.16f));
            GetOrCreateWeatheredGlassMaterial("Mat_Window_Glass");
            GetOrCreateWeatheredGlassMaterial("Mat_Weathered_Glass");
            GetOrCreateMaterial("Mat_Window_WarmLit", new Color(1.0f, 0.82f, 0.42f));
            GetOrCreateMaterial("Mat_Door_Wood", new Color(0.35f, 0.22f, 0.14f));
            GetOrCreateMaterial("Mat_Sign_Supermarket", new Color(0.12f, 0.42f, 0.32f));
            GetOrCreateMaterial("Mat_Sign_Pharmacy", new Color(0.10f, 0.38f, 0.62f));
            GetOrCreateMaterial("Mat_Plank_Barricade", new Color(0.48f, 0.34f, 0.22f));
            GetOrCreateMaterial("Mat_HVAC_Metal", new Color(0.30f, 0.33f, 0.36f));

            // Phase 5 Atmospheric Materials: Overgrown Ivy, Cobwebs, Storefront Wood, Decals
            GetOrCreateAlphaClipMaterial("Mat_Overgrown_Ivy", Color.white, "Assets/_Project/Textures/Tex_Ivy_Leaf_Albedo.png", cutoff: 0.45f, doubleSided: true);
            GetOrCreateAlphaClipMaterial("Mat_Cobweb", new Color(0.85f, 0.87f, 0.90f, 0.92f), "Assets/_Project/Textures/Tex_Cobweb_Albedo.png", cutoff: 0.25f, doubleSided: true);
            GetOrCreateMaterial("Mat_Storefront_Wood", Color.white, "Assets/_Project/Textures/Tex_Storefront_Wood_Albedo.png", "Assets/_Project/Textures/Tex_Storefront_Wood_Normal.png");
            GetOrCreateMaterial("Mat_Asphalt_Crack_Decal", Color.white, "Assets/_Project/Textures/Tex_Asphalt_Crack.png", isTransparent: true);
            GetOrCreateMaterial("Mat_Moss_Decal", Color.white, "Assets/_Project/Textures/Tex_Moss_Strip.png", isTransparent: true);

            // Characters:
            // Player (#2E6F95): 46, 111, 149
            GetOrCreateMaterial("Mat_Player", new Color(46f / 255f, 111f / 255f, 149f / 255f));
            // Player Visor (#E0E8F0): 224, 232, 240
            GetOrCreateMaterial("Mat_Player_Visor", new Color(224f / 255f, 232f / 255f, 240f / 255f));

            // Dr. Aras (Teal Doctor Coat):
            GetOrCreateMaterial("Mat_Dr_Aras", new Color(0.1f, 0.55f, 0.52f));
            GetOrCreateMaterial("Mat_NPC_DrAras", new Color(0.1f, 0.55f, 0.52f));
            GetOrCreateMaterial("Mat_NPC_Zeynep", new Color(0.52f, 0.37f, 0.76f));
            GetOrCreateMaterial("Mat_NPC_Kerem", new Color(0.84f, 0.36f, 0.2f));
            GetOrCreateMaterial("Mat_NPC_Elif", new Color(0.18f, 0.49f, 0.2f));
            GetOrCreateMaterial("Mat_Item_Antiviral", new Color(0f, 0.82f, 0.83f));

            // Zombies:
            // Standard Zombie (#4F772D): 79, 119, 45
            GetOrCreateMaterial("Mat_Zombie_Standard", new Color(79f / 255f, 119f / 255f, 45f / 255f));
            // Runner Zombie (#A4161A): 164, 22, 26
            GetOrCreateMaterial("Mat_Zombie_Runner", new Color(164f / 255f, 22f / 255f, 26f / 255f));
            // Screamer Zombie (#7209B7): 114, 9, 183
            GetOrCreateMaterial("Mat_Zombie_Screamer", new Color(114f / 255f, 9f / 255f, 183f / 255f));
            // Tank Zombie (#212529): 33, 37, 41
            GetOrCreateMaterial("Mat_Zombie_Tank", new Color(33f / 255f, 37f / 255f, 41f / 255f));
            // Zombie Eyes (#FF2200):
            GetOrCreateMaterial("Mat_Zombie_Eyes", new Color(1.0f, 0.13f, 0.0f));

            // Throwables:
            GetOrCreateMaterial("Mat_SoundBomb", new Color(224f / 255f, 176f / 255f, 32f / 255f));
            GetOrCreateMaterial("Mat_DistractionBottle", new Color(0.2f, 0.8f, 0.5f, 0.8f), isTransparent: true);
        }

        public static void CreateItemDataAssets(GameObject distractionPrefab, GameObject soundBombPrefab)
        {
            CreateSingleItem("item_canned_food", "Konserve Yiyecek", "Besleyici et konservesi. Açlığı giderir.", ItemType.Food, 10, health: 5f, hunger: 40f);
            CreateSingleItem("item_water_bottle", "Su Şişesi", "Temiz arıtılmış içme suyu. Susuzluğu giderir.", ItemType.Water, 10, thirst: 50f);
            CreateSingleItem("item_medkit", "İlk Yardım Kiti", "Tıbbi kiti. Sağlığı yeniler, enfeksiyonu düşürür.", ItemType.Medicine, 5, health: 50f, infection: -25f);
            CreateSingleItem("item_antiviral", "Deneysel Antiviral Serum", "Enfeksiyonu hücresel düzeyde durduran deneysel antiviral serum.", ItemType.Medicine, 1, health: 30f, infection: -60f);

            ItemData distBottle = CreateSingleItem("item_distraction_bottle", "Dikkat Dağıtıcı Şişe", "Zombileri sese çeken şişe.", ItemType.Distraction, 10, noiseRadius: 16f);
            if (distBottle != null && distractionPrefab != null)
            {
                distBottle.throwPrefab = distractionPrefab;
                EditorUtility.SetDirty(distBottle);
            }

            CreateSingleItem("item_cloth", "Bez Parçası", "Temiz kumaş parçası.", ItemType.Resource, 20);
            CreateSingleItem("item_alcohol", "Alkol", "Dezenfektan saf alkol.", ItemType.Resource, 10);
            CreateSingleItem("item_scrap", "Hurda Metal", "Kullanışlı metal parçaları.", ItemType.Resource, 20);
            CreateSingleItem("item_battery", "El Feneri Pili", "Feneri şarj eden pil.", ItemType.Resource, 10);

            CreateSingleItem("item_bandage", "Sargı Bezi", "Kanamayı durdurur ve can yeniler.", ItemType.Medicine, 10, health: 25f, infection: -15f);
            ItemData shivItem = CreateSingleItem("item_shiv", "Derme Çatma Bıçak", "Hurda ve kumaştan yapılan tek kullanımlık gizli suikast bıçağı.", ItemType.Weapon, 5);
            if (shivItem != null)
            {
                shivItem.weaponDamage = 60f;
                shivItem.maxDurability = 1;
                EditorUtility.SetDirty(shivItem);
            }

            ItemData crowbarItem = CreateSingleItem("item_crowbar", "Ağır Levye", "Zombilere karşı ağır hasar veren dayanıklı savunma aleti.", ItemType.Weapon, 1);
            if (crowbarItem != null)
            {
                crowbarItem.weaponDamage = 45f;
                crowbarItem.maxDurability = 6;
                EditorUtility.SetDirty(crowbarItem);
            }

            ItemData sbItem = CreateSingleItem("item_sound_bomb", "Gelişmiş Ses Bombası", "5 sn boyunca bip sesi çıkaran ses bombası.", ItemType.Distraction, 5, noiseRadius: 22f);
            if (sbItem != null && soundBombPrefab != null)
            {
                sbItem.throwPrefab = soundBombPrefab;
                EditorUtility.SetDirty(sbItem);
            }

            // Phase 8 Weapons & Ammo
            ItemData axeItem = CreateSingleItem("weapon_axe", "İtfaiye Baltası", "Ağır hasar veren sessiz yakın dövüş baltası.", ItemType.Weapon, 1);
            if (axeItem != null)
            {
                axeItem.weaponDamage = 55f;
                axeItem.maxDurability = 30;
                EditorUtility.SetDirty(axeItem);
            }

            ItemData pistolItem = CreateSingleItem("weapon_pistol", "9mm Taktik Tabanca", "Lazer nişangahlı 12 mermi kapasiteli taktik tabanca.", ItemType.Weapon, 1);
            if (pistolItem != null)
            {
                pistolItem.weaponDamage = 35f;
                pistolItem.maxDurability = 100;
                EditorUtility.SetDirty(pistolItem);
            }

            ItemData shotgunItem = CreateSingleItem("weapon_shotgun", "Pompalı Av Tüfeği", "Yakın mesafede zombileri deviren 6 fişek kapasiteli av tüfeği.", ItemType.Weapon, 1);
            if (shotgunItem != null)
            {
                shotgunItem.weaponDamage = 90f;
                shotgunItem.maxDurability = 50;
                EditorUtility.SetDirty(shotgunItem);
            }

            CreateSingleItem("ammo_9mm", "9mm Mermi Kutusu", "Tabanca için 9mm kurşun mühimmatı.", ItemType.Resource, 60);
            CreateSingleItem("ammo_shells", "Pompalı Fişek Kutusu", "Pompalı tüfek için 12 kalibre saçma fişeği.", ItemType.Resource, 30);
        }

        private static ItemData CreateSingleItem(
            string id, string displayName, string desc, ItemType type, int maxStack,
            float health = 0, float hunger = 0, float thirst = 0, float infection = 0, float noiseRadius = 0)
        {
            string pathData = $"Assets/_Project/Data/Items/{id}.asset";
            string pathRes = $"Assets/_Project/Resources/Items/{id}.asset";

            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(pathData);
            if (item == null) item = ScriptableObject.CreateInstance<ItemData>();

            item.itemId = id;
            item.itemName = displayName;
            item.description = desc;
            item.itemType = type;
            item.maxStack = maxStack;
            item.healthEffect = health;
            item.hungerEffect = hunger;
            item.thirstEffect = thirst;
            item.infectionEffect = infection;
            item.distractionNoiseRadius = noiseRadius;

            Sprite itemSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/UI/Sprites/{id}.png");
            if (itemSprite != null)
            {
                item.icon = itemSprite;
            }

            if (!File.Exists(pathData)) AssetDatabase.CreateAsset(item, pathData);
            else EditorUtility.SetDirty(item);

            if (File.Exists(pathRes)) AssetDatabase.DeleteAsset(pathRes);
            AssetDatabase.CopyAsset(pathData, pathRes);

            return item;
        }

        public static void CreateCraftingRecipes()
        {
            ItemData cloth = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_cloth.asset");
            ItemData alcohol = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_alcohol.asset");
            ItemData scrap = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_scrap.asset");
            ItemData battery = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_battery.asset");
            ItemData bandage = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_bandage.asset");
            ItemData soundBomb = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_sound_bomb.asset");
            ItemData shiv = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_shiv.asset");
            ItemData crowbar = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_crowbar.asset");

            CreateSingleRecipe("recipe_bandage", "Sargı Bezi", "2 Bez + 1 Alkol ile sargı bezi üretir.", bandage, 1, new (ItemData, int)[] { (cloth, 2), (alcohol, 1) });
            CreateSingleRecipe("recipe_sound_bomb", "Gelişmiş Ses Bombası", "2 Hurda + 1 Pil ile ses bombası üretir.", soundBomb, 1, new (ItemData, int)[] { (scrap, 2), (battery, 1) });
            CreateSingleRecipe("recipe_shiv", "Derme Çatma Bıçak", "1 Hurda + 1 Bez ile gizlilik bıçağı üretir.", shiv, 1, new (ItemData, int)[] { (scrap, 1), (cloth, 1) });
            CreateSingleRecipe("recipe_crowbar", "Ağır Levye", "3 Hurda + 1 Bez ile ağır savunma levyesi üretir.", crowbar, 1, new (ItemData, int)[] { (scrap, 3), (cloth, 1) });
        }

        private static void CreateSingleRecipe(string id, string name, string desc, ItemData result, int resultCount, (ItemData item, int count)[] ingredients)
        {
            string pathData = $"Assets/_Project/Data/Recipes/{id}.asset";
            string pathRes = $"Assets/_Project/Resources/Recipes/{id}.asset";

            CraftingRecipe r = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(pathData);
            if (r == null) r = ScriptableObject.CreateInstance<CraftingRecipe>();

            r.recipeId = id;
            r.recipeName = name;
            r.description = desc;
            r.resultItem = result;
            r.resultCount = resultCount;
            r.ingredients.Clear();

            foreach (var ing in ingredients)
            {
                if (ing.item != null) r.ingredients.Add(new IngredientEntry { item = ing.item, count = ing.count });
            }

            if (!File.Exists(pathData)) AssetDatabase.CreateAsset(r, pathData);
            else EditorUtility.SetDirty(r);

            if (!File.Exists(pathRes)) AssetDatabase.CopyAsset(pathData, pathRes);
        }

        private static GameObject CreateDistractionPrefab()
        {
            GameObject root = new GameObject("DistractionBottle_Root");
            SphereCollider col = root.AddComponent<SphereCollider>();
            col.radius = 0.25f;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.5f;

            root.AddComponent<ThrownDistraction>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual_Model";
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_DistractionBottle.mat");
            if (mat != null) visual.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DistractionPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateSoundBombPrefab()
        {
            GameObject root = new GameObject("SoundBomb_Root");
            SphereCollider col = root.AddComponent<SphereCollider>();
            col.radius = 0.25f;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.6f;

            root.AddComponent<SoundBomb>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual_Model";
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            GameObject led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            led.name = "Blinking_LED";
            led.transform.SetParent(visual.transform);
            led.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            led.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            Object.DestroyImmediate(led.GetComponent<Collider>());

            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_SoundBomb.mat");
            if (mat != null) visual.GetComponent<Renderer>().sharedMaterial = mat;

            Material ledMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Zombie_Eyes.mat");
            if (ledMat != null) led.GetComponent<Renderer>().sharedMaterial = ledMat;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SoundBombPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePlayerPrefab(GameObject distractionPrefab)
        {
            GameObject root = new GameObject("Player_Root");
            root.tag = "Player";

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 2.0f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0, 1.0f, 0);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

            NoiseEmitter ne = root.AddComponent<NoiseEmitter>();
            StealthSystem ss = root.AddComponent<StealthSystem>();
            SurvivalStats stats = root.AddComponent<SurvivalStats>();
            InventorySystem inv = root.AddComponent<InventorySystem>();
            PlayerController pc = root.AddComponent<PlayerController>();
            FlashlightController fc = root.AddComponent<FlashlightController>();
            PlayerAnimationBridge animBridge = root.AddComponent<PlayerAnimationBridge>();
            CombatSystem combat = root.AddComponent<CombatSystem>();
            WeaponSystem ws = root.AddComponent<WeaponSystem>();
            WeaponFeelController feel = root.AddComponent<WeaponFeelController>();

            // FAZ 6: Articulated 3D Humanoid Survivor Model with Animator
            GameObject visual = ProceduralHumanoidBuilder.CreateHumanoidModel(root.transform, "Visual_Model", ProceduralHumanoidBuilder.PlayerConfig());
            Animator playerAnimator = visual.AddComponent<Animator>();
            playerAnimator.runtimeAnimatorController = AnimatorBuilder.CreatePlayerAnimatorController();

            GameObject lightObj = new GameObject("Flashlight_SpotLight");
            lightObj.transform.SetParent(root.transform);
            lightObj.transform.localPosition = new Vector3(0.2f, 1.35f, 0.4f);
            lightObj.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);

            Light spot = lightObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 55f;
            spot.innerSpotAngle = 40f;
            spot.range = 26f;
            spot.intensity = 2.8f;
            spot.color = new Color(1f, 0.96f, 0.85f);
            spot.shadows = LightShadows.Soft;
            spot.enabled = false;

            SerializedObject soFc = new SerializedObject(fc);
            soFc.FindProperty("spotLight").objectReferenceValue = spot;
            soFc.ApplyModifiedProperties();

            // Load initial items
            ItemData food = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_canned_food.asset");
            ItemData water = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_water_bottle.asset");
            ItemData medkit = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_medkit.asset");
            ItemData bottle = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_distraction_bottle.asset");

            SerializedObject soInv = new SerializedObject(inv);
            SerializedProperty initItemsProp = soInv.FindProperty("initialItems");
            initItemsProp.ClearArray();
            if (food != null) { initItemsProp.InsertArrayElementAtIndex(0); initItemsProp.GetArrayElementAtIndex(0).objectReferenceValue = food; }
            if (water != null) { initItemsProp.InsertArrayElementAtIndex(1); initItemsProp.GetArrayElementAtIndex(1).objectReferenceValue = water; }
            if (medkit != null) { initItemsProp.InsertArrayElementAtIndex(2); initItemsProp.GetArrayElementAtIndex(2).objectReferenceValue = medkit; }
            if (bottle != null) { initItemsProp.InsertArrayElementAtIndex(3); initItemsProp.GetArrayElementAtIndex(3).objectReferenceValue = bottle; }
            soInv.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateStandardZombiePrefab()
        {
            ZombiePrefabBuilder.SetupMaterialsAndTextures();
            ZombiePrefabBuilder.ConfigureAnimationClips();
            return ZombiePrefabBuilder.BuildStandardZombiePrefab();
        }

        private static GameObject CreateRunnerZombiePrefab()
        {
            return ZombiePrefabBuilder.BuildRunnerZombiePrefab();
        }

        private static GameObject CreateScreamerZombiePrefab()
        {
            return ZombiePrefabBuilder.BuildScreamerZombiePrefab();
        }

        private static GameObject CreateTankZombiePrefab()
        {
            return ZombiePrefabBuilder.BuildTankZombiePrefab();
        }

        private static GameObject BuildZombiePrefab(
            string name, string prefabPath, ZombieType type, Vector3 scale, Material bodyMat,
            float hp, float chaseSpd, float patrolSpd, float dmg, float infect, float viewDist, float hearingMult, float attackRange = 1.4f)
        {
            GameObject root = new GameObject(name);
            root.tag = "Respawn";

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = chaseSpd;
            agent.radius = 0.45f * scale.x;
            agent.height = 2.0f * scale.y;
            agent.stoppingDistance = attackRange * 0.9f;

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.0f * scale.y, 0);
            col.radius = 0.45f * scale.x;
            col.height = 2.0f * scale.y;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

            ZombieSensor sensor = root.AddComponent<ZombieSensor>();
            sensor.ViewDistance = viewDist;
            sensor.HearingMultiplier = hearingMult;

            ZombieAnimationBridge animBridge = root.AddComponent<ZombieAnimationBridge>();
            ZombieLocomotionEngine loco = root.AddComponent<ZombieLocomotionEngine>();
            ZombieAI ai = root.AddComponent<ZombieAI>();

            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("zombieType").enumValueIndex = (int)type;
            soAi.FindProperty("maxHealth").floatValue = hp;
            soAi.FindProperty("currentHealth").floatValue = hp;
            soAi.FindProperty("chaseSpeed").floatValue = chaseSpd;
            soAi.FindProperty("patrolSpeed").floatValue = patrolSpd;
            soAi.FindProperty("attackDamage").floatValue = dmg;
            soAi.FindProperty("infectionInflicted").floatValue = infect;
            soAi.FindProperty("attackRange").floatValue = attackRange;
            soAi.ApplyModifiedProperties();

            // FAZ 6: Articulated 3D Zombie Models (Standard, Runner, Screamer, Tank) with Animator
            ProceduralHumanoidBuilder.HumanoidConfig zConfig;
            switch (type)
            {
                case ZombieType.Runner:
                    zConfig = ProceduralHumanoidBuilder.ZombieRunnerConfig();
                    break;
                case ZombieType.Screamer:
                    zConfig = ProceduralHumanoidBuilder.ZombieScreamerConfig();
                    break;
                case ZombieType.Tank:
                    zConfig = ProceduralHumanoidBuilder.ZombieTankConfig();
                    break;
                default:
                    zConfig = ProceduralHumanoidBuilder.ZombieStandardConfig();
                    break;
            }

            GameObject visual = ProceduralHumanoidBuilder.CreateHumanoidModel(root.transform, "Visual_Model", zConfig);
            visual.transform.localScale = scale;
            Animator zAnim = visual.AddComponent<Animator>();
            zAnim.runtimeAnimatorController = AnimatorBuilder.CreateZombieAnimatorController();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateLootContainerPrefab()
        {
            GameObject root = new GameObject("LootContainer_Root");
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2.2f, 2.0f, 2.2f);
            col.center = new Vector3(0, 1.0f, 0);

            LootContainer lc = root.AddComponent<LootContainer>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual_Model";
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = new Vector3(0, 0.5f, 0);
            visual.transform.localScale = new Vector3(1.2f, 1.0f, 0.8f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            GameObject metalBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            metalBand.name = "Metal_Lock";
            metalBand.transform.SetParent(visual.transform);
            metalBand.transform.localPosition = new Vector3(0, 0.1f, 0.52f);
            metalBand.transform.localScale = new Vector3(0.3f, 0.3f, 0.1f);
            Object.DestroyImmediate(metalBand.GetComponent<Collider>());

            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Cover_Wood.mat");
            if (woodMat != null) visual.GetComponent<Renderer>().sharedMaterial = woodMat;

            Material metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Chest_Metal.mat");
            if (metalMat != null) metalBand.GetComponent<Renderer>().sharedMaterial = metalMat;

            ItemData food = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_canned_food.asset");
            ItemData water = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_water_bottle.asset");
            ItemData bottle = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_distraction_bottle.asset");
            ItemData cloth = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_cloth.asset");
            ItemData alcohol = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_alcohol.asset");
            ItemData scrap = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_scrap.asset");
            ItemData battery = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_battery.asset");

            SerializedObject so = new SerializedObject(lc);
            SerializedProperty lootList = so.FindProperty("possibleLoot");
            lootList.ClearArray();

            void AddEntry(ItemData it, int count, float chance)
            {
                if (it == null) return;
                int idx = lootList.arraySize;
                lootList.InsertArrayElementAtIndex(idx);
                SerializedProperty elem = lootList.GetArrayElementAtIndex(idx);
                elem.FindPropertyRelative("item").objectReferenceValue = it;
                elem.FindPropertyRelative("count").intValue = count;
                elem.FindPropertyRelative("dropChance").floatValue = chance;
            }

            AddEntry(food, 1, 0.75f);
            AddEntry(water, 1, 0.75f);
            AddEntry(bottle, 1, 0.6f);
            AddEntry(cloth, 2, 0.9f);
            AddEntry(alcohol, 1, 0.8f);
            AddEntry(scrap, 2, 0.85f);
            AddEntry(battery, 1, 0.7f);
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, LootPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateNPCDrArasPrefab()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_NPC_DrAras.mat");
            return BuildNPCPrefab("NPC_DrAras_Root", NPCDrArasPrefabPath, "aras", "Dr. Aras", "chapter1_outbreak", "node_doc_intro", mat, false);
        }

        private static GameObject CreateNPCZeynepPrefab()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_NPC_Zeynep.mat");
            return BuildNPCPrefab("NPC_Zeynep_Root", NPCZeynepPrefabPath, "zeynep", "Zeynep", "chapter1_outbreak", "node_zeynep_encounter", mat, false, addWoundedBrother: true);
        }

        private static GameObject CreateNPCKeremPrefab()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_NPC_Kerem.mat");
            return BuildNPCPrefab("NPC_Kerem_Root", NPCKeremPrefabPath, "kerem", "Kerem (Hurda Toplayıcısı)", "sidequest_kerem_tank", "node_kerem_intro", mat, false);
        }

        private static GameObject CreateNPCElifPrefab()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_NPC_Elif.mat");
            return BuildNPCPrefab("NPC_Elif_Root", NPCElifPrefabPath, "elif", "Elif (Kurye)", "sidequest_elif_rescue", "node_elif_intro", mat, true);
        }

        private static GameObject CreateNPCScavengerPrefab()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Street.mat");
            return BuildNPCPrefab("Survivor_Scavenger_Root", NPCScavengerPrefabPath, "scavenger", "Hayatta Kalan Gezgin", "chapter1_outbreak", "node_doc_intro", mat, false);
        }

        private static GameObject BuildNPCPrefab(
            string name, string prefabPath, string npcId, string displayName,
            string storyResource, string startNodeId, Material bodyMat, bool hasFollower, bool addWoundedBrother = false)
        {
            GameObject root = new GameObject(name);
            root.tag = "Untagged";

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 3.8f;
            agent.radius = 0.45f;
            agent.height = 2.0f;
            agent.stoppingDistance = 2.0f;

            CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
            cc.height = 2.0f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0, 1.0f, 0);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

            SphereCollider sc = root.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.8f;
            sc.center = new Vector3(0, 1.0f, 0);

            InteractableNPC interactable = root.AddComponent<InteractableNPC>();
            SerializedObject soNpc = new SerializedObject(interactable);
            soNpc.FindProperty("npcId").stringValue = npcId;
            soNpc.FindProperty("npcDisplayName").stringValue = displayName;
            soNpc.FindProperty("storyResourceName").stringValue = storyResource;
            soNpc.FindProperty("startNodeId").stringValue = startNodeId;
            soNpc.FindProperty("interactionRadius").floatValue = 2.8f;
            soNpc.ApplyModifiedProperties();

            // Faz 2: Autonomous Survivor AI, Threat Sensor & Procedural Motion
            root.AddComponent<NPCSensor>();
            root.AddComponent<NPCProceduralMotion>();
            SurvivorNPC_AI survivorAi = root.AddComponent<SurvivorNPC_AI>();

            SerializedObject soAi = new SerializedObject(survivorAi);
            soAi.FindProperty("npcId").stringValue = npcId;
            soAi.FindProperty("npcDisplayName").stringValue = displayName;

            NPCArchetype arch = NPCArchetype.Armed;
            if (npcId == "aras" || npcId == "zeynep") arch = NPCArchetype.Medic;
            soAi.FindProperty("archetype").enumValueIndex = (int)arch;
            soAi.ApplyModifiedProperties();

            if (hasFollower)
            {
                root.AddComponent<CivilianFollower>();
            }

            // FAZ 6: Articulated 3D Story NPC Models with Distinct Clothing & Animator
            ProceduralHumanoidBuilder.HumanoidConfig npcConfig;
            switch (npcId)
            {
                case "aras":
                    npcConfig = ProceduralHumanoidBuilder.DrArasConfig();
                    break;
                case "zeynep":
                    npcConfig = ProceduralHumanoidBuilder.ZeynepConfig();
                    break;
                case "kerem":
                    npcConfig = ProceduralHumanoidBuilder.KeremConfig();
                    break;
                case "elif":
                    npcConfig = ProceduralHumanoidBuilder.ElifConfig();
                    break;
                case "scavenger":
                    npcConfig = ProceduralHumanoidBuilder.ScavengerConfig();
                    break;
                default:
                    npcConfig = ProceduralHumanoidBuilder.PlayerConfig();
                    break;
            }

            GameObject visual = ProceduralHumanoidBuilder.CreateHumanoidModel(root.transform, "Visual_Model", npcConfig);
            Animator npcAnim = visual.AddComponent<Animator>();
            npcAnim.runtimeAnimatorController = AnimatorBuilder.CreatePlayerAnimatorController();

            if (addWoundedBrother)
            {
                GameObject brother = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                brother.name = "Brother_Visual";
                brother.transform.SetParent(root.transform);
                brother.transform.localPosition = new Vector3(0.9f, 0.25f, 0f);
                brother.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                brother.transform.localScale = new Vector3(0.65f, 0.75f, 0.65f);
                Object.DestroyImmediate(brother.GetComponent<Collider>());
                Material brotherMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Street.mat");
                if (brotherMat != null) brother.GetComponent<Renderer>().sharedMaterial = brotherMat;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildScene(
            GameObject playerPrefab,
            GameObject zombieStandardPrefab,
            GameObject zombieRunnerPrefab,
            GameObject zombieScreamerPrefab,
            GameObject zombieTankPrefab,
            GameObject lootPrefab,
            GameObject soundBombPrefab,
            GameObject distractionPrefab,
            GameObject npcDrArasPrefab,
            GameObject npcZeynepPrefab,
            GameObject npcKeremPrefab,
            GameObject npcElifPrefab,
            GameObject npcScavengerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- 1. LIGHTING & ATMOSPHERE (Cinematic Procedural Skybox & Moonlit Night) ---
            Material skyboxMat = EnsureSkyboxMaterial();
            RenderSettings.skybox = skyboxMat;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.137f, 0.176f, 0.247f);     // Sky: #232D3F
            RenderSettings.ambientEquatorColor = new Color(0.102f, 0.125f, 0.173f); // Equator: #1A202C
            RenderSettings.ambientGroundColor = new Color(0.059f, 0.075f, 0.102f);  // Ground: #0F131A
            DynamicGI.UpdateEnvironment();

            GameObject dirLight = new GameObject("Directional Light");
            Light lightComp = dirLight.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.intensity = 0.75f;
            lightComp.color = new Color(0.478f, 0.612f, 0.776f); // Cold overcast moonlight #7A9CC6
            lightComp.shadows = LightShadows.Soft;
            lightComp.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
            lightComp.shadowBias = 0.04f;
            lightComp.shadowNormalBias = 0.35f;
            dirLight.transform.rotation = Quaternion.Euler(46f, -38f, 0f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.086f, 0.106f, 0.141f); // Deep atmospheric horizon #161B24
            RenderSettings.fogDensity = 0.011f;

            // Global URP Cinematic Post-Processing Volume (ACES Tonemapping & Bloom)
            GameObject volumeObj = new GameObject("Global_PostProcess_Volume");
            Volume vol = volumeObj.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            if (!Directory.Exists("Assets/_Project/Settings")) Directory.CreateDirectory("Assets/_Project/Settings");
            string profilePath = "Assets/_Project/Settings/GlobalPostProcessProfile.asset";
            VolumeProfile existingProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (existingProfile == null)
            {
                AssetDatabase.CreateAsset(profile, profilePath);
                existingProfile = profile;
            }

            if (!existingProfile.Has<Bloom>())
            {
                Bloom bloom = existingProfile.Add<Bloom>(true);
                bloom.threshold.Override(1.05f);
                bloom.intensity.Override(0.35f);
                bloom.scatter.Override(0.65f);
            }
            else
            {
                if (existingProfile.TryGet<Bloom>(out Bloom bloom))
                {
                    bloom.threshold.Override(1.05f);
                    bloom.intensity.Override(0.35f);
                    bloom.scatter.Override(0.65f);
                }
            }

            if (!existingProfile.Has<Tonemapping>())
            {
                Tonemapping tone = existingProfile.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.ACES);
            }

            if (!existingProfile.Has<ColorAdjustments>())
            {
                ColorAdjustments ca = existingProfile.Add<ColorAdjustments>(true);
                ca.contrast.Override(14f);
                ca.saturation.Override(-8f);
                ca.postExposure.Override(0f);
            }
            else
            {
                if (existingProfile.TryGet<ColorAdjustments>(out ColorAdjustments ca))
                {
                    ca.contrast.Override(14f);
                    ca.saturation.Override(-8f);
                    ca.postExposure.Override(0f);
                }
            }

            if (!existingProfile.Has<Vignette>())
            {
                Vignette vig = existingProfile.Add<Vignette>(true);
                vig.color.Override(Color.black);
                vig.intensity.Override(0.24f);
                vig.smoothness.Override(0.45f);
            }
            else
            {
                if (existingProfile.TryGet<Vignette>(out Vignette vig))
                {
                    vig.color.Override(Color.black);
                    vig.intensity.Override(0.24f);
                    vig.smoothness.Override(0.45f);
                }
            }

            vol.sharedProfile = existingProfile;

            // --- 2. ENVIRONMENT (Expanded Metropolis & Diverse Vehicle Fleet - FAZ 6) ---
            GameObject envParent = new GameObject("Environment");

            // Build Comprehensive 240x240m City: Roads, 8 Skyscrapers (15-40 floors), Houses, Apartments, Gas Station, Walk-in Clinic, SafeHouse, Nature, Fire Barrels
            CityLayoutBuilder.BuildExpandedCity(envParent.transform, lootPrefab);

            // Place Diverse 7-Type Vehicle Fleet (25+ vehicles: Police, Taxis, Ambulances, Sedans, SUVs, Vans, Heavy Truck Barricades)
            VehicleFleetBuilder.PlaceVehicleFleet(envParent.transform);

            // Phase 5 Atmospheric City Overgrowth & Weathering
            AbandonedSurfaceBuilder.BuildAbandonedCitySurfaces(envParent.transform);
            OvergrownIvyGenerator.GenerateUrbanIvy(envParent.transform);
            SpiderwebGenerator.GenerateCobwebsAndDustMotes(envParent.transform);

            // 3D Waypoint Beacon (Light column beaming to the sky + floating distance)
            GameObject beaconObj = new GameObject("Waypoint_Beacon");
            beaconObj.transform.position = new Vector3(0.5f, 0.4f, -22f);
            beaconObj.AddComponent<WaypointBeacon>();

            // --- 3. MANAGERS ---
            GameObject managersObj = new GameObject("--- MANAGERS ---");
            GameManager gm = managersObj.AddComponent<GameManager>();
            SettingsManager smgr = managersObj.AddComponent<SettingsManager>();
            ObjectPooler pooler = managersObj.AddComponent<ObjectPooler>();
            ChoiceProcessor cp = managersObj.AddComponent<ChoiceProcessor>();
            MissionManager mm = managersObj.AddComponent<MissionManager>();
            StoryManager sm = managersObj.AddComponent<StoryManager>();
            CraftingSystem cs = managersObj.AddComponent<CraftingSystem>();
            DayNightManager dnm = managersObj.AddComponent<DayNightManager>();
            ProceduralAudioManager pam = managersObj.AddComponent<ProceduralAudioManager>();
            FeedbackEffects fe = managersObj.AddComponent<FeedbackEffects>();
            RandomEncounterManager rem = managersObj.AddComponent<RandomEncounterManager>();

            DynamicEventDirector ded = managersObj.AddComponent<DynamicEventDirector>();
            SerializedObject soDed = new SerializedObject(ded);
            soDed.FindProperty("lootCratePrefab").objectReferenceValue = lootPrefab;
            soDed.FindProperty("zombieStandardPrefab").objectReferenceValue = zombieStandardPrefab;
            soDed.FindProperty("zombieRunnerPrefab").objectReferenceValue = zombieRunnerPrefab;
            soDed.ApplyModifiedProperties();

            MobilePerformanceOptimizer mpo = managersObj.AddComponent<MobilePerformanceOptimizer>();

            // Setup ObjectPooler definitions
            SerializedObject soPooler = new SerializedObject(pooler);
            SerializedProperty poolsProp = soPooler.FindProperty("poolDefinitions");
            poolsProp.ClearArray();

            void AddPool(string tag, GameObject pfb, int count)
            {
                if (pfb == null) return;
                int idx = poolsProp.arraySize;
                poolsProp.InsertArrayElementAtIndex(idx);
                SerializedProperty p = poolsProp.GetArrayElementAtIndex(idx);
                p.FindPropertyRelative("tag").stringValue = tag;
                p.FindPropertyRelative("prefab").objectReferenceValue = pfb;
                p.FindPropertyRelative("initialSize").intValue = count;
                p.FindPropertyRelative("expandable").boolValue = true;
            }

            AddPool("Zombie_Standard", zombieStandardPrefab, 6);
            AddPool("Zombie_Runner", zombieRunnerPrefab, 4);
            AddPool("Zombie_Screamer", zombieScreamerPrefab, 3);
            AddPool("Zombie_Tank", zombieTankPrefab, 3);
            AddPool("Distraction_Bottle", distractionPrefab, 5);
            AddPool("Sound_Bomb", soundBombPrefab, 5);
            soPooler.ApplyModifiedProperties();

            // Register Crafting Recipes
            CraftingRecipe rBandage = AssetDatabase.LoadAssetAtPath<CraftingRecipe>("Assets/_Project/Data/Recipes/recipe_bandage.asset");
            CraftingRecipe rSoundBomb = AssetDatabase.LoadAssetAtPath<CraftingRecipe>("Assets/_Project/Data/Recipes/recipe_sound_bomb.asset");
            CraftingRecipe rShiv = AssetDatabase.LoadAssetAtPath<CraftingRecipe>("Assets/_Project/Data/Recipes/recipe_shiv.asset");
            CraftingRecipe rCrowbar = AssetDatabase.LoadAssetAtPath<CraftingRecipe>("Assets/_Project/Data/Recipes/recipe_crowbar.asset");
            if (rBandage != null) cs.RegisterRecipe(rBandage);
            if (rSoundBomb != null) cs.RegisterRecipe(rSoundBomb);
            if (rShiv != null) cs.RegisterRecipe(rShiv);
            if (rCrowbar != null) cs.RegisterRecipe(rCrowbar);

            // --- 4. PLAYER INSTANTIATION ---
            GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            player.name = "Player_Root";
            player.transform.position = new Vector3(0f, 0.1f, -28f);

            WeaponFeelController feel = player.GetComponent<WeaponFeelController>() ?? player.AddComponent<WeaponFeelController>();
            Transform socket = player.transform.Find("Weapon_Socket") ?? player.transform.Find("Visual_Model/Weapon_Socket");
            if (socket != null) feel.SetWeaponSocket(socket);

            // Camera Setup: AAA Over-The-Shoulder Third Person Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camObj.AddComponent<AudioListener>();
            ThirdPersonCamera tpc = camObj.AddComponent<ThirdPersonCamera>();
            tpc.SetTarget(player.transform);
            camObj.transform.position = player.transform.position + new Vector3(0.65f, 1.65f, -3.2f);
            camObj.transform.rotation = Quaternion.identity;

            // --- 5. LOOT CONTAINERS ---
            GameObject lootParent = new GameObject("--- LOOT CONTAINERS ---");
            Vector3[] lootPositions = new Vector3[] {
                new Vector3(-33f, 0f, 34f),  // Clinic Interior Medical Stash
                new Vector3(34f, 0f, 32f),   // Supermarket Aisle Shelf
                new Vector3(28f, 0f, 28f),   // Supermarket Cash Counter
                new Vector3(-36f, 0f, -30f), // SafeHouse Secure Stash
                new Vector3(30f, 0f, -46f),  // Pharmacy Medicine Counter
                new Vector3(56f, 0f, -33f),  // Gas Station Supply Cabinet
                new Vector3(0f, 0f, 0f)      // Central Metropolis Crossroads
            };

            for (int i = 0; i < lootPositions.Length; i++)
            {
                GameObject lootInstance = PrefabUtility.InstantiatePrefab(lootPrefab) as GameObject;
                lootInstance.name = $"LootContainer_{i + 1}";
                lootInstance.transform.SetParent(lootParent.transform);
                lootInstance.transform.position = lootPositions[i];
            }

            // Kerem's Quest Target: Military Supply Chest guarded by Tank
            GameObject militaryChest = PrefabUtility.InstantiatePrefab(lootPrefab) as GameObject;
            militaryChest.name = "LootContainer_Military_Kerem";
            militaryChest.transform.SetParent(lootParent.transform);
            militaryChest.transform.position = new Vector3(20f, 0f, -36f);
            LootContainer chestLc = militaryChest.GetComponent<LootContainer>();
            if (chestLc != null)
            {
                SerializedObject soChest = new SerializedObject(chestLc);
                SerializedProperty lootList = soChest.FindProperty("possibleLoot");
                lootList.ClearArray();
                ItemData crowbar = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_crowbar.asset");
                ItemData scrap = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_scrap.asset");
                ItemData medkit = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_medkit.asset");
                void AddChestItem(ItemData it, int cnt, float chance)
                {
                    if (it == null) return;
                    int idx = lootList.arraySize;
                    lootList.InsertArrayElementAtIndex(idx);
                    SerializedProperty elem = lootList.GetArrayElementAtIndex(idx);
                    elem.FindPropertyRelative("item").objectReferenceValue = it;
                    elem.FindPropertyRelative("count").intValue = cnt;
                    elem.FindPropertyRelative("dropChance").floatValue = chance;
                }
                AddChestItem(crowbar, 1, 1.0f);
                AddChestItem(scrap, 3, 1.0f);
                AddChestItem(medkit, 1, 0.8f);
                soChest.ApplyModifiedProperties();
            }

            // --- 6. ZOMBIES (28 Active Zombies: 12 Lone Roamers, 10 Scavengers, 6 Dormant Lurkers) ---
            GameObject zombieParent = new GameObject("--- ZOMBIES ---");
            zombieParent.AddComponent<ZombieDirector>();
            zombieParent.AddComponent<FlankingCoordinator>();
            zombieParent.AddComponent<ZombiePackManager>();
            zombieParent.AddComponent<HordeAlertSystem>();
            DistrictZombieEcology ecologyComp = zombieParent.AddComponent<DistrictZombieEcology>();
            ecologyComp.InitializeZones();
            zombieParent.AddComponent<NocturnalZombieEscalator>();

            // 1. Lone Roamers (12 active street/sidewalk/roundabout roamers)
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Roamer_BoulevardCentral", new Vector3(0f, 0.1f, -15f), ZombieArchetype.LoneRoamer);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Roamer_BoulevardNorth", new Vector3(1.5f, 0.1f, 15f), ZombieArchetype.LoneRoamer);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Roamer_BoulevardSouth", new Vector3(-2f, 0.1f, -45f), ZombieArchetype.LoneRoamer);
            InstantiateZombie(zombieRunnerPrefab, zombieParent.transform, "Zombie_Roamer_RoundaboutEast", new Vector3(25f, 0.1f, -5f), ZombieArchetype.LoneRoamer);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Roamer_RoundaboutWest", new Vector3(-25f, 0.1f, 0f), ZombieArchetype.LoneRoamer);
            InstantiateZombie(zombieScreamerPrefab, zombieParent.transform, "Zombie_Roamer_NorthPlaza", new Vector3(12f, 0.1f, 55f), ZombieArchetype.LoneRoamer);
            InstantiateZombie(zombieTankPrefab, zombieParent.transform, "Zombie_Roamer_DowntownSidewalk", new Vector3(-15f, 0.1f, 30f), ZombieArchetype.LoneRoamer);
            InstantiateZombie(zombieRunnerPrefab, zombieParent.transform, "Zombie_Roamer_CommercialMarket", new Vector3(20f, 0.1f, 25f), ZombieArchetype.LoneRoamer);
            InstantiateZombie(zombieRunnerPrefab, zombieParent.transform, "Zombie_Roamer_ResidentialAlley", new Vector3(-45f, 0.1f, -15f), ZombieArchetype.LoneRoamer);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Roamer_HighwayEast", new Vector3(45f, 0.1f, -15f), ZombieArchetype.LoneRoamer);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Roamer_GasStationRoad", new Vector3(52f, 0.1f, -30f), ZombieArchetype.LoneRoamer);
            InstantiateZombie(zombieRunnerPrefab, zombieParent.transform, "Zombie_Roamer_SuburbsAvenue", new Vector3(-55f, 0.1f, 35f), ZombieArchetype.LoneRoamer);

            // 2. Stationary Scavengers (10 hunched over wrecked cruisers, ambulances, vans, dumpsters)
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Scavenger_PoliceCruiserFlare", new Vector3(2.5f, 0.1f, -21f), ZombieArchetype.Scavenger);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Scavenger_PoliceIntersection", new Vector3(4.8f, 0.1f, -11f), ZombieArchetype.Scavenger);
            InstantiateZombie(zombieRunnerPrefab, zombieParent.transform, "Zombie_Scavenger_AmbulanceClinic", new Vector3(-30f, 0.1f, 33f), ZombieArchetype.Scavenger);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Scavenger_AmbulanceDowntown", new Vector3(17f, 0.1f, -24f), ZombieArchetype.Scavenger);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Scavenger_SedanRust", new Vector3(-16.5f, 0.1f, 86f), ZombieArchetype.Scavenger);
            InstantiateZombie(zombieRunnerPrefab, zombieParent.transform, "Zombie_Scavenger_VanSupermarket", new Vector3(49.5f, 0.1f, 29f), ZombieArchetype.Scavenger);
            InstantiateZombie(zombieTankPrefab, zombieParent.transform, "Zombie_Scavenger_OverturnedSUV", new Vector3(-43f, 0.1f, 76f), ZombieArchetype.Scavenger);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Scavenger_DumpsterBoulevard", new Vector3(-8.2f, 0.1f, -17f), ZombieArchetype.Scavenger);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Scavenger_DumpsterPlaza", new Vector3(10.5f, 0.1f, 23f), ZombieArchetype.Scavenger);
            InstantiateZombie(zombieRunnerPrefab, zombieParent.transform, "Zombie_Scavenger_DumpsterMarket", new Vector3(26.5f, 0.1f, 43f), ZombieArchetype.Scavenger);

            // 3. Dormant Lurkers (6 slumped inside Clinic, Police Station, Supermarket, Pharmacy, Residential Houses)
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Lurker_ClinicExamRoom", new Vector3(-33.5f, 0.1f, 34.5f), ZombieArchetype.DormantLurker);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Lurker_PoliceStationCell", new Vector3(-24f, 0.1f, 5.5f), ZombieArchetype.DormantLurker);
            InstantiateZombie(zombieRunnerPrefab, zombieParent.transform, "Zombie_Lurker_SupermarketStorage", new Vector3(38f, 0.1f, 32f), ZombieArchetype.DormantLurker);
            InstantiateZombie(zombieScreamerPrefab, zombieParent.transform, "Zombie_Lurker_PharmacyBack", new Vector3(32.5f, 0.1f, -46f), ZombieArchetype.DormantLurker);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Lurker_ResidentialVictorian1", new Vector3(-66f, 0.1f, -10f), ZombieArchetype.DormantLurker);
            InstantiateZombie(zombieStandardPrefab, zombieParent.transform, "Zombie_Lurker_ResidentialVictorian2", new Vector3(-66f, 0.1f, 14f), ZombieArchetype.DormantLurker);

            // 4. Roaming Zombie Packs (4 Packs, 20 Pack Zombies total)
            // A. Pack 1: Central Roundabout (Leader: Tank)
            GameObject pack1Obj = new GameObject("ZombiePack_Roundabout");
            pack1Obj.transform.SetParent(zombieParent.transform);
            ZombiePack pack1 = pack1Obj.AddComponent<ZombiePack>();
            ZombieAI p1Leader = InstantiateZombie(zombieTankPrefab, pack1Obj.transform, "Pack_Roundabout_Leader_Tank", new Vector3(0f, 0.1f, 0f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>();
            List<ZombieAI> p1Followers = new List<ZombieAI>
            {
                InstantiateZombie(zombieRunnerPrefab, pack1Obj.transform, "Pack_Roundabout_Follower_Runner1", new Vector3(-2.2f, 0.1f, -2.2f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieRunnerPrefab, pack1Obj.transform, "Pack_Roundabout_Follower_Runner2", new Vector3(2.2f, 0.1f, -2.2f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieStandardPrefab, pack1Obj.transform, "Pack_Roundabout_Follower_Standard1", new Vector3(-1.8f, 0.1f, -4.2f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieStandardPrefab, pack1Obj.transform, "Pack_Roundabout_Follower_Standard2", new Vector3(1.8f, 0.1f, -4.2f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>()
            };
            pack1.InitializePack("Pack_Roundabout", p1Leader, p1Followers);

            // B. Pack 2: Main Boulevard (Leader: Screamer)
            GameObject pack2Obj = new GameObject("ZombiePack_Boulevard");
            pack2Obj.transform.SetParent(zombieParent.transform);
            ZombiePack pack2 = pack2Obj.AddComponent<ZombiePack>();
            ZombieAI p2Leader = InstantiateZombie(zombieScreamerPrefab, pack2Obj.transform, "Pack_Boulevard_Leader_Screamer", new Vector3(1f, 0.1f, -32f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>();
            List<ZombieAI> p2Followers = new List<ZombieAI>
            {
                InstantiateZombie(zombieRunnerPrefab, pack2Obj.transform, "Pack_Boulevard_Follower_Runner1", new Vector3(-2.2f, 0.1f, -34.2f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieRunnerPrefab, pack2Obj.transform, "Pack_Boulevard_Follower_Runner2", new Vector3(2.2f, 0.1f, -34.2f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieStandardPrefab, pack2Obj.transform, "Pack_Boulevard_Follower_Standard1", new Vector3(-1.8f, 0.1f, -36.2f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieStandardPrefab, pack2Obj.transform, "Pack_Boulevard_Follower_Standard2", new Vector3(1.8f, 0.1f, -36.2f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>()
            };
            pack2.InitializePack("Pack_Boulevard", p2Leader, p2Followers);

            // C. Pack 3: East Commercial District (Leader: Resilient Standard)
            GameObject pack3Obj = new GameObject("ZombiePack_Commercial");
            pack3Obj.transform.SetParent(zombieParent.transform);
            ZombiePack pack3 = pack3Obj.AddComponent<ZombiePack>();
            ZombieAI p3Leader = InstantiateZombie(zombieStandardPrefab, pack3Obj.transform, "Pack_Commercial_Leader_Standard", new Vector3(36f, 0.1f, 15f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>();
            List<ZombieAI> p3Followers = new List<ZombieAI>
            {
                InstantiateZombie(zombieRunnerPrefab, pack3Obj.transform, "Pack_Commercial_Follower_Runner1", new Vector3(33.8f, 0.1f, 12.8f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieRunnerPrefab, pack3Obj.transform, "Pack_Commercial_Follower_Runner2", new Vector3(38.2f, 0.1f, 12.8f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieStandardPrefab, pack3Obj.transform, "Pack_Commercial_Follower_Standard1", new Vector3(34.2f, 0.1f, 10.8f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieStandardPrefab, pack3Obj.transform, "Pack_Commercial_Follower_Standard2", new Vector3(37.8f, 0.1f, 10.8f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>()
            };
            pack3.InitializePack("Pack_Commercial", p3Leader, p3Followers);

            // D. Pack 4: West Residential Avenues (Leader: Tank)
            GameObject pack4Obj = new GameObject("ZombiePack_Residential");
            pack4Obj.transform.SetParent(zombieParent.transform);
            ZombiePack pack4 = pack4Obj.AddComponent<ZombiePack>();
            ZombieAI p4Leader = InstantiateZombie(zombieTankPrefab, pack4Obj.transform, "Pack_Residential_Leader_Tank", new Vector3(-45f, 0.1f, 10f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>();
            List<ZombieAI> p4Followers = new List<ZombieAI>
            {
                InstantiateZombie(zombieRunnerPrefab, pack4Obj.transform, "Pack_Residential_Follower_Runner1", new Vector3(-47.2f, 0.1f, 7.8f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieRunnerPrefab, pack4Obj.transform, "Pack_Residential_Follower_Runner2", new Vector3(-42.8f, 0.1f, 7.8f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieStandardPrefab, pack4Obj.transform, "Pack_Residential_Follower_Standard1", new Vector3(-46.8f, 0.1f, 5.8f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>(),
                InstantiateZombie(zombieStandardPrefab, pack4Obj.transform, "Pack_Residential_Follower_Standard2", new Vector3(-43.2f, 0.1f, 5.8f), ZombieArchetype.LoneRoamer)?.GetComponent<ZombieAI>()
            };
            pack4.InitializePack("Pack_Residential", p4Leader, p4Followers);

            ZombiePrefabBuilder.FixAllZombiesInScene();

            // --- 7. NPCS & ALLIES ---
            GameObject npcParent = new GameObject("--- NPCS & ALLIES ---");
            if (npcDrArasPrefab != null)
            {
                GameObject aras = PrefabUtility.InstantiatePrefab(npcDrArasPrefab) as GameObject;
                aras.name = "NPC_DrAras_Root";
                aras.transform.SetParent(npcParent.transform);
                aras.transform.position = new Vector3(-28f, 0.1f, 32f);
            }
            if (npcZeynepPrefab != null)
            {
                GameObject zeynep = PrefabUtility.InstantiatePrefab(npcZeynepPrefab) as GameObject;
                zeynep.name = "NPC_Zeynep_Root";
                zeynep.transform.SetParent(npcParent.transform);
                zeynep.transform.position = new Vector3(-48f, 0.1f, 12f);
            }
            if (npcKeremPrefab != null)
            {
                GameObject kerem = PrefabUtility.InstantiatePrefab(npcKeremPrefab) as GameObject;
                kerem.name = "NPC_Kerem_Root";
                kerem.transform.SetParent(npcParent.transform);
                kerem.transform.position = new Vector3(16f, 0.1f, -34f);
            }
            if (npcElifPrefab != null)
            {
                GameObject elif = PrefabUtility.InstantiatePrefab(npcElifPrefab) as GameObject;
                elif.name = "NPC_Elif_Root";
                elif.transform.SetParent(npcParent.transform);
                elif.transform.position = new Vector3(40f, 0.1f, 32f);
            }

            // --- 7b. DYNAMIC SURVIVOR ENCOUNTERS (FAZ 2) ---
            GameObject scavengerParent = new GameObject("--- SURVIVOR ENCOUNTERS ---");
            if (npcScavengerPrefab != null)
            {
                // Encounter 1: South Avenue Wrecked Car Siege (fighting 2 zombies)
                GameObject scav1 = PrefabUtility.InstantiatePrefab(npcScavengerPrefab) as GameObject;
                scav1.name = "Survivor_Scavenger_Avenue";
                scav1.transform.SetParent(scavengerParent.transform);
                scav1.transform.position = new Vector3(-6f, 0.1f, -48f);
                var ai1 = scav1.GetComponent<ProjectSurvival.AI.SurvivorNPC_AI>();
                if (ai1 != null)
                {
                    SerializedObject so = new SerializedObject(ai1);
                    so.FindProperty("npcId").stringValue = "scavenger_avenue";
                    so.FindProperty("npcDisplayName").stringValue = "Murat (Gezgin)";
                    so.ApplyModifiedProperties();
                }

                // Encounter 2: Gun Shop Alley Corner (cornered by zombies)
                GameObject scav2 = PrefabUtility.InstantiatePrefab(npcScavengerPrefab) as GameObject;
                scav2.name = "Survivor_Scavenger_Alley";
                scav2.transform.SetParent(scavengerParent.transform);
                scav2.transform.position = new Vector3(25f, 0.1f, 7f);
                var ai2 = scav2.GetComponent<ProjectSurvival.AI.SurvivorNPC_AI>();
                if (ai2 != null)
                {
                    SerializedObject so = new SerializedObject(ai2);
                    so.FindProperty("npcId").stringValue = "scavenger_alley";
                    so.FindProperty("npcDisplayName").stringValue = "Cem (Hurda Arayıcı)";
                    so.ApplyModifiedProperties();
                }

                // Encounter 3: Skyscraper Plaza Barricade Siege (defending barricade)
                GameObject scav3 = PrefabUtility.InstantiatePrefab(npcScavengerPrefab) as GameObject;
                scav3.name = "Survivor_Scavenger_Plaza";
                scav3.transform.SetParent(scavengerParent.transform);
                scav3.transform.position = new Vector3(46f, 0.1f, 38f);
                var ai3 = scav3.GetComponent<ProjectSurvival.AI.SurvivorNPC_AI>();
                if (ai3 != null)
                {
                    SerializedObject so = new SerializedObject(ai3);
                    so.FindProperty("npcId").stringValue = "scavenger_plaza";
                    so.FindProperty("npcDisplayName").stringValue = "Deniz (Milis)";
                    so.ApplyModifiedProperties();
                }
            }

            // --- 8. STORY TRIGGER ZONES ---
            GameObject storyParent = new GameObject("--- STORY TRIGGERS ---");

            // Story Trigger 1: Dr. Aras Intro Encounter
            GameObject trigger1 = new GameObject("StoryTrigger_DrAras_Intro");
            trigger1.transform.SetParent(storyParent.transform);
            trigger1.transform.position = new Vector3(-25f, 0.5f, 30f);
            BoxCollider tcol1 = trigger1.AddComponent<BoxCollider>();
            tcol1.isTrigger = true;
            tcol1.size = new Vector3(6f, 3f, 6f);
            StoryTriggerZone stz1 = trigger1.AddComponent<StoryTriggerZone>();
            SerializedObject soStz1 = new SerializedObject(stz1);
            soStz1.FindProperty("storyResourceName").stringValue = "chapter1_outbreak";
            soStz1.FindProperty("startNodeId").stringValue = "node_doc_intro";
            soStz1.FindProperty("triggerOnEnter").boolValue = true;
            soStz1.FindProperty("triggerOnce").boolValue = true;
            soStz1.ApplyModifiedProperties();

            // Story Trigger 2: Clinic entrance encounter
            GameObject trigger2 = new GameObject("StoryTrigger_ClinicGate");
            trigger2.transform.SetParent(storyParent.transform);
            trigger2.transform.position = new Vector3(-35f, 0.5f, 23f);
            BoxCollider tcol2 = trigger2.AddComponent<BoxCollider>();
            tcol2.isTrigger = true;
            tcol2.size = new Vector3(6f, 3f, 6f);
            StoryTriggerZone stz2 = trigger2.AddComponent<StoryTriggerZone>();
            SerializedObject soStz2 = new SerializedObject(stz2);
            soStz2.FindProperty("storyResourceName").stringValue = "chapter1_outbreak";
            soStz2.FindProperty("startNodeId").stringValue = "node_mission_success";
            soStz2.FindProperty("triggerOnEnter").boolValue = false;
            soStz2.FindProperty("triggerOnce").boolValue = true;
            soStz2.FindProperty("interactionPromptText").stringValue = "E - Klinik Kapısını Zorla";
            soStz2.ApplyModifiedProperties();

            // NPC Wounded Civilian (Mert)
            CreateCivilianNPC(storyParent.transform, new Vector3(-46f, 0.1f, 14f));

            // --- 9. UI CANVAS & HUD (Fully Fixed Anchors & Layouts) ---
            CreateFullHUDCanvas();

            // --- 9.5 UNIVERSAL MULTI-VERB INTERACTION RETROFIT (STAGE 1) ---
            UniversalInteractionRetrofit.RetrofitScene(false);

            // --- 9.6 STAGE 2: AAA VISUAL COHESION, PBR PIPELINE & CINEMATIC GROUNDING ---
            AAA_PBRMaterialLibrary.GenerateAll(false);
            VisualCohesionOverhaul.OverhaulAll(false);
            ArchitecturalSetDresser.DressAll(false);
            CinematographyRig.SetupRig(false);

            // --- 9.7 BAKE NAVMESH (Post-Dressing with all new plinths and curbs) ---
            Debug.Log("[SceneBuilder] Baking NavMesh Surface for Metropolis map...");
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

            // --- 10. SAVE SCENE ---
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SceneBuilder] Scene saved successfully with URP materials & clean UI to {ScenePath}");
        }

        private static GameObject InstantiateZombie(GameObject prefab, Transform parent, string name, Vector3 pos, ZombieArchetype archetype = ZombieArchetype.LoneRoamer)
        {
            if (prefab == null) return null;
            GameObject z = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            z.name = name;
            z.transform.SetParent(parent);
            z.transform.position = pos;

            ZombieAI ai = z.GetComponent<ZombieAI>();
            if (ai != null)
            {
                ai.SetArchetype(archetype);
            }
            return z;
        }

        private static void CreateSidewalk(Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sw.name = "Street_Roadway";
            sw.transform.SetParent(parent);
            sw.transform.position = pos;
            sw.transform.localScale = size;
            if (mat != null) sw.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(sw, StaticEditorFlags.NavigationStatic);
        }

        private static GameObject CreateBuilding(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject bldg = new GameObject(name);
            bldg.transform.SetParent(parent);
            bldg.transform.position = pos;

            float w = size.x;
            float h = size.y;
            float d = size.z;
            float wallThick = 0.4f;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(bldg.transform);
            floor.transform.localPosition = new Vector3(0, 0.05f, 0);
            floor.transform.localScale = new Vector3(w, 0.1f, d);
            if (mat != null) floor.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);

            CreateWallPiece(bldg.transform, new Vector3(0, h * 0.5f, d * 0.5f), new Vector3(w, h, wallThick), mat);

            float doorGap = 3.2f;
            float sideWidth = (w - doorGap) * 0.5f;
            CreateWallPiece(bldg.transform, new Vector3(-(w * 0.5f) + (sideWidth * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideWidth, h, wallThick), mat);
            CreateWallPiece(bldg.transform, new Vector3((w * 0.5f) - (sideWidth * 0.5f), h * 0.5f, -d * 0.5f), new Vector3(sideWidth, h, wallThick), mat);

            CreateWallPiece(bldg.transform, new Vector3(w * 0.5f, h * 0.5f, 0), new Vector3(wallThick, h, d), mat);
            CreateWallPiece(bldg.transform, new Vector3(-w * 0.5f, h * 0.5f, 0), new Vector3(wallThick, h, d), mat);

            return bldg;
        }

        private static void CreateWallPiece(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
            if (mat != null) wall.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.NavigationStatic);
        }

        private static void CreateCoverObstacle(Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obs.name = "Cover_Obstacle";
            obs.transform.SetParent(parent);
            obs.transform.position = pos;
            obs.transform.localScale = size;
            if (mat != null) obs.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(obs, StaticEditorFlags.NavigationStatic);
        }

        private static Material EnsureSkyboxMaterial()
        {
            string path = "Assets/_Project/Materials/Mat_Skybox_Night.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader skyShader = Shader.Find("Skybox/Procedural");
                if (skyShader == null) skyShader = Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(skyShader);
                AssetDatabase.CreateAsset(mat, path);
            }

            if (mat.shader != null && mat.shader.name == "Skybox/Procedural")
            {
                mat.SetFloat("_SunDisk", 1);
                mat.SetFloat("_SunSize", 0.035f);
                mat.SetFloat("_SunSizeConvergence", 6f);
                mat.SetFloat("_AtmosphereThickness", 0.75f);
                mat.SetColor("_SkyTint", new Color(0.06f, 0.085f, 0.145f));
                mat.SetColor("_GroundColor", new Color(0.08f, 0.095f, 0.125f));
                mat.SetFloat("_Exposure", 0.85f);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void AddBuildingFacadeDetails(
            GameObject bldg, string bldgType,
            Material frameMat, Material glassMat, Material litMat, Material woodMat, Material signMat, Material plankMat, Material hvacMat,
            Vector3 bldgSize)
        {
            float w = bldgSize.x;
            float h = bldgSize.y;
            float d = bldgSize.z;
            float frontZ = -d * 0.5f - 0.08f;

            // 1. Entrance Wooden Door & Frame
            GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "Door_Frame";
            doorFrame.transform.SetParent(bldg.transform);
            doorFrame.transform.localPosition = new Vector3(0f, 1.4f, frontZ);
            doorFrame.transform.localScale = new Vector3(2.4f, 2.8f, 0.2f);
            if (frameMat != null) doorFrame.GetComponent<Renderer>().sharedMaterial = frameMat;

            GameObject doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorLeaf.name = "Door_Leaf";
            doorLeaf.transform.SetParent(bldg.transform);
            doorLeaf.transform.localPosition = new Vector3(0f, 1.35f, frontZ - 0.05f);
            doorLeaf.transform.localScale = new Vector3(2.1f, 2.6f, 0.12f);
            if (woodMat != null) doorLeaf.GetComponent<Renderer>().sharedMaterial = woodMat;

            // 2. Commercial Signboard above Entrance
            if (signMat != null)
            {
                GameObject signObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                signObj.name = $"Storefront_Sign_{bldgType}";
                signObj.transform.SetParent(bldg.transform);
                signObj.transform.localPosition = new Vector3(0f, 3.4f, frontZ - 0.12f);
                signObj.transform.localScale = new Vector3(5.2f, 0.85f, 0.22f);
                signObj.GetComponent<Renderer>().sharedMaterial = signMat;

                GameObject signLightObj = new GameObject("Sign_DownLight");
                signLightObj.transform.SetParent(signObj.transform);
                signLightObj.transform.localPosition = new Vector3(0f, -0.4f, -0.2f);
                Light sLight = signLightObj.AddComponent<Light>();
                sLight.type = LightType.Point;
                sLight.range = 4.0f;
                sLight.intensity = 1.4f;
                sLight.color = new Color(1f, 0.9f, 0.7f);
                sLight.shadows = LightShadows.None;
            }

            // 3. Facade Windows (Left and Right wings)
            float[] xOffsets = new float[] { -w * 0.32f, -w * 0.18f, w * 0.18f, w * 0.32f };
            float[] yHeights = new float[] { 1.5f, 3.2f };

            int winIndex = 0;
            for (int yi = 0; yi < yHeights.Length; yi++)
            {
                float winY = yHeights[yi];
                for (int xi = 0; xi < xOffsets.Length; xi++)
                {
                    float winX = xOffsets[xi];
                    winIndex++;

                    GameObject winFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    winFrame.name = $"Window_Frame_{winIndex}";
                    winFrame.transform.SetParent(bldg.transform);
                    winFrame.transform.localPosition = new Vector3(winX, winY, frontZ);
                    winFrame.transform.localScale = new Vector3(1.35f, 1.35f, 0.16f);
                    if (frameMat != null) winFrame.GetComponent<Renderer>().sharedMaterial = frameMat;

                    bool isLit = (winIndex == 2 || winIndex == 7);
                    bool isBarricaded = (winIndex == 1 || winIndex == 4);

                    GameObject winGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    winGlass.name = $"Window_Glass_{winIndex}";
                    winGlass.transform.SetParent(bldg.transform);
                    winGlass.transform.localPosition = new Vector3(winX, winY, frontZ - 0.02f);
                    winGlass.transform.localScale = new Vector3(1.15f, 1.15f, 0.08f);
                    Material glassM = isLit ? litMat : glassMat;
                    if (glassM != null) winGlass.GetComponent<Renderer>().sharedMaterial = glassM;

                    // Wooden Planks over barricaded windows
                    if (isBarricaded && plankMat != null)
                    {
                        for (int p = 0; p < 3; p++)
                        {
                            GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            plank.name = $"Plank_{p + 1}";
                            plank.transform.SetParent(winFrame.transform);
                            float pAngle = (p == 1) ? 22f : -18f;
                            plank.transform.localPosition = new Vector3(0f, (p - 1) * 0.35f, -0.65f);
                            plank.transform.localRotation = Quaternion.Euler(0f, 0f, pAngle);
                            plank.transform.localScale = new Vector3(1.25f, 0.22f, 0.35f);
                            plank.GetComponent<Renderer>().sharedMaterial = plankMat;
                        }
                    }
                }
            }

            // 4. Rooftop HVAC Units and Vents
            if (hvacMat != null)
            {
                GameObject hvac1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hvac1.name = "Rooftop_HVAC_1";
                hvac1.transform.SetParent(bldg.transform);
                hvac1.transform.localPosition = new Vector3(-w * 0.25f, h + 0.6f, 0f);
                hvac1.transform.localScale = new Vector3(2.4f, 1.2f, 1.8f);
                hvac1.GetComponent<Renderer>().sharedMaterial = hvacMat;

                GameObject ventFan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ventFan.name = "Vent_Fan";
                ventFan.transform.SetParent(hvac1.transform);
                ventFan.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                ventFan.transform.localScale = new Vector3(0.55f, 0.15f, 0.55f);
                ventFan.GetComponent<Renderer>().sharedMaterial = frameMat;

                GameObject hvac2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hvac2.name = "Rooftop_HVAC_2";
                hvac2.transform.SetParent(bldg.transform);
                hvac2.transform.localPosition = new Vector3(w * 0.22f, h + 0.5f, d * 0.2f);
                hvac2.transform.localScale = new Vector3(1.8f, 1.0f, 2.2f);
                hvac2.GetComponent<Renderer>().sharedMaterial = hvacMat;
            }
        }

        private static void CreateCitySkyline(Transform parent, Material skylineMat, Material litMat)
        {
            GameObject skylineParent = new GameObject("--- DISTANT CITY SKYLINE ---");
            skylineParent.transform.SetParent(parent);

            System.Random rnd = new System.Random(42);

            // North Edge (X: -65 to 65, Z: 54 to 75)
            for (float x = -65f; x <= 65f; x += 14f)
            {
                float z = 56f + (float)(rnd.NextDouble() * 18f);
                float w = 12f + (float)(rnd.NextDouble() * 8f);
                float d = 12f + (float)(rnd.NextDouble() * 8f);
                float h = 24f + (float)(rnd.NextDouble() * 32f);
                CreateSkylineTower(skylineParent.transform, new Vector3(x, 0f, z), new Vector3(w, h, d), skylineMat, litMat, rnd);
            }

            // South Edge (X: -65 to 65, Z: -54 to -75)
            for (float x = -65f; x <= 65f; x += 14f)
            {
                float z = -56f - (float)(rnd.NextDouble() * 18f);
                float w = 12f + (float)(rnd.NextDouble() * 8f);
                float d = 12f + (float)(rnd.NextDouble() * 8f);
                float h = 22f + (float)(rnd.NextDouble() * 30f);
                CreateSkylineTower(skylineParent.transform, new Vector3(x, 0f, z), new Vector3(w, h, d), skylineMat, litMat, rnd);
            }

            // East Edge (X: 54 to 75, Z: -50 to 50)
            for (float z = -50f; z <= 50f; z += 14f)
            {
                float x = 56f + (float)(rnd.NextDouble() * 18f);
                float w = 12f + (float)(rnd.NextDouble() * 8f);
                float d = 12f + (float)(rnd.NextDouble() * 8f);
                float h = 26f + (float)(rnd.NextDouble() * 30f);
                CreateSkylineTower(skylineParent.transform, new Vector3(x, 0f, z), new Vector3(w, h, d), skylineMat, litMat, rnd);
            }

            // West Edge (X: -54 to -75, Z: -50 to 50)
            for (float z = -50f; z <= 50f; z += 14f)
            {
                float x = -56f - (float)(rnd.NextDouble() * 18f);
                float w = 12f + (float)(rnd.NextDouble() * 8f);
                float d = 12f + (float)(rnd.NextDouble() * 8f);
                float h = 24f + (float)(rnd.NextDouble() * 32f);
                CreateSkylineTower(skylineParent.transform, new Vector3(x, 0f, z), new Vector3(w, h, d), skylineMat, litMat, rnd);
            }
        }

        private static void CreateSkylineTower(Transform parent, Vector3 basePos, Vector3 size, Material mat, Material litMat, System.Random rnd)
        {
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "Skyline_Tower";
            tower.transform.SetParent(parent);
            tower.transform.position = new Vector3(basePos.x, size.y * 0.5f, basePos.z);
            tower.transform.localScale = size;
            if (mat != null) tower.GetComponent<Renderer>().sharedMaterial = mat;

            // Rooftop antenna on taller buildings
            if (size.y > 35f)
            {
                GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                antenna.name = "Antenna";
                antenna.transform.SetParent(tower.transform);
                antenna.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                antenna.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
                if (mat != null) antenna.GetComponent<Renderer>().sharedMaterial = mat;
            }

            // 1-2 Subtle warm-lit window slit bands
            if (litMat != null && rnd.NextDouble() > 0.4)
            {
                GameObject litBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                litBand.name = "Lit_Window_Band";
                litBand.transform.SetParent(tower.transform);
                float bandY = (float)(rnd.NextDouble() * 0.6f - 0.2f);
                litBand.transform.localPosition = new Vector3(0f, bandY, 0.51f);
                litBand.transform.localScale = new Vector3(0.7f, 0.025f, 0.02f);
                litBand.GetComponent<Renderer>().sharedMaterial = litMat;
            }
        }

        private static void CreateGlobalPostProcessing(Transform parent)
        {
            string profilePath = "Assets/_Project/Settings/AtmosphereVolumeProfile.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();

                Tonemapping tonemapping = profile.Add<Tonemapping>(true);
                tonemapping.mode.overrideState = true;
                tonemapping.mode.value = TonemappingMode.ACES;

                Bloom bloom = profile.Add<Bloom>(true);
                bloom.threshold.overrideState = true;
                bloom.threshold.value = 1.1f;
                bloom.intensity.overrideState = true;
                bloom.intensity.value = 0.35f;
                bloom.scatter.overrideState = true;
                bloom.scatter.value = 0.7f;

                ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
                colorAdj.contrast.overrideState = true;
                colorAdj.contrast.value = 12f;
                colorAdj.saturation.overrideState = true;
                colorAdj.saturation.value = -8f;

                Vignette vignette = profile.Add<Vignette>(true);
                vignette.intensity.overrideState = true;
                vignette.intensity.value = 0.22f;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = 0.35f;

                AssetDatabase.CreateAsset(profile, profilePath);
            }

            GameObject volumeObj = new GameObject("Global_PostProcess_Volume");
            volumeObj.transform.SetParent(parent);
            Volume vol = volumeObj.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.profile = profile;
            vol.weight = 1.0f;
        }

        private static void CreateCurbsAndSidewalks(Transform parent, Material streetMat, Material curbMat)
        {
            GameObject swRoot = new GameObject("Curbs_And_Sidewalks");
            swRoot.transform.SetParent(parent);

            Vector3[] swPos = new Vector3[] {
                new Vector3(24.5f, 0.10f, 24.5f),   // North-East quadrant
                new Vector3(-24.5f, 0.10f, 24.5f),  // North-West quadrant
                new Vector3(-24.5f, 0.10f, -24.5f), // South-West quadrant
                new Vector3(24.5f, 0.10f, -24.5f)   // South-East quadrant
            };
            Vector3 swScale = new Vector3(41f, 0.20f, 41f);

            for (int i = 0; i < swPos.Length; i++)
            {
                GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sw.name = $"Sidewalk_Quadrant_{i + 1}";
                sw.transform.SetParent(swRoot.transform);
                sw.transform.position = swPos[i];
                sw.transform.localScale = swScale;
                if (streetMat != null) sw.GetComponent<Renderer>().sharedMaterial = streetMat;
                GameObjectUtility.SetStaticEditorFlags(sw, StaticEditorFlags.NavigationStatic);
            }

            void AddCurb(string name, Vector3 pos, Vector3 size)
            {
                GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = name;
                curb.transform.SetParent(swRoot.transform);
                curb.transform.position = pos;
                curb.transform.localScale = size;
                if (curbMat != null) curb.GetComponent<Renderer>().sharedMaterial = curbMat;
                GameObjectUtility.SetStaticEditorFlags(curb, StaticEditorFlags.NavigationStatic);
            }

            // North segment curbs
            AddCurb("Curb_N_Left", new Vector3(-4.14f, 0.13f, 24.5f), new Vector3(0.28f, 0.26f, 41f));
            AddCurb("Curb_N_Right", new Vector3(4.14f, 0.13f, 24.5f), new Vector3(0.28f, 0.26f, 41f));

            // South segment curbs
            AddCurb("Curb_S_Left", new Vector3(-4.14f, 0.13f, -24.5f), new Vector3(0.28f, 0.26f, 41f));
            AddCurb("Curb_S_Right", new Vector3(4.14f, 0.13f, -24.5f), new Vector3(0.28f, 0.26f, 41f));

            // East segment curbs
            AddCurb("Curb_E_Top", new Vector3(24.5f, 0.13f, 4.14f), new Vector3(41f, 0.26f, 0.28f));
            AddCurb("Curb_E_Bottom", new Vector3(24.5f, 0.13f, -4.14f), new Vector3(41f, 0.26f, 0.28f));

            // West segment curbs
            AddCurb("Curb_W_Top", new Vector3(-24.5f, 0.13f, 4.14f), new Vector3(41f, 0.26f, 0.28f));
            AddCurb("Curb_W_Bottom", new Vector3(-24.5f, 0.13f, -4.14f), new Vector3(41f, 0.26f, 0.28f));
        }

        private static void CreateRoadMarkings(Transform parent, Material whiteMarkMat, Material yellowMarkMat)
        {
            GameObject markRoot = new GameObject("Road_Markings");
            markRoot.transform.SetParent(parent);

            void CreateDashedLine(Vector3 start, Vector3 end, bool alongZ)
            {
                float dist = alongZ ? Mathf.Abs(end.z - start.z) : Mathf.Abs(end.x - start.x);
                float segLen = 2.4f;
                float gapLen = 2.0f;
                float totalStep = segLen + gapLen;
                int count = Mathf.FloorToInt(dist / totalStep);

                for (int i = 0; i < count; i++)
                {
                    GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    dash.name = "YellowDash";
                    dash.transform.SetParent(markRoot.transform);
                    if (alongZ)
                    {
                        float z = start.z + (i * totalStep) + (segLen * 0.5f);
                        dash.transform.position = new Vector3(start.x, 0.02f, z);
                        dash.transform.localScale = new Vector3(0.18f, 0.02f, segLen);
                    }
                    else
                    {
                        float x = start.x + (i * totalStep) + (segLen * 0.5f);
                        dash.transform.position = new Vector3(x, 0.02f, start.z);
                        dash.transform.localScale = new Vector3(segLen, 0.02f, 0.18f);
                    }
                    Object.DestroyImmediate(dash.GetComponent<Collider>());
                    if (yellowMarkMat != null) dash.GetComponent<Renderer>().sharedMaterial = yellowMarkMat;
                }
            }

            CreateDashedLine(new Vector3(0f, 0f, -42f), new Vector3(0f, 0f, -6f), true);
            CreateDashedLine(new Vector3(0f, 0f, 6f), new Vector3(0f, 0f, 42f), true);
            CreateDashedLine(new Vector3(-42f, 0f, 0f), new Vector3(-6f, 0f, 0f), false);
            CreateDashedLine(new Vector3(6f, 0f, 0f), new Vector3(42f, 0f, 0f), false);

            void CreateCrosswalk(Vector3 center, bool isHorizontal)
            {
                int stripes = 7;
                float stripeWidth = 0.5f;
                float stripeGap = 0.45f;
                float stripeLength = 3.2f;

                for (int i = 0; i < stripes; i++)
                {
                    GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stripe.name = "ZebraStripe";
                    stripe.transform.SetParent(markRoot.transform);
                    float offset = (i - (stripes - 1) * 0.5f) * (stripeWidth + stripeGap);

                    if (isHorizontal)
                    {
                        stripe.transform.position = center + new Vector3(offset, 0.025f, 0f);
                        stripe.transform.localScale = new Vector3(stripeWidth, 0.02f, stripeLength);
                    }
                    else
                    {
                        stripe.transform.position = center + new Vector3(0f, 0.025f, offset);
                        stripe.transform.localScale = new Vector3(stripeLength, 0.02f, stripeWidth);
                    }

                    Object.DestroyImmediate(stripe.GetComponent<Collider>());
                    if (whiteMarkMat != null) stripe.GetComponent<Renderer>().sharedMaterial = whiteMarkMat;
                }
            }

            CreateCrosswalk(new Vector3(0f, 0f, -5.2f), true);
            CreateCrosswalk(new Vector3(0f, 0f, 5.2f), true);
            CreateCrosswalk(new Vector3(-5.2f, 0f, 0f), false);
            CreateCrosswalk(new Vector3(5.2f, 0f, 0f), false);
        }

        private static void CreateStreetLamp(Transform parent, Vector3 pos, float yRot, Material lampMat, Material bulbMat, bool flicker)
        {
            GameObject lamp = new GameObject("StreetLamp");
            lamp.transform.SetParent(parent);
            lamp.transform.position = pos;
            lamp.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "LampPole";
            pole.transform.SetParent(lamp.transform);
            pole.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            pole.transform.localScale = new Vector3(0.18f, 2.25f, 0.18f);
            if (lampMat != null) pole.GetComponent<Renderer>().sharedMaterial = lampMat;
            GameObjectUtility.SetStaticEditorFlags(pole, StaticEditorFlags.NavigationStatic);

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "LampArm";
            arm.transform.SetParent(lamp.transform);
            arm.transform.localPosition = new Vector3(0f, 4.4f, 0.75f);
            arm.transform.localScale = new Vector3(0.12f, 0.12f, 1.5f);
            if (lampMat != null) arm.GetComponent<Renderer>().sharedMaterial = lampMat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "LampHead";
            head.transform.SetParent(lamp.transform);
            head.transform.localPosition = new Vector3(0f, 4.35f, 1.45f);
            head.transform.localScale = new Vector3(0.42f, 0.18f, 0.55f);
            if (lampMat != null) head.GetComponent<Renderer>().sharedMaterial = lampMat;

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bulb.name = "LampBulb";
            bulb.transform.SetParent(lamp.transform);
            bulb.transform.localPosition = new Vector3(0f, 4.25f, 1.45f);
            bulb.transform.localScale = new Vector3(0.32f, 0.05f, 0.45f);
            Object.DestroyImmediate(bulb.GetComponent<Collider>());
            if (bulbMat != null) bulb.GetComponent<Renderer>().sharedMaterial = bulbMat;

            GameObject lightObj = new GameObject("SpotLight");
            lightObj.transform.SetParent(lamp.transform);
            lightObj.transform.localPosition = new Vector3(0f, 4.2f, 1.45f);
            lightObj.transform.localRotation = Quaternion.Euler(82f, 0f, 0f);

            Light spot = lightObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 16f;
            spot.spotAngle = 70f;
            spot.innerSpotAngle = 40f;
            spot.intensity = 5.5f;
            spot.color = new Color(1.0f, 0.718f, 0.012f); // #FFB703 Warm Amber
            spot.shadows = LightShadows.None;

            if (flicker)
            {
                FlickeringLight fl = lightObj.AddComponent<FlickeringLight>();
                SerializedObject soFl = new SerializedObject(fl);
                soFl.FindProperty("baseIntensity").floatValue = 5.5f;
                soFl.FindProperty("flickerAmount").floatValue = 1.6f;
                soFl.FindProperty("flickerSpeed").floatValue = 8.5f;
                soFl.ApplyModifiedProperties();
            }
        }

        private static void CreateVehicleWreck(Transform parent, string name, Vector3 pos, float yRot, Material bodyMat, Material rustMat, Material glassMat)
        {
            GameObject wreck = new GameObject(name);
            wreck.transform.SetParent(parent);
            wreck.transform.position = pos;
            wreck.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "Chassis";
            chassis.transform.SetParent(wreck.transform);
            chassis.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            chassis.transform.localScale = new Vector3(2.0f, 0.75f, 4.4f);
            if (bodyMat != null) chassis.GetComponent<Renderer>().sharedMaterial = bodyMat;
            GameObjectUtility.SetStaticEditorFlags(chassis, StaticEditorFlags.NavigationStatic);

            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(wreck.transform);
            cabin.transform.localPosition = new Vector3(0f, 1.15f, -0.3f);
            cabin.transform.localScale = new Vector3(1.75f, 0.55f, 2.3f);
            if (rustMat != null) cabin.GetComponent<Renderer>().sharedMaterial = rustMat;
            GameObjectUtility.SetStaticEditorFlags(cabin, StaticEditorFlags.NavigationStatic);

            GameObject hood = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hood.name = "Hood";
            hood.transform.SetParent(wreck.transform);
            hood.transform.localPosition = new Vector3(0f, 0.95f, 1.4f);
            hood.transform.localRotation = Quaternion.Euler(-8f, 0f, 4f);
            hood.transform.localScale = new Vector3(1.85f, 0.15f, 1.4f);
            if (rustMat != null) hood.GetComponent<Renderer>().sharedMaterial = rustMat;

            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Windshield";
            glass.transform.SetParent(wreck.transform);
            glass.transform.localPosition = new Vector3(0f, 1.12f, 0.92f);
            glass.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);
            glass.transform.localScale = new Vector3(1.65f, 0.08f, 0.8f);
            Object.DestroyImmediate(glass.GetComponent<Collider>());
            if (glassMat != null) glass.GetComponent<Renderer>().sharedMaterial = glassMat;

            Vector3[] wheelOffsets = new Vector3[] {
                new Vector3(-0.95f, 0.35f, 1.3f),
                new Vector3(0.95f, 0.35f, 1.3f),
                new Vector3(-0.95f, 0.35f, -1.3f),
                new Vector3(0.95f, 0.35f, -1.3f)
            };
            for (int i = 0; i < wheelOffsets.Length; i++)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = $"Wheel_{i + 1}";
                wheel.transform.SetParent(wreck.transform);
                wheel.transform.localPosition = wheelOffsets[i];
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(0.7f, 0.15f, 0.7f);
                if (rustMat != null) wheel.GetComponent<Renderer>().sharedMaterial = rustMat;
                GameObjectUtility.SetStaticEditorFlags(wheel, StaticEditorFlags.NavigationStatic);
            }
        }

        private static void CreateConcreteBarrier(Transform parent, Vector3 pos, float yRot, Material barrierMat)
        {
            GameObject barrier = new GameObject("JerseyBarrier");
            barrier.transform.SetParent(parent);
            barrier.transform.position = pos;
            barrier.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            GameObject bBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bBase.name = "Barrier_Base";
            bBase.transform.SetParent(barrier.transform);
            bBase.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            bBase.transform.localScale = new Vector3(3.0f, 0.44f, 0.72f);
            if (barrierMat != null) bBase.GetComponent<Renderer>().sharedMaterial = barrierMat;
            GameObjectUtility.SetStaticEditorFlags(bBase, StaticEditorFlags.NavigationStatic);

            GameObject bTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bTop.name = "Barrier_Top";
            bTop.transform.SetParent(barrier.transform);
            bTop.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            bTop.transform.localScale = new Vector3(3.0f, 0.52f, 0.36f);
            if (barrierMat != null) bTop.GetComponent<Renderer>().sharedMaterial = barrierMat;
            GameObjectUtility.SetStaticEditorFlags(bTop, StaticEditorFlags.NavigationStatic);
        }

        private static void CreateDumpster(Transform parent, Vector3 pos, float yRot, Material metalMat)
        {
            GameObject dumpster = new GameObject("MetalDumpster");
            dumpster.transform.SetParent(parent);
            dumpster.transform.position = pos;
            dumpster.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Dumpster_Body";
            body.transform.SetParent(dumpster.transform);
            body.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            body.transform.localScale = new Vector3(2.2f, 1.25f, 1.3f);
            if (metalMat != null) body.GetComponent<Renderer>().sharedMaterial = metalMat;
            GameObjectUtility.SetStaticEditorFlags(body, StaticEditorFlags.NavigationStatic);

            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Dumpster_Lid";
            lid.transform.SetParent(dumpster.transform);
            lid.transform.localPosition = new Vector3(0f, 1.32f, 0.05f);
            lid.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            lid.transform.localScale = new Vector3(2.26f, 0.08f, 1.34f);
            if (metalMat != null) lid.GetComponent<Renderer>().sharedMaterial = metalMat;
            GameObjectUtility.SetStaticEditorFlags(lid, StaticEditorFlags.NavigationStatic);
        }

        private static GameObject CreateDetailedClinic(Transform parent, Vector3 pos, Material clinicMat, Material wallMat, Material woodMat, Material metalMat, Material signMat, GameObject lootPrefab)
        {
            GameObject clinic = CreateBuilding(parent, "Building_OldClinic", pos, new Vector3(18f, 4.8f, 16f), clinicMat);

            // 1. Entrance Stairs (3 steps leading up to entrance)
            for (int i = 0; i < 3; i++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Entrance_Stair_{i + 1}";
                step.transform.SetParent(clinic.transform);
                float stepY = (i + 1) * 0.08f;
                float stepZ = -8.0f - ((2 - i) * 0.45f);
                step.transform.localPosition = new Vector3(0f, stepY * 0.5f, stepZ);
                step.transform.localScale = new Vector3(3.6f, stepY, 0.45f);
                if (clinicMat != null) step.GetComponent<Renderer>().sharedMaterial = clinicMat;
                GameObjectUtility.SetStaticEditorFlags(step, StaticEditorFlags.NavigationStatic);
            }

            // 2. Weathered Signboard above Entrance
            GameObject signObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signObj.name = "Clinic_Signboard";
            signObj.transform.SetParent(clinic.transform);
            signObj.transform.localPosition = new Vector3(0f, 3.6f, -8.25f);
            signObj.transform.localScale = new Vector3(4.5f, 0.85f, 0.18f);
            if (signMat != null) signObj.GetComponent<Renderer>().sharedMaterial = signMat;

            // 3. Interior Reception Desk
            GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "ReceptionDesk_Front";
            desk.transform.SetParent(clinic.transform);
            desk.transform.localPosition = new Vector3(-2.5f, 0.55f, -3.5f);
            desk.transform.localScale = new Vector3(3.4f, 1.1f, 0.75f);
            if (woodMat != null) desk.GetComponent<Renderer>().sharedMaterial = woodMat;
            GameObjectUtility.SetStaticEditorFlags(desk, StaticEditorFlags.NavigationStatic);

            GameObject deskSide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskSide.name = "ReceptionDesk_Side";
            deskSide.transform.SetParent(clinic.transform);
            deskSide.transform.localPosition = new Vector3(-4.2f, 0.55f, -2.4f);
            deskSide.transform.localScale = new Vector3(0.75f, 1.1f, 1.8f);
            if (woodMat != null) deskSide.GetComponent<Renderer>().sharedMaterial = woodMat;
            GameObjectUtility.SetStaticEditorFlags(deskSide, StaticEditorFlags.NavigationStatic);

            // 4. Examination Room Partition Wall
            GameObject partWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            partWall.name = "Partition_Wall";
            partWall.transform.SetParent(clinic.transform);
            partWall.transform.localPosition = new Vector3(1.2f, 1.8f, 1.5f);
            partWall.transform.localScale = new Vector3(8.5f, 3.6f, 0.35f);
            if (wallMat != null) partWall.GetComponent<Renderer>().sharedMaterial = wallMat;
            GameObjectUtility.SetStaticEditorFlags(partWall, StaticEditorFlags.NavigationStatic);

            // 5. Metal Examination Tables
            void CreateExamTable(string tName, Vector3 localPos, float rot)
            {
                GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
                table.name = tName;
                table.transform.SetParent(clinic.transform);
                table.transform.localPosition = localPos;
                table.transform.localRotation = Quaternion.Euler(0f, rot, 0f);
                table.transform.localScale = new Vector3(1.0f, 0.85f, 2.2f);
                if (metalMat != null) table.GetComponent<Renderer>().sharedMaterial = metalMat;
                GameObjectUtility.SetStaticEditorFlags(table, StaticEditorFlags.NavigationStatic);
            }
            CreateExamTable("ExamTable_1", new Vector3(-4.5f, 0.45f, 4.0f), 0f);
            CreateExamTable("ExamTable_2", new Vector3(4.5f, 0.45f, 4.0f), 0f);

            // 6. Medicine Storage Shelves & Loot Container with Antiviral
            GameObject shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf.name = "Medicine_Shelf";
            shelf.transform.SetParent(clinic.transform);
            shelf.transform.localPosition = new Vector3(0f, 1.4f, 7.2f);
            shelf.transform.localScale = new Vector3(4.8f, 2.6f, 0.65f);
            if (woodMat != null) shelf.GetComponent<Renderer>().sharedMaterial = woodMat;
            GameObjectUtility.SetStaticEditorFlags(shelf, StaticEditorFlags.NavigationStatic);

            if (lootPrefab != null)
            {
                GameObject clinicChest = PrefabUtility.InstantiatePrefab(lootPrefab) as GameObject;
                clinicChest.name = "LootContainer_Clinic_Medical";
                clinicChest.transform.SetParent(clinic.transform);
                clinicChest.transform.localPosition = new Vector3(0f, 0.1f, 5.8f);

                LootContainer lc = clinicChest.GetComponent<LootContainer>();
                if (lc != null)
                {
                    SerializedObject soLc = new SerializedObject(lc);
                    SerializedProperty lootList = soLc.FindProperty("possibleLoot");
                    lootList.ClearArray();

                    ItemData antiviral = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_antiviral.asset");
                    ItemData medkit = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_medkit.asset");
                    ItemData bandage = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_bandage.asset");
                    ItemData scrap = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/item_scrap.asset");

                    void AddLoot(ItemData it, int cnt, float chance)
                    {
                        if (it == null) return;
                        int idx = lootList.arraySize;
                        lootList.InsertArrayElementAtIndex(idx);
                        SerializedProperty elem = lootList.GetArrayElementAtIndex(idx);
                        elem.FindPropertyRelative("item").objectReferenceValue = it;
                        elem.FindPropertyRelative("count").intValue = cnt;
                        elem.FindPropertyRelative("dropChance").floatValue = chance;
                    }

                    AddLoot(antiviral, 1, 1.0f);
                    AddLoot(medkit, 1, 0.8f);
                    AddLoot(bandage, 2, 1.0f);
                    AddLoot(scrap, 2, 0.75f);
                    soLc.ApplyModifiedProperties();
                }
            }

            GameObject clinicLight = new GameObject("ClinicInteriorLight");
            clinicLight.transform.SetParent(clinic.transform);
            clinicLight.transform.localPosition = new Vector3(0f, 4.0f, 0f);
            Light cLight = clinicLight.AddComponent<Light>();
            cLight.type = LightType.Point;
            cLight.range = 20f;
            cLight.intensity = 2.4f;
            cLight.color = new Color(0.85f, 0.95f, 1.0f);
            cLight.shadows = LightShadows.Soft;

            return clinic;
        }

        private static GameObject CreateDetailedSafeHouse(Transform parent, Vector3 pos, Material wallMat, Material woodMat, Material metalMat)
        {
            GameObject safeHouse = CreateBuilding(parent, "SafeHouse_Building", pos, new Vector3(16f, 4.5f, 16f), wallMat);

            // 1. Roll-up Garage Metal Shutter Frame at entrance
            GameObject shutterFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shutterFrame.name = "RollUpShutter_Frame";
            shutterFrame.transform.SetParent(safeHouse.transform);
            shutterFrame.transform.localPosition = new Vector3(0f, 2.1f, -8.15f);
            shutterFrame.transform.localScale = new Vector3(3.6f, 4.2f, 0.25f);
            if (metalMat != null) shutterFrame.GetComponent<Renderer>().sharedMaterial = metalMat;
            GameObjectUtility.SetStaticEditorFlags(shutterFrame, StaticEditorFlags.NavigationStatic);

            // 2. Warm Interior Lighting
            GameObject lightObj = new GameObject("InteriorWarmLight");
            lightObj.transform.SetParent(safeHouse.transform);
            lightObj.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            Light interiorLight = lightObj.AddComponent<Light>();
            interiorLight.type = LightType.Point;
            interiorLight.color = new Color(1f, 0.85f, 0.6f);
            interiorLight.range = 18f;
            interiorLight.intensity = 2.8f;
            interiorLight.shadows = LightShadows.Soft;

            // 3. Safe House Zone Trigger
            GameObject zoneObj = new GameObject("Zone_SafeHouse_Trigger");
            zoneObj.name = "Zone_SafeHouse";
            zoneObj.tag = "SafeHouse";
            zoneObj.transform.SetParent(safeHouse.transform);
            zoneObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            BoxCollider zCol = zoneObj.AddComponent<BoxCollider>();
            zCol.isTrigger = true;
            zCol.size = new Vector3(14.5f, 3.6f, 14.5f);

            SafeHouseManager shm = zoneObj.AddComponent<SafeHouseManager>();
            SerializedObject soShm = new SerializedObject(shm);
            soShm.FindProperty("safeZoneId").stringValue = "Zone_SafeHouse";
            soShm.FindProperty("safeZoneTrigger").objectReferenceValue = zCol;
            soShm.FindProperty("safeHouseInteriorLight").objectReferenceValue = interiorLight;
            soShm.ApplyModifiedProperties();

            MissionZone mz = zoneObj.AddComponent<MissionZone>();
            SerializedObject soMz = new SerializedObject(mz);
            soMz.FindProperty("zoneId").stringValue = "Zone_SafeHouse";
            soMz.FindProperty("zoneDisplayName").stringValue = "Güvenli Sığınak";
            soMz.ApplyModifiedProperties();

            // 4. Stash Storage Chest (24 Slots)
            GameObject stashRoot = new GameObject("StashContainer_Root");
            stashRoot.transform.SetParent(safeHouse.transform);
            stashRoot.transform.localPosition = new Vector3(-5f, 0f, 4f);
            BoxCollider scCol = stashRoot.AddComponent<BoxCollider>();
            scCol.isTrigger = true;
            scCol.size = new Vector3(2.5f, 2.0f, 2.5f);
            scCol.center = new Vector3(0, 1.0f, 0);
            stashRoot.AddComponent<StashContainer>();

            GameObject stashVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stashVisual.name = "Visual_Model";
            stashVisual.transform.SetParent(stashRoot.transform);
            stashVisual.transform.localPosition = new Vector3(0, 0.45f, 0);
            stashVisual.transform.localScale = new Vector3(1.4f, 0.9f, 0.9f);
            Object.DestroyImmediate(stashVisual.GetComponent<Collider>());
            if (woodMat != null) stashVisual.GetComponent<Renderer>().sharedMaterial = woodMat;

            // 5. Rest Point (Bed / Cot)
            GameObject restRoot = new GameObject("RestPoint_Root");
            restRoot.transform.SetParent(safeHouse.transform);
            restRoot.transform.localPosition = new Vector3(5f, 0f, 4f);
            BoxCollider rpCol = restRoot.AddComponent<BoxCollider>();
            rpCol.isTrigger = true;
            rpCol.size = new Vector3(2.5f, 2.0f, 2.5f);
            rpCol.center = new Vector3(0, 1.0f, 0);
            restRoot.AddComponent<RestPoint>();

            GameObject bedVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bedVisual.name = "Visual_Model";
            bedVisual.transform.SetParent(restRoot.transform);
            bedVisual.transform.localPosition = new Vector3(0, 0.25f, 0);
            bedVisual.transform.localScale = new Vector3(1.2f, 0.5f, 2.2f);
            Object.DestroyImmediate(bedVisual.GetComponent<Collider>());
            Material streetMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Street.mat");
            if (streetMat != null) bedVisual.GetComponent<Renderer>().sharedMaterial = streetMat;

            // 6. Emergency Radio Station
            GameObject radioRoot = new GameObject("RadioStation_Root");
            radioRoot.transform.SetParent(safeHouse.transform);
            radioRoot.transform.localPosition = new Vector3(0f, 0f, 6.2f);
            BoxCollider radCol = radioRoot.AddComponent<BoxCollider>();
            radCol.isTrigger = true;
            radCol.size = new Vector3(2.5f, 2.0f, 2.5f);
            radCol.center = new Vector3(0, 1.0f, 0);
            radioRoot.AddComponent<RadioStation>();

            GameObject deskVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskVisual.name = "Visual_Model";
            deskVisual.transform.SetParent(radioRoot.transform);
            deskVisual.transform.localPosition = new Vector3(0, 0.45f, 0);
            deskVisual.transform.localScale = new Vector3(1.6f, 0.9f, 0.8f);
            Object.DestroyImmediate(deskVisual.GetComponent<Collider>());
            if (woodMat != null) deskVisual.GetComponent<Renderer>().sharedMaterial = woodMat;

            GameObject radioVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            radioVisual.name = "Radio_Device";
            radioVisual.transform.SetParent(deskVisual.transform);
            radioVisual.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            radioVisual.transform.localScale = new Vector3(0.45f, 0.35f, 0.3f);
            Object.DestroyImmediate(radioVisual.GetComponent<Collider>());
            if (metalMat != null) radioVisual.GetComponent<Renderer>().sharedMaterial = metalMat;

            // 7. Outdoor Defense Barricades
            Material barrierMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Metal_Barrier.mat");
            CreateConcreteBarrier(safeHouse.transform, new Vector3(-2.8f, 0f, -9.5f), 12f, barrierMat ?? metalMat);
            CreateConcreteBarrier(safeHouse.transform, new Vector3(2.8f, 0f, -9.5f), -12f, barrierMat ?? metalMat);

            return safeHouse;
        }

        private static void CreateCivilianNPC(Transform parent, Vector3 pos)
        {
            GameObject civ = new GameObject("Civilian_NPC_Root");
            civ.transform.SetParent(parent);
            civ.transform.position = pos;

            NavMeshAgent agent = civ.AddComponent<NavMeshAgent>();
            agent.speed = 4.0f;
            agent.radius = 0.45f;
            agent.height = 2.0f;
            agent.stoppingDistance = 2.0f;

            CapsuleCollider col = civ.AddComponent<CapsuleCollider>();
            col.height = 2.0f;
            col.radius = 0.45f;
            col.center = new Vector3(0, 1.0f, 0);

            civ.AddComponent<CivilianFollower>();

            BoxCollider trig = civ.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.size = new Vector3(3.0f, 2.5f, 3.0f);
            trig.center = new Vector3(0, 1.0f, 0);

            StoryTriggerZone stz = civ.AddComponent<StoryTriggerZone>();
            SerializedObject soStz = new SerializedObject(stz);
            soStz.FindProperty("storyResourceName").stringValue = "chapter1_outbreak";
            soStz.FindProperty("startNodeId").stringValue = "node_civilian_encounter";
            soStz.FindProperty("triggerOnEnter").boolValue = false;
            soStz.FindProperty("triggerOnce").boolValue = true;
            soStz.FindProperty("interactionPromptText").stringValue = "E - Yaralı Sivil (Mert) ile Konuş";
            soStz.ApplyModifiedProperties();

            // FAZ 6: Articulated 3D Wounded Civilian Model with Animator
            var civConfig = ProceduralHumanoidBuilder.PlayerConfig();
            civConfig.height = 1.72f;
            civConfig.shirtColor = new Color(0.42f, 0.45f, 0.48f);
            civConfig.hasBackpack = false;
            civConfig.hasVest = false;
            civConfig.isHunched = true;
            civConfig.hunchAngle = 18f;
            GameObject visual = ProceduralHumanoidBuilder.CreateHumanoidModel(civ.transform, "Visual_Model", civConfig);
            Animator civAnim = visual.AddComponent<Animator>();
            civAnim.runtimeAnimatorController = AnimatorBuilder.CreatePlayerAnimatorController();
        }

        private static void CreateFullHUDCanvas()
        {
            // EventSystem (Unity 6 Input System compatible)
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();

            System.Type inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                eventSystem.AddComponent(inputSystemModuleType);
            }
            else
            {
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Canvas (1920x1080 Reference Resolution, Match 0.5)
            GameObject canvasObj = new GameObject("HUD_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<SafeAreaHandler>();

            // TouchLookArea at back of HUD hierarchy (SiblingIndex = 0)
            GameObject touchLookObj = new GameObject("TouchLookArea");
            touchLookObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtTouchLook = touchLookObj.AddComponent<RectTransform>();
            rtTouchLook.anchorMin = new Vector2(0.38f, 0f);
            rtTouchLook.anchorMax = Vector2.one;
            rtTouchLook.sizeDelta = Vector2.zero;
            Image touchLookImg = touchLookObj.AddComponent<Image>();
            touchLookImg.color = Color.clear;
            touchLookImg.raycastTarget = false;
            touchLookObj.transform.SetSiblingIndex(0);

            HUDController hud = canvasObj.AddComponent<HUDController>();
            canvasObj.AddComponent<FogOfWarManager>();
            canvasObj.AddComponent<CustomWaypointManager>();
            PauseMenuController pauseMenuCtrl = canvasObj.AddComponent<PauseMenuController>();
            canvasObj.AddComponent<UIInputPriorityFixer>();

            // Load procedural UI sprites & icons generated by AssetGenerator
            Sprite glassPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/panel_glass_9slice.png");
            Sprite circleRingSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/button_circle_ring.png");
            Sprite circleGlowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/button_circle_glow.png");
            Sprite circleRedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/button_circle_red.png");
            Sprite radialFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/ring_radial_fill.png");
            Sprite threatArcSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/threat_arc.png");
            Sprite iconDodge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_dodge.png");
            Sprite compassWaypointSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/compass_waypoint.png");

            Sprite iconSprint = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_sprint.png");
            Sprite iconCrouch = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_crouch.png");
            Sprite iconInteract = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_interact.png");
            Sprite iconFlashlight = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_flashlight.png");
            Sprite iconBackpack = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_backpack.png");
            Sprite iconCraft = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_craft.png");
            Sprite iconPush = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_push.png");
            Sprite iconPause = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_pause.png");

            Sprite iconHp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_hp.png");
            Sprite iconSta = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_stamina.png");
            Sprite iconHunger = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_hunger.png");
            Sprite iconThirst = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_thirst.png");
            Sprite iconInf = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_infection.png");

            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd") ?? knobSprite;

            // Step 2 Tactical Vital HUD Sprites
            Sprite vitalPanelBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/VitalHUD/vital_panel_bg.png") ?? glassPanelSprite;
            Sprite vitalBarFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/VitalHUD/vital_bar_frame.png") ?? glassPanelSprite;
            Sprite vitalBarFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/VitalHUD/vital_bar_fill.png") ?? uiSprite;
            Sprite iconHeart = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/VitalHUD/icon_heart.png") ?? iconHp;
            Sprite iconBolt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/VitalHUD/icon_bolt.png") ?? iconSta;
            Sprite iconVitalHunger = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/VitalHUD/icon_hunger.png") ?? iconHunger;
            Sprite iconVitalThirst = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/VitalHUD/icon_thirst.png") ?? iconThirst;
            Sprite iconVitalInfection = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/VitalHUD/icon_infection.png") ?? iconInf;
            Sprite iconRadialRing = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/VitalHUD/icon_radial_ring.png") ?? radialFillSprite;

            // Step 3 Tactical Minimap & Cartography Sprites & Textures
            Sprite minimapFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/Map/minimap_frame.png");
            Sprite minimapMaskSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/Map/minimap_mask.png");
            Sprite playerBlipSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/Map/player_blip.png");
            Sprite customPinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/Map/waypoint_custom_pin.png");
            Sprite zombieBlipSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/Map/zombie_radar_blip.png");
            Texture2D bakedCityMapTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/UI/Map/BakedCityMap.png");

            // 1. Screen-Center Stealth Threat Direction Indicator
            GameObject stealthObj = new GameObject("StealthIndicator_Panel");
            stealthObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtStealth = stealthObj.AddComponent<RectTransform>();
            rtStealth.anchorMin = new Vector2(0.5f, 0.5f);
            rtStealth.anchorMax = new Vector2(0.5f, 0.5f);
            rtStealth.pivot = new Vector2(0.5f, 0.5f);
            rtStealth.anchoredPosition = Vector2.zero;
            rtStealth.sizeDelta = new Vector2(360f, 360f);

            GameObject pArcObj = new GameObject("PrimaryThreatArc");
            pArcObj.transform.SetParent(stealthObj.transform, false);
            RectTransform rtPArc = pArcObj.AddComponent<RectTransform>();
            rtPArc.anchorMin = new Vector2(0.5f, 0.5f);
            rtPArc.anchorMax = new Vector2(0.5f, 0.5f);
            rtPArc.pivot = new Vector2(0.5f, 0.5f);
            rtPArc.anchoredPosition = Vector2.zero;
            rtPArc.sizeDelta = new Vector2(160f, 160f);
            Image pArcImg = pArcObj.AddComponent<Image>();
            pArcImg.sprite = threatArcSprite ?? circleRingSprite ?? knobSprite;
            pArcImg.color = new Color(1f, 1f, 1f, 0f);
            pArcImg.raycastTarget = false;

            GameObject sArcObj = new GameObject("SecondaryThreatArc");
            sArcObj.transform.SetParent(stealthObj.transform, false);
            RectTransform rtSArc = sArcObj.AddComponent<RectTransform>();
            rtSArc.anchorMin = new Vector2(0.5f, 0.5f);
            rtSArc.anchorMax = new Vector2(0.5f, 0.5f);
            rtSArc.pivot = new Vector2(0.5f, 0.5f);
            rtSArc.anchoredPosition = Vector2.zero;
            rtSArc.sizeDelta = new Vector2(160f, 160f);
            Image sArcImg = sArcObj.AddComponent<Image>();
            sArcImg.sprite = threatArcSprite ?? circleRingSprite ?? knobSprite;
            sArcImg.color = new Color(1f, 1f, 1f, 0f);
            sArcImg.raycastTarget = false;

            StealthIndicatorUI stealthUI = stealthObj.AddComponent<StealthIndicatorUI>();
            SerializedObject soStealth = new SerializedObject(stealthUI);
            soStealth.FindProperty("primaryThreatArc").objectReferenceValue = pArcImg;
            soStealth.FindProperty("secondaryThreatArc").objectReferenceValue = sArcImg;
            soStealth.FindProperty("arcDistance").floatValue = 140f;
            soStealth.FindProperty("maxDetectionDistance").floatValue = 24f;
            soStealth.ApplyModifiedProperties();

            // 2. Top-Center Minimalist Compass Bar (Replaces old square minimap)
            GameObject compassPanel = new GameObject("CompassBar_Panel");
            compassPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtCompass = compassPanel.AddComponent<RectTransform>();
            rtCompass.anchorMin = new Vector2(0.5f, 1f);
            rtCompass.anchorMax = new Vector2(0.5f, 1f);
            rtCompass.pivot = new Vector2(0.5f, 1f);
            rtCompass.anchoredPosition = new Vector2(0f, -20f);
            rtCompass.sizeDelta = new Vector2(440f, 38f);

            Image compassBg = compassPanel.AddComponent<Image>();
            compassBg.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);
            if (glassPanelSprite != null)
            {
                compassBg.sprite = glassPanelSprite;
                compassBg.type = Image.Type.Sliced;
            }
            compassBg.raycastTarget = false;

            GameObject centerNotch = new GameObject("CenterNotch");
            centerNotch.transform.SetParent(compassPanel.transform, false);
            RectTransform rtNotch = centerNotch.AddComponent<RectTransform>();
            rtNotch.anchorMin = new Vector2(0.5f, 1f);
            rtNotch.anchorMax = new Vector2(0.5f, 1f);
            rtNotch.pivot = new Vector2(0.5f, 1f);
            rtNotch.anchoredPosition = new Vector2(0f, -1f);
            rtNotch.sizeDelta = new Vector2(3f, 10f);
            Image notchImg = centerNotch.AddComponent<Image>();
            notchImg.color = new Color(1f, 0.85f, 0.35f, 0.95f);
            notchImg.raycastTarget = false;

            GameObject markersMask = new GameObject("MarkersMask");
            markersMask.transform.SetParent(compassPanel.transform, false);
            RectTransform rtMask = markersMask.AddComponent<RectTransform>();
            rtMask.anchorMin = Vector2.zero;
            rtMask.anchorMax = Vector2.one;
            rtMask.offsetMin = new Vector2(10f, 0f);
            rtMask.offsetMax = new Vector2(-10f, 0f);
            Image maskImg = markersMask.AddComponent<Image>();
            maskImg.color = new Color(1f, 1f, 1f, 0.05f);
            Mask maskComp = markersMask.AddComponent<Mask>();
            maskComp.showMaskGraphic = false;

            GameObject markersParent = new GameObject("CardinalMarkersParent");
            markersParent.transform.SetParent(markersMask.transform, false);
            RectTransform rtMarkers = markersParent.AddComponent<RectTransform>();
            rtMarkers.anchorMin = new Vector2(0.5f, 0.5f);
            rtMarkers.anchorMax = new Vector2(0.5f, 0.5f);
            rtMarkers.pivot = new Vector2(0.5f, 0.5f);
            rtMarkers.sizeDelta = new Vector2(420f, 36f);

            GameObject wpMarkerObj = new GameObject("WaypointMarker");
            wpMarkerObj.transform.SetParent(markersMask.transform, false);
            RectTransform rtWp = wpMarkerObj.AddComponent<RectTransform>();
            rtWp.anchorMin = new Vector2(0.5f, 0.5f);
            rtWp.anchorMax = new Vector2(0.5f, 0.5f);
            rtWp.pivot = new Vector2(0.5f, 0.5f);
            rtWp.anchoredPosition = Vector2.zero;
            rtWp.sizeDelta = new Vector2(18f, 18f);

            Image wpImg = wpMarkerObj.AddComponent<Image>();
            wpImg.sprite = compassWaypointSprite ?? knobSprite;
            wpImg.color = new Color(1f, 0.85f, 0.25f, 0.95f);
            wpImg.raycastTarget = false;

            Text wpDistTxt = CreateUIText(wpMarkerObj.transform, "DistanceText", "42m", 10, TextAnchor.UpperCenter, new Color(1f, 0.92f, 0.5f));
            wpDistTxt.fontStyle = FontStyle.Bold;
            wpDistTxt.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            wpDistTxt.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            wpDistTxt.rectTransform.pivot = new Vector2(0.5f, 1f);
            wpDistTxt.rectTransform.anchoredPosition = new Vector2(0f, -2f);
            wpDistTxt.rectTransform.sizeDelta = new Vector2(60f, 14f);

            // Custom Waypoint Marker on Compass Bar
            GameObject customWpMarkerObj = new GameObject("CustomWaypointMarker");
            customWpMarkerObj.transform.SetParent(markersMask.transform, false);
            RectTransform rtCustomWp = customWpMarkerObj.AddComponent<RectTransform>();
            rtCustomWp.anchorMin = new Vector2(0.5f, 0.5f);
            rtCustomWp.anchorMax = new Vector2(0.5f, 0.5f);
            rtCustomWp.pivot = new Vector2(0.5f, 0.5f);
            rtCustomWp.anchoredPosition = Vector2.zero;
            rtCustomWp.sizeDelta = new Vector2(18f, 18f);

            Image customWpImg = customWpMarkerObj.AddComponent<Image>();
            customWpImg.sprite = customPinSprite ?? compassWaypointSprite ?? knobSprite;
            customWpImg.color = new Color(1.0f, 0.72f, 0.01f, 0.95f);
            customWpImg.raycastTarget = false;

            Text customWpDistTxt = CreateUIText(customWpMarkerObj.transform, "DistanceText", "", 10, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.2f));
            customWpDistTxt.fontStyle = FontStyle.Bold;
            customWpDistTxt.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            customWpDistTxt.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            customWpDistTxt.rectTransform.pivot = new Vector2(0.5f, 1f);
            customWpDistTxt.rectTransform.anchoredPosition = new Vector2(0f, -2f);
            customWpDistTxt.rectTransform.sizeDelta = new Vector2(60f, 14f);
            customWpMarkerObj.SetActive(false);

            CompassBarUI compassUI = compassPanel.AddComponent<CompassBarUI>();
            SerializedObject soComp = new SerializedObject(compassUI);
            soComp.FindProperty("compassContainer").objectReferenceValue = rtCompass;
            soComp.FindProperty("cardinalMarkersParent").objectReferenceValue = rtMarkers;
            soComp.FindProperty("waypointMarker").objectReferenceValue = rtWp;
            soComp.FindProperty("waypointDistanceText").objectReferenceValue = wpDistTxt;
            soComp.FindProperty("customWaypointMarker").objectReferenceValue = rtCustomWp;
            soComp.FindProperty("customWaypointDistanceText").objectReferenceValue = customWpDistTxt;
            soComp.FindProperty("compassWidth").floatValue = 420f;
            soComp.FindProperty("fovDegrees").floatValue = 160f;
            soComp.ApplyModifiedProperties();

            // 3. Top-Left Compact Quest Tracker Panel
            GameObject questPanel = new GameObject("QuestTracker_Panel");
            questPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtQuest = questPanel.AddComponent<RectTransform>();
            rtQuest.anchorMin = new Vector2(0f, 1f);
            rtQuest.anchorMax = new Vector2(0f, 1f);
            rtQuest.pivot = new Vector2(0f, 1f);
            rtQuest.anchoredPosition = new Vector2(35f, -25f);
            rtQuest.sizeDelta = new Vector2(290f, 85f);

            Image questBg = questPanel.AddComponent<Image>();
            questBg.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);
            if (glassPanelSprite != null)
            {
                questBg.sprite = glassPanelSprite;
                questBg.type = Image.Type.Sliced;
            }
            questBg.raycastTarget = false;

            GameObject dotObj = new GameObject("StatusDot");
            dotObj.transform.SetParent(questPanel.transform, false);
            RectTransform rtDot = dotObj.AddComponent<RectTransform>();
            rtDot.anchorMin = new Vector2(0f, 1f);
            rtDot.anchorMax = new Vector2(0f, 1f);
            rtDot.pivot = new Vector2(0f, 1f);
            rtDot.anchoredPosition = new Vector2(12f, -11f);
            rtDot.sizeDelta = new Vector2(7f, 7f);
            Image dotImg = dotObj.AddComponent<Image>();
            dotImg.color = new Color(0.2f, 0.85f, 0.45f);
            if (circleGlowSprite != null) dotImg.sprite = circleGlowSprite;
            else if (knobSprite != null) dotImg.sprite = knobSprite;
            dotImg.raycastTarget = false;

            Text questHeader = CreateUIText(questPanel.transform, "QuestHeader", "AKTİF GÖREV", 12, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f));
            questHeader.rectTransform.anchorMin = new Vector2(0f, 1f);
            questHeader.rectTransform.anchorMax = new Vector2(0f, 1f);
            questHeader.rectTransform.pivot = new Vector2(0f, 1f);
            questHeader.rectTransform.anchoredPosition = new Vector2(24f, -6f);
            questHeader.rectTransform.sizeDelta = new Vector2(255f, 16f);

            Text questTitle = CreateUIText(questPanel.transform, "QuestTitle", "Serbest Keşif", 15, TextAnchor.MiddleLeft, Color.white);
            questTitle.fontStyle = FontStyle.Bold;
            questTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            questTitle.rectTransform.anchorMax = new Vector2(0f, 1f);
            questTitle.rectTransform.pivot = new Vector2(0f, 1f);
            questTitle.rectTransform.anchoredPosition = new Vector2(12f, -25f);
            questTitle.rectTransform.sizeDelta = new Vector2(270f, 18f);

            Text questDesc = CreateUIText(questPanel.transform, "QuestDesc", "Çevredeki hayatta kalanları araştır.", 12, TextAnchor.MiddleLeft, new Color(0.8f, 0.85f, 0.9f));
            questDesc.rectTransform.anchorMin = new Vector2(0f, 1f);
            questDesc.rectTransform.anchorMax = new Vector2(0f, 1f);
            questDesc.rectTransform.pivot = new Vector2(0f, 1f);
            questDesc.rectTransform.anchoredPosition = new Vector2(12f, -44f);
            questDesc.rectTransform.sizeDelta = new Vector2(270f, 16f);

            Text questDist = CreateUIText(questPanel.transform, "QuestDistance", "Yeni Görevler Bekleniyor", 12, TextAnchor.MiddleLeft, new Color(0.3f, 0.8f, 1f));
            questDist.fontStyle = FontStyle.Bold;
            questDist.rectTransform.anchorMin = new Vector2(0f, 1f);
            questDist.rectTransform.anchorMax = new Vector2(0f, 1f);
            questDist.rectTransform.pivot = new Vector2(0f, 1f);
            questDist.rectTransform.anchoredPosition = new Vector2(12f, -62f);
            questDist.rectTransform.sizeDelta = new Vector2(270f, 16f);

            QuestTrackerUI questTrackerUI = questPanel.AddComponent<QuestTrackerUI>();
            SerializedObject soQuest = new SerializedObject(questTrackerUI);
            soQuest.FindProperty("panelRoot").objectReferenceValue = questPanel;
            soQuest.FindProperty("missionHeaderLabel").objectReferenceValue = questHeader;
            soQuest.FindProperty("missionTitleText").objectReferenceValue = questTitle;
            soQuest.FindProperty("missionDescText").objectReferenceValue = questDesc;
            soQuest.FindProperty("missionDistanceText").objectReferenceValue = questDist;
            soQuest.FindProperty("statusIndicatorDot").objectReferenceValue = dotImg;
            soQuest.ApplyModifiedProperties();

            // 3b. Top-Left Companion Status Card (Under Quest Tracker Panel)
            GameObject companionCard = new GameObject("CompanionCard_Panel");
            companionCard.transform.SetParent(canvasObj.transform, false);
            RectTransform rtCompanion = companionCard.AddComponent<RectTransform>();
            rtCompanion.anchorMin = new Vector2(0f, 1f);
            rtCompanion.anchorMax = new Vector2(0f, 1f);
            rtCompanion.pivot = new Vector2(0f, 1f);
            rtCompanion.anchoredPosition = new Vector2(35f, -118f);
            rtCompanion.sizeDelta = new Vector2(290f, 74f);

            Image compBg = companionCard.AddComponent<Image>();
            compBg.color = new Color(0.06f, 0.08f, 0.12f, 0.90f);
            if (glassPanelSprite != null)
            {
                compBg.sprite = glassPanelSprite;
                compBg.type = Image.Type.Sliced;
            }

            // Status Dot
            GameObject compDot = new GameObject("StatusDot");
            compDot.transform.SetParent(companionCard.transform, false);
            RectTransform rtCompDot = compDot.AddComponent<RectTransform>();
            rtCompDot.anchorMin = new Vector2(0f, 1f);
            rtCompDot.anchorMax = new Vector2(0f, 1f);
            rtCompDot.pivot = new Vector2(0f, 1f);
            rtCompDot.anchoredPosition = new Vector2(10f, -8f);
            rtCompDot.sizeDelta = new Vector2(6f, 6f);
            Image compDotImg = compDot.AddComponent<Image>();
            compDotImg.color = new Color(0.2f, 0.85f, 0.45f);
            if (circleGlowSprite != null) compDotImg.sprite = circleGlowSprite;
            else if (knobSprite != null) compDotImg.sprite = knobSprite;
            compDotImg.raycastTarget = false;

            // Companion Name
            Text compName = CreateUIText(companionCard.transform, "CompanionName", "Yoldaş", 12, TextAnchor.MiddleLeft, new Color(0.4f, 0.9f, 0.65f));
            compName.fontStyle = FontStyle.Bold;
            compName.rectTransform.anchorMin = new Vector2(0f, 1f);
            compName.rectTransform.anchorMax = new Vector2(0f, 1f);
            compName.rectTransform.pivot = new Vector2(0f, 1f);
            compName.rectTransform.anchoredPosition = new Vector2(22f, -4f);
            compName.rectTransform.sizeDelta = new Vector2(255f, 15f);

            // Health Bar Slider
            GameObject compSliderObj = new GameObject("HealthBar");
            compSliderObj.transform.SetParent(companionCard.transform, false);
            RectTransform rtCompSlider = compSliderObj.AddComponent<RectTransform>();
            rtCompSlider.anchorMin = new Vector2(0f, 1f);
            rtCompSlider.anchorMax = new Vector2(0f, 1f);
            rtCompSlider.pivot = new Vector2(0f, 1f);
            rtCompSlider.anchoredPosition = new Vector2(10f, -22f);
            rtCompSlider.sizeDelta = new Vector2(270f, 13f);

            Slider compSlider = compSliderObj.AddComponent<Slider>();
            compSlider.interactable = false;
            compSlider.transition = Selectable.Transition.None;

            // Slider BG
            GameObject compSliderBg = new GameObject("Background");
            compSliderBg.transform.SetParent(compSliderObj.transform, false);
            RectTransform rtCompSliderBg = compSliderBg.AddComponent<RectTransform>();
            rtCompSliderBg.anchorMin = Vector2.zero;
            rtCompSliderBg.anchorMax = Vector2.one;
            rtCompSliderBg.sizeDelta = Vector2.zero;
            Image compSliderBgImg = compSliderBg.AddComponent<Image>();
            compSliderBgImg.color = new Color(0.12f, 0.15f, 0.20f, 0.9f);
            if (glassPanelSprite != null) compSliderBgImg.sprite = glassPanelSprite;
            compSliderBgImg.raycastTarget = false;

            // Slider Fill Area
            GameObject compFillArea = new GameObject("Fill Area");
            compFillArea.transform.SetParent(compSliderObj.transform, false);
            RectTransform rtCompFillArea = compFillArea.AddComponent<RectTransform>();
            rtCompFillArea.anchorMin = Vector2.zero;
            rtCompFillArea.anchorMax = Vector2.one;
            rtCompFillArea.sizeDelta = Vector2.zero;

            GameObject compFill = new GameObject("Fill");
            compFill.transform.SetParent(compFillArea.transform, false);
            RectTransform rtCompFill = compFill.AddComponent<RectTransform>();
            rtCompFill.anchorMin = Vector2.zero;
            rtCompFill.anchorMax = Vector2.one;
            rtCompFill.sizeDelta = Vector2.zero;
            Image compFillImg = compFill.AddComponent<Image>();
            compFillImg.color = new Color(0.2f, 0.85f, 0.45f);
            if (glassPanelSprite != null) compFillImg.sprite = glassPanelSprite;
            compFillImg.raycastTarget = false;

            compSlider.fillRect = rtCompFill;
            compSlider.minValue = 0f;
            compSlider.maxValue = 100f;
            compSlider.value = 100f;

            // Health Text
            Text compHealthText = CreateUIText(compSliderObj.transform, "HealthText", "100 / 100", 10, TextAnchor.MiddleCenter, Color.white);
            compHealthText.fontStyle = FontStyle.Bold;
            compHealthText.rectTransform.anchorMin = Vector2.zero;
            compHealthText.rectTransform.anchorMax = Vector2.one;
            compHealthText.rectTransform.sizeDelta = Vector2.zero;

            // Button 1: Order (BEKLE / TAKİP ET)
            Button btnOrder = CreateCustomButton(
                companionCard.transform, "Btn_Order", "BEKLE",
                new Vector2(10f, -44f), new Vector2(130f, 22f),
                new Color(0.18f, 0.24f, 0.35f, 0.95f),
                glassPanelSprite ?? knobSprite,
                null, 11,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f)
            );

            // Button 2: Heal (İYİLEŞTİR)
            Button btnHeal = CreateCustomButton(
                companionCard.transform, "Btn_Heal", "+ İYİLEŞTİR",
                new Vector2(150f, -44f), new Vector2(130f, 22f),
                new Color(0.15f, 0.32f, 0.22f, 0.95f),
                glassPanelSprite ?? knobSprite,
                null, 11,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f)
            );

            // Wire CompanionManager
            ProjectSurvival.AI.CompanionManager companionManager = canvasObj.AddComponent<ProjectSurvival.AI.CompanionManager>();
            SerializedObject soCompanion = new SerializedObject(companionManager);
            soCompanion.FindProperty("companionCardRoot").objectReferenceValue = companionCard;
            soCompanion.FindProperty("companionNameText").objectReferenceValue = compName;
            soCompanion.FindProperty("companionHealthSlider").objectReferenceValue = compSlider;
            soCompanion.FindProperty("companionHealthText").objectReferenceValue = compHealthText;
            soCompanion.FindProperty("orderButton").objectReferenceValue = btnOrder;
            soCompanion.FindProperty("orderButtonText").objectReferenceValue = btnOrder.GetComponentInChildren<Text>();
            soCompanion.FindProperty("healButton").objectReferenceValue = btnHeal;
            soCompanion.ApplyModifiedProperties();

            btnOrder.onClick.AddListener(companionManager.ToggleOrder);
            btnHeal.onClick.AddListener(companionManager.HealCompanion);

            companionCard.SetActive(false);

            // 4. Top-Right Buttons (SafeArea aligned: Pause at -36,-36, size 58x58; Backpack at -104,-36, size 54x54)
            Button btnBackpack = CreateCustomButton(
                canvasObj.transform, "Btn_Backpack", "",
                new Vector2(-104f, -36f), new Vector2(54f, 54f),
                new Color(0.12f, 0.16f, 0.22f, 0.9f),
                circleRingSprite ?? knobSprite,
                iconBackpack, 14,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f)
            );

            Button btnPause = CreateCustomButton(
                canvasObj.transform, "Btn_Pause", "",
                new Vector2(-36f, -36f), new Vector2(58f, 58f),
                new Color(0.12f, 0.16f, 0.22f, 0.95f),
                circleRingSprite ?? knobSprite,
                iconPause, 16,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f)
            );
            btnPause.transform.SetAsLastSibling();

            // 4b. Top-Right Live Tactical Minimap (Anchor: Top-Right, Pos: -32, -104, Size: 176x176)
            GameObject minimapObj = new GameObject("TacticalMinimap_Panel");
            minimapObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtMinimap = minimapObj.AddComponent<RectTransform>();
            rtMinimap.anchorMin = new Vector2(1f, 1f);
            rtMinimap.anchorMax = new Vector2(1f, 1f);
            rtMinimap.pivot = new Vector2(1f, 1f);
            rtMinimap.anchoredPosition = new Vector2(-32f, -104f);
            rtMinimap.sizeDelta = new Vector2(176f, 176f);

            Button btnOpenMinimap = minimapObj.AddComponent<Button>();
            Image minimapTouchTrigger = minimapObj.AddComponent<Image>();
            minimapTouchTrigger.color = new Color(0f, 0f, 0f, 0.005f); // Invisible raycast target for touch
            minimapTouchTrigger.raycastTarget = true;

            // Viewport Mask Container (164x164)
            GameObject maskObj = new GameObject("Minimap_Mask");
            maskObj.transform.SetParent(minimapObj.transform, false);
            RectTransform rtMinimapMask = maskObj.AddComponent<RectTransform>();
            rtMinimapMask.anchorMin = new Vector2(0.5f, 0.5f);
            rtMinimapMask.anchorMax = new Vector2(0.5f, 0.5f);
            rtMinimapMask.pivot = new Vector2(0.5f, 0.5f);
            rtMinimapMask.anchoredPosition = Vector2.zero;
            rtMinimapMask.sizeDelta = new Vector2(164f, 164f);

            Image minimapMaskImg = maskObj.AddComponent<Image>();
            minimapMaskImg.sprite = minimapMaskSprite ?? circleRingSprite ?? knobSprite;
            minimapMaskImg.color = Color.white;
            minimapMaskImg.raycastTarget = false;
            Mask minimapMask = maskObj.AddComponent<Mask>();
            minimapMask.showMaskGraphic = false;

            // Map Rotator (Rotates with Camera Heading)
            GameObject rotatorObj = new GameObject("Map_Rotator");
            rotatorObj.transform.SetParent(maskObj.transform, false);
            RectTransform rtRotator = rotatorObj.AddComponent<RectTransform>();
            rtRotator.anchorMin = new Vector2(0.5f, 0.5f);
            rtRotator.anchorMax = new Vector2(0.5f, 0.5f);
            rtRotator.pivot = new Vector2(0.5f, 0.5f);
            rtRotator.anchoredPosition = Vector2.zero;
            rtRotator.sizeDelta = Vector2.zero;

            // Map Content (564x564, 240m city at 2.35px/m)
            GameObject contentObj = new GameObject("Map_Content");
            contentObj.transform.SetParent(rotatorObj.transform, false);
            RectTransform rtContent = contentObj.AddComponent<RectTransform>();
            rtContent.anchorMin = new Vector2(0.5f, 0.5f);
            rtContent.anchorMax = new Vector2(0.5f, 0.5f);
            rtContent.pivot = new Vector2(0.5f, 0.5f);
            rtContent.anchoredPosition = Vector2.zero;
            rtContent.sizeDelta = new Vector2(564f, 564f);

            // Layer 1: Base City Cartography Map
            GameObject baseMapObj = new GameObject("BaseMap");
            baseMapObj.transform.SetParent(contentObj.transform, false);
            RectTransform rtBaseMap = baseMapObj.AddComponent<RectTransform>();
            rtBaseMap.anchorMin = Vector2.zero;
            rtBaseMap.anchorMax = Vector2.one;
            rtBaseMap.sizeDelta = Vector2.zero;
            RawImage rawBaseMap = baseMapObj.AddComponent<RawImage>();
            if (bakedCityMapTex != null) rawBaseMap.texture = bakedCityMapTex;
            rawBaseMap.color = new Color(0.9f, 0.95f, 1.0f, 1.0f);
            rawBaseMap.raycastTarget = false;

            // Layer 2: Live Fog of War Overlay
            GameObject fogMapObj = new GameObject("FogOfWar");
            fogMapObj.transform.SetParent(contentObj.transform, false);
            RectTransform rtFogMap = fogMapObj.AddComponent<RectTransform>();
            rtFogMap.anchorMin = Vector2.zero;
            rtFogMap.anchorMax = Vector2.one;
            rtFogMap.sizeDelta = Vector2.zero;
            RawImage rawFogMap = fogMapObj.AddComponent<RawImage>();
            rawFogMap.color = Color.white;
            rawFogMap.raycastTarget = false;

            // Layer 3: POI Container
            GameObject poiContainerObj = new GameObject("POI_Container");
            poiContainerObj.transform.SetParent(contentObj.transform, false);
            RectTransform rtPoiCont = poiContainerObj.AddComponent<RectTransform>();
            rtPoiCont.anchorMin = Vector2.zero;
            rtPoiCont.anchorMax = Vector2.one;
            rtPoiCont.sizeDelta = Vector2.zero;

            // Layer 4: Zombie Blips Container
            GameObject zombieContainerObj = new GameObject("Zombie_Container");
            zombieContainerObj.transform.SetParent(contentObj.transform, false);
            RectTransform rtZombieCont = zombieContainerObj.AddComponent<RectTransform>();
            rtZombieCont.anchorMin = Vector2.zero;
            rtZombieCont.anchorMax = Vector2.one;
            rtZombieCont.sizeDelta = Vector2.zero;

            // Layer 5: Objective Marker
            GameObject mObjMarker = new GameObject("ObjectiveMarker");
            mObjMarker.transform.SetParent(contentObj.transform, false);
            RectTransform rtMObj = mObjMarker.AddComponent<RectTransform>();
            rtMObj.anchorMin = new Vector2(0.5f, 0.5f);
            rtMObj.anchorMax = new Vector2(0.5f, 0.5f);
            rtMObj.pivot = new Vector2(0.5f, 0.5f);
            rtMObj.sizeDelta = new Vector2(18f, 18f);
            Image mObjImg = mObjMarker.AddComponent<Image>();
            mObjImg.sprite = circleGlowSprite ?? knobSprite;
            mObjImg.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            mObjImg.raycastTarget = false;
            mObjMarker.SetActive(false);

            // Layer 6: Custom Waypoint Marker
            GameObject mWpMarker = new GameObject("CustomWaypointMarker");
            mWpMarker.transform.SetParent(contentObj.transform, false);
            RectTransform rtMWp = mWpMarker.AddComponent<RectTransform>();
            rtMWp.anchorMin = new Vector2(0.5f, 0.5f);
            rtMWp.anchorMax = new Vector2(0.5f, 0.5f);
            rtMWp.pivot = new Vector2(0.5f, 0.5f);
            rtMWp.sizeDelta = new Vector2(18f, 18f);
            Image mWpImg = mWpMarker.AddComponent<Image>();
            mWpImg.sprite = customPinSprite ?? knobSprite;
            mWpImg.color = new Color(1.0f, 0.72f, 0.01f, 0.95f);
            mWpImg.raycastTarget = false;
            mWpMarker.SetActive(false);

            // Center Player Blip (24x24)
            GameObject pBlipObj = new GameObject("Player_Blip");
            pBlipObj.transform.SetParent(minimapObj.transform, false);
            RectTransform rtPBlip = pBlipObj.AddComponent<RectTransform>();
            rtPBlip.anchorMin = new Vector2(0.5f, 0.5f);
            rtPBlip.anchorMax = new Vector2(0.5f, 0.5f);
            rtPBlip.pivot = new Vector2(0.5f, 0.5f);
            rtPBlip.anchoredPosition = Vector2.zero;
            rtPBlip.sizeDelta = new Vector2(24f, 24f);
            Image pBlipImg = pBlipObj.AddComponent<Image>();
            pBlipImg.sprite = playerBlipSprite ?? compassWaypointSprite ?? knobSprite;
            pBlipImg.color = Color.white;
            pBlipImg.raycastTarget = false;

            // GPS Bezel Frame (176x176)
            GameObject frameObj = new GameObject("Minimap_Frame");
            frameObj.transform.SetParent(minimapObj.transform, false);
            RectTransform rtFrame = frameObj.AddComponent<RectTransform>();
            rtFrame.anchorMin = new Vector2(0.5f, 0.5f);
            rtFrame.anchorMax = new Vector2(0.5f, 0.5f);
            rtFrame.pivot = new Vector2(0.5f, 0.5f);
            rtFrame.anchoredPosition = Vector2.zero;
            rtFrame.sizeDelta = new Vector2(176f, 176f);
            Image frameImg = frameObj.AddComponent<Image>();
            frameImg.sprite = minimapFrameSprite ?? circleRingSprite ?? knobSprite;
            frameImg.color = Color.white;
            frameImg.raycastTarget = false;

            Text CreateMinimapCardinal(string label, Color col)
            {
                Text ct = CreateUIText(frameObj.transform, "Card_" + label, label, 11, TextAnchor.MiddleCenter, col);
                ct.fontStyle = FontStyle.Bold;
                ct.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                ct.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ct.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                ct.rectTransform.sizeDelta = new Vector2(18f, 16f);
                ct.raycastTarget = false;
                return ct;
            }

            Text cardK = CreateMinimapCardinal("K", new Color(0.95f, 0.15f, 0.25f, 1f));
            Text cardD = CreateMinimapCardinal("D", new Color(0.30f, 0.79f, 0.94f, 0.95f));
            Text cardG = CreateMinimapCardinal("G", new Color(0.65f, 0.72f, 0.82f, 0.85f));
            Text cardB = CreateMinimapCardinal("B", new Color(0.30f, 0.79f, 0.94f, 0.95f));

            // Bottom "TAM HARİTA" Badge
            Text badgeText = CreateUIText(frameObj.transform, "BadgeText", "TAM HARİTA", 9, TextAnchor.MiddleCenter, new Color(0.35f, 0.85f, 1.0f, 0.95f));
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            badgeText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            badgeText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            badgeText.rectTransform.anchoredPosition = new Vector2(0f, 18f);
            badgeText.rectTransform.sizeDelta = new Vector2(80f, 16f);
            badgeText.raycastTarget = false;

            MinimapHUDWidget minimapWidget = minimapObj.AddComponent<MinimapHUDWidget>();
            minimapWidget.Configure(rtMinimap, btnOpenMinimap, rtMinimapMask, rtRotator, rtContent, rawBaseMap, rawFogMap,
                frameImg, rtPBlip, cardK, cardD, cardG, cardB, badgeText, rtMObj, rtMWp, rtPoiCont, rtZombieCont);

            // 5. Bottom-Left AAA Tactical Survival Vital HUD (Above Mobile Joystick)
            GameObject vitalPanelObj = new GameObject("TacticalVitalHUD_Panel");
            vitalPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtVital = vitalPanelObj.AddComponent<RectTransform>();
            rtVital.anchorMin = new Vector2(0f, 0f);
            rtVital.anchorMax = new Vector2(0f, 0f);
            rtVital.pivot = new Vector2(0f, 0f);
            rtVital.anchoredPosition = new Vector2(36f, 255f);
            rtVital.sizeDelta = new Vector2(340f, 118f);

            Image vitalPanelBgImg = vitalPanelObj.AddComponent<Image>();
            vitalPanelBgImg.sprite = vitalPanelBg;
            vitalPanelBgImg.type = Image.Type.Sliced;
            vitalPanelBgImg.color = Color.white;
            vitalPanelBgImg.raycastTarget = false;

            // --- A. HEALTH BAR (22px high, 4-layer stacked fill) ---
            GameObject hpContainerObj = new GameObject("HealthBar_Container");
            hpContainerObj.transform.SetParent(vitalPanelObj.transform, false);
            RectTransform rtHpContainer = hpContainerObj.AddComponent<RectTransform>();
            rtHpContainer.anchorMin = new Vector2(0f, 1f);
            rtHpContainer.anchorMax = new Vector2(0f, 1f);
            rtHpContainer.pivot = new Vector2(0f, 1f);
            rtHpContainer.anchoredPosition = new Vector2(14f, -10f);
            rtHpContainer.sizeDelta = new Vector2(312f, 22f);

            // Health Heart Icon
            GameObject hpIconObj = new GameObject("Icon_Heart");
            hpIconObj.transform.SetParent(hpContainerObj.transform, false);
            RectTransform rtHpIcon = hpIconObj.AddComponent<RectTransform>();
            rtHpIcon.anchorMin = new Vector2(0f, 0.5f);
            rtHpIcon.anchorMax = new Vector2(0f, 0.5f);
            rtHpIcon.pivot = new Vector2(0f, 0.5f);
            rtHpIcon.anchoredPosition = new Vector2(0f, 0f);
            rtHpIcon.sizeDelta = new Vector2(20f, 20f);
            Image hpIconImg = hpIconObj.AddComponent<Image>();
            hpIconImg.sprite = iconHeart;
            hpIconImg.color = new Color(0.95f, 0.15f, 0.25f, 1f);
            hpIconImg.raycastTarget = false;

            // Health Bar Box & Layers
            GameObject hpBarBox = new GameObject("Bar_Box");
            hpBarBox.transform.SetParent(hpContainerObj.transform, false);
            RectTransform rtHpBarBox = hpBarBox.AddComponent<RectTransform>();
            rtHpBarBox.anchorMin = new Vector2(0f, 0f);
            rtHpBarBox.anchorMax = new Vector2(1f, 1f);
            rtHpBarBox.offsetMin = new Vector2(26f, 0f);
            rtHpBarBox.offsetMax = new Vector2(0f, 0f);

            // Layer 1: Recessed Track
            GameObject hpTrackObj = new GameObject("Track");
            hpTrackObj.transform.SetParent(hpBarBox.transform, false);
            RectTransform rtHpTrack = hpTrackObj.AddComponent<RectTransform>();
            rtHpTrack.anchorMin = Vector2.zero;
            rtHpTrack.anchorMax = Vector2.one;
            rtHpTrack.sizeDelta = Vector2.zero;
            Image hpTrackImg = hpTrackObj.AddComponent<Image>();
            hpTrackImg.sprite = vitalBarFrame;
            hpTrackImg.type = Image.Type.Sliced;
            hpTrackImg.color = new Color(0.11f, 0.07f, 0.09f, 0.95f);
            hpTrackImg.raycastTarget = false;

            // Layer 2: Ghost Damage Trail
            GameObject hpGhostObj = new GameObject("GhostFill");
            hpGhostObj.transform.SetParent(hpBarBox.transform, false);
            RectTransform rtHpGhost = hpGhostObj.AddComponent<RectTransform>();
            rtHpGhost.anchorMin = Vector2.zero;
            rtHpGhost.anchorMax = Vector2.one;
            rtHpGhost.offsetMin = new Vector2(2f, 2f);
            rtHpGhost.offsetMax = new Vector2(-2f, -2f);
            Image hpGhostFillImg = hpGhostObj.AddComponent<Image>();
            hpGhostFillImg.sprite = vitalBarFill;
            hpGhostFillImg.type = Image.Type.Filled;
            hpGhostFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpGhostFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpGhostFillImg.fillAmount = 1.0f;
            hpGhostFillImg.color = new Color(1f, 0.72f, 0.01f, 0.85f);
            hpGhostFillImg.raycastTarget = false;

            // Layer 3: Primary Health Fill
            GameObject hpFillObj = new GameObject("PrimaryFill");
            hpFillObj.transform.SetParent(hpBarBox.transform, false);
            RectTransform rtHpFill = hpFillObj.AddComponent<RectTransform>();
            rtHpFill.anchorMin = Vector2.zero;
            rtHpFill.anchorMax = Vector2.one;
            rtHpFill.offsetMin = new Vector2(2f, 2f);
            rtHpFill.offsetMax = new Vector2(-2f, -2f);
            Image hpPrimaryFillImg = hpFillObj.AddComponent<Image>();
            hpPrimaryFillImg.sprite = vitalBarFill;
            hpPrimaryFillImg.type = Image.Type.Filled;
            hpPrimaryFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpPrimaryFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpPrimaryFillImg.fillAmount = 1.0f;
            hpPrimaryFillImg.color = new Color(0.85f, 0.02f, 0.16f, 1f);
            hpPrimaryFillImg.raycastTarget = false;

            // Layer 4: Heal Shimmer Flash
            GameObject hpShimmerObj = new GameObject("HealShimmer");
            hpShimmerObj.transform.SetParent(hpBarBox.transform, false);
            RectTransform rtHpShimmer = hpShimmerObj.AddComponent<RectTransform>();
            rtHpShimmer.anchorMin = Vector2.zero;
            rtHpShimmer.anchorMax = Vector2.one;
            rtHpShimmer.offsetMin = new Vector2(2f, 2f);
            rtHpShimmer.offsetMax = new Vector2(-2f, -2f);
            Image hpShimmerImg = hpShimmerObj.AddComponent<Image>();
            hpShimmerImg.sprite = vitalBarFill;
            hpShimmerImg.type = Image.Type.Sliced;
            hpShimmerImg.color = new Color(0.50f, 0.93f, 0.60f, 0f);
            hpShimmerImg.raycastTarget = false;

            // Health Typography
            Text hpText = CreateUIText(hpBarBox.transform, "HealthText", "CAN  100 / 100", 11, TextAnchor.MiddleLeft, new Color(0.97f, 0.98f, 0.98f, 1f));
            hpText.fontStyle = FontStyle.Bold;
            hpText.rectTransform.anchorMin = Vector2.zero;
            hpText.rectTransform.anchorMax = Vector2.one;
            hpText.rectTransform.offsetMin = new Vector2(8f, 0f);
            hpText.rectTransform.offsetMax = new Vector2(-8f, 0f);
            hpText.raycastTarget = false;
            Shadow hpShadow = hpText.gameObject.AddComponent<Shadow>();
            hpShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            hpShadow.effectDistance = new Vector2(1f, -1f);

            // --- B. ENERGY / STAMINA BAR (16px high, 3-layer stacked fill) ---
            GameObject staContainerObj = new GameObject("StaminaBar_Container");
            staContainerObj.transform.SetParent(vitalPanelObj.transform, false);
            RectTransform rtStaContainer = staContainerObj.AddComponent<RectTransform>();
            rtStaContainer.anchorMin = new Vector2(0f, 1f);
            rtStaContainer.anchorMax = new Vector2(0f, 1f);
            rtStaContainer.pivot = new Vector2(0f, 1f);
            rtStaContainer.anchoredPosition = new Vector2(14f, -36f);
            rtStaContainer.sizeDelta = new Vector2(312f, 16f);

            // Stamina Bolt Icon
            GameObject staIconObj = new GameObject("Icon_Bolt");
            staIconObj.transform.SetParent(staContainerObj.transform, false);
            RectTransform rtStaIcon = staIconObj.AddComponent<RectTransform>();
            rtStaIcon.anchorMin = new Vector2(0f, 0.5f);
            rtStaIcon.anchorMax = new Vector2(0f, 0.5f);
            rtStaIcon.pivot = new Vector2(0f, 0.5f);
            rtStaIcon.anchoredPosition = new Vector2(2f, 0f);
            rtStaIcon.sizeDelta = new Vector2(16f, 16f);
            Image staIconImg = staIconObj.AddComponent<Image>();
            staIconImg.sprite = iconBolt;
            staIconImg.color = new Color(0f, 0.71f, 0.85f, 1f);
            staIconImg.raycastTarget = false;

            // Stamina Bar Box & Layers
            GameObject staBarBox = new GameObject("Bar_Box");
            staBarBox.transform.SetParent(staContainerObj.transform, false);
            RectTransform rtStaBarBox = staBarBox.AddComponent<RectTransform>();
            rtStaBarBox.anchorMin = new Vector2(0f, 0f);
            rtStaBarBox.anchorMax = new Vector2(1f, 1f);
            rtStaBarBox.offsetMin = new Vector2(26f, 0f);
            rtStaBarBox.offsetMax = new Vector2(0f, 0f);

            // Layer 1: Recessed Track
            GameObject staTrackObj = new GameObject("Track");
            staTrackObj.transform.SetParent(staBarBox.transform, false);
            RectTransform rtStaTrack = staTrackObj.AddComponent<RectTransform>();
            rtStaTrack.anchorMin = Vector2.zero;
            rtStaTrack.anchorMax = Vector2.one;
            rtStaTrack.sizeDelta = Vector2.zero;
            Image staTrackImg = staTrackObj.AddComponent<Image>();
            staTrackImg.sprite = vitalBarFrame;
            staTrackImg.type = Image.Type.Sliced;
            staTrackImg.color = new Color(0.07f, 0.10f, 0.13f, 0.95f);
            staTrackImg.raycastTarget = false;

            // Layer 2: Ghost Fill
            GameObject staGhostObj = new GameObject("GhostFill");
            staGhostObj.transform.SetParent(staBarBox.transform, false);
            RectTransform rtStaGhost = staGhostObj.AddComponent<RectTransform>();
            rtStaGhost.anchorMin = Vector2.zero;
            rtStaGhost.anchorMax = Vector2.one;
            rtStaGhost.offsetMin = new Vector2(2f, 2f);
            rtStaGhost.offsetMax = new Vector2(-2f, -2f);
            Image staGhostFillImg = staGhostObj.AddComponent<Image>();
            staGhostFillImg.sprite = vitalBarFill;
            staGhostFillImg.type = Image.Type.Filled;
            staGhostFillImg.fillMethod = Image.FillMethod.Horizontal;
            staGhostFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            staGhostFillImg.fillAmount = 1.0f;
            staGhostFillImg.color = new Color(0.56f, 0.88f, 0.94f, 0.45f);
            staGhostFillImg.raycastTarget = false;

            // Layer 3: Primary Stamina Fill
            GameObject staFillObj = new GameObject("PrimaryFill");
            staFillObj.transform.SetParent(staBarBox.transform, false);
            RectTransform rtStaFill = staFillObj.AddComponent<RectTransform>();
            rtStaFill.anchorMin = Vector2.zero;
            rtStaFill.anchorMax = Vector2.one;
            rtStaFill.offsetMin = new Vector2(2f, 2f);
            rtStaFill.offsetMax = new Vector2(-2f, -2f);
            Image staPrimaryFillImg = staFillObj.AddComponent<Image>();
            staPrimaryFillImg.sprite = vitalBarFill;
            staPrimaryFillImg.type = Image.Type.Filled;
            staPrimaryFillImg.fillMethod = Image.FillMethod.Horizontal;
            staPrimaryFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            staPrimaryFillImg.fillAmount = 1.0f;
            staPrimaryFillImg.color = new Color(0f, 0.71f, 0.85f, 1f);
            staPrimaryFillImg.raycastTarget = false;

            // Stamina Typography
            Text staText = CreateUIText(staBarBox.transform, "StaminaText", "ENERJİ  100%", 10, TextAnchor.MiddleLeft, new Color(0.97f, 0.98f, 0.98f, 1f));
            staText.fontStyle = FontStyle.Bold;
            staText.rectTransform.anchorMin = Vector2.zero;
            staText.rectTransform.anchorMax = Vector2.one;
            staText.rectTransform.offsetMin = new Vector2(8f, 0f);
            staText.rectTransform.offsetMax = new Vector2(-8f, 0f);
            staText.raycastTarget = false;
            Shadow staShadow = staText.gameObject.AddComponent<Shadow>();
            staShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            staShadow.effectDistance = new Vector2(1f, -1f);

            // --- C. RADIAL SURVIVAL GAUGES (AÇLIK, SUSUZLUK, ENFEKSİYON) ---
            GameObject gaugesRow = new GameObject("SurvivalGauges_Row");
            gaugesRow.transform.SetParent(vitalPanelObj.transform, false);
            RectTransform rtGauges = gaugesRow.AddComponent<RectTransform>();
            rtGauges.anchorMin = new Vector2(0f, 0f);
            rtGauges.anchorMax = new Vector2(1f, 0f);
            rtGauges.pivot = new Vector2(0.5f, 0f);
            rtGauges.anchoredPosition = new Vector2(0f, 10f);
            rtGauges.sizeDelta = new Vector2(-28f, 44f);

            HorizontalLayoutGroup hlgGauges = gaugesRow.AddComponent<HorizontalLayoutGroup>();
            hlgGauges.spacing = 15f;
            hlgGauges.childAlignment = TextAnchor.MiddleCenter;
            hlgGauges.childForceExpandWidth = true;
            hlgGauges.childForceExpandHeight = true;

            void BuildRadialGauge(Transform parent, string name, Sprite icon, string label, Color fillCol, float defaultFill,
                out Image fillImage, out Image iconImage, out Text valText)
            {
                GameObject gObj = new GameObject(name);
                gObj.transform.SetParent(parent, false);
                RectTransform rtG = gObj.AddComponent<RectTransform>();
                rtG.sizeDelta = new Vector2(94f, 44f);

                GameObject cObj = new GameObject("CircleContainer");
                cObj.transform.SetParent(gObj.transform, false);
                RectTransform rtC = cObj.AddComponent<RectTransform>();
                rtC.anchorMin = new Vector2(0f, 0.5f);
                rtC.anchorMax = new Vector2(0f, 0.5f);
                rtC.pivot = new Vector2(0f, 0.5f);
                rtC.anchoredPosition = new Vector2(2f, 0f);
                rtC.sizeDelta = new Vector2(36f, 36f);

                GameObject trObj = new GameObject("TrackRing");
                trObj.transform.SetParent(cObj.transform, false);
                RectTransform rtTr = trObj.AddComponent<RectTransform>();
                rtTr.anchorMin = Vector2.zero;
                rtTr.anchorMax = Vector2.one;
                rtTr.sizeDelta = Vector2.zero;
                Image trImg = trObj.AddComponent<Image>();
                trImg.sprite = iconRadialRing;
                trImg.color = new Color(0.12f, 0.15f, 0.20f, 0.75f);
                trImg.raycastTarget = false;

                GameObject rfObj = new GameObject("RadialFill");
                rfObj.transform.SetParent(cObj.transform, false);
                RectTransform rtRf = rfObj.AddComponent<RectTransform>();
                rtRf.anchorMin = Vector2.zero;
                rtRf.anchorMax = Vector2.one;
                rtRf.sizeDelta = Vector2.zero;
                fillImage = rfObj.AddComponent<Image>();
                fillImage.sprite = iconRadialRing;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Radial360;
                fillImage.fillOrigin = (int)Image.Origin360.Top;
                fillImage.fillClockwise = true;
                fillImage.fillAmount = defaultFill;
                fillImage.color = fillCol;
                fillImage.raycastTarget = false;

                GameObject icObj = new GameObject("CenterIcon");
                icObj.transform.SetParent(cObj.transform, false);
                RectTransform rtIc = icObj.AddComponent<RectTransform>();
                rtIc.anchorMin = new Vector2(0.5f, 0.5f);
                rtIc.anchorMax = new Vector2(0.5f, 0.5f);
                rtIc.pivot = new Vector2(0.5f, 0.5f);
                rtIc.sizeDelta = new Vector2(18f, 18f);
                iconImage = icObj.AddComponent<Image>();
                iconImage.sprite = icon;
                iconImage.color = Color.white;
                iconImage.raycastTarget = false;

                GameObject txtContainer = new GameObject("TextContainer");
                txtContainer.transform.SetParent(gObj.transform, false);
                RectTransform rtTc = txtContainer.AddComponent<RectTransform>();
                rtTc.anchorMin = new Vector2(0f, 0f);
                rtTc.anchorMax = new Vector2(1f, 1f);
                rtTc.offsetMin = new Vector2(42f, 2f);
                rtTc.offsetMax = new Vector2(0f, -2f);

                Text lblText = CreateUIText(txtContainer.transform, "Label", label, 9, TextAnchor.MiddleLeft, new Color(0.65f, 0.72f, 0.82f, 0.95f));
                lblText.fontStyle = FontStyle.Bold;
                lblText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                lblText.rectTransform.anchorMax = new Vector2(1f, 1f);
                lblText.rectTransform.offsetMin = Vector2.zero;
                lblText.rectTransform.offsetMax = Vector2.zero;
                lblText.raycastTarget = false;

                valText = CreateUIText(txtContainer.transform, "Value", defaultFill > 0f ? $"{Mathf.CeilToInt(defaultFill * 100f)}%" : "0%", 11, TextAnchor.MiddleLeft, Color.white);
                valText.fontStyle = FontStyle.Bold;
                valText.rectTransform.anchorMin = new Vector2(0f, 0f);
                valText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                valText.rectTransform.offsetMin = Vector2.zero;
                valText.rectTransform.offsetMax = Vector2.zero;
                valText.raycastTarget = false;
                Shadow valShadow = valText.gameObject.AddComponent<Shadow>();
                valShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                valShadow.effectDistance = new Vector2(1f, -1f);
            }

            BuildRadialGauge(gaugesRow.transform, "Gauge_Hunger", iconVitalHunger, "AÇLIK", new Color(0.96f, 0.64f, 0.38f), 1.0f,
                out Image hungerRadialFillImg, out Image hungerIconImg, out Text hungerValText);

            BuildRadialGauge(gaugesRow.transform, "Gauge_Thirst", iconVitalThirst, "SUSUZ", new Color(0.30f, 0.79f, 0.94f), 1.0f,
                out Image thirstRadialFillImg, out Image thirstIconImg, out Text thirstValText);

            BuildRadialGauge(gaugesRow.transform, "Gauge_Infection", iconVitalInfection, "ENFEK", new Color(0.32f, 0.48f, 0.44f, 0.55f), 0f,
                out Image infRadialFillImg, out Image infIconImg, out Text infValText);

            // Attach & Wire VitalStatusHUD Component
            VitalStatusHUD vitalHUD = vitalPanelObj.AddComponent<VitalStatusHUD>();
            SerializedObject soVital = new SerializedObject(vitalHUD);

            soVital.FindProperty("healthTrack").objectReferenceValue = hpTrackImg;
            soVital.FindProperty("healthGhostFill").objectReferenceValue = hpGhostFillImg;
            soVital.FindProperty("healthPrimaryFill").objectReferenceValue = hpPrimaryFillImg;
            soVital.FindProperty("healthShimmer").objectReferenceValue = hpShimmerImg;
            soVital.FindProperty("healthIcon").objectReferenceValue = hpIconImg;
            soVital.FindProperty("healthText").objectReferenceValue = hpText;
            soVital.FindProperty("healthContainer").objectReferenceValue = rtHpContainer;

            soVital.FindProperty("staminaTrack").objectReferenceValue = staTrackImg;
            soVital.FindProperty("staminaGhostFill").objectReferenceValue = staGhostFillImg;
            soVital.FindProperty("staminaPrimaryFill").objectReferenceValue = staPrimaryFillImg;
            soVital.FindProperty("staminaIcon").objectReferenceValue = staIconImg;
            soVital.FindProperty("staminaText").objectReferenceValue = staText;

            soVital.FindProperty("hungerRadialFill").objectReferenceValue = hungerRadialFillImg;
            soVital.FindProperty("hungerIcon").objectReferenceValue = hungerIconImg;
            soVital.FindProperty("hungerText").objectReferenceValue = hungerValText;

            soVital.FindProperty("thirstRadialFill").objectReferenceValue = thirstRadialFillImg;
            soVital.FindProperty("thirstIcon").objectReferenceValue = thirstIconImg;
            soVital.FindProperty("thirstText").objectReferenceValue = thirstValText;

            soVital.FindProperty("infectionRadialFill").objectReferenceValue = infRadialFillImg;
            soVital.FindProperty("infectionIcon").objectReferenceValue = infIconImg;
            soVital.FindProperty("infectionText").objectReferenceValue = infValText;

            soVital.ApplyModifiedProperties();

            // 6. Bottom-Left Virtual Joystick (Bottom-Left 0,0, pos 140, 140)
            GameObject joyRoot = new GameObject("Mobile_Joystick");
            joyRoot.transform.SetParent(canvasObj.transform, false);
            RectTransform rtJoy = joyRoot.AddComponent<RectTransform>();
            rtJoy.anchorMin = new Vector2(0f, 0f);
            rtJoy.anchorMax = new Vector2(0f, 0f);
            rtJoy.pivot = new Vector2(0.5f, 0.5f);
            rtJoy.anchoredPosition = new Vector2(140f, 140f);
            rtJoy.sizeDelta = new Vector2(190f, 190f);

            Image joyBg = joyRoot.AddComponent<Image>();
            if (circleRingSprite != null) joyBg.sprite = circleRingSprite;
            else if (knobSprite != null) joyBg.sprite = knobSprite;
            joyBg.color = new Color(1f, 1f, 1f, 0.25f);
            joyBg.raycastTarget = true;

            GameObject joyHandle = new GameObject("Handle");
            joyHandle.transform.SetParent(joyRoot.transform, false);
            RectTransform rtHandle = joyHandle.AddComponent<RectTransform>();
            rtHandle.anchorMin = new Vector2(0.5f, 0.5f);
            rtHandle.anchorMax = new Vector2(0.5f, 0.5f);
            rtHandle.pivot = new Vector2(0.5f, 0.5f);
            rtHandle.anchoredPosition = Vector2.zero;
            rtHandle.sizeDelta = new Vector2(75f, 75f);

            Image handleImg = joyHandle.AddComponent<Image>();
            if (circleGlowSprite != null) handleImg.sprite = circleGlowSprite;
            else if (knobSprite != null) handleImg.sprite = knobSprite;
            handleImg.color = new Color(1f, 1f, 1f, 0.85f);
            handleImg.raycastTarget = false;

            MobileJoystick joystick = joyRoot.AddComponent<MobileJoystick>();

            // 7. Bottom-Right Action Buttons (TPS Ergonomic 5-Control Cluster)
            GameObject actionPanel = new GameObject("Action_Buttons_Panel");
            actionPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtAction = actionPanel.AddComponent<RectTransform>();
            rtAction.anchorMin = new Vector2(1f, 0f);
            rtAction.anchorMax = new Vector2(1f, 0f);
            rtAction.pivot = new Vector2(1f, 0f);
            rtAction.anchoredPosition = Vector2.zero;
            rtAction.sizeDelta = new Vector2(440f, 280f);

            // ATEŞ ET / VUR: Large main action button in bottom-right corner (108x108 at -75, 75)
            Button btnFire = CreateActionButtonWithIconAndText(
                actionPanel.transform, "Btn_Fire", "ATEŞ",
                new Vector2(-75f, 75f), new Vector2(108f, 108f),
                new Color(0.92f, 0.20f, 0.22f, 0.95f),
                circleRedSprite ?? knobSprite,
                null, 32f, 14
            );

            // NİŞAN AL: Immediately left of Fire (90x90 at -195, 75)
            Button btnAim = CreateActionButtonWithIconAndText(
                actionPanel.transform, "Btn_Aim", "NİŞAN",
                new Vector2(-195f, 75f), new Vector2(90f, 90f),
                new Color(0.12f, 0.65f, 0.95f, 0.95f),
                circleGlowSprite ?? knobSprite,
                null, 28f, 13
            );

            // ETKİLEŞ: Prominent button left of Aim (90x90 at -315, 75) - Inactive by default, pulses when near targets
            Button btnInteract = CreateActionButtonWithIconAndText(
                actionPanel.transform, "Btn_Interact", "ETKİLEŞ",
                new Vector2(-315f, 75f), new Vector2(90f, 90f),
                new Color(0.18f, 0.78f, 0.45f, 0.95f),
                circleGlowSprite ?? circleRingSprite ?? knobSprite,
                iconInteract, 36f, 13
            );
            Text interactBtnText = btnInteract.GetComponentInChildren<Text>();
            btnInteract.gameObject.SetActive(false);

            // KOŞ: Top row above Fire (80x80 at -75, 185)
            Button btnSprint = CreateActionButtonWithIconAndText(
                actionPanel.transform, "Btn_Sprint", "KOŞ",
                new Vector2(-75f, 185f), new Vector2(80f, 80f),
                new Color(0.95f, 0.68f, 0.15f, 0.92f),
                circleRingSprite ?? knobSprite,
                iconSprint, 34f, 12
            );

            // EĞİL: Top row above Aim (80x80 at -185, 185)
            Button btnCrouch = CreateActionButtonWithIconAndText(
                actionPanel.transform, "Btn_Crouch", "EĞİL",
                new Vector2(-185f, 185f), new Vector2(80f, 80f),
                new Color(0.22f, 0.58f, 0.90f, 0.92f),
                circleRingSprite ?? knobSprite,
                iconCrouch, 34f, 12
            );

            // Crouch Glow Overlay
            GameObject crouchGlowObj = new GameObject("CrouchGlow");
            crouchGlowObj.transform.SetParent(btnCrouch.transform, false);
            RectTransform rtCrouchGlow = crouchGlowObj.AddComponent<RectTransform>();
            rtCrouchGlow.anchorMin = Vector2.zero;
            rtCrouchGlow.anchorMax = Vector2.one;
            rtCrouchGlow.sizeDelta = Vector2.zero;
            Image crouchGlowImg = crouchGlowObj.AddComponent<Image>();
            crouchGlowImg.sprite = circleGlowSprite ?? knobSprite;
            crouchGlowImg.color = new Color(0.2f, 0.85f, 1f, 0.75f);
            crouchGlowImg.raycastTarget = false;
            crouchGlowObj.SetActive(false);

            // Phase 8: Dynamic Center Crosshair
            GameObject crosshairObj = new GameObject("Crosshair_Panel");
            crosshairObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtCross = crosshairObj.AddComponent<RectTransform>();
            rtCross.anchorMin = new Vector2(0.5f, 0.5f);
            rtCross.anchorMax = new Vector2(0.5f, 0.5f);
            rtCross.pivot = new Vector2(0.5f, 0.5f);
            rtCross.anchoredPosition = Vector2.zero;
            rtCross.sizeDelta = new Vector2(60f, 60f);

            Color crossCol = new Color(1f, 1f, 1f, 0.88f);
            RectTransform CreateCrosshairTick(string name, Vector2 size, Vector2 pos)
            {
                GameObject t = new GameObject(name);
                t.transform.SetParent(crosshairObj.transform, false);
                RectTransform rt = t.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
                Image img = t.AddComponent<Image>();
                img.color = crossCol;
                img.raycastTarget = false;
                return rt;
            }

            RectTransform chTop = CreateCrosshairTick("Tick_Top", new Vector2(2f, 10f), new Vector2(0f, 14f));
            RectTransform chBottom = CreateCrosshairTick("Tick_Bottom", new Vector2(2f, 10f), new Vector2(0f, -14f));
            RectTransform chLeft = CreateCrosshairTick("Tick_Left", new Vector2(10f, 2f), new Vector2(-14f, 0f));
            RectTransform chRight = CreateCrosshairTick("Tick_Right", new Vector2(10f, 2f), new Vector2(14f, 0f));

            // Hit Marker
            GameObject hitMarkerObj = new GameObject("HitMarker_Panel");
            hitMarkerObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtHit = hitMarkerObj.AddComponent<RectTransform>();
            rtHit.anchorMin = new Vector2(0.5f, 0.5f);
            rtHit.anchorMax = new Vector2(0.5f, 0.5f);
            rtHit.pivot = new Vector2(0.5f, 0.5f);
            rtHit.anchoredPosition = Vector2.zero;
            rtHit.sizeDelta = new Vector2(32f, 32f);
            Image hitImg = hitMarkerObj.AddComponent<Image>();
            Sprite hitMarkerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_hitmarker_x.png");
            if (hitMarkerSprite != null) hitImg.sprite = hitMarkerSprite;
            hitImg.color = Color.white;
            hitImg.raycastTarget = false;
            hitMarkerObj.SetActive(false);

            // Weapon HUD Panel (Interactive Button for cycle)
            GameObject weaponHudObj = new GameObject("WeaponHUD_Panel");
            weaponHudObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtWeaponHud = weaponHudObj.AddComponent<RectTransform>();
            rtWeaponHud.anchorMin = new Vector2(1f, 0f);
            rtWeaponHud.anchorMax = new Vector2(1f, 0f);
            rtWeaponHud.pivot = new Vector2(1f, 0f);
            rtWeaponHud.anchoredPosition = new Vector2(-25f, 280f);
            rtWeaponHud.sizeDelta = new Vector2(200f, 68f);

            Image wpHudBg = weaponHudObj.AddComponent<Image>();
            wpHudBg.color = new Color(0.08f, 0.1f, 0.14f, 0.90f);
            if (glassPanelSprite != null) { wpHudBg.sprite = glassPanelSprite; wpHudBg.type = Image.Type.Sliced; }
            wpHudBg.raycastTarget = true;
            Button cardBtn = weaponHudObj.AddComponent<Button>();

            // Weapon Icon (Left side of card)
            GameObject wpIconObj = new GameObject("WeaponIcon");
            wpIconObj.transform.SetParent(weaponHudObj.transform, false);
            RectTransform rtWpIcon = wpIconObj.AddComponent<RectTransform>();
            rtWpIcon.anchorMin = new Vector2(0f, 0.5f);
            rtWpIcon.anchorMax = new Vector2(0f, 0.5f);
            rtWpIcon.pivot = new Vector2(0f, 0.5f);
            rtWpIcon.anchoredPosition = new Vector2(12f, 0f);
            rtWpIcon.sizeDelta = new Vector2(44f, 44f);
            Image wpIconImg = wpIconObj.AddComponent<Image>();
            wpIconImg.raycastTarget = false;

            Text wpNameTxt = CreateUIText(weaponHudObj.transform, "WeaponName", "ELLER BOŞ (SİLAHSIZ)", 12, TextAnchor.MiddleLeft, new Color(0.75f, 0.85f, 0.95f));
            wpNameTxt.fontStyle = FontStyle.Bold;
            wpNameTxt.rectTransform.anchoredPosition = new Vector2(62f, 14f);
            wpNameTxt.rectTransform.sizeDelta = new Vector2(130f, 18f);

            Text ammoTxt = CreateUIText(weaponHudObj.transform, "AmmoCounter", "---", 22, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.25f));
            ammoTxt.fontStyle = FontStyle.Bold;
            ammoTxt.rectTransform.anchoredPosition = new Vector2(62f, -12f);
            ammoTxt.rectTransform.sizeDelta = new Vector2(130f, 24f);

            // Tactile mini buttons on left edge of weapon card
            Sprite iconSwitch = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_switch_weapon.png");
            Sprite iconHolster = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_holster.png");

            Button btnSwitch = CreateCustomButton(
                weaponHudObj.transform, "Btn_SwitchWeapon", "",
                new Vector2(-28f, 0f), new Vector2(48f, 48f),
                new Color(0.12f, 0.16f, 0.22f, 0.95f),
                circleRingSprite ?? knobSprite,
                iconSwitch, 12,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f)
            );

            Button btnHolster = CreateCustomButton(
                weaponHudObj.transform, "Btn_HolsterWeapon", "",
                new Vector2(-82f, 0f), new Vector2(48f, 48f),
                new Color(0.12f, 0.16f, 0.22f, 0.95f),
                circleRingSprite ?? knobSprite,
                iconHolster, 12,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f)
            );

            Sprite iconFist = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_unarmed_fist.png");
            Sprite iconAxe = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_weapon_axe.png");
            Sprite iconPistol = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_weapon_pistol.png");
            Sprite iconShotgun = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/icon_weapon_shotgun.png");

            WeaponHUDWidget wWidget = weaponHudObj.AddComponent<WeaponHUDWidget>();
            wWidget.Configure(cardBtn, wpIconImg, wpNameTxt, ammoTxt, btnHolster, btnSwitch, iconFist, iconAxe, iconPistol, iconShotgun);

            // Toast Banner
            GameObject toastObj = new GameObject("Toast_Panel");
            toastObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtToast = toastObj.AddComponent<RectTransform>();
            rtToast.anchorMin = new Vector2(0.5f, 1f);
            rtToast.anchorMax = new Vector2(0.5f, 1f);
            rtToast.pivot = new Vector2(0.5f, 1f);
            rtToast.anchoredPosition = new Vector2(0f, -65f);
            rtToast.sizeDelta = new Vector2(740f, 48f);

            Image toastBg = toastObj.AddComponent<Image>();
            toastBg.color = new Color(0.08f, 0.10f, 0.14f, 0.92f);
            if (glassPanelSprite != null) { toastBg.sprite = glassPanelSprite; toastBg.type = Image.Type.Sliced; }
            toastBg.raycastTarget = false;

            Text toastTxt = CreateUIText(toastObj.transform, "ToastText", "", 16, TextAnchor.MiddleCenter, Color.white);
            toastTxt.fontStyle = FontStyle.Bold;
            toastTxt.rectTransform.anchorMin = Vector2.zero;
            toastTxt.rectTransform.anchorMax = Vector2.one;
            toastTxt.rectTransform.sizeDelta = Vector2.zero;
            toastObj.SetActive(false);

            // 8. QuickSlots (Bottom Center)
            GameObject qsPanel = new GameObject("QuickSlots_Bar");
            qsPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtQs = qsPanel.AddComponent<RectTransform>();
            rtQs.anchorMin = new Vector2(0.5f, 0f);
            rtQs.anchorMax = new Vector2(0.5f, 0f);
            rtQs.pivot = new Vector2(0.5f, 0f);
            rtQs.anchoredPosition = new Vector2(0f, 30f);
            rtQs.sizeDelta = new Vector2(330f, 75f);

            Image qsBg = qsPanel.AddComponent<Image>();
            qsBg.color = new Color(0.06f, 0.08f, 0.12f, 0.65f);
            if (glassPanelSprite != null)
            {
                qsBg.sprite = glassPanelSprite;
                qsBg.type = Image.Type.Sliced;
            }
            qsBg.raycastTarget = false;

            HorizontalLayoutGroup hlg = qsPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.padding = new RectOffset(10, 10, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Sprite iconCanned = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/item_canned_food.png");
            Sprite iconWater = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/item_water_bottle.png");
            Sprite iconMedkit = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/item_medkit.png");
            Sprite iconDistraction = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/item_distraction_bottle.png");
            Sprite[] qsIcons = new Sprite[] { iconCanned, iconWater, iconMedkit, iconDistraction };
            string[] qsQtys = new string[] { "x2", "x2", "x1", "x1" };

            List<Button> qsButtons = new List<Button>();
            List<Image> qsIconImgs = new List<Image>();
            List<Text> qsBadgeTexts = new List<Text>();

            for (int i = 0; i < 4; i++)
            {
                Button qb = CreateQuickSlotButton(qsPanel.transform, $"Slot_{i + 1}", glassPanelSprite ?? uiSprite, qsIcons[i], qsQtys[i]);
                qsButtons.Add(qb);
                Transform icTr = qb.transform.Find("ItemIcon");
                if (icTr != null) qsIconImgs.Add(icTr.GetComponent<Image>());
                Transform bdTr = qb.transform.Find("CountBadge");
                if (bdTr != null) qsBadgeTexts.Add(bdTr.GetComponent<Text>());
            }

            GameObject distractionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/DistractionBottle_Root.prefab");
            QuickSlotUI quickSlotUI = qsPanel.AddComponent<QuickSlotUI>();
            quickSlotUI.Configure(qsButtons.ToArray(), qsIconImgs.ToArray(), qsBadgeTexts.ToArray(), distractionPrefab);

            // 9. Interaction Prompt Banner
            GameObject promptObj = new GameObject("Interaction_Prompt");
            promptObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtPrompt = promptObj.AddComponent<RectTransform>();
            rtPrompt.anchorMin = new Vector2(0.5f, 0.4f);
            rtPrompt.anchorMax = new Vector2(0.5f, 0.4f);
            rtPrompt.pivot = new Vector2(0.5f, 0.5f);
            rtPrompt.anchoredPosition = Vector2.zero;
            rtPrompt.sizeDelta = new Vector2(380f, 50f);

            Image promptBg = promptObj.AddComponent<Image>();
            promptBg.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);
            if (glassPanelSprite != null)
            {
                promptBg.sprite = glassPanelSprite;
                promptBg.type = Image.Type.Sliced;
            }
            promptBg.raycastTarget = false;

            Text promptText = CreateUIText(promptObj.transform, "PromptText", "E - Etkileşime Geç", 20, TextAnchor.MiddleCenter, Color.white);
            promptText.rectTransform.anchorMin = Vector2.zero;
            promptText.rectTransform.anchorMax = Vector2.one;
            promptText.rectTransform.sizeDelta = Vector2.zero;
            promptObj.AddComponent<InteractionPrompt>();

            // 10. Cinematic Lower-Third Letterbox Dialogue Panel
            GameObject diagPanel = new GameObject("Dialogue_Panel");
            diagPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtDiag = diagPanel.AddComponent<RectTransform>();
            rtDiag.anchorMin = new Vector2(0f, 0f);
            rtDiag.anchorMax = new Vector2(1f, 0f);
            rtDiag.pivot = new Vector2(0.5f, 0f);
            rtDiag.offsetMin = new Vector2(40f, 25f);
            rtDiag.offsetMax = new Vector2(-40f, 310f);

            Image diagBg = diagPanel.AddComponent<Image>();
            diagBg.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
            if (glassPanelSprite != null)
            {
                diagBg.sprite = glassPanelSprite;
                diagBg.type = Image.Type.Sliced;
            }

            DialogueUI dialogueUI = diagPanel.AddComponent<DialogueUI>();

            Text speakerText = CreateUIText(diagPanel.transform, "SpeakerName", "Dr. Aras", 24, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.35f));
            speakerText.fontStyle = FontStyle.Bold;
            speakerText.rectTransform.anchorMin = new Vector2(0f, 1f);
            speakerText.rectTransform.anchorMax = new Vector2(1f, 1f);
            speakerText.rectTransform.pivot = new Vector2(0f, 1f);
            speakerText.rectTransform.offsetMin = new Vector2(30f, -45f);
            speakerText.rectTransform.offsetMax = new Vector2(-30f, -10f);

            Text bodyText = CreateUIText(diagPanel.transform, "DialogueBody", "Diyalog metni...", 18, TextAnchor.UpperLeft, Color.white);
            bodyText.rectTransform.anchorMin = new Vector2(0f, 1f);
            bodyText.rectTransform.anchorMax = new Vector2(1f, 1f);
            bodyText.rectTransform.pivot = new Vector2(0f, 1f);
            bodyText.rectTransform.offsetMin = new Vector2(30f, -135f);
            bodyText.rectTransform.offsetMax = new Vector2(-30f, -50f);

            GameObject choicesContainer = new GameObject("ChoicesContainer");
            choicesContainer.transform.SetParent(diagPanel.transform, false);
            RectTransform rtChoices = choicesContainer.AddComponent<RectTransform>();
            rtChoices.anchorMin = new Vector2(0f, 0f);
            rtChoices.anchorMax = new Vector2(1f, 0f);
            rtChoices.pivot = new Vector2(0.5f, 0f);
            rtChoices.offsetMin = new Vector2(30f, 15f);
            rtChoices.offsetMax = new Vector2(-30f, 145f);

            VerticalLayoutGroup vlgChoices = choicesContainer.AddComponent<VerticalLayoutGroup>();
            vlgChoices.spacing = 8f;
            vlgChoices.childForceExpandHeight = false;

            List<Button> diagChoiceBtns = new List<Button>();
            for (int i = 0; i < 3; i++)
            {
                Button cbtn = CreateChoiceButton(choicesContainer.transform, $"ChoiceButton_{i + 1}", $"Seçenek {i + 1}", glassPanelSprite ?? uiSprite);
                cbtn.GetComponent<RectTransform>().sizeDelta = new Vector2(720f, 38f);
                diagChoiceBtns.Add(cbtn);
            }

            SerializedObject soDiag = new SerializedObject(dialogueUI);
            soDiag.FindProperty("dialoguePanel").objectReferenceValue = diagPanel;
            soDiag.FindProperty("speakerNameText").objectReferenceValue = speakerText;
            soDiag.FindProperty("dialogueBodyText").objectReferenceValue = bodyText;
            soDiag.FindProperty("choicesContainer").objectReferenceValue = choicesContainer.transform;
            SerializedProperty choiceProp = soDiag.FindProperty("choiceButtons");
            choiceProp.ClearArray();
            for (int i = 0; i < diagChoiceBtns.Count; i++)
            {
                choiceProp.InsertArrayElementAtIndex(i);
                choiceProp.GetArrayElementAtIndex(i).objectReferenceValue = diagChoiceBtns[i];
            }
            soDiag.ApplyModifiedProperties();

            diagPanel.SetActive(false);

            // 11. Crafting UI Panel (Deactivated by default!)
            GameObject craftPanelObj = new GameObject("Crafting_Panel");
            craftPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtCraft = craftPanelObj.AddComponent<RectTransform>();
            rtCraft.anchorMin = new Vector2(0.5f, 0.5f);
            rtCraft.anchorMax = new Vector2(0.5f, 0.5f);
            rtCraft.pivot = new Vector2(0.5f, 0.5f);
            rtCraft.anchoredPosition = Vector2.zero;
            rtCraft.sizeDelta = new Vector2(700f, 460f);

            Image craftBg = craftPanelObj.AddComponent<Image>();
            craftBg.color = new Color(0.1f, 0.12f, 0.16f, 0.96f);
            if (glassPanelSprite != null)
            {
                craftBg.sprite = glassPanelSprite;
                craftBg.type = Image.Type.Sliced;
            }

            Text craftTitle = CreateUIText(craftPanelObj.transform, "Title", "ÜRETİM TEZGAHI (CRAFTING)", 26, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f));
            craftTitle.rectTransform.anchoredPosition = new Vector2(0f, 195f);
            craftTitle.rectTransform.sizeDelta = new Vector2(500f, 40f);

            Button btnCloseCraft = CreateCustomButton(craftPanelObj.transform, "Btn_CloseCraft", "X", new Vector2(310f, 195f), new Vector2(45f, 45f), new Color(0.8f, 0.2f, 0.2f, 0.95f), knobSprite, null, 20);

            GameObject craftListContainer = new GameObject("RecipeList");
            craftListContainer.transform.SetParent(craftPanelObj.transform, false);
            RectTransform rtCl = craftListContainer.AddComponent<RectTransform>();
            rtCl.anchorMin = new Vector2(0.5f, 0.5f);
            rtCl.anchorMax = new Vector2(0.5f, 0.5f);
            rtCl.pivot = new Vector2(0.5f, 0.5f);
            rtCl.anchoredPosition = new Vector2(0f, -15f);
            rtCl.sizeDelta = new Vector2(640f, 340f);

            VerticalLayoutGroup vlgCraft = craftListContainer.AddComponent<VerticalLayoutGroup>();
            vlgCraft.spacing = 15f;
            vlgCraft.childForceExpandHeight = false;

            CraftingUI craftingUI = craftPanelObj.AddComponent<CraftingUI>();
            SerializedObject soCraftUI = new SerializedObject(craftingUI);
            soCraftUI.FindProperty("panelRoot").objectReferenceValue = craftPanelObj;
            soCraftUI.FindProperty("closeButton").objectReferenceValue = btnCloseCraft;

            SerializedProperty recipeSlotsProp = soCraftUI.FindProperty("recipeSlots");
            recipeSlotsProp.ClearArray();

            for (int i = 0; i < 3; i++)
            {
                recipeSlotsProp.InsertArrayElementAtIndex(i);
                SerializedProperty slotElem = recipeSlotsProp.GetArrayElementAtIndex(i);

                GameObject slotObj = new GameObject($"RecipeSlot_{i + 1}");
                slotObj.transform.SetParent(craftListContainer.transform, false);
                RectTransform rtSlot = slotObj.AddComponent<RectTransform>();
                rtSlot.sizeDelta = new Vector2(620f, 90f);

                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.color = new Color(0.18f, 0.22f, 0.28f, 0.9f);
                if (glassPanelSprite != null)
                {
                    slotBg.sprite = glassPanelSprite;
                    slotBg.type = Image.Type.Sliced;
                }

                Text title = CreateUIText(slotObj.transform, "RecipeName", "Tarif İsmi", 20, TextAnchor.MiddleLeft, Color.white);
                title.rectTransform.anchoredPosition = new Vector2(-150f, 18f);
                title.rectTransform.sizeDelta = new Vector2(280f, 30f);

                Text ings = CreateUIText(slotObj.transform, "Ingredients", "Gereksinimler...", 15, TextAnchor.MiddleLeft, new Color(0.8f, 0.85f, 0.9f));
                ings.rectTransform.anchoredPosition = new Vector2(-150f, -18f);
                ings.rectTransform.sizeDelta = new Vector2(280f, 30f);

                Button btnCraftAction = CreateChoiceButton(slotObj.transform, "Btn_CraftAction", "ÜRET", glassPanelSprite ?? uiSprite);
                RectTransform rtBtn = btnCraftAction.GetComponent<RectTransform>();
                rtBtn.sizeDelta = new Vector2(120f, 55f);
                rtBtn.anchoredPosition = new Vector2(220f, 0f);

                slotElem.FindPropertyRelative("rootObject").objectReferenceValue = slotObj;
                slotElem.FindPropertyRelative("recipeTitleText").objectReferenceValue = title;
                slotElem.FindPropertyRelative("ingredientsText").objectReferenceValue = ings;
                slotElem.FindPropertyRelative("craftButton").objectReferenceValue = btnCraftAction;
            }
            soCraftUI.ApplyModifiedProperties();

            craftPanelObj.SetActive(false);

            // 12. GameOver Panel
            GameObject goPanel = new GameObject("GameOver_Panel");
            goPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtGo = goPanel.AddComponent<RectTransform>();
            rtGo.anchorMin = new Vector2(0.5f, 0.5f);
            rtGo.anchorMax = new Vector2(0.5f, 0.5f);
            rtGo.pivot = new Vector2(0.5f, 0.5f);
            rtGo.anchoredPosition = Vector2.zero;
            rtGo.sizeDelta = new Vector2(520f, 280f);

            Image goBg = goPanel.AddComponent<Image>();
            goBg.color = new Color(0.1f, 0.02f, 0.02f, 0.96f);
            if (glassPanelSprite != null)
            {
                goBg.sprite = glassPanelSprite;
                goBg.type = Image.Type.Sliced;
            }

            Text goTitle = CreateUIText(goPanel.transform, "Title", "HAYATTA KALAMADIN", 34, TextAnchor.MiddleCenter, new Color(0.95f, 0.15f, 0.15f));
            goTitle.rectTransform.anchoredPosition = new Vector2(0f, 50f);
            goTitle.rectTransform.sizeDelta = new Vector2(480f, 60f);
            goTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            goTitle.verticalOverflow = VerticalWrapMode.Overflow;

            Button btnRestart = CreateChoiceButton(goPanel.transform, "Btn_Restart", "Yeniden Başlat", glassPanelSprite ?? uiSprite);
            RectTransform rtRestart = btnRestart.GetComponent<RectTransform>();
            rtRestart.anchoredPosition = new Vector2(0f, -40f);
            rtRestart.sizeDelta = new Vector2(240f, 50f);

            goPanel.SetActive(false);

            // 13. Screen-Center Search Progress Ring (Vehicle Looting)
            GameObject searchProgObj = new GameObject("SearchProgress_Panel");
            searchProgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtSearch = searchProgObj.AddComponent<RectTransform>();
            rtSearch.anchorMin = new Vector2(0.5f, 0.5f);
            rtSearch.anchorMax = new Vector2(0.5f, 0.5f);
            rtSearch.pivot = new Vector2(0.5f, 0.5f);
            rtSearch.anchoredPosition = Vector2.zero;
            rtSearch.sizeDelta = new Vector2(160f, 160f);

            GameObject sRingBgObj = new GameObject("RingBg");
            sRingBgObj.transform.SetParent(searchProgObj.transform, false);
            RectTransform rtSRBg = sRingBgObj.AddComponent<RectTransform>();
            rtSRBg.anchorMin = Vector2.zero;
            rtSRBg.anchorMax = Vector2.one;
            rtSRBg.sizeDelta = Vector2.zero;
            Image sRingBgImg = sRingBgObj.AddComponent<Image>();
            sRingBgImg.sprite = circleRingSprite ?? knobSprite;
            sRingBgImg.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);
            sRingBgImg.raycastTarget = false;

            GameObject sFillObj = new GameObject("RadialFill");
            sFillObj.transform.SetParent(searchProgObj.transform, false);
            RectTransform rtSFill = sFillObj.AddComponent<RectTransform>();
            rtSFill.anchorMin = Vector2.zero;
            rtSFill.anchorMax = Vector2.one;
            rtSFill.sizeDelta = Vector2.zero;
            Image sFillImg = sFillObj.AddComponent<Image>();
            sFillImg.sprite = radialFillSprite ?? knobSprite;
            sFillImg.type = Image.Type.Filled;
            sFillImg.fillMethod = Image.FillMethod.Radial360;
            sFillImg.fillOrigin = (int)Image.Origin360.Top;
            sFillImg.fillClockwise = true;
            sFillImg.fillAmount = 0.5f;
            sFillImg.color = new Color(0.2f, 0.85f, 0.45f, 0.95f);
            sFillImg.raycastTarget = false;

            Text sProgTxt = CreateUIText(searchProgObj.transform, "SearchProgressText", "ARANIYOR... %0", 14, TextAnchor.MiddleCenter, Color.white);
            sProgTxt.fontStyle = FontStyle.Bold;
            sProgTxt.rectTransform.anchorMin = Vector2.zero;
            sProgTxt.rectTransform.anchorMax = Vector2.one;
            sProgTxt.rectTransform.sizeDelta = Vector2.zero;

            searchProgObj.SetActive(false);

            // 14. Fullscreen Tactical World Map (240x240m)
            GameObject mapPanelObj = new GameObject("FullMap_Panel");
            mapPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtMap = mapPanelObj.AddComponent<RectTransform>();
            rtMap.anchorMin = new Vector2(0.5f, 0.5f);
            rtMap.anchorMax = new Vector2(0.5f, 0.5f);
            rtMap.pivot = new Vector2(0.5f, 0.5f);
            rtMap.anchoredPosition = Vector2.zero;
            rtMap.sizeDelta = new Vector2(900f, 900f);

            Image mapBg = mapPanelObj.AddComponent<Image>();
            mapBg.color = new Color(0.05f, 0.07f, 0.10f, 0.98f);
            if (glassPanelSprite != null)
            {
                mapBg.sprite = glassPanelSprite;
                mapBg.type = Image.Type.Sliced;
            }

            // Top Header: Title & Exploration Percentage
            Text mapTitle = CreateUIText(mapPanelObj.transform, "Title", "ŞEHİR TAKTİKSEL HARİTASI (240x240M)", 22, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.35f));
            mapTitle.rectTransform.anchoredPosition = new Vector2(-120f, 415f);
            mapTitle.rectTransform.sizeDelta = new Vector2(500f, 35f);

            Text expPercentTxt = CreateUIText(mapPanelObj.transform, "ExplorationPercent", "KEŞİF: %0", 18, TextAnchor.MiddleRight, new Color(0.35f, 0.85f, 1f));
            expPercentTxt.fontStyle = FontStyle.Bold;
            expPercentTxt.rectTransform.anchoredPosition = new Vector2(230f, 415f);
            expPercentTxt.rectTransform.sizeDelta = new Vector2(160f, 35f);

            Button btnCloseMap = CreateCustomButton(mapPanelObj.transform, "Btn_CloseMap", "X", new Vector2(405f, 415f), new Vector2(54f, 54f), new Color(0.85f, 0.2f, 0.2f, 0.95f), knobSprite, null, 22);

            // Viewport Scroll / Clip Area (760x760)
            GameObject viewAreaObj = new GameObject("MapViewportArea");
            viewAreaObj.transform.SetParent(mapPanelObj.transform, false);
            RectTransform rtViewArea = viewAreaObj.AddComponent<RectTransform>();
            rtViewArea.anchorMin = new Vector2(0.5f, 0.5f);
            rtViewArea.anchorMax = new Vector2(0.5f, 0.5f);
            rtViewArea.pivot = new Vector2(0.5f, 0.5f);
            rtViewArea.anchoredPosition = new Vector2(0f, -10f);
            rtViewArea.sizeDelta = new Vector2(760f, 760f);

            Image viewBg = viewAreaObj.AddComponent<Image>();
            viewBg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);
            viewAreaObj.AddComponent<RectMask2D>();

            // Map Draggable & Zoomable Content (Starts 760x760)
            GameObject fullContentObj = new GameObject("FullMap_Content");
            fullContentObj.transform.SetParent(viewAreaObj.transform, false);
            RectTransform rtFullContent = fullContentObj.AddComponent<RectTransform>();
            rtFullContent.anchorMin = new Vector2(0.5f, 0.5f);
            rtFullContent.anchorMax = new Vector2(0.5f, 0.5f);
            rtFullContent.pivot = new Vector2(0.5f, 0.5f);
            rtFullContent.anchoredPosition = Vector2.zero;
            rtFullContent.sizeDelta = new Vector2(760f, 760f);

            // Full Map Layer 1: Base City Cartography
            GameObject fullBaseObj = new GameObject("FullBaseMap");
            fullBaseObj.transform.SetParent(fullContentObj.transform, false);
            RectTransform rtFullBase = fullBaseObj.AddComponent<RectTransform>();
            rtFullBase.anchorMin = Vector2.zero;
            rtFullBase.anchorMax = Vector2.one;
            rtFullBase.sizeDelta = Vector2.zero;
            RawImage rawFullBase = fullBaseObj.AddComponent<RawImage>();
            if (bakedCityMapTex != null) rawFullBase.texture = bakedCityMapTex;
            rawFullBase.color = new Color(0.9f, 0.95f, 1.0f, 1.0f);

            // Full Map Layer 2: Live Fog of War Overlay
            GameObject fullFogObj = new GameObject("FullFogOfWar");
            fullFogObj.transform.SetParent(fullContentObj.transform, false);
            RectTransform rtFullFog = fullFogObj.AddComponent<RectTransform>();
            rtFullFog.anchorMin = Vector2.zero;
            rtFullFog.anchorMax = Vector2.one;
            rtFullFog.sizeDelta = Vector2.zero;
            RawImage rawFullFog = fullFogObj.AddComponent<RawImage>();
            rawFullFog.color = Color.white;

            // Full Map Layer 3: POI Container
            GameObject fullPoiContObj = new GameObject("Full_POI_Container");
            fullPoiContObj.transform.SetParent(fullContentObj.transform, false);
            RectTransform rtFullPoiCont = fullPoiContObj.AddComponent<RectTransform>();
            rtFullPoiCont.anchorMin = Vector2.zero;
            rtFullPoiCont.anchorMax = Vector2.one;
            rtFullPoiCont.sizeDelta = Vector2.zero;

            // Full Map Layer 4: Player Marker
            GameObject fullPMarkerObj = new GameObject("FullPlayerMarker");
            fullPMarkerObj.transform.SetParent(fullContentObj.transform, false);
            RectTransform rtFullPMarker = fullPMarkerObj.AddComponent<RectTransform>();
            rtFullPMarker.anchorMin = new Vector2(0.5f, 0.5f);
            rtFullPMarker.anchorMax = new Vector2(0.5f, 0.5f);
            rtFullPMarker.pivot = new Vector2(0.5f, 0.5f);
            rtFullPMarker.sizeDelta = new Vector2(24f, 24f);

            Image fullPMarkerImg = fullPMarkerObj.AddComponent<Image>();
            fullPMarkerImg.sprite = playerBlipSprite ?? compassWaypointSprite ?? knobSprite;
            fullPMarkerImg.color = new Color(0.2f, 0.85f, 1f, 1f);
            fullPMarkerImg.raycastTarget = false;

            // Full Map Layer 5: Objective Marker
            GameObject fullObjMarkerObj = new GameObject("FullObjectiveMarker");
            fullObjMarkerObj.transform.SetParent(fullContentObj.transform, false);
            RectTransform rtFullObjMarker = fullObjMarkerObj.AddComponent<RectTransform>();
            rtFullObjMarker.anchorMin = new Vector2(0.5f, 0.5f);
            rtFullObjMarker.anchorMax = new Vector2(0.5f, 0.5f);
            rtFullObjMarker.pivot = new Vector2(0.5f, 0.5f);
            rtFullObjMarker.sizeDelta = new Vector2(30f, 30f);

            Image fullObjMarkerImg = fullObjMarkerObj.AddComponent<Image>();
            fullObjMarkerImg.sprite = circleGlowSprite ?? knobSprite;
            fullObjMarkerImg.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            fullObjMarkerImg.raycastTarget = false;
            fullObjMarkerObj.SetActive(false);

            // Full Map Layer 6: Custom Waypoint Marker
            GameObject fullWpMarkerObj = new GameObject("FullCustomWaypointMarker");
            fullWpMarkerObj.transform.SetParent(fullContentObj.transform, false);
            RectTransform rtFullWpMarker = fullWpMarkerObj.AddComponent<RectTransform>();
            rtFullWpMarker.anchorMin = new Vector2(0.5f, 0.5f);
            rtFullWpMarker.anchorMax = new Vector2(0.5f, 0.5f);
            rtFullWpMarker.pivot = new Vector2(0.5f, 0.5f);
            rtFullWpMarker.sizeDelta = new Vector2(30f, 30f);

            Image fullWpMarkerImg = fullWpMarkerObj.AddComponent<Image>();
            fullWpMarkerImg.sprite = customPinSprite ?? knobSprite;
            fullWpMarkerImg.color = new Color(1f, 0.72f, 0.01f, 1f);
            fullWpMarkerImg.raycastTarget = false;
            fullWpMarkerObj.SetActive(false);

            // Tactile Zoom & Navigation Buttons (Side Floating Controls)
            Button btnZoomIn = CreateCustomButton(mapPanelObj.transform, "Btn_ZoomIn", "+", new Vector2(405f, 120f), new Vector2(46f, 46f), new Color(0.18f, 0.24f, 0.35f, 0.95f), knobSprite, null, 24);
            Button btnZoomOut = CreateCustomButton(mapPanelObj.transform, "Btn_ZoomOut", "-", new Vector2(405f, 65f), new Vector2(46f, 46f), new Color(0.18f, 0.24f, 0.35f, 0.95f), knobSprite, null, 24);
            Button btnRecenter = CreateCustomButton(mapPanelObj.transform, "Btn_Recenter", "O", new Vector2(405f, 10f), new Vector2(46f, 46f), new Color(0.18f, 0.24f, 0.35f, 0.95f), knobSprite, null, 20);

            // Clear Waypoint Button ("İŞARETİ KALDIR")
            Button btnClearWp = CreateCustomButton(mapPanelObj.transform, "Btn_ClearWaypoint", "İŞARETİ KALDIR", new Vector2(-280f, -400f), new Vector2(160f, 38f), new Color(0.6f, 0.25f, 0.1f, 0.95f), glassPanelSprite ?? knobSprite, null, 11);

            // Bottom Location Info Card (Hidden by default, shown on POI tap)
            GameObject infoCardObj = new GameObject("Location_InfoCard");
            infoCardObj.transform.SetParent(mapPanelObj.transform, false);
            RectTransform rtInfoCard = infoCardObj.AddComponent<RectTransform>();
            rtInfoCard.anchorMin = new Vector2(0.5f, 0f);
            rtInfoCard.anchorMax = new Vector2(0.5f, 0f);
            rtInfoCard.pivot = new Vector2(0.5f, 0f);
            rtInfoCard.anchoredPosition = new Vector2(40f, 30f);
            rtInfoCard.sizeDelta = new Vector2(460f, 75f);

            Image infoCardBg = infoCardObj.AddComponent<Image>();
            infoCardBg.color = new Color(0.08f, 0.11f, 0.16f, 0.96f);
            if (glassPanelSprite != null)
            {
                infoCardBg.sprite = glassPanelSprite;
                infoCardBg.type = Image.Type.Sliced;
            }

            Text cardTitle = CreateUIText(infoCardObj.transform, "CardTitle", "Konum İsmi", 16, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f));
            cardTitle.fontStyle = FontStyle.Bold;
            cardTitle.rectTransform.anchoredPosition = new Vector2(-60f, 16f);
            cardTitle.rectTransform.sizeDelta = new Vector2(300f, 24f);

            Text cardStatus = CreateUIText(infoCardObj.transform, "CardStatus", "[GÜVENLİ BÖLGE]", 12, TextAnchor.MiddleLeft, new Color(0.35f, 0.85f, 1f));
            cardStatus.fontStyle = FontStyle.Bold;
            cardStatus.rectTransform.anchoredPosition = new Vector2(-60f, -14f);
            cardStatus.rectTransform.sizeDelta = new Vector2(160f, 20f);

            Text cardDist = CreateUIText(infoCardObj.transform, "CardDistance", "64m", 12, TextAnchor.MiddleLeft, new Color(0.8f, 0.85f, 0.9f));
            cardDist.rectTransform.anchoredPosition = new Vector2(110f, -14f);
            cardDist.rectTransform.sizeDelta = new Vector2(100f, 20f);

            Button btnCardNav = CreateChoiceButton(infoCardObj.transform, "Btn_Navigate", "GİT", glassPanelSprite ?? uiSprite);
            RectTransform rtNavBtn = btnCardNav.GetComponent<RectTransform>();
            rtNavBtn.anchoredPosition = new Vector2(165f, 0f);
            rtNavBtn.sizeDelta = new Vector2(75f, 42f);

            infoCardObj.SetActive(false);

            FullTacticalMapUI fullTacticalMapUI = mapPanelObj.AddComponent<FullTacticalMapUI>();
            fullTacticalMapUI.Configure(mapPanelObj, btnCloseMap, rtViewArea, rtFullContent,
                rawFullBase, rawFullFog, rtFullPMarker, rtFullObjMarker, rtFullWpMarker,
                mapTitle, expPercentTxt, btnZoomIn, btnZoomOut, btnRecenter, btnClearWp,
                infoCardObj, cardTitle, cardDist, cardStatus, btnCardNav);

            mapPanelObj.SetActive(false);

            // Wire HUD properties
            SerializedObject soHud = new SerializedObject(hud);
            soHud.FindProperty("healthSlider").objectReferenceValue = null;
            soHud.FindProperty("staminaSlider").objectReferenceValue = null;
            soHud.FindProperty("healthText").objectReferenceValue = null;
            soHud.FindProperty("staminaText").objectReferenceValue = null;
            soHud.FindProperty("hungerSlider").objectReferenceValue = null;
            soHud.FindProperty("thirstSlider").objectReferenceValue = null;
            soHud.FindProperty("infectionSlider").objectReferenceValue = null;

            soHud.FindProperty("sprintButton").objectReferenceValue = btnSprint;
            soHud.FindProperty("crouchButton").objectReferenceValue = btnCrouch;
            soHud.FindProperty("interactButton").objectReferenceValue = btnInteract;
            soHud.FindProperty("dodgeButton").objectReferenceValue = null;
            soHud.FindProperty("craftButton").objectReferenceValue = null;
            soHud.FindProperty("flashlightButton").objectReferenceValue = null;
            soHud.FindProperty("flashlightText").objectReferenceValue = null;
            soHud.FindProperty("timeText").objectReferenceValue = null;

            soHud.FindProperty("takedownButton").objectReferenceValue = null;
            soHud.FindProperty("pushButton").objectReferenceValue = null;
            soHud.FindProperty("backpackButton").objectReferenceValue = btnBackpack;
            soHud.FindProperty("pauseButton").objectReferenceValue = btnPause;
            soHud.FindProperty("mapButton").objectReferenceValue = btnOpenMinimap;

            soHud.FindProperty("hungerCriticalBadge").objectReferenceValue = null;
            soHud.FindProperty("thirstCriticalBadge").objectReferenceValue = null;
            soHud.FindProperty("infectionCriticalBadge").objectReferenceValue = null;

            soHud.FindProperty("flashlightRadialFill").objectReferenceValue = null;
            soHud.FindProperty("crouchGlowImage").objectReferenceValue = crouchGlowImg;
            soHud.FindProperty("interactButtonText").objectReferenceValue = interactBtnText;

            soHud.FindProperty("gameOverPanel").objectReferenceValue = goPanel;
            soHud.FindProperty("restartButton").objectReferenceValue = btnRestart;

            soHud.FindProperty("searchProgressRoot").objectReferenceValue = searchProgObj;
            soHud.FindProperty("searchRadialFill").objectReferenceValue = sFillImg;
            soHud.FindProperty("searchProgressText").objectReferenceValue = sProgTxt;

            // Phase 8 HUD bindings
            soHud.FindProperty("crosshairRoot").objectReferenceValue = rtCross;
            soHud.FindProperty("crosshairTop").objectReferenceValue = chTop;
            soHud.FindProperty("crosshairBottom").objectReferenceValue = chBottom;
            soHud.FindProperty("crosshairLeft").objectReferenceValue = chLeft;
            soHud.FindProperty("crosshairRight").objectReferenceValue = chRight;
            soHud.FindProperty("hitMarkerObj").objectReferenceValue = hitMarkerObj;
            soHud.FindProperty("weaponNameText").objectReferenceValue = wpNameTxt;
            soHud.FindProperty("ammoCounterText").objectReferenceValue = ammoTxt;
            soHud.FindProperty("fireButton").objectReferenceValue = btnFire;
            soHud.FindProperty("aimButton").objectReferenceValue = btnAim;
            soHud.FindProperty("reloadButton").objectReferenceValue = null;
            soHud.FindProperty("toastPanel").objectReferenceValue = toastObj;
            soHud.FindProperty("toastText").objectReferenceValue = toastTxt;

            SerializedProperty qsProp = soHud.FindProperty("quickSlotButtons");
            qsProp.ClearArray();
            for (int i = 0; i < qsButtons.Count; i++)
            {
                qsProp.InsertArrayElementAtIndex(i);
                qsProp.GetArrayElementAtIndex(i).objectReferenceValue = qsButtons[i];
            }
            soHud.ApplyModifiedProperties();

            // 11. Fullscreen Tabbed Backpack UI (Envanter & Notlar/Günlük)
            GameObject backpackPanelObj = new GameObject("Backpack_Panel");
            backpackPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtBp = backpackPanelObj.AddComponent<RectTransform>();
            rtBp.anchorMin = new Vector2(0.5f, 0.5f);
            rtBp.anchorMax = new Vector2(0.5f, 0.5f);
            rtBp.pivot = new Vector2(0.5f, 0.5f);
            rtBp.anchoredPosition = Vector2.zero;
            rtBp.sizeDelta = new Vector2(880f, 540f);

            Image bpBg = backpackPanelObj.AddComponent<Image>();
            bpBg.color = new Color(0.08f, 0.1f, 0.14f, 0.97f);
            if (glassPanelSprite != null)
            {
                bpBg.sprite = glassPanelSprite;
                bpBg.type = Image.Type.Sliced;
            }

            Button btnCloseBp = CreateCustomButton(backpackPanelObj.transform, "Btn_CloseBackpack", "X", new Vector2(400f, 235f), new Vector2(45f, 45f), new Color(0.8f, 0.2f, 0.2f, 0.95f), knobSprite, null, 20);

            GameObject tabsArea = new GameObject("TabButtons");
            tabsArea.transform.SetParent(backpackPanelObj.transform, false);
            RectTransform rtTabs = tabsArea.AddComponent<RectTransform>();
            rtTabs.anchorMin = new Vector2(0.5f, 1f);
            rtTabs.anchorMax = new Vector2(0.5f, 1f);
            rtTabs.pivot = new Vector2(0.5f, 1f);
            rtTabs.anchoredPosition = new Vector2(-60f, -20f);
            rtTabs.sizeDelta = new Vector2(400f, 45f);

            HorizontalLayoutGroup hlgTabs = tabsArea.AddComponent<HorizontalLayoutGroup>();
            hlgTabs.spacing = 15f;

            Button btnTabInv = CreateChoiceButton(tabsArea.transform, "Tab_Inventory", "ÇANTA (ENVANTER)", glassPanelSprite ?? uiSprite);
            btnTabInv.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 42f);

            Button btnTabNotes = CreateChoiceButton(tabsArea.transform, "Tab_Notes", "NOTLAR / GÜNLÜK", glassPanelSprite ?? uiSprite);
            btnTabNotes.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 42f);

            // Inventory Area
            GameObject invArea = new GameObject("InventoryContent");
            invArea.transform.SetParent(backpackPanelObj.transform, false);
            RectTransform rtInvArea = invArea.AddComponent<RectTransform>();
            rtInvArea.anchorMin = new Vector2(0.5f, 0.5f);
            rtInvArea.anchorMax = new Vector2(0.5f, 0.5f);
            rtInvArea.pivot = new Vector2(0.5f, 0.5f);
            rtInvArea.anchoredPosition = new Vector2(0f, -30f);
            rtInvArea.sizeDelta = new Vector2(820f, 420f);

            GameObject slotsGridObj = new GameObject("SlotsGrid");
            slotsGridObj.transform.SetParent(invArea.transform, false);
            RectTransform rtGrid = slotsGridObj.AddComponent<RectTransform>();
            rtGrid.anchorMin = new Vector2(0f, 0.5f);
            rtGrid.anchorMax = new Vector2(0f, 0.5f);
            rtGrid.pivot = new Vector2(0f, 0.5f);
            rtGrid.anchoredPosition = new Vector2(20f, 0f);
            rtGrid.sizeDelta = new Vector2(460f, 400f);

            GridLayoutGroup glg = slotsGridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(100f, 85f);
            glg.spacing = new Vector2(12f, 12f);

            GameObject itemInfoObj = new GameObject("ItemInfoArea");
            itemInfoObj.transform.SetParent(invArea.transform, false);
            RectTransform rtInfo = itemInfoObj.AddComponent<RectTransform>();
            rtInfo.anchorMin = new Vector2(1f, 0.5f);
            rtInfo.anchorMax = new Vector2(1f, 0.5f);
            rtInfo.pivot = new Vector2(1f, 0.5f);
            rtInfo.anchoredPosition = new Vector2(-20f, 0f);
            rtInfo.sizeDelta = new Vector2(300f, 400f);

            Image infoBg = itemInfoObj.AddComponent<Image>();
            infoBg.color = new Color(0.12f, 0.15f, 0.2f, 0.9f);
            if (glassPanelSprite != null)
            {
                infoBg.sprite = glassPanelSprite;
                infoBg.type = Image.Type.Sliced;
            }

            Text infoTitle = CreateUIText(itemInfoObj.transform, "ItemTitle", "Eşya Seçiniz", 22, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.3f));
            infoTitle.rectTransform.anchoredPosition = new Vector2(0f, 150f);
            infoTitle.rectTransform.sizeDelta = new Vector2(280f, 35f);

            Text infoDesc = CreateUIText(itemInfoObj.transform, "ItemDesc", "Açıklama...", 16, TextAnchor.UpperLeft, Color.white);
            infoDesc.rectTransform.anchoredPosition = new Vector2(0f, 60f);
            infoDesc.rectTransform.sizeDelta = new Vector2(260f, 140f);

            Button btnUseItem = CreateChoiceButton(itemInfoObj.transform, "Btn_UseItem", "KULLAN / KUŞAN", glassPanelSprite ?? uiSprite);
            btnUseItem.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 48f);
            btnUseItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -80f);

            Button btnDropItem = CreateChoiceButton(itemInfoObj.transform, "Btn_DropItem", "YERE AT", glassPanelSprite ?? uiSprite);
            btnDropItem.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 48f);
            btnDropItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -140f);

            // Notes Area
            GameObject notesArea = new GameObject("NotesContent");
            notesArea.transform.SetParent(backpackPanelObj.transform, false);
            RectTransform rtNotesArea = notesArea.AddComponent<RectTransform>();
            rtNotesArea.anchorMin = new Vector2(0.5f, 0.5f);
            rtNotesArea.anchorMax = new Vector2(0.5f, 0.5f);
            rtNotesArea.pivot = new Vector2(0.5f, 0.5f);
            rtNotesArea.anchoredPosition = new Vector2(0f, -30f);
            rtNotesArea.sizeDelta = new Vector2(820f, 420f);

            VerticalLayoutGroup vlgNotes = notesArea.AddComponent<VerticalLayoutGroup>();
            vlgNotes.spacing = 15f;
            vlgNotes.padding = new RectOffset(20, 20, 15, 15);
            vlgNotes.childForceExpandHeight = false;

            notesArea.SetActive(false);

            BackpackUI bpUI = backpackPanelObj.AddComponent<BackpackUI>();
            SerializedObject soBp = new SerializedObject(bpUI);
            soBp.FindProperty("backpackPanel").objectReferenceValue = backpackPanelObj;
            soBp.FindProperty("closeButton").objectReferenceValue = btnCloseBp;
            soBp.FindProperty("inventoryTabButton").objectReferenceValue = btnTabInv;
            soBp.FindProperty("notesTabButton").objectReferenceValue = btnTabNotes;
            soBp.FindProperty("inventoryContentArea").objectReferenceValue = invArea;
            soBp.FindProperty("notesContentArea").objectReferenceValue = notesArea;
            soBp.FindProperty("slotsContainer").objectReferenceValue = slotsGridObj.transform;
            soBp.FindProperty("selectedItemTitle").objectReferenceValue = infoTitle;
            soBp.FindProperty("selectedItemDescription").objectReferenceValue = infoDesc;
            soBp.FindProperty("useItemButton").objectReferenceValue = btnUseItem;
            soBp.FindProperty("dropItemButton").objectReferenceValue = btnDropItem;
            soBp.FindProperty("notesContainer").objectReferenceValue = notesArea.transform;
            soBp.ApplyModifiedProperties();

            backpackPanelObj.SetActive(false);

            // 12. Stash UI (İki Kolonlu Sığınak Sandığı)
            GameObject stashPanelObj = new GameObject("Stash_Panel");
            stashPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtSp = stashPanelObj.AddComponent<RectTransform>();
            rtSp.anchorMin = new Vector2(0.5f, 0.5f);
            rtSp.anchorMax = new Vector2(0.5f, 0.5f);
            rtSp.pivot = new Vector2(0.5f, 0.5f);
            rtSp.anchoredPosition = Vector2.zero;
            rtSp.sizeDelta = new Vector2(940f, 560f);

            Image spBg = stashPanelObj.AddComponent<Image>();
            spBg.color = new Color(0.08f, 0.1f, 0.14f, 0.97f);
            if (glassPanelSprite != null)
            {
                spBg.sprite = glassPanelSprite;
                spBg.type = Image.Type.Sliced;
            }

            Button btnCloseStash = CreateCustomButton(stashPanelObj.transform, "Btn_CloseStash", "X", new Vector2(430f, 245f), new Vector2(45f, 45f), new Color(0.8f, 0.2f, 0.2f, 0.95f), knobSprite, null, 20);

            Text stashTitleText = CreateUIText(stashPanelObj.transform, "Title", "SIĞINAK DEPOSU (24 GÖZ)", 26, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f));
            stashTitleText.rectTransform.anchoredPosition = new Vector2(0f, 245f);
            stashTitleText.rectTransform.sizeDelta = new Vector2(400f, 40f);

            GameObject pColObj = new GameObject("PlayerSlotsContainer");
            pColObj.transform.SetParent(stashPanelObj.transform, false);
            RectTransform rtPCol = pColObj.AddComponent<RectTransform>();
            rtPCol.anchorMin = new Vector2(0.26f, 0.45f);
            rtPCol.anchorMax = new Vector2(0.26f, 0.45f);
            rtPCol.pivot = new Vector2(0.5f, 0.5f);
            rtPCol.sizeDelta = new Vector2(420f, 420f);

            GridLayoutGroup glgP = pColObj.AddComponent<GridLayoutGroup>();
            glgP.cellSize = new Vector2(95f, 85f);
            glgP.spacing = new Vector2(10f, 10f);

            GameObject sColObj = new GameObject("StashSlotsContainer");
            sColObj.transform.SetParent(stashPanelObj.transform, false);
            RectTransform rtSCol = sColObj.AddComponent<RectTransform>();
            rtSCol.anchorMin = new Vector2(0.74f, 0.45f);
            rtSCol.anchorMax = new Vector2(0.74f, 0.45f);
            rtSCol.pivot = new Vector2(0.5f, 0.5f);
            rtSCol.sizeDelta = new Vector2(420f, 420f);

            GridLayoutGroup glgS = sColObj.AddComponent<GridLayoutGroup>();
            glgS.cellSize = new Vector2(95f, 85f);
            glgS.spacing = new Vector2(10f, 10f);

            StashUI stashUI = stashPanelObj.AddComponent<StashUI>();
            SerializedObject soStashUI = new SerializedObject(stashUI);
            soStashUI.FindProperty("stashPanel").objectReferenceValue = stashPanelObj;
            soStashUI.FindProperty("closeButton").objectReferenceValue = btnCloseStash;
            soStashUI.FindProperty("playerSlotsContainer").objectReferenceValue = pColObj.transform;
            soStashUI.FindProperty("stashSlotsContainer").objectReferenceValue = sColObj.transform;
            soStashUI.ApplyModifiedProperties();

            stashPanelObj.SetActive(false);

            // 13. Settings Menu UI
            GameObject settingsPanelObj = new GameObject("Settings_Panel");
            settingsPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtSettings = settingsPanelObj.AddComponent<RectTransform>();
            rtSettings.anchorMin = new Vector2(0.5f, 0.5f);
            rtSettings.anchorMax = new Vector2(0.5f, 0.5f);
            rtSettings.pivot = new Vector2(0.5f, 0.5f);
            rtSettings.anchoredPosition = Vector2.zero;
            rtSettings.sizeDelta = new Vector2(880f, 620f);

            Image settingsBg = settingsPanelObj.AddComponent<Image>();
            settingsBg.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
            if (glassPanelSprite != null)
            {
                settingsBg.sprite = glassPanelSprite;
                settingsBg.type = Image.Type.Sliced;
            }

            Text settingsTitle = CreateUIText(settingsPanelObj.transform, "Title", "AYARLAR", 26, TextAnchor.MiddleCenter, new Color(0.95f, 0.75f, 0.2f));
            settingsTitle.fontStyle = FontStyle.Bold;
            settingsTitle.rectTransform.anchoredPosition = new Vector2(0f, 270f);
            settingsTitle.rectTransform.sizeDelta = new Vector2(400f, 36f);

            Button btnCloseSettings = CreateCustomButton(
                settingsPanelObj.transform, "Btn_CloseSettings", "X",
                new Vector2(400f, 270f), new Vector2(38f, 38f),
                new Color(0.85f, 0.2f, 0.2f, 0.95f),
                knobSprite, null, 18,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f)
            );

            // Left Column: Audio Settings (X: -210, Y: -5, Size: 400x460)
            GameObject leftCol = new GameObject("LeftCol_Audio");
            leftCol.transform.SetParent(settingsPanelObj.transform, false);
            RectTransform rtLeftCol = leftCol.AddComponent<RectTransform>();
            rtLeftCol.anchorMin = new Vector2(0.5f, 0.5f);
            rtLeftCol.anchorMax = new Vector2(0.5f, 0.5f);
            rtLeftCol.pivot = new Vector2(0.5f, 0.5f);
            rtLeftCol.anchoredPosition = new Vector2(-210f, -5f);
            rtLeftCol.sizeDelta = new Vector2(400f, 460f);

            VerticalLayoutGroup vlgLeft = leftCol.AddComponent<VerticalLayoutGroup>();
            vlgLeft.spacing = 12f;
            vlgLeft.childControlWidth = true;
            vlgLeft.childControlHeight = false;
            vlgLeft.childForceExpandWidth = true;
            vlgLeft.childForceExpandHeight = false;

            Text audioSectionHdr = CreateUIText(leftCol.transform, "AudioHeader", "SES AYARLARI", 16, TextAnchor.MiddleLeft, new Color(0.25f, 0.75f, 1f));
            audioSectionHdr.fontStyle = FontStyle.Bold;
            audioSectionHdr.rectTransform.sizeDelta = new Vector2(380f, 24f);

            Slider sldMaster = CreateInteractiveSlider(leftCol.transform, "Slider_Master", "Ana Ses Seviyesi (Master)", 0f, 1f, 0.8f, "%", knobSprite, glassPanelSprite ?? uiSprite, out Text txtMaster);
            Slider sldSfx = CreateInteractiveSlider(leftCol.transform, "Slider_SFX", "Efekt Sesleri (SFX)", 0f, 1f, 0.8f, "%", knobSprite, glassPanelSprite ?? uiSprite, out Text txtSfx);
            Slider sldAmb = CreateInteractiveSlider(leftCol.transform, "Slider_Ambient", "Ortam Sesi (Atmosfer)", 0f, 1f, 0.6f, "%", knobSprite, glassPanelSprite ?? uiSprite, out Text txtAmb);
            Toggle togMute = CreateInteractiveToggle(leftCol.transform, "Toggle_Mute", "Tüm Sesleri Kapat (Mute)", false, knobSprite, glassPanelSprite ?? uiSprite);

            // Right Column: Camera, Controls & Graphics (X: 210, Y: -5, Size: 400x460)
            GameObject rightCol = new GameObject("RightCol_Controls");
            rightCol.transform.SetParent(settingsPanelObj.transform, false);
            RectTransform rtRightCol = rightCol.AddComponent<RectTransform>();
            rtRightCol.anchorMin = new Vector2(0.5f, 0.5f);
            rtRightCol.anchorMax = new Vector2(0.5f, 0.5f);
            rtRightCol.pivot = new Vector2(0.5f, 0.5f);
            rtRightCol.anchoredPosition = new Vector2(210f, -5f);
            rtRightCol.sizeDelta = new Vector2(400f, 460f);

            VerticalLayoutGroup vlgRight = rightCol.AddComponent<VerticalLayoutGroup>();
            vlgRight.spacing = 10f;
            vlgRight.childControlWidth = true;
            vlgRight.childControlHeight = false;
            vlgRight.childForceExpandWidth = true;
            vlgRight.childForceExpandHeight = false;

            Text camSectionHdr = CreateUIText(rightCol.transform, "CameraHeader", "KAMERA VE KONTROL", 16, TextAnchor.MiddleLeft, new Color(0.25f, 0.75f, 1f));
            camSectionHdr.fontStyle = FontStyle.Bold;
            camSectionHdr.rectTransform.sizeDelta = new Vector2(380f, 22f);

            Slider sldLook = CreateInteractiveSlider(rightCol.transform, "Slider_LookSens", "Kamera Hassasiyeti", 0.2f, 3.0f, 1.2f, "x", knobSprite, glassPanelSprite ?? uiSprite, out Text txtLook);
            Slider sldAim = CreateInteractiveSlider(rightCol.transform, "Slider_AimSens", "Nişan Hassasiyeti", 0.2f, 2.5f, 0.8f, "x", knobSprite, glassPanelSprite ?? uiSprite, out Text txtAim);
            Slider sldFov = CreateInteractiveSlider(rightCol.transform, "Slider_FOV", "Görüş Açısı (FOV)", 50f, 75f, 60f, "°", knobSprite, glassPanelSprite ?? uiSprite, out Text txtFov);
            Toggle togInvertY = CreateInteractiveToggle(rightCol.transform, "Toggle_InvertY", "Y Eksenini Ters Çevir", false, knobSprite, glassPanelSprite ?? uiSprite);

            Text gfxSectionHdr = CreateUIText(rightCol.transform, "GraphicsHeader", "GRAFİK VE OYNANIŞ", 16, TextAnchor.MiddleLeft, new Color(0.25f, 0.75f, 1f));
            gfxSectionHdr.fontStyle = FontStyle.Bold;
            gfxSectionHdr.rectTransform.sizeDelta = new Vector2(380f, 22f);

            CreateQualitySelectorUI(rightCol.transform, glassPanelSprite, uiSprite, out Button btnQualLow, out Button btnQualMed, out Button btnQualHigh, out Text txtQualStatus);
            Toggle togShake = CreateInteractiveToggle(rightCol.transform, "Toggle_Shake", "Kamera Sarsıntısı (Shake)", true, knobSprite, glassPanelSprite ?? uiSprite);

            // Bottom Bar: Actions
            GameObject bottomBar = new GameObject("BottomActions");
            bottomBar.transform.SetParent(settingsPanelObj.transform, false);
            RectTransform rtBottom = bottomBar.AddComponent<RectTransform>();
            rtBottom.anchorMin = new Vector2(0.5f, 0.5f);
            rtBottom.anchorMax = new Vector2(0.5f, 0.5f);
            rtBottom.pivot = new Vector2(0.5f, 0.5f);
            rtBottom.anchoredPosition = new Vector2(0f, -265f);
            rtBottom.sizeDelta = new Vector2(820f, 48f);

            Button btnResetDef = CreateChoiceButton(bottomBar.transform, "Btn_ResetDefaults", "VARSAYILANLARA SIFIRLA", glassPanelSprite ?? uiSprite);
            RectTransform rtReset = btnResetDef.GetComponent<RectTransform>();
            rtReset.anchorMin = new Vector2(0f, 0.5f);
            rtReset.anchorMax = new Vector2(0f, 0.5f);
            rtReset.pivot = new Vector2(0f, 0.5f);
            rtReset.anchoredPosition = Vector2.zero;
            rtReset.sizeDelta = new Vector2(260f, 44f);
            btnResetDef.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.28f, 0.95f);

            Button btnSaveBack = CreateChoiceButton(bottomBar.transform, "Btn_SaveBack", "KAYDET VE GERİ DÖN", glassPanelSprite ?? uiSprite);
            RectTransform rtSaveBack = btnSaveBack.GetComponent<RectTransform>();
            rtSaveBack.anchorMin = new Vector2(1f, 0.5f);
            rtSaveBack.anchorMax = new Vector2(1f, 0.5f);
            rtSaveBack.pivot = new Vector2(1f, 0.5f);
            rtSaveBack.anchoredPosition = Vector2.zero;
            rtSaveBack.sizeDelta = new Vector2(260f, 44f);
            btnSaveBack.GetComponent<Image>().color = new Color(0.12f, 0.55f, 0.85f, 0.95f);

            SettingsUI settingsUI = settingsPanelObj.AddComponent<SettingsUI>();
            SerializedObject soSettingsUI = new SerializedObject(settingsUI);
            soSettingsUI.FindProperty("settingsPanel").objectReferenceValue = settingsPanelObj;
            soSettingsUI.FindProperty("masterVolumeSlider").objectReferenceValue = sldMaster;
            soSettingsUI.FindProperty("masterVolumeText").objectReferenceValue = txtMaster;
            soSettingsUI.FindProperty("sfxVolumeSlider").objectReferenceValue = sldSfx;
            soSettingsUI.FindProperty("sfxVolumeText").objectReferenceValue = txtSfx;
            soSettingsUI.FindProperty("ambientVolumeSlider").objectReferenceValue = sldAmb;
            soSettingsUI.FindProperty("ambientVolumeText").objectReferenceValue = txtAmb;
            soSettingsUI.FindProperty("muteToggle").objectReferenceValue = togMute;

            soSettingsUI.FindProperty("lookSensitivitySlider").objectReferenceValue = sldLook;
            soSettingsUI.FindProperty("lookSensitivityText").objectReferenceValue = txtLook;
            soSettingsUI.FindProperty("aimSensitivitySlider").objectReferenceValue = sldAim;
            soSettingsUI.FindProperty("aimSensitivityText").objectReferenceValue = txtAim;
            soSettingsUI.FindProperty("fovSlider").objectReferenceValue = sldFov;
            soSettingsUI.FindProperty("fovText").objectReferenceValue = txtFov;
            soSettingsUI.FindProperty("invertYToggle").objectReferenceValue = togInvertY;

            soSettingsUI.FindProperty("qualityLowButton").objectReferenceValue = btnQualLow;
            soSettingsUI.FindProperty("qualityMedButton").objectReferenceValue = btnQualMed;
            soSettingsUI.FindProperty("qualityHighButton").objectReferenceValue = btnQualHigh;
            soSettingsUI.FindProperty("qualityCurrentText").objectReferenceValue = txtQualStatus;
            soSettingsUI.FindProperty("cameraShakeToggle").objectReferenceValue = togShake;

            soSettingsUI.FindProperty("resetDefaultsButton").objectReferenceValue = btnResetDef;
            soSettingsUI.FindProperty("saveAndBackButton").objectReferenceValue = btnSaveBack;
            soSettingsUI.FindProperty("closeButton").objectReferenceValue = btnCloseSettings;
            soSettingsUI.ApplyModifiedProperties();

            settingsPanelObj.SetActive(false);

            // 14. Pause Menu UI (Dark Glassmorphism)
            GameObject pausePanelObj = new GameObject("Pause_Panel");
            pausePanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtPause = pausePanelObj.AddComponent<RectTransform>();
            rtPause.anchorMin = new Vector2(0.5f, 0.5f);
            rtPause.anchorMax = new Vector2(0.5f, 0.5f);
            rtPause.pivot = new Vector2(0.5f, 0.5f);
            rtPause.anchoredPosition = Vector2.zero;
            rtPause.sizeDelta = new Vector2(480f, 440f);

            Image pauseBg = pausePanelObj.AddComponent<Image>();
            pauseBg.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
            if (glassPanelSprite != null)
            {
                pauseBg.sprite = glassPanelSprite;
                pauseBg.type = Image.Type.Sliced;
            }

            Text pauseTitle = CreateUIText(pausePanelObj.transform, "Title", "DURAKLATILDI", 30, TextAnchor.MiddleCenter, Color.white);
            pauseTitle.fontStyle = FontStyle.Bold;
            pauseTitle.rectTransform.anchoredPosition = new Vector2(0f, 155f);
            pauseTitle.rectTransform.sizeDelta = new Vector2(400f, 40f);

            GameObject pauseBtns = new GameObject("PauseButtons");
            pauseBtns.transform.SetParent(pausePanelObj.transform, false);
            RectTransform rtPb = pauseBtns.AddComponent<RectTransform>();
            rtPb.anchorMin = new Vector2(0.5f, 0.5f);
            rtPb.anchorMax = new Vector2(0.5f, 0.5f);
            rtPb.pivot = new Vector2(0.5f, 0.5f);
            rtPb.anchoredPosition = new Vector2(0f, 5f);
            rtPb.sizeDelta = new Vector2(340f, 250f);

            VerticalLayoutGroup vlgPb = pauseBtns.AddComponent<VerticalLayoutGroup>();
            vlgPb.spacing = 14f;
            vlgPb.childForceExpandHeight = false;
            vlgPb.childControlWidth = true;
            vlgPb.childControlHeight = false;

            Button btnResume = CreateChoiceButton(pauseBtns.transform, "Btn_Resume", "DEVAM ET", glassPanelSprite ?? uiSprite);
            btnResume.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 48f);

            Button btnSave = CreateChoiceButton(pauseBtns.transform, "Btn_Save", "OYUNU KAYDET", glassPanelSprite ?? uiSprite);
            btnSave.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 48f);

            Button btnSettings = CreateChoiceButton(pauseBtns.transform, "Btn_Settings", "AYARLAR", glassPanelSprite ?? uiSprite);
            btnSettings.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 48f);

            Button btnPauseRestart = CreateChoiceButton(pauseBtns.transform, "Btn_Restart", "YENİDEN BAŞLAT", glassPanelSprite ?? uiSprite);
            btnPauseRestart.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 48f);

            Text saveFeedback = CreateUIText(pausePanelObj.transform, "SaveFeedback", "", 16, TextAnchor.MiddleCenter, new Color(0.2f, 0.9f, 0.4f));
            saveFeedback.rectTransform.anchoredPosition = new Vector2(0f, -165f);
            saveFeedback.rectTransform.sizeDelta = new Vector2(380f, 30f);

            pauseMenuCtrl.Configure(pausePanelObj, settingsUI, btnPause, btnResume, btnSave, btnSettings, btnPauseRestart, saveFeedback);

            pausePanelObj.SetActive(false);

            // 14. Main Menu UI Overlay (Non-blocking modern card overlay)
            GameObject menuPanelObj = new GameObject("MainMenu_Panel");
            menuPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtMenu = menuPanelObj.AddComponent<RectTransform>();
            rtMenu.anchorMin = Vector2.zero;
            rtMenu.anchorMax = Vector2.one;
            rtMenu.sizeDelta = Vector2.zero;

            Image menuBg = menuPanelObj.AddComponent<Image>();
            menuBg.color = new Color(0.04f, 0.05f, 0.07f, 0.75f); // Translucent backdrop so 3D world is atmospheric
            menuBg.raycastTarget = true;

            // Center Card
            GameObject cardObj = new GameObject("MenuCard");
            cardObj.transform.SetParent(menuPanelObj.transform, false);
            RectTransform rtCard = cardObj.AddComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtCard.pivot = new Vector2(0.5f, 0.5f);
            rtCard.anchoredPosition = Vector2.zero;
            rtCard.sizeDelta = new Vector2(560f, 400f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);
            if (glassPanelSprite != null)
            {
                cardBg.sprite = glassPanelSprite;
                cardBg.type = Image.Type.Sliced;
            }
            cardBg.raycastTarget = false;

            Text menuTitle = CreateUIText(cardObj.transform, "GameTitle", "LAST DAY: SURVIVAL", 36, TextAnchor.MiddleCenter, new Color(0.95f, 0.22f, 0.22f));
            menuTitle.rectTransform.anchoredPosition = new Vector2(0f, 135f);
            menuTitle.rectTransform.sizeDelta = new Vector2(520f, 50f);

            Text menuSub = CreateUIText(cardObj.transform, "Subtitle", "ZOMBİ HAYATTA KALMA & İNTERAKTİF HİKAYE", 16, TextAnchor.MiddleCenter, new Color(0.7f, 0.75f, 0.8f));
            menuSub.rectTransform.anchoredPosition = new Vector2(0f, 95f);
            menuSub.rectTransform.sizeDelta = new Vector2(520f, 30f);

            GameObject menuBtnsContainer = new GameObject("MenuButtons");
            menuBtnsContainer.transform.SetParent(cardObj.transform, false);
            RectTransform rtMb = menuBtnsContainer.AddComponent<RectTransform>();
            rtMb.anchorMin = new Vector2(0.5f, 0.5f);
            rtMb.anchorMax = new Vector2(0.5f, 0.5f);
            rtMb.pivot = new Vector2(0.5f, 0.5f);
            rtMb.anchoredPosition = new Vector2(0f, -40f);
            rtMb.sizeDelta = new Vector2(340f, 210f);

            VerticalLayoutGroup vlgMenu = menuBtnsContainer.AddComponent<VerticalLayoutGroup>();
            vlgMenu.spacing = 14f;
            vlgMenu.childForceExpandHeight = false;

            Button btnNewGame = CreateChoiceButton(menuBtnsContainer.transform, "Btn_NewGame", "OYUNA BAŞLA", glassPanelSprite ?? uiSprite);
            btnNewGame.GetComponent<Image>().color = new Color(0.18f, 0.75f, 0.45f, 0.95f);
            btnNewGame.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 54f);

            Button btnContinue = CreateChoiceButton(menuBtnsContainer.transform, "Btn_Continue", "DEVAM ET", glassPanelSprite ?? uiSprite);
            btnContinue.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 46f);

            Button btnAudioToggle = CreateChoiceButton(menuBtnsContainer.transform, "Btn_AudioToggle", "SES: AÇIK", glassPanelSprite ?? uiSprite);
            btnAudioToggle.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 46f);

            MainMenuController mmc = menuPanelObj.AddComponent<MainMenuController>();
            SerializedObject soMmc = new SerializedObject(mmc);
            soMmc.FindProperty("menuRootPanel").objectReferenceValue = menuPanelObj;
            soMmc.FindProperty("newGameButton").objectReferenceValue = btnNewGame;
            soMmc.FindProperty("continueButton").objectReferenceValue = btnContinue;
            soMmc.FindProperty("audioToggleButton").objectReferenceValue = btnAudioToggle;
            soMmc.FindProperty("audioButtonText").objectReferenceValue = btnAudioToggle.GetComponentInChildren<Text>();
            soMmc.ApplyModifiedProperties();

            // 15. Chapter 1 Summary & Free Roam UI Panel (Faz 4)
            GameObject summaryPanelObj = new GameObject("ChapterSummary_Panel");
            summaryPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtSummary = summaryPanelObj.AddComponent<RectTransform>();
            rtSummary.anchorMin = Vector2.zero;
            rtSummary.anchorMax = Vector2.one;
            rtSummary.sizeDelta = Vector2.zero;

            Image summaryBg = summaryPanelObj.AddComponent<Image>();
            summaryBg.color = new Color(0.03f, 0.04f, 0.06f, 0.88f); // Dark translucent backdrop
            summaryBg.raycastTarget = true;

            // Center Card
            GameObject summaryCard = new GameObject("SummaryCard");
            summaryCard.transform.SetParent(summaryPanelObj.transform, false);
            RectTransform rtSumCard = summaryCard.AddComponent<RectTransform>();
            rtSumCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtSumCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtSumCard.pivot = new Vector2(0.5f, 0.5f);
            rtSumCard.anchoredPosition = Vector2.zero;
            rtSumCard.sizeDelta = new Vector2(620f, 480f);

            Image sumCardBg = summaryCard.AddComponent<Image>();
            sumCardBg.color = new Color(0.07f, 0.09f, 0.13f, 0.98f);
            if (glassPanelSprite != null)
            {
                sumCardBg.sprite = glassPanelSprite;
                sumCardBg.type = Image.Type.Sliced;
            }

            Text sumTitle = CreateUIText(summaryCard.transform, "SummaryTitle", "BÖLÜM 1 TAMAMLANDI", 30, TextAnchor.MiddleCenter, new Color(0.95f, 0.75f, 0.2f));
            sumTitle.rectTransform.anchoredPosition = new Vector2(0f, 190f);
            sumTitle.rectTransform.sizeDelta = new Vector2(580f, 40f);

            Text sumSubtitle = CreateUIText(summaryCard.transform, "SummarySubtitle", "Şehirdeki İlk Hayatta Kalma Mücadelesi Sona Erdi", 15, TextAnchor.MiddleCenter, new Color(0.7f, 0.75f, 0.82f));
            sumSubtitle.rectTransform.anchoredPosition = new Vector2(0f, 155f);
            sumSubtitle.rectTransform.sizeDelta = new Vector2(580f, 25f);

            // Stat Box
            GameObject statBox = new GameObject("StatBox");
            statBox.transform.SetParent(summaryCard.transform, false);
            RectTransform rtStatBox = statBox.AddComponent<RectTransform>();
            rtStatBox.anchorMin = new Vector2(0.5f, 0.5f);
            rtStatBox.anchorMax = new Vector2(0.5f, 0.5f);
            rtStatBox.pivot = new Vector2(0.5f, 0.5f);
            rtStatBox.anchoredPosition = new Vector2(0f, 15f);
            rtStatBox.sizeDelta = new Vector2(520f, 220f);

            Text statTime = CreateUIText(statBox.transform, "Stat_Time", "Hayatta Kalınan Süre: Gün 1, 14:30", 17, TextAnchor.MiddleLeft, Color.white);
            statTime.rectTransform.anchoredPosition = new Vector2(20f, 75f);
            statTime.rectTransform.sizeDelta = new Vector2(480f, 28f);

            Text statZombies = CreateUIText(statBox.transform, "Stat_Zombies", "Etkisiz Hale Getirilen Zombiler: 0", 17, TextAnchor.MiddleLeft, new Color(0.95f, 0.35f, 0.35f));
            statZombies.rectTransform.anchoredPosition = new Vector2(20f, 35f);
            statZombies.rectTransform.sizeDelta = new Vector2(480f, 28f);

            Text statCivilians = CreateUIText(statBox.transform, "Stat_Civilians", "Kurtarılan Hayatta Kalanlar: 0", 17, TextAnchor.MiddleLeft, new Color(0.35f, 0.85f, 0.45f));
            statCivilians.rectTransform.anchoredPosition = new Vector2(20f, -5f);
            statCivilians.rectTransform.sizeDelta = new Vector2(480f, 28f);

            Text statDecisions = CreateUIText(statBox.transform, "Stat_Decisions", "Kritik Kararlar: Dr. Aras Tedavi Edildi, Güvenli Bölge Keşfedildi", 15, TextAnchor.MiddleLeft, new Color(0.85f, 0.85f, 0.6f));
            statDecisions.rectTransform.anchoredPosition = new Vector2(20f, -55f);
            statDecisions.rectTransform.sizeDelta = new Vector2(480f, 55f);

            Button btnContinueRoam = CreateChoiceButton(summaryCard.transform, "Btn_ContinueRoam", "ŞEHRİ KEŞFETMEYE DEVAM ET (SERBEST MOD)", glassPanelSprite ?? uiSprite);
            btnContinueRoam.GetComponent<Image>().color = new Color(0.2f, 0.65f, 0.85f, 0.95f);
            RectTransform rtBtnRoam = btnContinueRoam.GetComponent<RectTransform>();
            rtBtnRoam.anchoredPosition = new Vector2(0f, -170f);
            rtBtnRoam.sizeDelta = new Vector2(480f, 50f);

            ChapterSummaryUI sumUI = summaryPanelObj.AddComponent<ChapterSummaryUI>();
            SerializedObject soSumUI = new SerializedObject(sumUI);
            soSumUI.FindProperty("summaryPanelRoot").objectReferenceValue = summaryPanelObj;
            soSumUI.FindProperty("titleText").objectReferenceValue = sumTitle;
            soSumUI.FindProperty("subtitleText").objectReferenceValue = sumSubtitle;
            soSumUI.FindProperty("survivedTimeText").objectReferenceValue = statTime;
            soSumUI.FindProperty("zombiesKilledText").objectReferenceValue = statZombies;
            soSumUI.FindProperty("civiliansRescuedText").objectReferenceValue = statCivilians;
            soSumUI.FindProperty("decisionsMadeText").objectReferenceValue = statDecisions;
            soSumUI.FindProperty("continueFreeRoamButton").objectReferenceValue = btnContinueRoam;
            soSumUI.ApplyModifiedProperties();

            summaryPanelObj.SetActive(false);
        }

        private static Slider CreateSlimBar(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color fillColor, Sprite iconSprite, Sprite barBgSprite, out Text valueText)
        {
            GameObject barObj = new GameObject(name);
            barObj.transform.SetParent(parent, false);
            RectTransform rt = barObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            float leftOffset = 0f;
            if (iconSprite != null)
            {
                leftOffset = size.y + 6f;
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(barObj.transform, false);
                RectTransform rtIcon = iconObj.AddComponent<RectTransform>();
                rtIcon.anchorMin = new Vector2(0f, 0.5f);
                rtIcon.anchorMax = new Vector2(0f, 0.5f);
                rtIcon.pivot = new Vector2(0f, 0.5f);
                rtIcon.anchoredPosition = new Vector2(0f, 0f);
                rtIcon.sizeDelta = new Vector2(size.y + 2f, size.y + 2f);
                Image imgIcon = iconObj.AddComponent<Image>();
                imgIcon.sprite = iconSprite;
                imgIcon.color = Color.white;
                imgIcon.raycastTarget = false;
            }

            Slider slider = barObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(barObj.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(leftOffset, 0f);
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.14f, 0.18f, 0.9f);
            if (barBgSprite != null) bgImg.sprite = barBgSprite;
            bgImg.raycastTarget = false;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(barObj.transform, false);
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(leftOffset, 0f);
            fillAreaRt.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.AddComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            if (barBgSprite != null) fillImg.sprite = barBgSprite;
            fillImg.raycastTarget = false;

            slider.fillRect = fillRt;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(barObj.transform, false);
            RectTransform rtTxt = textObj.AddComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.offsetMin = new Vector2(leftOffset + 4f, 0f);
            rtTxt.offsetMax = new Vector2(-4f, 0f);
            Text txt = textObj.AddComponent<Text>();
            txt.text = "";
            txt.fontSize = 10;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            valueText = txt;

            return slider;
        }

        private static Slider CreateHiddenSlider(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.SetActive(false);
            Slider s = obj.AddComponent<Slider>();
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = 1f;
            return s;
        }

        private static GameObject CreateCriticalBadge(Transform parent, string name, Sprite iconSprite, string label, Color color)
        {
            GameObject badge = new GameObject(name);
            badge.transform.SetParent(parent, false);
            RectTransform rt = badge.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(62f, 20f);

            Image bg = badge.AddComponent<Image>();
            bg.color = new Color(color.r * 0.35f, color.g * 0.35f, color.b * 0.35f, 0.88f);
            bg.raycastTarget = false;

            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(badge.transform, false);
                RectTransform rtIcon = iconObj.AddComponent<RectTransform>();
                rtIcon.anchorMin = new Vector2(0f, 0.5f);
                rtIcon.anchorMax = new Vector2(0f, 0.5f);
                rtIcon.pivot = new Vector2(0f, 0.5f);
                rtIcon.anchoredPosition = new Vector2(3f, 0f);
                rtIcon.sizeDelta = new Vector2(14f, 14f);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.color = color;
                iconImg.raycastTarget = false;
            }

            Text txt = CreateUIText(badge.transform, "Label", label, 9, TextAnchor.MiddleRight, color);
            txt.fontStyle = FontStyle.Bold;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(18f, 0f);
            txt.rectTransform.offsetMax = new Vector2(-2f, 0f);

            return badge;
        }

        private static Slider CreateStatSlider(Transform parent, string name, string label, Color fillColor, Sprite iconSprite = null, Sprite barBgSprite = null)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);
            RectTransform rt = sliderObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(290f, 16f);

            float leftOffset = 0f;
            if (iconSprite != null)
            {
                leftOffset = 22f;
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(sliderObj.transform, false);
                RectTransform rtIcon = iconObj.AddComponent<RectTransform>();
                rtIcon.anchorMin = new Vector2(0f, 0.5f);
                rtIcon.anchorMax = new Vector2(0f, 0.5f);
                rtIcon.pivot = new Vector2(0f, 0.5f);
                rtIcon.anchoredPosition = new Vector2(0f, 0f);
                rtIcon.sizeDelta = new Vector2(16f, 16f);

                Image imgIcon = iconObj.AddComponent<Image>();
                imgIcon.sprite = iconSprite;
                imgIcon.color = Color.white;
                imgIcon.raycastTarget = false;
            }

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(leftOffset, 0f);
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
            if (barBgSprite != null) bgImg.sprite = barBgSprite;
            bgImg.raycastTarget = false;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(leftOffset, 0f);
            fillAreaRt.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.AddComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            if (barBgSprite != null) fillImg.sprite = barBgSprite;
            fillImg.raycastTarget = false;

            slider.fillRect = fillRt;

            Text txt = CreateUIText(sliderObj.transform, "Label", label, 11, TextAnchor.MiddleRight, Color.white);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(leftOffset + 4f, 0f);
            txt.rectTransform.offsetMax = new Vector2(-4f, 0f);

            return slider;
        }

        private static Button CreateActionButtonWithIconAndText(
            Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor,
            Sprite bgSprite, Sprite iconSprite, float iconSize, int fontSize,
            Vector2 anchorMin = default, Vector2 anchorMax = default)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            if (anchorMin == default && anchorMax == default)
            {
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
            }
            else
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
            }
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
                if (bgSprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            }
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();

            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(btnObj.transform, false);
                RectTransform rtIcon = iconObj.AddComponent<RectTransform>();
                rtIcon.anchorMin = new Vector2(0.5f, 0.5f);
                rtIcon.anchorMax = new Vector2(0.5f, 0.5f);
                rtIcon.pivot = new Vector2(0.5f, 0.5f);
                rtIcon.anchoredPosition = string.IsNullOrEmpty(label) ? Vector2.zero : new Vector2(0f, 8f);
                rtIcon.sizeDelta = new Vector2(iconSize, iconSize);

                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;
            }

            if (!string.IsNullOrEmpty(label))
            {
                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(btnObj.transform, false);
                RectTransform rtTxt = txtObj.AddComponent<RectTransform>();
                rtTxt.anchorMin = new Vector2(0.5f, 0.5f);
                rtTxt.anchorMax = new Vector2(0.5f, 0.5f);
                rtTxt.pivot = new Vector2(0.5f, 0.5f);
                rtTxt.anchoredPosition = iconSprite != null ? new Vector2(0f, -size.y * 0.28f) : Vector2.zero;
                rtTxt.sizeDelta = new Vector2(size.x, 24f);

                Text txt = txtObj.AddComponent<Text>();
                txt.text = label;
                txt.fontSize = fontSize;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.raycastTarget = false;
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;
            }

            return btn;
        }

        private static Slider CreateInteractiveSlider(
            Transform parent, string name, string labelText, float minVal, float maxVal, float initialVal,
            string formatSuffix, Sprite knobSprite, Sprite barBgSprite, out Text valueText)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(parent, false);
            RectTransform rtContainer = container.AddComponent<RectTransform>();
            rtContainer.sizeDelta = new Vector2(380f, 50f);

            // Label & Value Header Row
            GameObject headerObj = new GameObject("HeaderRow");
            headerObj.transform.SetParent(container.transform, false);
            RectTransform rtHeader = headerObj.AddComponent<RectTransform>();
            rtHeader.anchorMin = new Vector2(0f, 1f);
            rtHeader.anchorMax = new Vector2(1f, 1f);
            rtHeader.pivot = new Vector2(0.5f, 1f);
            rtHeader.anchoredPosition = Vector2.zero;
            rtHeader.sizeDelta = new Vector2(0f, 20f);

            Text label = CreateUIText(headerObj.transform, "Label", labelText, 13, TextAnchor.MiddleLeft, new Color(0.85f, 0.9f, 0.95f));
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(0.7f, 1f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            valueText = CreateUIText(headerObj.transform, "ValueText", string.Format("{0}{1}", initialVal, formatSuffix), 13, TextAnchor.MiddleRight, new Color(0.95f, 0.75f, 0.25f));
            valueText.rectTransform.anchorMin = new Vector2(0.7f, 0f);
            valueText.rectTransform.anchorMax = new Vector2(1f, 1f);
            valueText.rectTransform.offsetMin = Vector2.zero;
            valueText.rectTransform.offsetMax = Vector2.zero;

            // Slider Component
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(container.transform, false);
            RectTransform rtSlider = sliderObj.AddComponent<RectTransform>();
            rtSlider.anchorMin = new Vector2(0f, 0f);
            rtSlider.anchorMax = new Vector2(1f, 0f);
            rtSlider.pivot = new Vector2(0.5f, 0f);
            rtSlider.anchoredPosition = new Vector2(0f, 2f);
            rtSlider.sizeDelta = new Vector2(0f, 22f);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = minVal;
            slider.maxValue = maxVal;
            slider.value = initialVal;
            slider.direction = Slider.Direction.LeftToRight;

            // Background track
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.25f);
            bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
            if (barBgSprite != null) { bgImg.sprite = barBgSprite; bgImg.type = Image.Type.Sliced; }
            bgImg.raycastTarget = true;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRt.offsetMin = new Vector2(0f, 0f);
            fillAreaRt.offsetMax = new Vector2(-10f, 0f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.AddComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.65f, 0.95f, 0.95f);
            if (barBgSprite != null) { fillImg.sprite = barBgSprite; fillImg.type = Image.Type.Sliced; }
            fillImg.raycastTarget = false;

            slider.fillRect = fillRt;

            // Handle Slide Area & Handle
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRt = handle.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20f, 20f);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            if (knobSprite != null) handleImg.sprite = knobSprite;
            handleImg.raycastTarget = true;

            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;

            return slider;
        }

        private static Toggle CreateInteractiveToggle(
            Transform parent, string name, string labelText, bool initialVal, Sprite knobSprite, Sprite barBgSprite)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(parent, false);
            RectTransform rtContainer = container.AddComponent<RectTransform>();
            rtContainer.sizeDelta = new Vector2(380f, 36f);

            Toggle toggle = container.AddComponent<Toggle>();
            toggle.isOn = initialVal;

            // Checkbox background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(container.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(0f, 0.5f);
            bgRt.pivot = new Vector2(0f, 0.5f);
            bgRt.anchoredPosition = new Vector2(0f, 0f);
            bgRt.sizeDelta = new Vector2(26f, 26f);

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
            if (barBgSprite != null) { bgImg.sprite = barBgSprite; bgImg.type = Image.Type.Sliced; }
            bgImg.raycastTarget = true;

            // Checkmark
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(bgObj.transform, false);
            RectTransform checkRt = checkObj.AddComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.5f, 0.5f);
            checkRt.anchorMax = new Vector2(0.5f, 0.5f);
            checkRt.pivot = new Vector2(0.5f, 0.5f);
            checkRt.anchoredPosition = Vector2.zero;
            checkRt.sizeDelta = new Vector2(16f, 16f);

            Image checkImg = checkObj.AddComponent<Image>();
            checkImg.color = new Color(0.2f, 0.85f, 0.45f, 1f);
            if (knobSprite != null) checkImg.sprite = knobSprite;
            checkImg.raycastTarget = false;

            toggle.graphic = checkImg;
            toggle.targetGraphic = bgImg;

            // Label
            Text label = CreateUIText(container.transform, "Label", labelText, 14, TextAnchor.MiddleLeft, new Color(0.85f, 0.9f, 0.95f));
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.offsetMin = new Vector2(36f, 0f);
            label.rectTransform.offsetMax = Vector2.zero;

            return toggle;
        }

        private static void CreateQualitySelectorUI(
            Transform parent, Sprite glassSprite, Sprite uiSprite,
            out Button btnLow, out Button btnMed, out Button btnHigh, out Text statusText)
        {
            GameObject container = new GameObject("QualitySelector_Row");
            container.transform.SetParent(parent, false);
            RectTransform rt = container.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380f, 60f);

            Text label = CreateUIText(container.transform, "Label", "Grafik Kalitesi", 13, TextAnchor.MiddleLeft, new Color(0.85f, 0.9f, 0.95f));
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            label.rectTransform.pivot = new Vector2(0f, 1f);
            label.rectTransform.anchoredPosition = Vector2.zero;
            label.rectTransform.sizeDelta = new Vector2(0f, 20f);

            statusText = CreateUIText(container.transform, "StatusText", "Orta (Dengeli)", 12, TextAnchor.MiddleRight, new Color(0.4f, 0.8f, 1f));
            statusText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            statusText.rectTransform.anchorMax = new Vector2(1f, 1f);
            statusText.rectTransform.pivot = new Vector2(1f, 1f);
            statusText.rectTransform.anchoredPosition = Vector2.zero;
            statusText.rectTransform.sizeDelta = new Vector2(0f, 20f);

            GameObject btnsRow = new GameObject("ButtonsRow");
            btnsRow.transform.SetParent(container.transform, false);
            RectTransform rtRow = btnsRow.AddComponent<RectTransform>();
            rtRow.anchorMin = new Vector2(0f, 0f);
            rtRow.anchorMax = new Vector2(1f, 0f);
            rtRow.pivot = new Vector2(0.5f, 0f);
            rtRow.anchoredPosition = Vector2.zero;
            rtRow.sizeDelta = new Vector2(0f, 34f);

            HorizontalLayoutGroup hlg = btnsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            btnLow = CreateChoiceButton(btnsRow.transform, "Btn_Quality_Low", "Düşük", glassSprite ?? uiSprite);
            btnMed = CreateChoiceButton(btnsRow.transform, "Btn_Quality_Med", "Orta", glassSprite ?? uiSprite);
            btnHigh = CreateChoiceButton(btnsRow.transform, "Btn_Quality_High", "Yüksek", glassSprite ?? uiSprite);
        }

        private static Button CreateCustomButton(
            Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor,
            Sprite sprite, Sprite iconSprite = null, int fontSize = 18,
            Vector2 anchorMin = default, Vector2 anchorMax = default, Vector2 pivot = default)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            if (anchorMin == default && anchorMax == default)
            {
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
            }
            else
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
            }
            rt.pivot = pivot == default ? new Vector2(0.5f, 0.5f) : pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            if (sprite != null)
            {
                img.sprite = sprite;
                if (sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            }
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();

            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(btnObj.transform, false);
                RectTransform rtIcon = iconObj.AddComponent<RectTransform>();
                rtIcon.anchorMin = new Vector2(0.5f, 0.5f);
                rtIcon.anchorMax = new Vector2(0.5f, 0.5f);
                rtIcon.pivot = new Vector2(0.5f, 0.5f);
                rtIcon.anchoredPosition = Vector2.zero;
                rtIcon.sizeDelta = new Vector2(size.x * 0.6f, size.y * 0.6f);

                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;
            }

            if (!string.IsNullOrEmpty(label))
            {
                Text txt = CreateUIText(btnObj.transform, "Text", label, fontSize, TextAnchor.MiddleCenter, Color.white);
                txt.rectTransform.anchorMin = Vector2.zero;
                txt.rectTransform.anchorMax = Vector2.one;
                txt.rectTransform.sizeDelta = Vector2.zero;
            }

            return btn;
        }

        private static Button CreateQuickSlotButton(Transform parent, string name, Sprite bgSprite, Sprite defaultIcon = null, string defaultQty = "")
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(70f, 70f);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.22f, 0.85f);
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
                img.type = Image.Type.Sliced;
            }
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();

            // Icon (Centered)
            GameObject iconObj = new GameObject("ItemIcon");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtIcon = iconObj.AddComponent<RectTransform>();
            rtIcon.anchorMin = new Vector2(0.5f, 0.5f);
            rtIcon.anchorMax = new Vector2(0.5f, 0.5f);
            rtIcon.pivot = new Vector2(0.5f, 0.5f);
            rtIcon.sizeDelta = new Vector2(46f, 46f);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;
            if (defaultIcon != null)
            {
                iconImg.sprite = defaultIcon;
                iconImg.enabled = true;
            }

            // Quantity Badge (Bottom-Right)
            GameObject countObj = new GameObject("CountBadge");
            countObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtCount = countObj.AddComponent<RectTransform>();
            rtCount.anchorMin = new Vector2(1f, 0f);
            rtCount.anchorMax = new Vector2(1f, 0f);
            rtCount.pivot = new Vector2(1f, 0f);
            rtCount.anchoredPosition = new Vector2(-4f, 4f);
            rtCount.sizeDelta = new Vector2(30f, 18f);
            Text countTxt = countObj.AddComponent<Text>();
            countTxt.text = defaultQty;
            countTxt.fontSize = 12;
            countTxt.fontStyle = FontStyle.Bold;
            countTxt.alignment = TextAnchor.LowerRight;
            countTxt.color = new Color(1f, 0.9f, 0.4f);
            countTxt.raycastTarget = false;
            countTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;

            return btn;
        }

        private static Button CreateChoiceButton(Transform parent, string name, string label, Sprite sprite)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(700f, 42f);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.35f, 0.5f, 0.9f);
            if (sprite != null)
            {
                img.sprite = sprite;
                if (sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            }
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            CreateUIText(btnObj.transform, "ChoiceText", label, 16, TextAnchor.MiddleCenter, Color.white);
            return btn;
        }

        private static Text CreateUIText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent, false);
            RectTransform rt = txtObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 40f);

            Text txt = txtObj.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            txt.raycastTarget = false;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ;

            return txt;
        }
    }
}
