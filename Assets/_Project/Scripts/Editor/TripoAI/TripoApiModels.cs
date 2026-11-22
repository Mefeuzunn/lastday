using System;
using UnityEngine;

namespace ProjectSurvival.EditorTools.TripoAI
{
    [Serializable]
    public class TripoTextToModelRequest
    {
        public string type = "text_to_model";
        public string prompt;
        public int face_limit = 10000;
        public bool texture = true;
        public bool pbr = true;
    }

    [Serializable]
    public class TripoRigRequest
    {
        public string type = "animate_rig";
        public string original_model_task_id;
    }

    [Serializable]
    public class TripoTaskResponse
    {
        public int code;
        public string message;
        public TripoTaskData data;
    }

    [Serializable]
    public class TripoTaskData
    {
        public string task_id;
        public string type;
        public string status; // "queued", "running", "success", "failed", "cancelled"
        public int progress;
        public TripoTaskOutput output;
    }

    [Serializable]
    public class TripoTaskOutput
    {
        public string model;
        public string pbr_model;
        public string base_model;
        public string rendered_image;

        public string GetBestModelUrl()
        {
            if (!string.IsNullOrEmpty(pbr_model)) return pbr_model;
            if (!string.IsNullOrEmpty(model)) return model;
            if (!string.IsNullOrEmpty(base_model)) return base_model;
            return string.Empty;
        }
    }

    public enum TripoPresetType
    {
        Custom,
        SurvivorPlayer,
        ZombieStandard,
        ZombieRunner,
        ZombieScreamer,
        ZombieTank,
        LootCrate,
        EnvironmentProp
    }

    public class TripoPreset
    {
        public string Title;
        public string Prompt;
        public string DefaultPrefabPath;
        public bool AutoRig;
        public int FaceLimit;
        public float TargetHeight;

        public TripoPreset(string title, string prompt, string prefabPath, bool autoRig, int faceLimit, float targetHeight)
        {
            Title = title;
            Prompt = prompt;
            DefaultPrefabPath = prefabPath;
            AutoRig = autoRig;
            FaceLimit = faceLimit;
            TargetHeight = targetHeight;
        }
    }
}
