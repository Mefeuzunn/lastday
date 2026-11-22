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
    /// Phase 4 (Step 4A): Automated GitHub CC0 3D Exterior Architecture, Tree & Street Prop Fetcher.
    /// Downloads genuine CC0 3D models (.glb) for exterior building modules, roofs, trees, foliage,
    /// streetlights, fences, concrete road barriers, dumpsters, benches, and urban props into Assets/_Project/Models/CC0_Exterior/.
    /// </summary>
    public static class GitHubCC0ExteriorFetcher
    {
        public const string ModelDirectory = "Assets/_Project/Models/CC0_Exterior";

        public static readonly Dictionary<string, string> ExteriorModelUrls = new Dictionary<string, string>
        {
            // Commercial & City Buildings
            { "building-a.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-commercial/building-a.glb" },
            { "building-b.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-commercial/building-b.glb" },
            { "building-c.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-commercial/building-c.glb" },
            { "building-d.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-commercial/building-d.glb" },
            { "building-skyscraper-a.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-commercial/building-skyscraper-a.glb" },

            // Suburban Residential Buildings
            { "building-suburban-a.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-suburban/building-type-a.glb" },
            { "building-suburban-b.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-suburban/building-type-b.glb" },
            { "building-suburban-c.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-suburban/building-type-c.glb" },
            { "building-suburban-d.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-suburban/building-type-d.glb" },
            { "building-garage.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/retro-urban/wall-a-garage.glb" },
            { "door-exterior.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/retro-urban/door-type-a.glb" },

            // Roofs, Columns & Awnings
            { "roof-flat.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/building/roof-flat-patch-large.glb" },
            { "roof-center.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/building/roof-flat-center.glb" },
            { "wall-column.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/building/wall-corner-column.glb" },
            { "porch-awning.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-commercial/detail-awning-wide.glb" },
            { "awning.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-commercial/detail-awning.glb" },

            // Trees & Foliage
            { "tree_detailed.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/nature/tree_detailed.glb" },
            { "tree_pine.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/nature/tree_pineDefaultA.glb" },
            { "tree_pine_round.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/nature/tree_pineRoundA.glb" },
            { "tree_suburban.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-suburban/tree-large.glb" },
            { "tree_small.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-suburban/tree-small.glb" },
            { "bush.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/nature/plant_bushDetailed.glb" },
            { "bush_triangle.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/nature/plant_bushTriangle.glb" },
            { "rock.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/nature/rock_largeA.glb" },
            { "rock_small.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/nature/rock_smallA.glb" },

            // Street & Urban Props
            { "streetlight.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-roads/light-curved.glb" },
            { "streetlight_double.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-roads/light-curved-double.glb" },
            { "fence.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-suburban/fence-1x4.glb" },
            { "fence_short.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-suburban/fence-1x2.glb" },
            { "barrier.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-roads/construction-barrier.glb" },
            { "barrier_concrete.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/retro-urban/detail-barrier-strong-type-a.glb" },
            { "dumpster.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/retro-urban/detail-dumpster-closed.glb" },
            { "dumpster_open.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/retro-urban/detail-dumpster-open.glb" },
            { "bench.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/retro-urban/detail-bench.glb" },
            { "cone.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/city-roads/construction-cone.glb" },
            { "trashcan.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/coaster/trash.glb" },
            { "container.glb", "https://raw.githubusercontent.com/shorepine/kenney/main/3d/watercraft/cargo-container-a.glb" }
        };

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Fetch CC0 3D Exterior Models", false, 49)]
        public static void FetchAllModelsMenu()
        {
            var task = FetchAllModelsAsync();
            task.Wait();
        }

        public static async Task FetchAllModelsAsync()
        {
            Debug.Log("[GitHubCC0ExteriorFetcher] Starting download of CC0 3D exterior architecture, tree and street models...");

            if (!Directory.Exists(ModelDirectory))
            {
                Directory.CreateDirectory(ModelDirectory);
            }

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "Unity-ProjectSurvival-CC0-Exterior-Fetcher");

                int downloaded = 0;
                int skipped = 0;

                foreach (var kvp in ExteriorModelUrls)
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
                            Debug.Log($"[GitHubCC0ExteriorFetcher] Successfully downloaded {filename} ({bytes.Length:N0} bytes)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[GitHubCC0ExteriorFetcher] Failed to download {filename} from {url}: {ex.Message}");
                    }
                }

                Debug.Log($"[GitHubCC0ExteriorFetcher] Exterior fetch finished: {downloaded} downloaded, {skipped} existing.");
                AssetDatabase.Refresh();
            }
        }
#endif
    }
}
