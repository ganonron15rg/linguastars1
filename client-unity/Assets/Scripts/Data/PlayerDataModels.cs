using System;
using System.Collections.Generic;
using UnityEngine;

namespace LinguaStars.Client.Data
{
    [Serializable]
    public class ChildProfile
    {
        public string childId;
        public string name;
        public int age;
        public string language = "en";
        public string avatarBaseId;
        public long createdAtUnix;
    }

    [Serializable]
    public class LevelMastery
    {
        public string levelId;
        public int starsEarned;
        public float masteryPercent;
        public bool completed;
    }

    [Serializable]
    public class ProgressState
    {
        public int currentWorld = 1;
        public int currentStage = 1;
        public string lastLevelId;
        public List<LevelMastery> mastery = new List<LevelMastery>();
    }

    [Serializable]
    public class InventoryItem
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class EconomyState
    {
        public int coins = 0;
        public List<InventoryItem> inventory = new List<InventoryItem>();
        public List<string> equippedItems = new List<string>();
    }

    [Serializable]
    public class SyncAction
    {
        public string actionId;
        public string actionType;
        public string payloadJson;
        public long createdAtUnix;
    }

    [Serializable]
    public class SyncQueueState
    {
        public List<SyncAction> pendingActions = new List<SyncAction>();
    }

    [Serializable]
    public class PlayerDataState
    {
        public ChildProfile profile = new ChildProfile();
        public ProgressState progress = new ProgressState();
        public EconomyState economy = new EconomyState();
        public SyncQueueState syncQueue = new SyncQueueState();
    }
}
