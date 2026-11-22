using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectSurvival.Animation;
using ProjectSurvival.AI;
using ProjectSurvival.Player;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.EditorTools
{
    public static class HumanoidRigAutoBinder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        [MenuItem("Project Survival/Animation/Auto-Bind Humanoid Rig on Player & All NPCs", false, 10)]
        public static void RunAutoBindFromMenu()
        {
            ExecuteAutoBind();
        }

        public static void RunAutoBindCLI()
        {
            ExecuteAutoBind();
            EditorApplication.Exit(0);
        }

        public static void ExecuteAutoBind()
        {
            Debug.Log("================================================================================");
            Debug.Log(">>> EXECUTING HUMANOID RIG AUTO-BINDER ON PLAYER & ALL NPCS <<<");
            Debug.Log("================================================================================");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            int boundCount = 0;

            // 1. Process Player_Root in Scene
            var players = UnityEngine.Object.FindObjectsByType<TPSPlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p == null) continue;
                BindAndAlignHumanoid(p.gameObject);
                boundCount++;
                Debug.Log($"[HumanoidRigAutoBinder] Processed Player `{p.gameObject.name}`.");
            }

            // 2. Process all NPCs in Scene
            var npcs = UnityEngine.Object.FindObjectsByType<SurvivorNPC_AI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (npc == null) continue;
                BindAndAlignHumanoid(npc.gameObject);
                boundCount++;
                Debug.Log($"[HumanoidRigAutoBinder] Processed NPC `{npc.gameObject.name}`.");
            }

            // Also check for any InteractableNPC not using SurvivorNPC_AI
            var interactableNpcs = UnityEngine.Object.FindObjectsByType<InteractableNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var inpc in interactableNpcs)
            {
                if (inpc == null) continue;
                if (inpc.GetComponent<HumanoidProceduralAnimator>() == null)
                {
                    BindAndAlignHumanoid(inpc.gameObject);
                    boundCount++;
                    Debug.Log($"[HumanoidRigAutoBinder] Processed InteractableNPC `{inpc.gameObject.name}`.");
                }
            }

            // 3. Process Prefabs properly using PrefabUtility.LoadPrefabContents
            string[] prefabPaths = new string[]
            {
                "Assets/_Project/Prefabs/Player/Player_Root.prefab",
                "Assets/_Project/Prefabs/NPCs/Survivor_Scavenger_Root.prefab",
                "Assets/_Project/Prefabs/NPCs/NPC_DrAras_Root.prefab",
                "Assets/_Project/Prefabs/NPCs/NPC_Kerem_Root.prefab",
                "Assets/_Project/Prefabs/NPCs/NPC_Zeynep_Root.prefab",
                "Assets/_Project/Prefabs/NPCs/NPC_Elif_Root.prefab"
            };

            foreach (var path in prefabPaths)
            {
                try
                {
                    var contents = PrefabUtility.LoadPrefabContents(path);
                    if (contents != null)
                    {
                        var anim = contents.GetComponent<HumanoidProceduralAnimator>();
                        if (anim == null) anim = contents.AddComponent<HumanoidProceduralAnimator>();
                        anim.AutoBindLimbs();
                        anim.CacheRestTransforms();

                        var brother = contents.transform.Find("Brother_Visual");
                        if (brother != null)
                        {
                            Vector3 blp = brother.localPosition;
                            blp.y = 0.325f;
                            brother.localPosition = blp;
                        }

                        var animators = contents.GetComponentsInChildren<Animator>(true);
                        foreach (var a in animators)
                        {
                            if (a != null) a.applyRootMotion = false;
                        }

                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                        PrefabUtility.UnloadPrefabContents(contents);
                        Debug.Log($"[HumanoidRigAutoBinder] Saved updated Prefab `{path}`.");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[HumanoidRigAutoBinder] Error updating prefab `{path}`: {ex.Message}");
                }
            }

            // Save Scene & Assets
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[HumanoidRigAutoBinder] Successfully processed {boundCount} humanoids in scene. Scene saved: {saved}.");
            Debug.Log("================================================================================");
        }

        private static void BindAndAlignHumanoid(GameObject rootGo)
        {
            var animator = rootGo.GetComponent<HumanoidProceduralAnimator>();
            if (animator == null)
            {
                animator = rootGo.AddComponent<HumanoidProceduralAnimator>();
            }

            // Discover and bind
            animator.AutoBindLimbs();

            // Find visualModel
            Transform vm = animator.VisualModel;
            if (vm == null)
            {
                vm = rootGo.transform.Find("Visual_Model") ?? rootGo.transform.Find("Visual") ?? (rootGo.transform.childCount > 0 ? rootGo.transform.GetChild(0) : null);
                if (vm != null) animator.SetVisualModel(vm);
            }

            // Ensure applyRootMotion is false on any Animator component
            var animators = rootGo.GetComponentsInChildren<Animator>(true);
            foreach (var a in animators)
            {
                if (a != null)
                {
                    a.applyRootMotion = false;
                    EditorUtility.SetDirty(a);
                }
            }

            // Ground alignment check
            if (vm != null)
            {
                // Align Brother_Visual if present (e.g. Zeynep's wounded brother on stretcher)
                Transform brother = rootGo.transform.Find("Brother_Visual");
                if (brother != null)
                {
                    var brotherRends = brother.GetComponentsInChildren<Renderer>();
                    if (brotherRends.Length > 0)
                    {
                        Bounds bb = brotherRends[0].bounds;
                        for (int i = 1; i < brotherRends.Length; i++) bb.Encapsulate(brotherRends[i].bounds);
                        float bOffset = bb.min.y - rootGo.transform.position.y;
                        if (bOffset < 0f)
                        {
                            Vector3 blp = brother.localPosition;
                            blp.y -= bOffset;
                            brother.localPosition = blp;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(brother);
                            EditorUtility.SetDirty(brother);
                            Debug.Log($"[BindAndAlignHumanoid] Aligned Brother_Visual on `{rootGo.name}`: new lp={brother.localPosition}");
                        }
                    }
                }

                // Align Visual_Model based on its own renderers
                var vmRends = vm.GetComponentsInChildren<Renderer>();
                if (vmRends.Length > 0)
                {
                    Bounds vmb = vmRends[0].bounds;
                    for (int i = 1; i < vmRends.Length; i++) vmb.Encapsulate(vmRends[i].bounds);
                    float vmOffset = vmb.min.y - rootGo.transform.position.y;

                    Debug.Log($"[BindAndAlignHumanoid] `{rootGo.name}` vmOffset={vmOffset:F3}m, vm.lp={vm.localPosition}");

                    if (Mathf.Abs(vmOffset) > 0.005f)
                    {
                        Vector3 lp = vm.localPosition;
                        lp.y -= vmOffset;
                        vm.localPosition = lp;

                        PrefabUtility.RecordPrefabInstancePropertyModifications(vm);
                        EditorUtility.SetDirty(vm);
                        EditorUtility.SetDirty(rootGo);
                        Debug.Log($"[BindAndAlignHumanoid] `{rootGo.name}` aligned Visual_Model: new lp={vm.localPosition}");
                    }
                }

                // Verify overall bounds
                var rends = rootGo.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    float finalOffset = b.min.y - rootGo.transform.position.y;
                    Debug.Log($"[BindAndAlignHumanoid] `{rootGo.name}` FINAL offset={finalOffset:F3}m");
                }
            }

            animator.CacheRestTransforms();

            // Serialize into SerializedObject
            SerializedObject so = new SerializedObject(animator);
            so.Update();

            var vmProp = so.FindProperty("visualModel");
            if (vmProp != null && vm != null)
            {
                vmProp.objectReferenceValue = vm;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(rootGo);
        }
    }
}
