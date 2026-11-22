using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.AI;
using ProjectSurvival.AI;

namespace ProjectSurvival.EditorTools
{
    public static class ZombiePrefabBuilder
    {
        private const string StandardFbxPath = "Assets/_Project/Models/Zombies/StandardZombie/Zombie1.FBX";
        private const string RunnerGlbPath = "Assets/_Project/Models/Zombies/RunnerZombie/zombie-2.glb";
        private const string TankGlbPath = "Assets/_Project/Models/Zombies/TankZombie/meaty_zombie.glb";

        private const string StandardPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Standard_Root.prefab";
        private const string RunnerPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Runner_Root.prefab";
        private const string ScreamerPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Screamer_Root.prefab";
        private const string TankPrefabPath = "Assets/_Project/Prefabs/Zombies/Zombie_Tank_Root.prefab";

        public const float StandardTargetHeight = 1.80f;
        public const float ScreamerTargetHeight = 1.80f;
        public const float RunnerTargetHeight = 1.80f;
        public const float TankTargetHeight = 2.25f;

        [MenuItem("ProjectSurvival/Test Build New Zombie Prefabs", false, 11)]
        public static void TestBuildAll()
        {
            SetupMaterialsAndTextures();
            ConfigureAnimationClips();

            BuildStandardZombiePrefab();
            BuildScreamerZombiePrefab();
            BuildRunnerZombiePrefab();
            BuildTankZombiePrefab();

            Debug.Log("[ZombiePrefabBuilder] All 4 Zombie Prefabs built and auto-scaled successfully!");
        }

        [MenuItem("ProjectSurvival/Auto-Scale & Align All Zombies in Scene", false, 12)]
        public static void FixAllZombiesInSceneMenuItem()
        {
            FixAllZombiesInScene();
        }

        public static void SetupMaterialsAndTextures()
        {
            // Configure Normal Map texture
            string nmPath = "Assets/_Project/Models/Zombies/StandardZombie/textures/Zombie_nm.png";
            TextureImporter ti = AssetImporter.GetAtPath(nmPath) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Models/Zombies/StandardZombie/textures/Zombie.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(nmPath);
            Texture2D ao = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Models/Zombies/StandardZombie/textures/Zombie_ao.png");
            Texture2D gloss = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Models/Zombies/StandardZombie/textures/Zombie_gloss.png");
            Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Models/Zombies/StandardZombie/textures/zombie_emission.png");

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            // 1. Standard Zombie Material
            Material matStd = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Zombie_Standard_URP.mat");
            if (matStd == null)
            {
                matStd = new Material(urpLit);
                AssetDatabase.CreateAsset(matStd, "Assets/_Project/Materials/Mat_Zombie_Standard_URP.mat");
            }
            matStd.shader = urpLit;
            if (albedo != null) matStd.SetTexture("_BaseMap", albedo);
            if (normal != null) { matStd.SetTexture("_BumpMap", normal); matStd.EnableKeyword("_NORMALMAP"); }
            if (ao != null) matStd.SetTexture("_OcclusionMap", ao);
            if (gloss != null) matStd.SetTexture("_MetallicGlossMap", gloss);
            matStd.SetFloat("_Smoothness", 0.55f);
            if (emission != null)
            {
                matStd.SetTexture("_EmissionMap", emission);
                matStd.SetColor("_EmissionColor", new Color(0.2f, 0.45f, 0.15f) * 1.5f);
                matStd.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(matStd);

            // 2. Screamer Zombie Material (Sickly green/yellow decayed variant with piercing red eyes)
            Material matScreamer = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Zombie_Screamer_URP.mat");
            if (matScreamer == null)
            {
                matScreamer = new Material(urpLit);
                AssetDatabase.CreateAsset(matScreamer, "Assets/_Project/Materials/Mat_Zombie_Screamer_URP.mat");
            }
            matScreamer.shader = urpLit;
            matScreamer.SetColor("_BaseColor", new Color(0.75f, 0.95f, 0.70f));
            if (albedo != null) matScreamer.SetTexture("_BaseMap", albedo);
            if (normal != null) { matScreamer.SetTexture("_BumpMap", normal); matScreamer.EnableKeyword("_NORMALMAP"); }
            if (ao != null) matScreamer.SetTexture("_OcclusionMap", ao);
            matScreamer.SetFloat("_Smoothness", 0.4f);
            if (emission != null)
            {
                matScreamer.SetTexture("_EmissionMap", emission);
                matScreamer.SetColor("_EmissionColor", new Color(0.9f, 0.15f, 0.1f) * 2.5f); // Glowing red eyes
                matScreamer.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(matScreamer);

            AssetDatabase.SaveAssets();
        }

        public static void ConfigureAnimationClips()
        {
            string[] loopingClips = new string[]
            {
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Idle.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Walk.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Walk_InPlace.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Run.FBX",
                "Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Run_InPlace.FBX"
            };

            foreach (var path in loopingClips)
            {
                ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi != null)
                {
                    ModelImporterClipAnimation[] clips = mi.defaultClipAnimations;
                    bool modified = false;
                    for (int i = 0; i < clips.Length; i++)
                    {
                        if (!clips[i].loopTime)
                        {
                            clips[i].loopTime = true;
                            modified = true;
                        }
                    }
                    if (modified)
                    {
                        mi.clipAnimations = clips;
                        mi.SaveAndReimport();
                    }
                }
            }
        }

        public static AnimationClip FindClipInAsset(string assetPath, string clipNameFilter = "")
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null) return null;
            foreach (var a in assets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    if (string.IsNullOrEmpty(clipNameFilter) || c.name.Contains(clipNameFilter))
                    {
                        return c;
                    }
                }
            }
            return null;
        }

        public static AnimatorController BuildStandardZombieAnimatorController(string path)
        {
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null)
            {
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            EnsureParam(ctrl, "Speed", AnimatorControllerParameterType.Float);
            EnsureParam(ctrl, "TriggerAttack", AnimatorControllerParameterType.Trigger);
            EnsureParam(ctrl, "TriggerScream", AnimatorControllerParameterType.Trigger);
            EnsureParam(ctrl, "IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            foreach (var s in sm.states) sm.RemoveState(s.state);

            // Load MoCap Clips
            AnimationClip idleClip = FindClipInAsset("Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Idle.FBX", "Idle");
            AnimationClip walkClip = FindClipInAsset("Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Walk_InPlace.FBX", "Walk")
                                  ?? FindClipInAsset("Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Walk.FBX", "Walk");
            AnimationClip runClip = FindClipInAsset("Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Run_InPlace.FBX", "Run")
                                 ?? FindClipInAsset("Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Run.FBX", "Run");
            AnimationClip attackClip = FindClipInAsset("Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_Attack.FBX", "Attack");
            AnimationClip deathClip = FindClipInAsset("Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_FallingBack.FBX", "FallingBack")
                                   ?? FindClipInAsset("Assets/_Project/Models/Zombies/StandardZombie/animations/Zombie@Z_FallingForward.FBX", "FallingForward");

            AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = idleClip;
            AnimatorState stWalk = sm.AddState("Walk"); stWalk.motion = walkClip;
            AnimatorState stRun = sm.AddState("Run"); stRun.motion = runClip;
            AnimatorState stAttack = sm.AddState("Attack"); stAttack.motion = attackClip;
            AnimatorState stScream = sm.AddState("Scream"); stScream.motion = attackClip;
            AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = deathClip;

            sm.defaultState = stIdle;

            // Transitions: Idle <-> Walk
            var t1 = stIdle.AddTransition(stWalk);
            t1.hasExitTime = false; t1.duration = 0.2f;
            t1.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");

            var t2 = stWalk.AddTransition(stIdle);
            t2.hasExitTime = false; t2.duration = 0.2f;
            t2.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");

            // Transitions: Walk <-> Run
            var t3 = stWalk.AddTransition(stRun);
            t3.hasExitTime = false; t3.duration = 0.2f;
            t3.AddCondition(AnimatorConditionMode.Greater, 2.8f, "Speed");

            var t4 = stRun.AddTransition(stWalk);
            t4.hasExitTime = false; t4.duration = 0.2f;
            t4.AddCondition(AnimatorConditionMode.Less, 2.5f, "Speed");

            // Attack
            var tAttack = sm.AddAnyStateTransition(stAttack);
            tAttack.hasExitTime = false; tAttack.duration = 0.1f;
            tAttack.AddCondition(AnimatorConditionMode.If, 0f, "TriggerAttack");

            var tAttackOut = stAttack.AddTransition(stWalk);
            tAttackOut.hasExitTime = true; tAttackOut.exitTime = 0.85f; tAttackOut.duration = 0.2f;

            // Scream
            var tScream = sm.AddAnyStateTransition(stScream);
            tScream.hasExitTime = false; tScream.duration = 0.15f;
            tScream.AddCondition(AnimatorConditionMode.If, 0f, "TriggerScream");

            var tScreamOut = stScream.AddTransition(stIdle);
            tScreamOut.hasExitTime = true; tScreamOut.exitTime = 0.90f; tScreamOut.duration = 0.2f;

            // Death
            var tDeath = sm.AddAnyStateTransition(stDeath);
            tDeath.hasExitTime = false; tDeath.duration = 0.2f;
            tDeath.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        public static AnimatorController BuildGlbZombieAnimatorController(string rootName, string glbPath)
        {
            string ctrlPath = $"Assets/_Project/Animations/{rootName}_Animator.controller";
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (ctrl == null)
            {
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            }

            EnsureParam(ctrl, "Speed", AnimatorControllerParameterType.Float);
            EnsureParam(ctrl, "TriggerAttack", AnimatorControllerParameterType.Trigger);
            EnsureParam(ctrl, "TriggerScream", AnimatorControllerParameterType.Trigger);
            EnsureParam(ctrl, "IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            foreach (var s in sm.states) sm.RemoveState(s.state);

            AnimationClip mainClip = FindClipInAsset(glbPath);
            AnimatorState stAction = sm.AddState("Action");
            stAction.motion = mainClip;
            sm.defaultState = stAction;

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        private static void EnsureParam(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in ctrl.parameters)
            {
                if (p.name == name) return;
            }
            ctrl.AddParameter(name, type);
        }

        /// <summary>
        /// Calculates the combined axis-aligned bounding box of all MeshRenderer and SkinnedMeshRenderer
        /// components under target, transformed into the local coordinate space of root.
        /// </summary>
        public static Bounds CalculateBoundsInRootSpace(Transform root, GameObject target)
        {
            Bounds localBounds = new Bounds();
            bool hasBounds = false;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;

                Bounds b = r.bounds;
                Vector3 min = b.min;
                Vector3 max = b.max;

                // Fallback to sharedMesh bounds if renderer bounds are invalid or zero
                if (b.size.magnitude < 0.001f)
                {
                    if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                    {
                        min = smr.sharedMesh.bounds.min;
                        max = smr.sharedMesh.bounds.max;
                        Vector3[] localCorners = GetCorners(min, max);
                        for (int i = 0; i < 8; i++)
                        {
                            Vector3 worldPt = smr.transform.TransformPoint(localCorners[i]);
                            Vector3 rootPt = root.InverseTransformPoint(worldPt);
                            if (!hasBounds) { localBounds = new Bounds(rootPt, Vector3.zero); hasBounds = true; }
                            else { localBounds.Encapsulate(rootPt); }
                        }
                        continue;
                    }
                    else if (r is MeshRenderer mr)
                    {
                        var mf = mr.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            min = mf.sharedMesh.bounds.min;
                            max = mf.sharedMesh.bounds.max;
                            Vector3[] localCorners = GetCorners(min, max);
                            for (int i = 0; i < 8; i++)
                            {
                                Vector3 worldPt = mr.transform.TransformPoint(localCorners[i]);
                                Vector3 rootPt = root.InverseTransformPoint(worldPt);
                                if (!hasBounds) { localBounds = new Bounds(rootPt, Vector3.zero); hasBounds = true; }
                                else { localBounds.Encapsulate(rootPt); }
                            }
                            continue;
                        }
                    }
                }

                Vector3[] corners = GetCorners(min, max);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 rootPt = root.InverseTransformPoint(corners[i]);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(rootPt, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(rootPt);
                    }
                }
            }

            return localBounds;
        }

        private static Vector3[] GetCorners(Vector3 min, Vector3 max)
        {
            return new Vector3[8]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
            };
        }

        /// <summary>
        /// Precision auto-scaling and ground alignment algorithm:
        /// 1. Cleans up old procedural/primitive meshes under visualModel.
        /// 2. Resets transforms to measure raw bounding box height.
        /// 3. Computes scaleFactor = targetHeight / rawHeight and applies to modelInstance.
        /// 4. Offsets modelInstance so lowest point (min.y) sits exactly at root Y = 0 and centers X/Z.
        /// </summary>
        public static void AutoScaleAndAlignModel(GameObject root, GameObject visualModel, GameObject modelInstance, float targetHeight)
        {
            if (root == null || visualModel == null || modelInstance == null) return;

            // 1. Clean up old procedural or primitive meshes under visualModel
            for (int i = visualModel.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = visualModel.transform.GetChild(i);
                if (child.gameObject != modelInstance)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var mf = visualModel.GetComponent<MeshFilter>();
            if (mf != null) Object.DestroyImmediate(mf);
            var mr = visualModel.GetComponent<MeshRenderer>();
            if (mr != null) Object.DestroyImmediate(mr);

            // Ensure Visual_Model transform is neutral
            visualModel.transform.localPosition = Vector3.zero;
            visualModel.transform.localRotation = Quaternion.identity;
            visualModel.transform.localScale = Vector3.one;

            // Reset modelInstance transform for accurate raw measurement
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            // Strip any leftover colliders on modelInstance (Root holds the single physics collider)
            foreach (var c in modelInstance.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }

            // 2. Measure raw bounds height
            Bounds rawBounds = CalculateBoundsInRootSpace(root.transform, modelInstance);
            float rawHeight = rawBounds.size.y;
            if (rawHeight <= 0.001f)
            {
                Debug.LogWarning($"[ZombiePrefabBuilder] Measured raw bounds height is zero on {modelInstance.name}, fallback to 1.0f");
                rawHeight = 1.0f;
            }

            float scaleFactor = targetHeight / rawHeight;
            modelInstance.transform.localScale = Vector3.one * scaleFactor;

            // 3. Recalculate bounds after scale
            Bounds scaledBounds = CalculateBoundsInRootSpace(root.transform, modelInstance);

            // Shift so feet sit exactly at Y = 0 and horizontal center aligns with root (0, 0)
            float offsetX = -scaledBounds.center.x;
            float offsetY = -scaledBounds.min.y;
            float offsetZ = -scaledBounds.center.z;
            modelInstance.transform.localPosition = new Vector3(offsetX, offsetY, offsetZ);

            // 4. Verify final bounds
            Bounds finalBounds = CalculateBoundsInRootSpace(root.transform, modelInstance);
            Debug.Log($"[ZombiePrefabBuilder] {root.name} auto-scaled: rawHeight={rawHeight:F2}m -> scaleFactor={scaleFactor:F6} -> finalHeight={finalBounds.size.y:F2}m (target={targetHeight:F2}m), minY={finalBounds.min.y:F4}m, center=({finalBounds.center.x:F3}, {finalBounds.center.y:F3}, {finalBounds.center.z:F3})");
        }

        public static GameObject BuildStandardZombiePrefab()
        {
            return BuildFbxZombie(
                "Zombie_Standard_Root", StandardPrefabPath, ZombieType.Standard,
                StandardFbxPath, "Assets/_Project/Materials/Mat_Zombie_Standard_URP.mat",
                StandardTargetHeight, 0.35f, 1.80f,
                60f, 4.2f, 1.8f, 14f, 8f, 12f, 1.0f, 1.4f
            );
        }

        public static GameObject BuildScreamerZombiePrefab()
        {
            return BuildFbxZombie(
                "Zombie_Screamer_Root", ScreamerPrefabPath, ZombieType.Screamer,
                StandardFbxPath, "Assets/_Project/Materials/Mat_Zombie_Screamer_URP.mat",
                ScreamerTargetHeight, 0.35f, 1.80f,
                45f, 3.8f, 1.6f, 8f, 8f, 16f, 1.2f, 1.3f
            );
        }

        public static GameObject BuildRunnerZombiePrefab()
        {
            return BuildGlbZombie(
                "Zombie_Runner_Root", RunnerPrefabPath, ZombieType.Runner,
                RunnerGlbPath,
                RunnerTargetHeight, 0.35f, 1.80f,
                35f, 6.8f, 2.8f, 12f, 10f, 15f, 1.5f, 1.3f
            );
        }

        public static GameObject BuildTankZombiePrefab()
        {
            return BuildGlbZombie(
                "Zombie_Tank_Root", TankPrefabPath, ZombieType.Tank,
                TankGlbPath,
                TankTargetHeight, 0.50f, 2.25f,
                220f, 2.4f, 1.2f, 35f, 15f, 10f, 0.8f, 1.8f
            );
        }

        private static GameObject BuildFbxZombie(
            string rootName, string prefabPath, ZombieType type,
            string fbxPath, string matPath,
            float targetHeight, float colliderRadius, float colliderHeight,
            float hp, float chaseSpd, float patrolSpd, float dmg, float infect, float viewDist, float hearingMult, float attackRange)
        {
            GameObject root = new GameObject(rootName);
            root.tag = "Respawn";

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = chaseSpd;
            agent.radius = colliderRadius;
            agent.height = colliderHeight;
            agent.stoppingDistance = attackRange * 0.9f;

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, colliderHeight * 0.5f, 0);
            col.radius = colliderRadius;
            col.height = colliderHeight;

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

            // Visual_Model child
            GameObject visual = new GameObject("Visual_Model");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj != null)
            {
                GameObject modelInstance = Object.Instantiate(fbxObj, visual.transform);
                modelInstance.name = fbxObj.name;

                // Apply URP material
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat != null)
                {
                    foreach (var rend in modelInstance.GetComponentsInChildren<Renderer>(true))
                    {
                        rend.sharedMaterial = mat;
                    }
                }

                // Precision scale & alignment
                AutoScaleAndAlignModel(root, visual, modelInstance, targetHeight);

                // Setup Animator directly on modelInstance so generic clip curves map properly
                Animator anim = modelInstance.GetComponent<Animator>();
                if (anim == null) anim = modelInstance.AddComponent<Animator>();
                anim.runtimeAnimatorController = BuildStandardZombieAnimatorController("Assets/_Project/Animations/Zombie_Standard_MoCap.controller");
            }

            // Remove any shadow Animator on visual
            Animator shadowAnim = visual.GetComponent<Animator>();
            if (shadowAnim != null) Object.DestroyImmediate(shadowAnim);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildGlbZombie(
            string rootName, string prefabPath, ZombieType type,
            string glbPath,
            float targetHeight, float colliderRadius, float colliderHeight,
            float hp, float chaseSpd, float patrolSpd, float dmg, float infect, float viewDist, float hearingMult, float attackRange)
        {
            GameObject root = new GameObject(rootName);
            root.tag = "Respawn";

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = chaseSpd;
            agent.radius = colliderRadius;
            agent.height = colliderHeight;
            agent.stoppingDistance = attackRange * 0.9f;

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, colliderHeight * 0.5f, 0);
            col.radius = colliderRadius;
            col.height = colliderHeight;

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

            // Visual_Model child
            GameObject visual = new GameObject("Visual_Model");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            GameObject glbObj = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
            if (glbObj != null)
            {
                GameObject modelInstance = Object.Instantiate(glbObj, visual.transform);
                modelInstance.name = glbObj.name;

                // Precision scale & alignment
                AutoScaleAndAlignModel(root, visual, modelInstance, targetHeight);

                // Setup Animator directly on modelInstance
                Animator anim = modelInstance.GetComponent<Animator>();
                if (anim == null) anim = modelInstance.AddComponent<Animator>();
                anim.runtimeAnimatorController = BuildGlbZombieAnimatorController(rootName, glbPath);
            }

            // Remove any shadow Animator on visual
            Animator shadowAnim = visual.GetComponent<Animator>();
            if (shadowAnim != null) Object.DestroyImmediate(shadowAnim);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Scans all active and inactive zombie instances in the currently open scene,
        /// verifies collider and agent proportions, cleans Visual_Model, and applies auto-scaling.
        /// </summary>
        public static void FixAllZombiesInScene()
        {
            var sceneZombies = Object.FindObjectsByType<ZombieAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[ZombiePrefabBuilder] Scanning and fixing {sceneZombies.Length} zombies in scene...");

            foreach (var z in sceneZombies)
            {
                Transform visual = z.transform.Find("Visual_Model");
                if (visual == null) continue;

                float targetHeight = StandardTargetHeight;
                float radius = 0.35f;
                float height = 1.80f;

                if (z.Type == ZombieType.Tank)
                {
                    targetHeight = TankTargetHeight;
                    radius = 0.50f;
                    height = 2.25f;
                }

                // Update root CapsuleCollider
                CapsuleCollider col = z.GetComponent<CapsuleCollider>();
                if (col != null)
                {
                    col.radius = radius;
                    col.height = height;
                    col.center = new Vector3(0, height * 0.5f, 0);
                }

                // Update root NavMeshAgent
                NavMeshAgent agent = z.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.radius = radius;
                    agent.height = height;
                }

                // Find model child under Visual_Model
                Transform modelInstance = null;
                for (int i = 0; i < visual.childCount; i++)
                {
                    Transform c = visual.GetChild(i);
                    string cName = c.name;
                    if (cName.Contains("FBX") || cName.Contains("glb") || cName.Contains("Zombie") || cName.Contains("meaty") || cName.Contains("Object_"))
                    {
                        modelInstance = c;
                        break;
                    }
                }
                if (modelInstance == null && visual.childCount > 0)
                {
                    modelInstance = visual.GetChild(0);
                }

                if (modelInstance != null)
                {
                    AutoScaleAndAlignModel(z.gameObject, visual.gameObject, modelInstance.gameObject, targetHeight);

                    // Ensure Animator is active on modelInstance
                    Animator anim = modelInstance.GetComponent<Animator>();
                    if (anim == null) anim = modelInstance.gameObject.AddComponent<Animator>();

                    if (z.Type == ZombieType.Standard || z.Type == ZombieType.Screamer)
                    {
                        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Animations/Zombie_Standard_MoCap.controller");
                    }
                    else if (z.Type == ZombieType.Runner)
                    {
                        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Animations/Zombie_Runner_Root_Animator.controller");
                    }
                    else if (z.Type == ZombieType.Tank)
                    {
                        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Animations/Zombie_Tank_Root_Animator.controller");
                    }

                    // Remove shadow animator on Visual_Model if present
                    Animator vAnim = visual.GetComponent<Animator>();
                    if (vAnim != null) Object.DestroyImmediate(vAnim);

                    // Refresh ZombieAnimationBridge
                    ZombieAnimationBridge bridge = z.GetComponent<ZombieAnimationBridge>();
                    if (bridge != null) bridge.RefreshAnimator();

                    // Ensure ZombieLocomotionEngine is present
                    ZombieLocomotionEngine loco = z.GetComponent<ZombieLocomotionEngine>();
                    if (loco == null) loco = z.gameObject.AddComponent<ZombieLocomotionEngine>();

                    // Ensure ZombieEyeGlow is present
                    ZombieEyeGlow glow = z.GetComponent<ZombieEyeGlow>();
                    if (glow == null) glow = z.gameObject.AddComponent<ZombieEyeGlow>();
                }

                EditorUtility.SetDirty(z.gameObject);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log($"[ZombiePrefabBuilder] Successfully verified and auto-scaled all {sceneZombies.Length} zombies in scene.");
        }
    }
}
