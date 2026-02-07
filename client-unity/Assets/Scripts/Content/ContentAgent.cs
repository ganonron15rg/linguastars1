using System;
using System.Collections.Generic;
using System.IO;
using LinguaStars.Client.Activities;
using UnityEngine;

namespace LinguaStars.Client.Content
{
    public class ContentAgent
    {
        private const string ContentRootFolder = "Content";
        private const string WordsPath = "content/words_en.json";
        private const string LevelsPath = "content/levels_en.json";
        private const string SentencesPath = "content/sentences_en.json";
        private const string DialoguesPath = "content/dialogues_en.json";
        private const string ShopItemsPath = "shop/items.json";
        private const string ShopSetsPath = "shop/sets.json";
        private const string EconomyRulesPath = "economy/rules.json";

        private static ContentAgent instance;
        private ContentBundle cachedBundle;

        public static ContentAgent Instance => instance ??= new ContentAgent();

        public ContentBundle LoadContentBundle()
        {
            if (cachedBundle != null)
            {
                return cachedBundle;
            }

            ContentBundle bundle = new ContentBundle
            {
                Words = LoadJson<WordContentFile>(WordsPath),
                Levels = LoadJson<LevelContentFile>(LevelsPath),
                Sentences = LoadJson<SentenceContentFile>(SentencesPath),
                Dialogues = LoadJson<DialogueContentFile>(DialoguesPath),
                ShopItems = LoadJson<ShopItemsFile>(ShopItemsPath),
                ShopSets = LoadJson<ShopSetsFile>(ShopSetsPath),
                EconomyRules = LoadJson<EconomyRulesFile>(EconomyRulesPath)
            };

            cachedBundle = bundle;
            return bundle;
        }

        public ContentPack BuildContentPack()
        {
            ContentBundle bundle = LoadContentBundle();
            ContentPack pack = new ContentPack
            {
                packId = "english-v1",
                title = "English V1",
                heTitle = "אנגלית V1"
            };

            if (bundle.Levels?.levels != null)
            {
                foreach (LevelContent level in bundle.Levels.levels)
                {
                    pack.levels.Add(ConvertLevel(level));
                }
            }

            pack.levels.Add(BuildFreePlayLevel(bundle));
            return pack;
        }

        public IReadOnlyList<ShopItem> LoadShopItems()
        {
            return LoadContentBundle().ShopItems?.items ?? new List<ShopItem>();
        }

        public IReadOnlyList<ShopSet> LoadShopSets()
        {
            return LoadContentBundle().ShopSets?.sets ?? new List<ShopSet>();
        }

        private static LevelDefinition ConvertLevel(LevelContent level)
        {
            LevelDefinition definition = new LevelDefinition
            {
                levelId = level.level_id,
                title = level.title?.en ?? level.level_id,
                heTitle = level.title?.he ?? level.level_id,
                description = string.Empty,
                heDescription = string.Empty
            };

            if (level.activities != null)
            {
                foreach (LevelActivity activity in level.activities)
                {
                    definition.activities.Add(ConvertActivity(activity));
                }
            }

            return definition;
        }

        private static ActivityDefinition ConvertActivity(LevelActivity activity)
        {
            ActivityPayload payload = activity.payload ?? new ActivityPayload();
            return new ActivityDefinition
            {
                activityId = activity.activity_id,
                type = ParseActivityType(activity.type),
                prompt = activity.prompt?.en,
                hePrompt = activity.prompt?.he,
                itemId = payload.item_id,
                correctAnswer = payload.correct_answer,
                heCorrectAnswer = payload.he_correct_answer,
                options = payload.options,
                heOptions = payload.he_options,
                tokens = payload.tokens,
                heTokens = payload.he_tokens,
                pairLeft = payload.pair_left,
                pairRight = payload.pair_right,
                targetSentence = payload.target_sentence,
                heTargetSentence = payload.he_target_sentence,
                estimatedDurationSeconds = activity.estimated_duration_seconds,
                forceRetry = activity.force_retry
            };
        }

        private static LevelDefinition BuildFreePlayLevel(ContentBundle bundle)
        {
            LevelDefinition level = new LevelDefinition
            {
                levelId = "freeplay",
                title = "Free Play Mix",
                heTitle = "משחק חופשי",
                description = "Mixed practice pack",
                heDescription = "חבילת תרגול מעורבת"
            };

            List<WordEntry> words = bundle.Words?.words ?? new List<WordEntry>();
            List<SentenceEntry> sentences = bundle.Sentences?.sentences ?? new List<SentenceEntry>();
            if (words.Count == 0)
            {
                return level;
            }

            int[] picks = { 0, 1, 2, 3, 4 };
            for (int i = 0; i < picks.Length; i++)
            {
                if (picks[i] >= words.Count)
                {
                    picks[i] = words.Count - 1;
                }
            }

            WordEntry first = words[picks[0]];
            WordEntry second = words[picks[1]];
            WordEntry third = words[picks[2]];
            WordEntry fourth = words[picks[3]];
            WordEntry fifth = words[picks[4]];

            level.activities.Add(new ActivityDefinition
            {
                activityId = "freeplay-hear-1",
                type = ActivityType.HearTap,
                prompt = $"Tap the word you hear: {first.en}",
                hePrompt = $"גע במילה שאתה שומע: {first.he}",
                itemId = first.id,
                correctAnswer = first.en,
                heCorrectAnswer = first.he,
                options = new[] { first.en, second.en, third.en },
                heOptions = new[] { first.he, second.he, third.he },
                estimatedDurationSeconds = 20f,
                forceRetry = true
            });

            level.activities.Add(new ActivityDefinition
            {
                activityId = "freeplay-match-1",
                type = ActivityType.MatchPairs,
                prompt = "Match the pairs",
                hePrompt = "התאם זוגות",
                itemId = "freeplay-pairs",
                pairLeft = new[] { second.en, third.en, fourth.en },
                pairRight = new[] { second.he, third.he, fourth.he },
                estimatedDurationSeconds = 30f
            });

            level.activities.Add(new ActivityDefinition
            {
                activityId = "freeplay-choose-1",
                type = ActivityType.ChooseCorrect,
                prompt = $"Choose the correct word: {fifth.en}",
                hePrompt = $"בחר את המילה הנכונה: {fifth.he}",
                itemId = fifth.id,
                correctAnswer = fifth.en,
                heCorrectAnswer = fifth.he,
                options = new[] { fifth.en, second.en, third.en },
                heOptions = new[] { fifth.he, second.he, third.he },
                estimatedDurationSeconds = 25f,
                forceRetry = true
            });

            if (sentences.Count > 0)
            {
                SentenceEntry sentence = sentences[0];
                string[] tokens = sentence.en.Replace(".", string.Empty).Split(' ');
                string[] heTokens = sentence.he.Replace(".", string.Empty).Split(' ');
                level.activities.Add(new ActivityDefinition
                {
                    activityId = "freeplay-sentence-1",
                    type = ActivityType.BuildSentence,
                    prompt = "Build the sentence",
                    hePrompt = "בנה את המשפט",
                    itemId = sentence.id,
                    targetSentence = sentence.en.Replace(".", string.Empty),
                    heTargetSentence = sentence.he,
                    tokens = tokens,
                    heTokens = heTokens,
                    estimatedDurationSeconds = 40f
                });
            }

            return level;
        }

        private static ActivityType ParseActivityType(string type)
        {
            return type switch
            {
                "hear_tap" => ActivityType.HearTap,
                "match_pairs" => ActivityType.MatchPairs,
                "build_word" => ActivityType.BuildWord,
                "choose_correct" => ActivityType.ChooseCorrect,
                "build_sentence" => ActivityType.BuildSentence,
                "listen_choose" => ActivityType.ListenChoose,
                "listen_type" => ActivityType.ListenType,
                "speak" => ActivityType.Speak,
                _ => ActivityType.ChooseCorrect
            };
        }

        private static T LoadJson<T>(string relativePath) where T : class
        {
            string root = ResolveContentRoot();
            string filePath = Path.Combine(root, relativePath);
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Content file not found: {filePath}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load content file {filePath}: {ex.Message}");
                return null;
            }
        }

        private static string ResolveContentRoot()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, ContentRootFolder);
        }
    }

    public class ContentBundle
    {
        public WordContentFile Words;
        public LevelContentFile Levels;
        public SentenceContentFile Sentences;
        public DialogueContentFile Dialogues;
        public ShopItemsFile ShopItems;
        public ShopSetsFile ShopSets;
        public EconomyRulesFile EconomyRules;
    }
}
