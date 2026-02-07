using System;
using System.Collections.Generic;
using System.Linq;
using LinguaStars.Client.Data;
using UnityEngine;

namespace LinguaStars.Client.Activities
{
    public class ActivityEngine
    {
        private const int MaxLives = 3;
        private readonly PlayerDataStore dataStore;
        private readonly ActivityScoringRules scoringRules;
        private int successStreak;
        private int streakBonusCoins;

        public ActivityEngine(PlayerDataStore dataStore, ActivityScoringRules scoringRules)
        {
            this.dataStore = dataStore;
            this.scoringRules = scoringRules;
        }

        public ActivitySessionSummary RunSession(LevelDefinition level, List<ActivityDefinition> activities, bool applyLevelRewards = true)
        {
            int lives = MaxLives;
            int coinsEarned = 0;
            int completed = 0;
            int failed = 0;
            float totalAccuracy = 0f;
            float totalDuration = 0f;
            int countForAverage = 0;
            successStreak = 0;
            streakBonusCoins = 0;

            if (activities == null)
            {
                activities = level != null ? level.activities : new List<ActivityDefinition>();
            }

            foreach (ActivityDefinition definition in activities)
            {
                ActivityBase activity = ActivityFactory.Create(definition);
                if (!activity.IsPlayable)
                {
                    continue;
                }

                activity.StartActivity();
                ActivityResult result = ExecuteActivity(activity, definition);
                if (result == null)
                {
                    continue;
                }

                totalAccuracy += result.Accuracy;
                totalDuration += result.DurationSeconds;
                countForAverage += 1;

                if (result.Success)
                {
                    completed += 1;
                    int baseCoins = result.UsedRetry ? 2 : 4;
                    coinsEarned += baseCoins;
                    successStreak += 1;
                    if (successStreak % 3 == 0)
                    {
                        coinsEarned += 6;
                        streakBonusCoins += 6;
                    }
                }
                else
                {
                    failed += 1;
                    successStreak = 0;
                    lives -= 1;
                    if (lives <= 0)
                    {
                        break;
                    }
                }

                if (dataStore != null)
                {
                    dataStore.UpdateItemMastery(definition.itemId, result.Accuracy, result.Success, result.DurationSeconds);
                }
            }

            float averageAccuracy = countForAverage > 0 ? totalAccuracy / countForAverage : 0f;
            float averageDuration = countForAverage > 0 ? totalDuration / countForAverage : 0f;
            int stars = scoringRules.CalculateStars(averageAccuracy, averageDuration);
            float masteryPercent = scoringRules.CalculateMasteryPercent(averageAccuracy, averageDuration);
            if (applyLevelRewards)
            {
                coinsEarned += 20;
                if (stars == 2)
                {
                    coinsEarned += 10;
                }
                else if (stars >= 3)
                {
                    coinsEarned += 20;
                }
            }

            return new ActivitySessionSummary
            {
                LevelId = level?.levelId,
                StarsEarned = stars,
                MasteryPercent = masteryPercent,
                LivesRemaining = lives,
                CoinsEarned = coinsEarned,
                ActivitiesCompleted = completed,
                ActivitiesFailed = failed,
                AverageAccuracy = averageAccuracy,
                AverageDurationSeconds = averageDuration,
                SuccessStreakBonusCoins = streakBonusCoins
            };
        }

        private ActivityResult ExecuteActivity(ActivityBase activity, ActivityDefinition definition)
        {
            ActivitySubmission firstAttempt = ActivityAutoPlayer.BuildSubmission(definition, correct: !definition.forceRetry);
            ActivityResult result = activity.SubmitAnswer(firstAttempt);
            if (result != null)
            {
                result.DurationSeconds = Mathf.Max(result.DurationSeconds, definition.estimatedDurationSeconds);
                return result;
            }

            ActivitySubmission retryAttempt = ActivityAutoPlayer.BuildSubmission(definition, correct: true);
            ActivityResult retryResult = activity.SubmitAnswer(retryAttempt);
            if (retryResult != null)
            {
                retryResult.DurationSeconds = Mathf.Max(retryResult.DurationSeconds, definition.estimatedDurationSeconds + 10f);
            }

            return retryResult;
        }
    }

    public class ActivityScoringRules
    {
        public float AccuracyThreeStar = 0.9f;
        public float AccuracyTwoStar = 0.75f;
        public float DurationThreeStar = 35f;
        public float DurationTwoStar = 55f;

        public int CalculateStars(float accuracy, float avgDurationSeconds)
        {
            if (accuracy >= AccuracyThreeStar && avgDurationSeconds <= DurationThreeStar)
            {
                return 3;
            }

            if (accuracy >= AccuracyTwoStar && avgDurationSeconds <= DurationTwoStar)
            {
                return 2;
            }

            return 1;
        }

        public float CalculateMasteryPercent(float accuracy, float avgDurationSeconds)
        {
            float accuracyScore = Mathf.Clamp01(accuracy);
            float timeScore = Mathf.Clamp01(1f - (avgDurationSeconds / 90f));
            return Mathf.Round((accuracyScore * 70f + timeScore * 30f) * 100f) / 100f;
        }
    }

    public static class ActivityAutoPlayer
    {
        public static ActivitySubmission BuildSubmission(ActivityDefinition definition, bool correct)
        {
            ActivitySubmission submission = new ActivitySubmission();

            switch (definition.type)
            {
                case ActivityType.MatchPairs:
                    submission.PairMatches = new Dictionary<string, string>();
                    if (definition.pairLeft != null && definition.pairRight != null)
                    {
                        if (definition.pairRight.Length == 0)
                        {
                            break;
                        }

                        for (int i = 0; i < definition.pairLeft.Length; i++)
                        {
                            string left = definition.pairLeft[i];
                            string right = definition.pairRight[Mathf.Min(i, definition.pairRight.Length - 1)];
                            submission.PairMatches[left] = correct ? right : definition.pairRight.Last();
                        }
                    }
                    break;
                case ActivityType.BuildSentence:
                    submission.Responses = correct
                        ? definition.targetSentence?.Split(' ').ToList()
                        : new List<string> { "wrong", "order" };
                    break;
                default:
                    submission.Response = correct ? definition.correctAnswer : "wrong";
                    break;
            }

            return submission;
        }
    }
}
