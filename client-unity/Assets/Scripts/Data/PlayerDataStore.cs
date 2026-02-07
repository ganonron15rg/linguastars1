using System;
using System.Linq;
using UnityEngine;

namespace LinguaStars.Client.Data
{
    public class PlayerDataStore : MonoBehaviour
    {
        private const string FileName = "player_data.json";
        public static PlayerDataStore Instance { get; private set; }

        public PlayerDataState State { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Load()
        {
            State = JsonFileStorage.Load(FileName, CreateDefaultState);
        }

        public void Save()
        {
            JsonFileStorage.Save(FileName, State);
        }

        public void SaveProfile(string name, int age, string avatarBaseId)
        {
            State.profile.childId = string.IsNullOrEmpty(State.profile.childId) ? Guid.NewGuid().ToString("N") : State.profile.childId;
            State.profile.name = name;
            State.profile.age = age;
            State.profile.avatarBaseId = avatarBaseId;
            State.profile.createdAtUnix = State.profile.createdAtUnix == 0 ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : State.profile.createdAtUnix;
            Save();
            EnqueueSync("profile_update", JsonUtility.ToJson(State.profile));
        }

        public void UpdateProgress(int world, int stage, string levelId, int stars, float masteryPercent, bool completed)
        {
            State.progress.currentWorld = Mathf.Max(1, world);
            State.progress.currentStage = Mathf.Max(1, stage);
            State.progress.lastLevelId = levelId;

            LevelMastery mastery = State.progress.mastery.FirstOrDefault(item => item.levelId == levelId);
            if (mastery == null)
            {
                mastery = new LevelMastery { levelId = levelId };
                State.progress.mastery.Add(mastery);
            }

            mastery.starsEarned = Mathf.Max(mastery.starsEarned, stars);
            mastery.masteryPercent = Mathf.Max(mastery.masteryPercent, masteryPercent);
            mastery.completed = mastery.completed || completed;

            Save();
            EnqueueSync("progress_update", JsonUtility.ToJson(State.progress));
        }

        public void AddCoins(int amount, string reason)
        {
            if (amount <= 0)
            {
                return;
            }

            State.economy.coins += amount;
            Save();
            EnqueueSync("coins_add", $"{{\"amount\":{amount},\"reason\":\"{reason}\"}}");
        }

        public bool SpendCoins(int amount, string reason)
        {
            if (amount <= 0 || State.economy.coins < amount)
            {
                return false;
            }

            State.economy.coins -= amount;
            Save();
            EnqueueSync("coins_spend", $"{{\"amount\":{amount},\"reason\":\"{reason}\"}}");
            return true;
        }

        public void AddInventoryItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            {
                return;
            }

            InventoryItem item = State.economy.inventory.FirstOrDefault(entry => entry.itemId == itemId);
            if (item == null)
            {
                item = new InventoryItem { itemId = itemId, quantity = 0 };
                State.economy.inventory.Add(item);
            }

            item.quantity += quantity;
            Save();
            EnqueueSync("inventory_add", $"{{\"itemId\":\"{itemId}\",\"quantity\":{quantity}}}");
        }

        public void EquipItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            if (!State.economy.equippedItems.Contains(itemId))
            {
                State.economy.equippedItems.Add(itemId);
                Save();
                EnqueueSync("item_equip", $"{{\"itemId\":\"{itemId}\"}}");
            }
        }

        public void ResetProgress()
        {
            State.progress = new ProgressState();
            Save();
            EnqueueSync("progress_reset", "{}" );
        }

        public SyncAction PeekNextSyncAction()
        {
            return State.syncQueue.pendingActions.Count > 0 ? State.syncQueue.pendingActions[0] : null;
        }

        public void RemoveSyncAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
            {
                return;
            }

            State.syncQueue.pendingActions.RemoveAll(action => action.actionId == actionId);
            Save();
        }

        public void EnqueueSync(string actionType, string payloadJson)
        {
            SyncAction action = new SyncAction
            {
                actionId = Guid.NewGuid().ToString("N"),
                actionType = actionType,
                payloadJson = payloadJson,
                createdAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            State.syncQueue.pendingActions.Add(action);
            Save();
        }

        private PlayerDataState CreateDefaultState()
        {
            return new PlayerDataState
            {
                profile = new ChildProfile
                {
                    childId = string.Empty,
                    name = string.Empty,
                    age = 4,
                    language = "en",
                    avatarBaseId = "base-01",
                    createdAtUnix = 0
                },
                progress = new ProgressState(),
                economy = new EconomyState
                {
                    coins = 0,
                    inventory = new System.Collections.Generic.List<InventoryItem>(),
                    equippedItems = new System.Collections.Generic.List<string>()
                },
                syncQueue = new SyncQueueState()
            };
        }
    }
}
