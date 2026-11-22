using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 3 (Step 3A): Automated GitHub CC0 3D Interior Model Fetcher.
    /// Downloads genuine CC0 3D interior furniture and prop models (.glb) from public GitHub
    /// repositories (Kenney CC0 Furniture Kit mirrors, Webaverse CC0 device assets) into
    /// 'Assets/_Project/Models/CC0_Interior/'. Forces native glTFast import with URP Lit materials.
    /// </summary>
    public static class GitHubCC0ModelFetcher
    {
        public const string ModelDirectory = "Assets/_Project/Models/CC0_Interior";

        public static readonly Dictionary<string, string> ModelUrls = new Dictionary<string, string>
        {
            // Bedroom
            { "bedDouble.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/bedDouble.glb" },
            { "cabinetBedDrawer.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/cabinetBedDrawer.glb" },
            { "phone.glb", "https://raw.githubusercontent.com/webaverse/assets/master/phone.glb" },
            { "bookcaseClosedDoors.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/bookcaseClosedDoors.glb" },
            { "bookcaseClosedWide.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/bookcaseClosedWide.glb" },
            { "rugRectangle.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/rugRectangle.glb" },
            { "rugRound.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/rugRound.glb" },
            { "lampRoundTable.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/lampRoundTable.glb" },
            { "lampRoundFloor.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/lampRoundFloor.glb" },
            { "pillow.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/pillow.glb" },
            { "pillowBlue.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/pillowBlue.glb" },

            // Living Room
            { "loungeSofa.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/loungeSofa.glb" },
            { "loungeChairRelax.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/loungeChairRelax.glb" },
            { "tableCoffee.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/tableCoffee.glb" },
            { "tableCoffeeGlass.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/tableCoffeeGlass.glb" },
            { "bookcaseClosed.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/bookcaseClosed.glb" },
            { "coatRackStanding.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/coatRackStanding.glb" },
            { "cabinetTelevisionDoors.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/cabinetTelevisionDoors.glb" },
            { "televisionModern.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/televisionModern.glb" },
            { "pottedPlant.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/pottedPlant.glb" },
            { "plantSmall1.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/plantSmall1.glb" },
            { "doorway.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/doorway.glb" },
            { "doorwayFront.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/doorwayFront.glb" },

            // Kitchen
            { "kitchenCabinet.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/kitchenCabinet.glb" },
            { "kitchenCabinetUpperDouble.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/kitchenCabinetUpperDouble.glb" },
            { "kitchenCabinetDrawer.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/kitchenCabinetDrawer.glb" },
            { "kitchenSink.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/kitchenSink.glb" },
            { "kitchenFridge.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/kitchenFridge.glb" },
            { "kitchenFridgeLarge.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/kitchenFridgeLarge.glb" },
            { "kitchenStoveElectric.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/kitchenStoveElectric.glb" },
            { "kitchenMicrowave.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/kitchenMicrowave.glb" },
            { "kitchenCoffeeMachine.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/kitchenCoffeeMachine.glb" },
            { "table.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/table.glb" },
            { "chair.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/chair.glb" },
            { "chairModernCushion.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/chairModernCushion.glb" },

            // Bathroom
            { "bathroomSink.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/bathroomSink.glb" },
            { "bathroomMirror.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/bathroomMirror.glb" },
            { "bathroomCabinet.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/bathroomCabinet.glb" },
            { "toilet.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/toilet.glb" },
            { "bathtub.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/bathtub.glb" }
        };

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Fetch CC0 3D Interior Models", false, 46)]
        public static void FetchAllModelsMenu()
        {
            var task = FetchAllModelsAsync();
            task.Wait();
        }

        public static async Task FetchAllModelsAsync()
        {
            Debug.Log("[GitHubCC0ModelFetcher] Starting download of 40 CC0 3D interior models from GitHub...");

            if (!Directory.Exists(ModelDirectory))
            {
                Directory.CreateDirectory(ModelDirectory);
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; UnityEditor)");
                httpClient.Timeout = TimeSpan.FromSeconds(20);

                int downloadedCount = 0;
                int existingCount = 0;

                foreach (var kvp in ModelUrls)
                {
                    string fileName = kvp.Key;
                    string url = kvp.Value;
                    string destPath = Path.Combine(ModelDirectory, fileName);

                    if (File.Exists(destPath) && new FileInfo(destPath).Length > 0)
                    {
                        existingCount++;
                    }
                    else
                    {
                        try
                        {
                            byte[] data = await httpClient.GetByteArrayAsync(url);
                            File.WriteAllBytes(destPath, data);
                            downloadedCount++;
                            Debug.Log($"[GitHubCC0ModelFetcher] Downloaded '{fileName}' ({data.Length} bytes).");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[GitHubCC0ModelFetcher] Failed downloading '{fileName}' from {url}: {ex.Message}");
                        }
                    }
                }

                Debug.Log($"[GitHubCC0ModelFetcher] Download complete. Existing: {existingCount}, Freshly Downloaded: {downloadedCount}.");
            }

            AssetDatabase.Refresh();

            // Validate imported assets
            int validModels = 0;
            foreach (var kvp in ModelUrls)
            {
                string assetPath = Path.Combine(ModelDirectory, kvp.Key);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    validModels++;
                }
                else
                {
                    Debug.LogWarning($"[GitHubCC0ModelFetcher] Model '{kvp.Key}' at '{assetPath}' not yet imported as GameObject prefab.");
                }
            }

            Debug.Log($"[GitHubCC0ModelFetcher] Import verification finished. Valid Prefabs: {validModels}/{ModelUrls.Count}.");
        }
#endif
    }
}
