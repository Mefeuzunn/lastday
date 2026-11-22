using System;
using System.Collections.Generic;

namespace ProjectSurvival.Narrative
{
    [Serializable]
    public class StatEffects
    {
        public float healthChange = 0f;
        public float hungerChange = 0f;
        public float thirstChange = 0f;
        public float infectionChange = 0f;
    }

    [Serializable]
    public class MissionTriggerData
    {
        public string missionId;
        public string missionType; // ReachZone, CollectItem, RescueNPC, SurviveTimer
        public string title;
        public string description;
        public string targetZoneId;
        public string targetItemId;
        public int targetCount = 1;
        public float duration = 30f;
        public string onSuccessNodeId;
        public string onFailNodeId;
    }

    [Serializable]
    public class TrustChange
    {
        public string npcId;
        public int amount;
    }

    [Serializable]
    public class StoryChoice
    {
        public string choiceText;
        public string requiredItemId;
        public int requiredItemCount = 1;
        public bool consumeItem = true;
        public List<string> requiredFlags = new List<string>();
        public List<string> forbiddenFlags = new List<string>();
        public string requiredTimeOfDay = "Any"; // "Any", "Day", "Night"
        public List<TrustChange> trustChanges = new List<TrustChange>();
        public int minTrustRequired = 0;
        public string trustNpcId = "";
        public StatEffects statEffects;
        public string giveItemId;
        public int giveItemCount = 1;
        public List<string> worldFlags = new List<string>();
        public string nextNodeId;
        public MissionTriggerData triggerMission;
    }

    [Serializable]
    public class StoryNode
    {
        public string nodeId;
        public string speakerName;
        public string dialogueText;
        public List<StoryChoice> choices = new List<StoryChoice>();
    }

    [Serializable]
    public class StoryContainer
    {
        public string storyId;
        public string storyTitle;
        public string startNodeId;
        public List<StoryNode> nodes = new List<StoryNode>();

        public StoryNode FindNode(string id)
        {
            return nodes.Find(n => n.nodeId == id);
        }
    }
}
