using System.IO;
using UnityEngine;
using UnityEditor;

namespace ProjectSurvival.EditorTools
{
    public static class ZombieModelImporter
    {
        [MenuItem("ProjectSurvival/Inspect Zombie Animation Clips", false, 10)]
        public static void InspectClips()
        {
            string[] paths = new string[]
            {
                "Assets/_Project/Models/Zombies/StandardZombie/Zombie1.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Idle.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Walk.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Walk_InPlace.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Run.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Run_InPlace.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Attack.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_FallingBack.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_FallingForward.FBX",
                "Assets/_Project/Models/Zombies/RunnerZombie/zombie-2.glb",
                "Assets/_Project/Models/Zombies/TankZombie/meaty_zombie.glb"
            };

            foreach (var p in paths)
            {
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(p);
                if (subAssets == null) continue;
                foreach (var sa in subAssets)
                {
                    if (sa is AnimationClip clip)
                    {
                        Debug.Log($"CLIP FOUND in {Path.GetFileName(p)}: '{clip.name}', length: {clip.length}s, isLooping: {clip.isLooping}");
                    }
                    else if (sa is Mesh mesh)
                    {
                        Debug.Log($"MESH FOUND in {Path.GetFileName(p)}: '{mesh.name}', vertices: {mesh.vertexCount}");
                    }
                    else if (sa is Material mat)
                    {
                        Debug.Log($"MATERIAL FOUND in {Path.GetFileName(p)}: '{mat.name}'");
                    }
                }
            }
        }
    }
}
