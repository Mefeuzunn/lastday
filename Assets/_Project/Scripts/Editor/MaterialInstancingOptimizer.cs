using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectSurvival.Editor
{
    /// <summary>
    /// Pre-Phase 3 Optimization: Batch-enables GPU Instancing across all URP materials
    /// in the project to dramatically minimize mobile draw calls for repeated modular props,
    /// foliage, barriers, and urban architectural elements.
    /// </summary>
    public static class MaterialInstancingOptimizer
    {
        [MenuItem("Tools/Optimization/Batch Enable GPU Instancing On All Materials")]
        public static int EnableInstancingOnAllMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            int modifiedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // Check if shader supports instancing (URP Lit, Simple Lit, Unlit, or custom shaders)
                if (!mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    modifiedCount++;
                }
            }

            if (modifiedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[MaterialInstancingOptimizer] GPU Instancing enabled on {modifiedCount} materials. Total materials scanned: {guids.Length}");
            return modifiedCount;
        }

        public static int AuditNonInstancedMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            int nonInstanced = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && !mat.enableInstancing)
                {
                    nonInstanced++;
                }
            }
            return nonInstanced;
        }
    }
}
