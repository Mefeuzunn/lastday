using System;
using UnityEngine;
using ProjectSurvival.Narrative;

namespace ProjectSurvival.Missions
{
    [Serializable]
    public class DynamicObjective
    {
        public MissionTriggerData data;
        public float currentProgress = 0f;
        public bool isCompleted = false;
        public bool isFailed = false;

        public DynamicObjective(MissionTriggerData data)
        {
            this.data = data;
            this.currentProgress = 0f;
            this.isCompleted = false;
            this.isFailed = false;
        }

        public float RequiredAmount
        {
            get
            {
                if (data == null) return 1f;
                if (data.missionType == "SurviveTimer") return data.duration;
                if (data.missionType == "CollectItem") return data.targetCount;
                return 1f;
            }
        }
    }
}
