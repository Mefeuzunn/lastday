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
    /// Phase 3 (Step 3B): Automated GitHub CC0 3D Weapon, Item & Prop Model Fetcher.
    /// Downloads real CC0 3D models (.glb) for weapons (Fire Axe, 9mm Pistol, Pump Shotgun, Crossbow),
    /// consumables (canned food, water bottle, ammo boxes, medkits), and yard crates/barricades.
    /// </summary>
    public static class GitHubCC0WeaponItemFetcher
    {
        public const string ModelDirectory = "Assets/_Project/Models/CC0_Weapons_Items";

        public static readonly Dictionary<string, string> WeaponItemUrls = new Dictionary<string, string>
        {
            // Weapons
            { "axe.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/survival/tool-axe.glb" },
            { "axeUpgraded.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/survival/tool-axe-upgraded.glb" },
            { "pistol.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/weapon/pistol.glb" },
            { "shotgun.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/weapon/shotgun.glb" },
            { "crossbow.glb", "https://raw.githubusercontent.com/webaverse/assets/master/bow.glb" },
            { "knife.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/weapon/knife_sharp.glb" },

            // Ammo & Supplies
            { "ammoBox.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/weapon/ammo_machinegun.glb" },
            { "ammoShotgun.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/weapon/ammo_shotgun.glb" },
            { "can.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/food/can.glb" },
            { "bottle.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/survival/bottle.glb" },
            { "medkit.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/furniture/bathroomCabinet.glb" },

            // Crate & Barricade Props
            { "crate.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/pirate/crate.glb" },
            { "crateWood.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/survival/box-large.glb" },
            { "barricade.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/survival/fence-fortified.glb" },
            { "barrel.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/survival/barrel.glb" }
        };

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Fetch CC0 3D Weapons and Items", false, 48)]
        public static void FetchAllModelsMenu()
        {
            var task = FetchAllModelsAsync();
            task.Wait();
        }

        public static async Task FetchAllModelsAsync()
        {
            Debug.Log("[GitHubCC0WeaponItemFetcher] Starting download of CC0 3D weapon, item and prop models...");

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

                foreach (var kvp in WeaponItemUrls)
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
                            Debug.Log($"[GitHubCC0WeaponItemFetcher] Downloaded '{fileName}' ({data.Length} bytes).");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[GitHubCC0WeaponItemFetcher] Failed downloading '{fileName}' from {url}: {ex.Message}");
                        }
                    }
                }

                Debug.Log($"[GitHubCC0WeaponItemFetcher] Download complete. Existing: {existingCount}, Freshly Downloaded: {downloadedCount}.");
            }

            AssetDatabase.Refresh();
        }
#endif
    }
}
