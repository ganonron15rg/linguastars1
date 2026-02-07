using System;
using System.Collections.Generic;

namespace LinguaStars.Client.Activities
{
    [Serializable]
    public class ContentPack
    {
        public string packId;
        public string title;
        public string heTitle;
        public List<LevelDefinition> levels = new List<LevelDefinition>();
    }

    [Serializable]
    public class LevelDefinition
    {
        public string levelId;
        public string title;
        public string heTitle;
        public string description;
        public string heDescription;
        public List<ActivityDefinition> activities = new List<ActivityDefinition>();
    }

    [Serializable]
    public class ActivityDefinition
    {
        public string activityId;
        public ActivityType type;
        public string prompt;
        public string hePrompt;
        public string itemId;
        public string correctAnswer;
        public string heCorrectAnswer;
        public string[] options;
        public string[] heOptions;
        public string[] tokens;
        public string[] heTokens;
        public string[] pairLeft;
        public string[] pairRight;
        public string targetSentence;
        public string heTargetSentence;
        public float estimatedDurationSeconds = 30f;
        public bool forceRetry;
    }

    public class ActivitySubmission
    {
        public string Response;
        public List<string> Responses;
        public Dictionary<string, string> PairMatches;
    }

    public class ActivityResult
    {
        public string ActivityId;
        public ActivityType Type;
        public string ItemId;
        public bool Success;
        public bool UsedRetry;
        public int Attempts;
        public int CorrectCount;
        public int WrongCount;
        public float Accuracy;
        public float DurationSeconds;
    }

    public class ActivitySessionSummary
    {
        public string LevelId;
        public int StarsEarned;
        public float MasteryPercent;
        public int LivesRemaining;
        public int CoinsEarned;
        public int ActivitiesCompleted;
        public int ActivitiesFailed;
        public float AverageAccuracy;
        public float AverageDurationSeconds;
        public int SuccessStreakBonusCoins;
    }

    public class DailySessionPlan
    {
        public List<ActivityDefinition> WarmupActivities = new List<ActivityDefinition>();
        public LevelDefinition MainLevel;
    }
}
