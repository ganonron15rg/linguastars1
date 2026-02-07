using System;
using System.Collections.Generic;

namespace LinguaStars.Client.Content
{
    [Serializable]
    public class LocalizedText
    {
        public string en;
        public string he;
    }

    [Serializable]
    public class WordContentFile
    {
        public string version;
        public List<WordEntry> words = new List<WordEntry>();
    }

    [Serializable]
    public class WordEntry
    {
        public string id;
        public string en;
        public string he;
        public string audio;
        public string image;
        public string[] tags;
    }

    [Serializable]
    public class SentenceContentFile
    {
        public string version;
        public List<SentenceEntry> sentences = new List<SentenceEntry>();
    }

    [Serializable]
    public class SentenceEntry
    {
        public string id;
        public string en;
        public string he;
        public string audio;
        public string[] tags;
    }

    [Serializable]
    public class DialogueContentFile
    {
        public string version;
        public List<DialogueEntry> dialogues = new List<DialogueEntry>();
    }

    [Serializable]
    public class DialogueEntry
    {
        public string id;
        public LocalizedText title;
        public List<DialogueLine> lines = new List<DialogueLine>();
        public string[] tags;
    }

    [Serializable]
    public class DialogueLine
    {
        public string speaker;
        public string en;
        public string he;
    }

    [Serializable]
    public class LevelContentFile
    {
        public string version;
        public List<LevelContent> levels = new List<LevelContent>();
    }

    [Serializable]
    public class LevelContent
    {
        public string level_id;
        public LocalizedText title;
        public List<LocalizedText> goals = new List<LocalizedText>();
        public string[] items;
        public List<LevelActivity> activities = new List<LevelActivity>();
        public LevelRewards rewards;
    }

    [Serializable]
    public class LevelActivity
    {
        public string activity_id;
        public string type;
        public LocalizedText prompt;
        public ActivityPayload payload;
        public float estimated_duration_seconds = 30f;
        public bool force_retry;
    }

    [Serializable]
    public class ActivityPayload
    {
        public string item_id;
        public string correct_answer;
        public string he_correct_answer;
        public string[] options;
        public string[] he_options;
        public string[] tokens;
        public string[] he_tokens;
        public string[] pair_left;
        public string[] pair_right;
        public string target_sentence;
        public string he_target_sentence;
        public string audio;
        public string image;
    }

    [Serializable]
    public class LevelRewards
    {
        public int coins;
        public int stars;
        public string[] items;
    }

    [Serializable]
    public class ShopItemsFile
    {
        public string version;
        public List<ShopItem> items = new List<ShopItem>();
    }

    [Serializable]
    public class ShopItem
    {
        public string id;
        public string set_name;
        public string name_en;
        public string name_he;
        public string category;
        public int price_coins;
    }

    [Serializable]
    public class ShopSetsFile
    {
        public string version;
        public List<ShopSet> sets = new List<ShopSet>();
    }

    [Serializable]
    public class ShopSet
    {
        public string id;
        public LocalizedText title;
        public string[] item_ids;
        public ShopSetReward reward;
    }

    [Serializable]
    public class ShopSetReward
    {
        public string type;
        public int amount;
    }

    [Serializable]
    public class EconomyRulesFile
    {
        public string version;
        public EconomyCoinRules coin_rules;
        public EconomyAntiFarmRules anti_farm;
    }

    [Serializable]
    public class EconomyCoinRules
    {
        public int activity_success_first_try;
        public int activity_success_retry;
        public int streak_bonus_3;
        public int level_end;
        public EconomyStarsBonus stars_bonus;
        public int daily_complete;
        public int streak_3_days;
        public int streak_7_days;
    }

    [Serializable]
    public class EconomyStarsBonus
    {
        public int two_stars;
        public int three_stars;
    }

    [Serializable]
    public class EconomyAntiFarmRules
    {
        public float same_level_same_day_multiplier;
        public string notes;
    }
}
