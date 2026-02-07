using System;
using LinguaStars.Client.Activities;
using LinguaStars.Client.Core;
using LinguaStars.Client.Data;
using UnityEngine;

namespace LinguaStars.Client.Screens
{
    public class ActivityPlayerHostScreen : ScreenBase
    {
        private string levelId;

        public override void OnShow(object context)
        {
            base.OnShow(context);
            if (context is string levelContext)
            {
                levelId = levelContext;
            }

            RunSession();
        }

        private void RunSession()
        {
            ContentPack pack = ContentPackLoader.LoadDefault();
            LevelDefinition level = ContentPackLoader.FindLevel(pack, levelId);
            if (level == null)
            {
                return;
            }

            ActivityEngine engine = new ActivityEngine(PlayerDataStore.Instance, new ActivityScoringRules());
            ActivitySessionSummary summary;

            if (!string.IsNullOrEmpty(levelId) && levelId.StartsWith("daily"))
            {
                DailySessionPlanner planner = new DailySessionPlanner();
                DailySessionPlan plan = planner.BuildPlan(pack, PlayerDataStore.Instance?.State);

                if (plan.WarmupActivities.Count > 0)
                {
                    engine.RunSession(level, plan.WarmupActivities, applyLevelRewards: false);
                }

                summary = engine.RunSession(plan.MainLevel, plan.MainLevel.activities, applyLevelRewards: true);
                level = plan.MainLevel;
            }
            else
            {
                summary = engine.RunSession(level, level.activities, applyLevelRewards: true);
            }

            ApplySessionResults(level, summary);
        }

        private void ApplySessionResults(LevelDefinition level, ActivitySessionSummary summary)
        {
            if (summary == null || level == null)
            {
                return;
            }

            if (PlayerDataStore.Instance != null)
            {
                PlayerDataStore.Instance.UpdateProgress(1, 1, level.levelId, summary.StarsEarned, summary.MasteryPercent, true);
                PlayerDataStore.Instance.AddCoins(summary.CoinsEarned, "level_complete");
                PlayerDataStore.Instance.RecordSession(new SessionRecord
                {
                    levelId = level.levelId,
                    dateUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    durationSeconds = Mathf.RoundToInt(summary.AverageDurationSeconds * Mathf.Max(1, summary.ActivitiesCompleted + summary.ActivitiesFailed)),
                    successRate = Mathf.RoundToInt(summary.AverageAccuracy * 100f),
                    activitiesDone = summary.ActivitiesCompleted + summary.ActivitiesFailed
                });
            }

            RewardMomentContext rewardContext = new RewardMomentContext
            {
                LevelId = level.levelId,
                CoinsEarned = summary.CoinsEarned,
                StarsEarned = summary.StarsEarned,
                LivesRemaining = summary.LivesRemaining,
                ActivitiesCompleted = summary.ActivitiesCompleted,
                ActivitiesFailed = summary.ActivitiesFailed,
                SuccessStreakBonusCoins = summary.SuccessStreakBonusCoins
            };

            if (!ScreenManager.Instance.TryNavigate(ScreenId.RewardMoment, rewardContext))
            {
                ScreenManager.Instance.NavigateTo(ScreenId.LevelSummary, level.levelId, clearStack: false);
            }
        }
    }
}
