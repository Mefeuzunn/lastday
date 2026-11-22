using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Player;
using ProjectSurvival.AI;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.EditorTools
{
    public static class StudioAssetFitter
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player_Root.prefab";
        private const string NPCDrArasPrefabPath = "Assets/_Project/Prefabs/NPCs/NPC_DrAras_Root.prefab";
        private const string NPCZeynepPrefabPath = "Assets/_Project/Prefabs/NPCs/NPC_Zeynep_Root.prefab";
        private const string NPCKeremPrefabPath = "Assets/_Project/Prefabs/NPCs/NPC_Kerem_Root.prefab";
        private const string NPCElifPrefabPath = "Assets/_Project/Prefabs/NPCs/NPC_Elif_Root.prefab";

        private const string ZombieStandardPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Standard_Root.prefab";
        private const string ZombieRunnerPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Runner_Root.prefab";
        private const string ZombieScreamerPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Screamer_Root.prefab";
        private const string ZombieTankPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Tank_Root.prefab";
        private const string LootPrefabPath = "Assets/_Project/Prefabs/Environment/LootContainer_Root.prefab";

        private const string PlayerAnimatorPath = "Assets/_Project/Animations/Player_TPS_Animator.controller";
        private const string ZombieAnimatorPath = "Assets/_Project/Animations/Zombie_Animator.controller";

        [MenuItem("Project Survival/Apply 3D Studio Assets")]
        public static void ApplyAll()
        {
            Debug.Log("[StudioAssetFitter] Starting 3D Studio Asset Fitting Pipeline...");

            EnsureDirectories();

            // 1. Convert Materials to URP Lit
            int convertedMats = ConvertImportedMaterialsToURP();
            Debug.Log($"[StudioAssetFitter] Material Conversion Complete! Converted {convertedMats} materials to URP Lit.");

            // 2. Setup Player and NPC Models & Animations
            bool playerSuccess = SetupPlayerAndNPCModels();

            // 3. Setup Zombie Models & Animations
            bool zombieSuccess = SetupZombieModels();

            // 4. Setup POLYGON Environment Props & NavMesh
            bool envSuccess = SetupPolygonEnvironmentProps();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[StudioAssetFitter] Pipeline Finished! Player/NPC: {playerSuccess}, Zombies: {zombieSuccess}, Props: {envSuccess}");
        }

        public static bool AreStudioAssetsPresent()
        {
            // Check if StarterAssets or Zombie or Polygon packages are present in Assets
            string[] guids = AssetDatabase.FindAssets("t:Model");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("StarterAssets", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("Zombie", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("Polygon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("Synty", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/_Project/Animations"))
            {
                Directory.CreateDirectory("Assets/_Project/Animations");
                AssetDatabase.Refresh();
            }
        }

        // =========================================================================
        // 1. URP MATERIAL CONVERSION (Universal Render Pipeline/Lit)
        // =========================================================================
        public static int ConvertImportedMaterialsToURP()
        {
            int convertedCount = 0;
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogWarning("[StudioAssetFitter] Shader 'Universal Render Pipeline/Lit' not found! Falling back to Unlit.");
                urpLitShader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            string[] matGuids = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Exclude core _Project materials that are already properly configured
                if (path.StartsWith("Assets/_Project/Materials")) continue;

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // Check if material is using built-in standard or non-URP shaders
                string shaderName = mat.shader != null ? mat.shader.name : "";
                if (shaderName.Contains("Standard") || shaderName.Contains("Legacy") || shaderName.Contains("Mobile") || shaderName.Contains("Diffuse") || shaderName.Contains("Bumped"))
                {
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                    Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                    Texture metallicGloss = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") : null;
                    float glossiness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.4f;

                    mat.shader = urpLitShader;

                    if (mainTex != null)
                    {
                        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                    }
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                    if (bumpMap != null && mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", bumpMap);
                    if (metallicGloss != null && mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallicGloss);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", glossiness);

                    EditorUtility.SetDirty(mat);
                    convertedCount++;
                }
            }

            return convertedCount;
        }

        // =========================================================================
        // 2. PLAYER & NPC MODEL FITTING + TPS ANIMATIONS
        // =========================================================================
        public static bool SetupPlayerAndNPCModels()
        {
            GameObject characterModel = FindCharacterModel();
            if (characterModel == null)
            {
                Debug.LogWarning("[StudioAssetFitter] Starter Assets character model not found in Assets yet. Please import 'Starter Assets - ThirdPerson'.");
                return false;
            }

            // Create or update Player_TPS_Animator.controller
            AnimatorController animCtrl = GetOrCreatePlayerAnimator();

            // 1. Outfit Player_Root Prefab
            OutfitModelOnPrefab(PlayerPrefabPath, characterModel, animCtrl, null, Vector3.one);

            // 2. Outfit NPC Prefabs with Character Model + Custom Color Tints
            Material arasMat = GetOrCreateTintedMaterial("Mat_NPC_DrAras_Tint", new Color(0.94f, 0.96f, 0.98f)); // Doctor White Coat
            Material zeynepMat = GetOrCreateTintedMaterial("Mat_NPC_Zeynep_Tint", new Color(0.24f, 0.29f, 0.50f)); // Navy/Purple Jacket
            Material keremMat = GetOrCreateTintedMaterial("Mat_NPC_Kerem_Tint", new Color(0.36f, 0.31f, 0.24f)); // Khaki/Brown Worker
            Material elifMat = GetOrCreateTintedMaterial("Mat_NPC_Elif_Tint", new Color(0.85f, 0.33f, 0.16f)); // Red/Orange Courier

            OutfitModelOnPrefab(NPCDrArasPrefabPath, characterModel, animCtrl, arasMat, Vector3.one);
            OutfitModelOnPrefab(NPCZeynepPrefabPath, characterModel, animCtrl, zeynepMat, Vector3.one);
            OutfitModelOnPrefab(NPCKeremPrefabPath, characterModel, animCtrl, keremMat, Vector3.one);
            OutfitModelOnPrefab(NPCElifPrefabPath, characterModel, animCtrl, elifMat, Vector3.one);

            return true;
        }

        private static GameObject FindCharacterModel()
        {
            string[] candidateNames = new string[] { "Armature", "Character", "StarterAssetsThirdPerson", "ThirdPersonCharacter", "Mesh_Character" };
            foreach (string name in candidateNames)
            {
                string[] guids = AssetDatabase.FindAssets($"{name} t:Model");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.IndexOf("StarterAssets", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        path.IndexOf("Character", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (go != null) return go;
                    }
                }
            }

            // Fallback: check any prefab in StarterAssets
            string[] pGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in pGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("StarterAssets", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    path.IndexOf("Character", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null) return go;
                }
            }

            return null;
        }

        private static AnimatorController GetOrCreatePlayerAnimator()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerAnimatorPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerAnimatorPath);
            }

            // Ensure Parameters
            EnsureFloatParameter(controller, "Speed");
            EnsureBoolParameter(controller, "IsCrouching");
            EnsureBoolParameter(controller, "IsSprinting");
            EnsureTriggerParameter(controller, "TriggerThrow");
            EnsureBoolParameter(controller, "IsDead");

            // Find Clips
            AnimationClip idleClip = FindAnimationClip(new string[] { "Idle", "HumanoidIdle", "Stand" });
            AnimationClip walkClip = FindAnimationClip(new string[] { "Walk", "HumanoidWalk" });
            AnimationClip runClip = FindAnimationClip(new string[] { "Run", "Sprint", "HumanoidRun" });
            AnimationClip crouchClip = FindAnimationClip(new string[] { "Crouch", "CrouchIdle", "Crouching" });

            if (controller.layers.Length > 0)
            {
                AnimatorStateMachine sm = controller.layers[0].stateMachine;

                // Create Movement BlendTree or discrete states
                AnimatorState idleState = FindOrCreateState(sm, "Idle", idleClip);
                AnimatorState walkState = FindOrCreateState(sm, "Walk", walkClip);
                AnimatorState runState = FindOrCreateState(sm, "Run", runClip);

                sm.defaultState = idleState;

                // Transitions: Idle <-> Walk
                EnsureTransition(idleState, walkState, "Speed", AnimatorConditionMode.Greater, 0.15f);
                EnsureTransition(walkState, idleState, "Speed", AnimatorConditionMode.Less, 0.12f);

                // Transitions: Walk <-> Run
                EnsureTransition(walkState, runState, "Speed", AnimatorConditionMode.Greater, 4.2f);
                EnsureTransition(runState, walkState, "Speed", AnimatorConditionMode.Less, 4.0f);
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        // =========================================================================
        // 3. ZOMBIE MODEL FITTING + ANIMATION CONTROLLER
        // =========================================================================
        public static bool SetupZombieModels()
        {
            GameObject zombieModel = FindZombieModel();
            if (zombieModel == null)
            {
                Debug.LogWarning("[StudioAssetFitter] Pxltiger Zombie model not found in Assets yet. Please import 'Zombie (Pxltiger)'.");
                return false;
            }

            AnimatorController animCtrl = GetOrCreateZombieAnimator();

            // 1. Standard Zombie
            OutfitModelOnPrefab(ZombieStandardPrefabPath, zombieModel, animCtrl, null, Vector3.one);

            // 2. Runner Zombie: Lean scale (0.92, 1.02, 0.92) + Reddish Infected Tint
            Material runnerMat = GetOrCreateTintedMaterial("Mat_Zombie_Runner_Tint", new Color(0.68f, 0.12f, 0.14f));
            OutfitModelOnPrefab(ZombieRunnerPrefabPath, zombieModel, animCtrl, runnerMat, new Vector3(0.92f, 1.02f, 0.92f));

            // 3. Screamer Zombie: Purplish vein/skin tint
            Material screamerMat = GetOrCreateTintedMaterial("Mat_Zombie_Screamer_Tint", new Color(0.48f, 0.12f, 0.72f));
            OutfitModelOnPrefab(ZombieScreamerPrefabPath, zombieModel, animCtrl, screamerMat, new Vector3(0.95f, 1.02f, 0.95f));

            // 4. Tank Zombie: Heavy scale (1.35, 1.25, 1.35) + Armored Dark Slate Tint
            Material tankMat = GetOrCreateTintedMaterial("Mat_Zombie_Tank_Tint", new Color(0.18f, 0.20f, 0.24f));
            OutfitModelOnPrefab(ZombieTankPrefabPath, zombieModel, animCtrl, tankMat, new Vector3(1.35f, 1.25f, 1.35f));

            return true;
        }

        private static GameObject FindZombieModel()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("Zombie", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("Pxltiger", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null) return go;
                }
            }

            // Fallback: check prefabs
            string[] pGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in pGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/_Project") && path.IndexOf("Zombie", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null) return go;
                }
            }

            return null;
        }

        private static AnimatorController GetOrCreateZombieAnimator()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ZombieAnimatorPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ZombieAnimatorPath);
            }

            EnsureFloatParameter(controller, "Speed");
            EnsureTriggerParameter(controller, "TriggerAttack");
            EnsureTriggerParameter(controller, "TriggerScream");
            EnsureBoolParameter(controller, "IsDead");

            AnimationClip idleClip = FindAnimationClip(new string[] { "Z_Idle", "Zombie_Idle", "Idle" });
            AnimationClip walkClip = FindAnimationClip(new string[] { "Z_Walk", "Zombie_Walk", "Walk" });
            AnimationClip runClip = FindAnimationClip(new string[] { "Z_Run_InPlace", "Z_Run", "Zombie_Run", "Run" });
            AnimationClip attackClip = FindAnimationClip(new string[] { "Z_Attack", "Zombie_Attack", "Attack" });
            AnimationClip dieClip = FindAnimationClip(new string[] { "Z_FallingBack", "Zombie_Death", "Die", "Death" });

            if (controller.layers.Length > 0)
            {
                AnimatorStateMachine sm = controller.layers[0].stateMachine;

                AnimatorState idleState = FindOrCreateState(sm, "Idle", idleClip);
                AnimatorState walkState = FindOrCreateState(sm, "Walk", walkClip);
                AnimatorState runState = FindOrCreateState(sm, "Run", runClip);
                AnimatorState attackState = FindOrCreateState(sm, "Attack", attackClip);
                AnimatorState dieState = FindOrCreateState(sm, "Death", dieClip);

                sm.defaultState = idleState;

                // Speed transitions
                EnsureTransition(idleState, walkState, "Speed", AnimatorConditionMode.Greater, 0.15f);
                EnsureTransition(walkState, idleState, "Speed", AnimatorConditionMode.Less, 0.12f);
                EnsureTransition(walkState, runState, "Speed", AnimatorConditionMode.Greater, 3.4f);
                EnsureTransition(runState, walkState, "Speed", AnimatorConditionMode.Less, 3.2f);

                // AnyState to Attack
                EnsureAnyStateTransition(sm, attackState, "TriggerAttack", AnimatorConditionMode.If);
                EnsureTransition(attackState, idleState, null, 0, 0f, hasExitTime: true);

                // AnyState to Death
                EnsureAnyStateTransition(sm, dieState, "IsDead", AnimatorConditionMode.If);
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        // =========================================================================
        // 4. POLYGON ENVIRONMENT PROPS & NAVMESH
        // =========================================================================
        public static bool SetupPolygonEnvironmentProps()
        {
            GameObject chestModel = FindPolygonProp(new string[] { "SM_Prop_Chest", "Chest", "Chest_01" });
            GameObject barrelModel = FindPolygonProp(new string[] { "SM_Prop_Barrel", "Barrel", "Drum" });
            GameObject barrierModel = FindPolygonProp(new string[] { "SM_Prop_Barrier", "Barrier", "ConcreteBarrier" });
            GameObject crateModel = FindPolygonProp(new string[] { "SM_Prop_Crate", "Crate", "WoodenCrate" });

            if (chestModel == null && barrelModel == null && barrierModel == null)
            {
                Debug.LogWarning("[StudioAssetFitter] POLYGON Starter Pack props not found in Assets yet. Please import 'POLYGON Starter Pack'.");
                return false;
            }

            // 1. Update LootContainer_Root Prefab with 3D Chest
            if (chestModel != null)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(LootPrefabPath);
                if (root != null)
                {
                    Transform visual = root.transform.Find("Visual_Model");
                    if (visual != null)
                    {
                        // Remove temporary child cubes
                        for (int i = visual.childCount - 1; i >= 0; i--)
                        {
                            UnityEngine.Object.DestroyImmediate(visual.GetChild(i).gameObject);
                        }
                        // Remove renderer on visual itself if it's a cube
                        MeshRenderer mr = visual.GetComponent<MeshRenderer>();
                        if (mr != null) mr.enabled = false;

                        GameObject chestInstance = UnityEngine.Object.Instantiate(chestModel, visual);
                        chestInstance.name = "POLYGON_Chest";
                        chestInstance.transform.localPosition = Vector3.zero;
                        chestInstance.transform.localRotation = Quaternion.identity;
                        chestInstance.transform.localScale = Vector3.one;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, LootPrefabPath);
                    PrefabUtility.UnloadPrefabContents(root);
                    Debug.Log("[StudioAssetFitter] LootContainer_Root updated with POLYGON 3D chest model.");
                }
            }

            // 2. NavMesh Re-bake
            Debug.Log("[StudioAssetFitter] Re-baking NavMesh with static studio props...");
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

            return true;
        }

        private static GameObject FindPolygonProp(string[] names)
        {
            foreach (string name in names)
            {
                string[] guids = AssetDatabase.FindAssets($"{name} t:Prefab");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.IndexOf("Polygon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        path.IndexOf("Synty", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (go != null) return go;
                    }
                }
            }
            return null;
        }

        // =========================================================================
        // HELPER UTILITIES: OUTFITTING, ANIMATIONS & MATERIALS
        // =========================================================================
        private static void OutfitModelOnPrefab(string prefabPath, GameObject modelSource, AnimatorController animCtrl, Material customMat, Vector3 visualScale)
        {
            if (!File.Exists(prefabPath)) return;

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return;

            Transform visual = root.transform.Find("Visual_Model");
            if (visual == null)
            {
                GameObject vObj = new GameObject("Visual_Model");
                vObj.transform.SetParent(root.transform, false);
                visual = vObj.transform;
            }

            // Clean existing placeholder capsule and cubes
            for (int i = visual.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(visual.GetChild(i).gameObject);
            }
            MeshRenderer baseMr = visual.GetComponent<MeshRenderer>();
            if (baseMr != null) baseMr.enabled = false;
            MeshFilter baseMf = visual.GetComponent<MeshFilter>();
            if (baseMf != null) baseMf.sharedMesh = null;

            // Instantiate 3D model
            GameObject modelInst = UnityEngine.Object.Instantiate(modelSource, visual);
            modelInst.name = "Model_Mesh";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = visualScale;

            // Apply custom material if specified
            if (customMat != null)
            {
                Renderer[] rends = modelInst.GetComponentsInChildren<Renderer>();
                foreach (var r in rends)
                {
                    Material[] mats = new Material[r.sharedMaterials.Length];
                    for (int m = 0; m < mats.Length; m++) mats[m] = customMat;
                    r.sharedMaterials = mats;
                }
            }

            // Animator Setup
            Animator animator = modelInst.GetComponent<Animator>();
            if (animator == null) animator = modelInst.AddComponent<Animator>();
            if (animCtrl != null) animator.runtimeAnimatorController = animCtrl;

            // Refresh Bridge references
            PlayerAnimationBridge pBridge = root.GetComponent<PlayerAnimationBridge>();
            if (pBridge != null) pBridge.RefreshAnimator();

            ZombieAnimationBridge zBridge = root.GetComponent<ZombieAnimationBridge>();
            if (zBridge != null) zBridge.RefreshAnimator();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[StudioAssetFitter] Successfully outfitted 3D model on {Path.GetFileName(prefabPath)}");
        }

        private static Material GetOrCreateTintedMaterial(string name, Color tint)
        {
            string path = $"Assets/_Project/Materials/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(s);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static AnimationClip FindAnimationClip(string[] candidateNames)
        {
            foreach (string name in candidateNames)
            {
                string[] guids = AssetDatabase.FindAssets($"{name} t:AnimationClip");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip != null) return clip;
                }
            }
            return null;
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string stateName, AnimationClip clip)
        {
            foreach (var child in sm.states)
            {
                if (child.state.name == stateName)
                {
                    if (clip != null) child.state.motion = clip;
                    return child.state;
                }
            }
            AnimatorState s = sm.AddState(stateName);
            if (clip != null) s.motion = clip;
            return s;
        }

        private static void EnsureFloatParameter(AnimatorController ctrl, string name)
        {
            foreach (var p in ctrl.parameters) if (p.name == name) return;
            ctrl.AddParameter(name, AnimatorControllerParameterType.Float);
        }

        private static void EnsureBoolParameter(AnimatorController ctrl, string name)
        {
            foreach (var p in ctrl.parameters) if (p.name == name) return;
            ctrl.AddParameter(name, AnimatorControllerParameterType.Bool);
        }

        private static void EnsureTriggerParameter(AnimatorController ctrl, string name)
        {
            foreach (var p in ctrl.parameters) if (p.name == name) return;
            ctrl.AddParameter(name, AnimatorControllerParameterType.Trigger);
        }

        private static void EnsureTransition(AnimatorState from, AnimatorState to, string paramName, AnimatorConditionMode mode, float threshold, bool hasExitTime = false)
        {
            foreach (var t in from.transitions)
            {
                if (t.destinationState == to) return;
            }
            AnimatorStateTransition tr = from.AddTransition(to);
            tr.hasExitTime = hasExitTime;
            tr.duration = 0.15f;
            if (!string.IsNullOrEmpty(paramName))
            {
                tr.AddCondition(mode, threshold, paramName);
            }
        }

        private static void EnsureAnyStateTransition(AnimatorStateMachine sm, AnimatorState to, string paramName, AnimatorConditionMode mode)
        {
            foreach (var t in sm.anyStateTransitions)
            {
                if (t.destinationState == to) return;
            }
            AnimatorStateTransition tr = sm.AddAnyStateTransition(to);
            tr.hasExitTime = false;
            tr.duration = 0.1f;
            tr.AddCondition(mode, 0f, paramName);
        }
    }
}
