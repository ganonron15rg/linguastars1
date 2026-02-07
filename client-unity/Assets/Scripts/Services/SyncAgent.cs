using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LinguaStars.Client.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace LinguaStars.Client.Services
{
    public class SyncAgent : MonoBehaviour
    {
        private const string ChildTokenKey = "ls_child_token";
        private const string ChildIdKey = "ls_child_id";
        private const string ServerUrlKey = "ls_server_url";

        [SerializeField] private string serverBaseUrl = "http://localhost:8000";
        [SerializeField] private int childId;
        [SerializeField] private float syncIntervalSeconds = 10f;
        [SerializeField] private int maxBatchSize = 10;
        [SerializeField] private float minBackoffSeconds = 2f;
        [SerializeField] private float maxBackoffSeconds = 60f;
        [SerializeField] private bool autoStart = true;

        private bool syncing;
        private int failureCount;
        private float nextAttemptAfter;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (!string.IsNullOrEmpty(serverBaseUrl) && string.IsNullOrEmpty(PlayerPrefs.GetString(ServerUrlKey, string.Empty)))
            {
                PlayerPrefs.SetString(ServerUrlKey, serverBaseUrl);
            }

            if (childId > 0 && PlayerPrefs.GetInt(ChildIdKey, 0) == 0)
            {
                PlayerPrefs.SetInt(ChildIdKey, childId);
            }
        }

        private void Start()
        {
            if (autoStart)
            {
                StartCoroutine(SyncLoop());
            }
        }

        public void SetChildToken(string token, int tokenChildId)
        {
            PlayerPrefs.SetString(ChildTokenKey, token ?? string.Empty);
            if (tokenChildId > 0)
            {
                PlayerPrefs.SetInt(ChildIdKey, tokenChildId);
            }
        }

        public string GetChildToken()
        {
            return PlayerPrefs.GetString(ChildTokenKey, string.Empty);
        }

        public int GetChildId()
        {
            return PlayerPrefs.GetInt(ChildIdKey, childId);
        }

        public void SetServerUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                PlayerPrefs.SetString(ServerUrlKey, url);
            }
        }

        public string GetServerUrl()
        {
            return PlayerPrefs.GetString(ServerUrlKey, serverBaseUrl);
        }

        private IEnumerator SyncLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(syncIntervalSeconds);

                if (syncing)
                {
                    continue;
                }

                if (Time.unscaledTime < nextAttemptAfter)
                {
                    continue;
                }

                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    continue;
                }

                PlayerDataStore store = PlayerDataStore.Instance;
                if (store == null)
                {
                    continue;
                }

                string token = GetChildToken();
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                List<SyncAction> actions = store.GetPendingSyncActions(maxBatchSize);
                if (actions.Count == 0)
                {
                    continue;
                }

                syncing = true;
                yield return StartCoroutine(UploadBatch(store, actions, token));
                syncing = false;
            }
        }

        private IEnumerator UploadBatch(PlayerDataStore store, List<SyncAction> actions, string token)
        {
            int currentChildId = GetChildId();
            if (currentChildId <= 0)
            {
                yield break;
            }

            SyncBatchPayload payload = BuildPayload(store, actions, currentChildId);
            if (payload == null)
            {
                yield break;
            }

            string jsonBody = BuildPayloadJson(payload);
            if (string.IsNullOrEmpty(jsonBody))
            {
                yield break;
            }

            string endpoint = $"{GetServerUrl().TrimEnd('/')}/sync/upload";
            using (UnityWebRequest request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {token}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    foreach (SyncAction action in actions)
                    {
                        store.RemoveSyncAction(action.actionId);
                        if (action.actionType == "session_record")
                        {
                            SessionRecord record = JsonUtility.FromJson<SessionRecord>(action.payloadJson);
                            if (record != null)
                            {
                                store.RemoveSessionRecord(record.sessionId);
                            }
                        }
                    }

                    failureCount = 0;
                    nextAttemptAfter = Time.unscaledTime;
                }
                else
                {
                    failureCount += 1;
                    float backoff = Mathf.Min(maxBackoffSeconds, minBackoffSeconds * Mathf.Pow(2f, failureCount - 1));
                    nextAttemptAfter = Time.unscaledTime + backoff;
                }
            }
        }

        private SyncBatchPayload BuildPayload(PlayerDataStore store, List<SyncAction> actions, int currentChildId)
        {
            if (store == null || store.State == null)
            {
                return null;
            }

            bool includeProgress = false;
            bool includeInventory = false;
            List<SessionRecord> sessions = new List<SessionRecord>();
            List<SyncTransactionPayload> transactions = new List<SyncTransactionPayload>();

            foreach (SyncAction action in actions)
            {
                switch (action.actionType)
                {
                    case "progress_update":
                    case "item_mastery_update":
                    case "progress_reset":
                        includeProgress = true;
                        break;
                    case "coins_add":
                    case "coins_spend":
                        includeInventory = true;
                        transactions.Add(BuildTransaction(action));
                        break;
                    case "inventory_add":
                    case "item_equip":
                        includeInventory = true;
                        break;
                    case "session_record":
                        SessionRecord record = JsonUtility.FromJson<SessionRecord>(action.payloadJson);
                        if (record != null)
                        {
                            sessions.Add(record);
                        }
                        break;
                }
            }

            List<SyncProgressPayload> progress = new List<SyncProgressPayload>();
            if (includeProgress)
            {
                foreach (LevelMastery mastery in store.State.progress.mastery)
                {
                    progress.Add(new SyncProgressPayload
                    {
                        levelId = mastery.levelId,
                        mastery = Mathf.RoundToInt(mastery.masteryPercent),
                        attempts = 0,
                        errors = 0,
                        lastSeenUnix = null
                    });
                }
            }

            SyncInventoryPayload inventory = null;
            if (includeInventory)
            {
                inventory = new SyncInventoryPayload
                {
                    coinsBalance = store.State.economy.coins,
                    ownedItems = store.State.economy.inventory,
                    equippedItems = store.State.economy.equippedItems
                };
            }

            return new SyncBatchPayload
            {
                idempotencyKey = actions[0].actionId,
                childId = currentChildId,
                progress = progress,
                sessions = sessions,
                economyTransactions = transactions,
                inventory = inventory
            };
        }

        private SyncTransactionPayload BuildTransaction(SyncAction action)
        {
            CoinPayload payload = JsonUtility.FromJson<CoinPayload>(action.payloadJson);
            string type = action.actionType == "coins_spend" ? "spend" : "earn";
            return new SyncTransactionPayload
            {
                type = type,
                amount = payload != null ? payload.amount : 0,
                reason = payload != null ? payload.reason : string.Empty,
                timestampUnix = action.createdAtUnix
            };
        }

        private string BuildPayloadJson(SyncBatchPayload payload)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append('{');
            builder.Append("\"idempotency_key\":\"").Append(Escape(payload.idempotencyKey)).Append("\",");
            builder.Append("\"child_id\":").Append(payload.childId).Append(',');
            AppendProgress(builder, payload.progress);
            builder.Append(',');
            AppendSessions(builder, payload.sessions);
            builder.Append(',');
            AppendTransactions(builder, payload.economyTransactions);
            builder.Append(',');
            AppendInventory(builder, payload.inventory);
            builder.Append('}');
            return builder.ToString();
        }

        private void AppendProgress(StringBuilder builder, List<SyncProgressPayload> progress)
        {
            builder.Append("\"progress\":[");
            for (int i = 0; i < progress.Count; i++)
            {
                SyncProgressPayload entry = progress[i];
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append('{');
                builder.Append("\"level_id\":\"").Append(Escape(entry.levelId)).Append("\",");
                builder.Append("\"mastery\":").Append(entry.mastery).Append(',');
                builder.Append("\"attempts\":").Append(entry.attempts).Append(',');
                builder.Append("\"errors\":").Append(entry.errors).Append(',');
                builder.Append("\"last_seen\":null");
                builder.Append('}');
            }
            builder.Append(']');
        }

        private void AppendSessions(StringBuilder builder, List<SessionRecord> sessions)
        {
            builder.Append("\"sessions\":[");
            for (int i = 0; i < sessions.Count; i++)
            {
                SessionRecord record = sessions[i];
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append('{');
                builder.Append("\"date\":\"").Append(UnixToIso(record.dateUnix)).Append("\",");
                builder.Append("\"duration_seconds\":").Append(record.durationSeconds).Append(',');
                builder.Append("\"success_rate\":").Append(record.successRate).Append(',');
                builder.Append("\"activities_done\":").Append(record.activitiesDone);
                builder.Append('}');
            }
            builder.Append(']');
        }

        private void AppendTransactions(StringBuilder builder, List<SyncTransactionPayload> transactions)
        {
            builder.Append("\"economy_transactions\":[");
            for (int i = 0; i < transactions.Count; i++)
            {
                SyncTransactionPayload record = transactions[i];
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append('{');
                builder.Append("\"type\":\"").Append(Escape(record.type)).Append("\",");
                builder.Append("\"amount\":").Append(record.amount).Append(',');
                builder.Append("\"meta_json\":{");
                builder.Append("\"reason\":\"").Append(Escape(record.reason)).Append("\"");
                builder.Append("},");
                builder.Append("\"timestamp\":\"").Append(UnixToIso(record.timestampUnix)).Append("\"");
                builder.Append('}');
            }
            builder.Append(']');
        }

        private void AppendInventory(StringBuilder builder, SyncInventoryPayload inventory)
        {
            if (inventory == null)
            {
                builder.Append("\"inventory\":null");
                return;
            }

            builder.Append("\"inventory\":{");
            builder.Append("\"coins_balance\":").Append(inventory.coinsBalance).Append(',');
            builder.Append("\"owned_items_json\":");
            AppendOwnedItems(builder, inventory.ownedItems);
            builder.Append(',');
            builder.Append("\"equipped_json\":");
            AppendEquippedItems(builder, inventory.equippedItems);
            builder.Append('}');
        }

        private void AppendOwnedItems(StringBuilder builder, List<InventoryItem> items)
        {
            builder.Append('{');
            builder.Append("\"items\":[");
            for (int i = 0; i < items.Count; i++)
            {
                InventoryItem item = items[i];
                if (i > 0)
                {
                    builder.Append(',');
                }
                builder.Append('{');
                builder.Append("\"item_id\":\"").Append(Escape(item.itemId)).Append("\",");
                builder.Append("\"quantity\":").Append(item.quantity);
                builder.Append('}');
            }
            builder.Append("]}");
        }

        private void AppendEquippedItems(StringBuilder builder, List<string> items)
        {
            builder.Append('{');
            builder.Append("\"items\":[");
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }
                builder.Append('"').Append(Escape(items[i])).Append('"');
            }
            builder.Append("]}");
        }

        private string UnixToIso(long unixTime)
        {
            if (unixTime <= 0)
            {
                unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        }

        private string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        [Serializable]
        private class CoinPayload
        {
            public int amount;
            public string reason;
        }

        private class SyncBatchPayload
        {
            public string idempotencyKey;
            public int childId;
            public List<SyncProgressPayload> progress;
            public List<SessionRecord> sessions;
            public List<SyncTransactionPayload> economyTransactions;
            public SyncInventoryPayload inventory;
        }

        private class SyncProgressPayload
        {
            public string levelId;
            public int mastery;
            public int attempts;
            public int errors;
            public long? lastSeenUnix;
        }

        private class SyncTransactionPayload
        {
            public string type;
            public int amount;
            public string reason;
            public long timestampUnix;
        }

        private class SyncInventoryPayload
        {
            public int coinsBalance;
            public List<InventoryItem> ownedItems;
            public List<string> equippedItems;
        }
    }
}
