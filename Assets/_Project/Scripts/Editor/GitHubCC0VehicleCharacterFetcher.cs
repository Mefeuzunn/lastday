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
    /// Phase 4 (Step 4B): Automated GitHub CC0 3D Vehicle & Character Model Fetcher.
    /// Downloads genuine CC0 3D models (.glb) for vehicles (SUV, Police Interceptor, Pickup Truck, Jeep, Cargo Van, Wrecked Cars)
    /// and characters (Survivor Humanoids, Tactical Raiders, Infected Zombies) into Assets/_Project/Models/CC0_Vehicles_Characters/.
    /// </summary>
    public static class GitHubCC0VehicleCharacterFetcher
    {
        public const string ModelDirectory = "Assets/_Project/Models/CC0_Vehicles_Characters";

        public static readonly Dictionary<string, string> VehicleCharacterUrls = new Dictionary<string, string>
        {
            // Vehicles
            { "suv.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/car/suv.glb" },
            { "police.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/car/police.glb" },
            { "truck.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/car/truck.glb" },
            { "jeep.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/car/suv-luxury.glb" },
            { "van.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/car/van.glb" },
            { "sedan_wreck.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/car/sedan.glb" },
            { "wheel.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/car/wheel-default.glb" },
            { "wheel_dark.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/car/wheel-dark.glb" },
            { "wheel_truck.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/car/wheel-truck.glb" },

            // Characters
            { "survivor_human.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/mini-characters/character-male-a.glb" },
            { "survivor_male_b.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/mini-characters/character-male-b.glb" },
            { "survivor_female.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/mini-characters/character-female-a.glb" },
            { "survivor_female_b.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/mini-characters/character-female-b.glb" },
            { "raider_human.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/mini-arena/character-soldier.glb" },
            { "raider_keeper.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/graveyard/character-keeper.glb" },
            { "zombie_character.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/graveyard/character-zombie.glb" }
        };

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Fetch CC0 3D Vehicles and Characters", false, 50)]
        public static void FetchAllModelsMenu()
        {
            var task = FetchAllModelsAsync();
            task.Wait();
        }

        public static async Task FetchAllModelsAsync()
        {
            Debug.Log("[GitHubCC0VehicleCharacterFetcher] Starting download of CC0 3D vehicles and characters...");

            if (!Directory.Exists(ModelDirectory))
            {
                Directory.CreateDirectory(ModelDirectory);
            }

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "Unity-ProjectSurvival-CC0-VehicleCharacter-Fetcher");

                int downloaded = 0;
                int skipped = 0;

                foreach (var kvp in VehicleCharacterUrls)
                {
                    string filename = kvp.Key;
                    string url = kvp.Value;
                    string localPath = Path.Combine(ModelDirectory, filename);

                    if (File.Exists(localPath) && new FileInfo(localPath).Length > 500)
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        byte[] bytes = await client.GetByteArrayAsync(url);
                        if (bytes != null && bytes.Length > 0)
                        {
                            await File.WriteAllBytesAsync(localPath, bytes);
                            downloaded++;
                            Debug.Log($"[GitHubCC0VehicleCharacterFetcher] Downloaded {filename} ({bytes.Length:N0} bytes)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[GitHubCC0VehicleCharacterFetcher] Failed to download {filename} from {url}: {ex.Message}");
                    }
                }

                Debug.Log($"[GitHubCC0VehicleCharacterFetcher] Download finished: {downloaded} downloaded, {skipped} existing.");
                AssetDatabase.Refresh();
            }
        }
#endif
    }
}
