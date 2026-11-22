using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ProjectSurvival.EditorTools
{
    public static class AnimatorBuilder
    {
        private const string AnimFolder = "Assets/_Project/Animations";
        private const string ClipFolder = "Assets/_Project/Animations/Clips";
        private const string PlayerCtrlPath = "Assets/_Project/Animations/Player_TPS_Animator.controller";
        private const string ZombieCtrlPath = "Assets/_Project/Animations/Zombie_Animator.controller";

        public static AnimatorController CreatePlayerAnimatorController()
        {
            EnsureDirectories();

            // Load existing or create new
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerCtrlPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerCtrlPath);
            }

            // Parameters
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsCrouching", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "TriggerThrow", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine rootSm = controller.layers[0].stateMachine;

            // Clear old states
            foreach (var state in rootSm.states)
            {
                rootSm.RemoveState(state.state);
            }

            // Clips
            AnimationClip idleClip = GetOrCreateClip("Player_Idle", 2.0f, 0.02f, 0f);
            AnimationClip walkClip = GetOrCreateClip("Player_Walk", 0.9f, 0.06f, 25f);
            AnimationClip runClip = GetOrCreateClip("Player_Run", 0.65f, 0.10f, 45f);
            AnimationClip crouchIdleClip = GetOrCreateClip("Player_CrouchIdle", 2.0f, 0.01f, 0f, -0.3f);
            AnimationClip crouchWalkClip = GetOrCreateClip("Player_CrouchWalk", 1.1f, 0.04f, 15f, -0.3f);
            AnimationClip meleeClip = GetOrCreateClip("Player_MeleePush", 0.5f, 0.02f, 50f);
            AnimationClip deadClip = GetOrCreateClip("Player_Death", 1.5f, 0f, 0f, -0.7f, 90f);

            // States
            AnimatorState stIdle = rootSm.AddState("Idle");
            stIdle.motion = idleClip;

            AnimatorState stWalk = rootSm.AddState("Walk");
            stWalk.motion = walkClip;

            AnimatorState stRun = rootSm.AddState("Run");
            stRun.motion = runClip;

            AnimatorState stCrouchIdle = rootSm.AddState("CrouchIdle");
            stCrouchIdle.motion = crouchIdleClip;

            AnimatorState stCrouchWalk = rootSm.AddState("CrouchWalk");
            stCrouchWalk.motion = crouchWalkClip;

            AnimatorState stMelee = rootSm.AddState("MeleePush");
            stMelee.motion = meleeClip;

            AnimatorState stDead = rootSm.AddState("Death");
            stDead.motion = deadClip;

            rootSm.defaultState = stIdle;

            // Transitions: Idle <-> Walk
            var tIdleToWalk = stIdle.AddTransition(stWalk);
            tIdleToWalk.hasExitTime = false;
            tIdleToWalk.duration = 0.15f;
            tIdleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");

            var tWalkToIdle = stWalk.AddTransition(stIdle);
            tWalkToIdle.hasExitTime = false;
            tWalkToIdle.duration = 0.15f;
            tWalkToIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            // Transitions: Walk <-> Run
            var tWalkToRun = stWalk.AddTransition(stRun);
            tWalkToRun.hasExitTime = false;
            tWalkToRun.duration = 0.15f;
            tWalkToRun.AddCondition(AnimatorConditionMode.Greater, 3.5f, "Speed");

            var tRunToWalk = stRun.AddTransition(stWalk);
            tRunToWalk.hasExitTime = false;
            tRunToWalk.duration = 0.15f;
            tRunToWalk.AddCondition(AnimatorConditionMode.Less, 3.2f, "Speed");

            // Crouch transitions
            var tAnyToCrouch = rootSm.AddAnyStateTransition(stCrouchIdle);
            tAnyToCrouch.hasExitTime = false;
            tAnyToCrouch.duration = 0.2f;
            tAnyToCrouch.AddCondition(AnimatorConditionMode.If, 0f, "IsCrouching");
            tAnyToCrouch.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            var tCrouchIdleToWalk = stCrouchIdle.AddTransition(stCrouchWalk);
            tCrouchIdleToWalk.hasExitTime = false;
            tCrouchIdleToWalk.duration = 0.15f;
            tCrouchIdleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");

            var tCrouchWalkToIdle = stCrouchWalk.AddTransition(stCrouchIdle);
            tCrouchWalkToIdle.hasExitTime = false;
            tCrouchWalkToIdle.duration = 0.15f;
            tCrouchWalkToIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            var tCrouchToStand = stCrouchIdle.AddTransition(stIdle);
            tCrouchToStand.hasExitTime = false;
            tCrouchToStand.duration = 0.2f;
            tCrouchToStand.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");

            var tCrouchWalkToStand = stCrouchWalk.AddTransition(stWalk);
            tCrouchWalkToStand.hasExitTime = false;
            tCrouchWalkToStand.duration = 0.2f;
            tCrouchWalkToStand.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");

            // Any -> MeleePush
            var tAnyToMelee = rootSm.AddAnyStateTransition(stMelee);
            tAnyToMelee.hasExitTime = false;
            tAnyToMelee.duration = 0.1f;
            tAnyToMelee.AddCondition(AnimatorConditionMode.If, 0f, "TriggerThrow");

            var tMeleeToIdle = stMelee.AddTransition(stIdle);
            tMeleeToIdle.hasExitTime = true;
            tMeleeToIdle.exitTime = 0.85f;
            tMeleeToIdle.duration = 0.15f;

            // Any -> Death
            var tAnyToDead = rootSm.AddAnyStateTransition(stDead);
            tAnyToDead.hasExitTime = false;
            tAnyToDead.duration = 0.2f;
            tAnyToDead.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        public static AnimatorController CreateZombieAnimatorController()
        {
            EnsureDirectories();

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ZombieCtrlPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ZombieCtrlPath);
            }

            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "TriggerAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "TriggerScream", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine rootSm = controller.layers[0].stateMachine;

            foreach (var state in rootSm.states)
            {
                rootSm.RemoveState(state.state);
            }

            AnimationClip zIdleClip = GetOrCreateClip("Zombie_Idle", 2.4f, 0.03f, 5f);
            AnimationClip zWalkClip = GetOrCreateClip("Zombie_Walk", 1.2f, 0.07f, 20f);
            AnimationClip zRunClip = GetOrCreateClip("Zombie_Run", 0.7f, 0.11f, 35f);
            AnimationClip zAttackClip = GetOrCreateClip("Zombie_Attack", 0.8f, 0.04f, 55f);
            AnimationClip zScreamClip = GetOrCreateClip("Zombie_Scream", 1.4f, 0.08f, 15f);
            AnimationClip zDeathClip = GetOrCreateClip("Zombie_Death", 1.6f, 0f, 0f, -0.6f, 85f);

            AnimatorState stIdle = rootSm.AddState("Idle");
            stIdle.motion = zIdleClip;

            AnimatorState stWalk = rootSm.AddState("Walk");
            stWalk.motion = zWalkClip;

            AnimatorState stRun = rootSm.AddState("Run");
            stRun.motion = zRunClip;

            AnimatorState stAttack = rootSm.AddState("Attack");
            stAttack.motion = zAttackClip;

            AnimatorState stScream = rootSm.AddState("Scream");
            stScream.motion = zScreamClip;

            AnimatorState stDeath = rootSm.AddState("Death");
            stDeath.motion = zDeathClip;

            rootSm.defaultState = stIdle;

            // Idle <-> Walk
            var tIdleToWalk = stIdle.AddTransition(stWalk);
            tIdleToWalk.hasExitTime = false;
            tIdleToWalk.duration = 0.2f;
            tIdleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");

            var tWalkToIdle = stWalk.AddTransition(stIdle);
            tWalkToIdle.hasExitTime = false;
            tWalkToIdle.duration = 0.2f;
            tWalkToIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            // Walk <-> Run
            var tWalkToRun = stWalk.AddTransition(stRun);
            tWalkToRun.hasExitTime = false;
            tWalkToRun.duration = 0.2f;
            tWalkToRun.AddCondition(AnimatorConditionMode.Greater, 2.8f, "Speed");

            var tRunToWalk = stRun.AddTransition(stWalk);
            tRunToWalk.hasExitTime = false;
            tRunToWalk.duration = 0.2f;
            tRunToWalk.AddCondition(AnimatorConditionMode.Less, 2.5f, "Speed");

            // Attack
            var tAnyToAttack = rootSm.AddAnyStateTransition(stAttack);
            tAnyToAttack.hasExitTime = false;
            tAnyToAttack.duration = 0.1f;
            tAnyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "TriggerAttack");

            var tAttackToWalk = stAttack.AddTransition(stWalk);
            tAttackToWalk.hasExitTime = true;
            tAttackToWalk.exitTime = 0.85f;
            tAttackToWalk.duration = 0.2f;

            // Scream
            var tAnyToScream = rootSm.AddAnyStateTransition(stScream);
            tAnyToScream.hasExitTime = false;
            tAnyToScream.duration = 0.15f;
            tAnyToScream.AddCondition(AnimatorConditionMode.If, 0f, "TriggerScream");

            var tScreamToIdle = stScream.AddTransition(stIdle);
            tScreamToIdle.hasExitTime = true;
            tScreamToIdle.exitTime = 0.90f;
            tScreamToIdle.duration = 0.2f;

            // Death
            var tAnyToDeath = rootSm.AddAnyStateTransition(stDeath);
            tAnyToDeath.hasExitTime = false;
            tAnyToDeath.duration = 0.25f;
            tAnyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void EnsureParameter(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in ctrl.parameters)
            {
                if (p.name == name) return;
            }
            ctrl.AddParameter(name, type);
        }

        private static AnimationClip GetOrCreateClip(string clipName, float length, float bobAmount, float armSwingAngle, float yOffset = 0f, float pitchAngle = 0f)
        {
            string path = $"{ClipFolder}/{clipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                clip.name = clipName;
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();

            // Loop Settings
            bool isLooping = !clipName.Contains("Death") && !clipName.Contains("Attack") && !clipName.Contains("MeleePush");
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = isLooping;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Curve: Vertical Bobbing on Body_Hips
            Keyframe[] bobKeys = new Keyframe[]
            {
                new Keyframe(0f, 0.88f + yOffset),
                new Keyframe(length * 0.25f, 0.88f + yOffset + bobAmount),
                new Keyframe(length * 0.50f, 0.88f + yOffset),
                new Keyframe(length * 0.75f, 0.88f + yOffset + bobAmount * 0.8f),
                new Keyframe(length, 0.88f + yOffset)
            };
            clip.SetCurve("Body_Hips", typeof(Transform), "m_LocalPosition.y", new AnimationCurve(bobKeys));

            // Curve: Pitch / Collapse on Death
            if (pitchAngle != 0f)
            {
                Keyframe[] pitchKeys = new Keyframe[]
                {
                    new Keyframe(0f, 0f),
                    new Keyframe(length * 0.6f, pitchAngle * 0.8f),
                    new Keyframe(length, pitchAngle)
                };
                clip.SetCurve("Body_Hips", typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(pitchKeys));
            }

            // Curve: Arm swings if nonzero
            if (armSwingAngle > 0f)
            {
                Keyframe[] armLKeys = new Keyframe[]
                {
                    new Keyframe(0f, -armSwingAngle),
                    new Keyframe(length * 0.5f, armSwingAngle),
                    new Keyframe(length, -armSwingAngle)
                };
                Keyframe[] armRKeys = new Keyframe[]
                {
                    new Keyframe(0f, armSwingAngle),
                    new Keyframe(length * 0.5f, -armSwingAngle),
                    new Keyframe(length, armSwingAngle)
                };
                clip.SetCurve("Body_Hips/Torso/Shoulder_L", typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(armLKeys));
                clip.SetCurve("Body_Hips/Torso/Shoulder_R", typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(armRKeys));

                // Natural Leg Swing opposite to arms
                float legAngle = armSwingAngle * 0.65f;
                Keyframe[] legLKeys = new Keyframe[]
                {
                    new Keyframe(0f, legAngle),
                    new Keyframe(length * 0.5f, -legAngle),
                    new Keyframe(length, legAngle)
                };
                Keyframe[] legRKeys = new Keyframe[]
                {
                    new Keyframe(0f, -legAngle),
                    new Keyframe(length * 0.5f, legAngle),
                    new Keyframe(length, -legAngle)
                };
                clip.SetCurve("Body_Hips/Hip_L", typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(legLKeys));
                clip.SetCurve("Body_Hips/Hip_R", typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(legRKeys));
            }

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(AnimFolder)) Directory.CreateDirectory(AnimFolder);
            if (!Directory.Exists(ClipFolder)) Directory.CreateDirectory(ClipFolder);
        }
    }
}
