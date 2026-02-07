using System.Collections.Generic;
using System.Linq;
using LinguaStars.Client.Data;
using UnityEngine;

namespace LinguaStars.Client.Activities
{
    public class DailySessionPlanner
    {
        private const float WarmupMinSeconds = 120f;
        private const float WarmupMaxSeconds = 180f;

        public DailySessionPlan BuildPlan(ContentPack pack, PlayerDataState state)
        {
            DailySessionPlan plan = new DailySessionPlan();
            if (pack == null || pack.levels == null || pack.levels.Count == 0)
            {
                return plan;
            }

            plan.MainLevel = pack.levels[0];
            if (state != null && !string.IsNullOrEmpty(state.progress.lastLevelId))
            {
                LevelDefinition requested = pack.levels.FirstOrDefault(level => level.levelId == state.progress.lastLevelId);
                if (requested != null)
                {
                    plan.MainLevel = requested;
                }
            }

            float targetSeconds = Random.Range(WarmupMinSeconds, WarmupMaxSeconds);
            float total = 0f;
            List<ActivityDefinition> reviewActivities = BuildReviewActivities(plan.MainLevel, state?.progress?.reviewPool);

            foreach (ActivityDefinition activity in reviewActivities)
            {
                if (total >= targetSeconds)
                {
                    break;
                }

                plan.WarmupActivities.Add(activity);
                total += Mathf.Max(15f, activity.estimatedDurationSeconds);
            }

            if (plan.WarmupActivities.Count == 0)
            {
                plan.WarmupActivities.AddRange(plan.MainLevel.activities.Take(2));
            }

            return plan;
        }

        private List<ActivityDefinition> BuildReviewActivities(LevelDefinition level, List<ReviewEntry> reviewPool)
        {
            List<ActivityDefinition> activities = new List<ActivityDefinition>();
            if (level == null || level.activities == null)
            {
                return activities;
            }

            if (reviewPool == null || reviewPool.Count == 0)
            {
                activities.AddRange(level.activities.Take(3));
                return activities;
            }

            HashSet<string> reviewIds = new HashSet<string>(reviewPool.Select(entry => entry.itemId));
            foreach (ActivityDefinition activity in level.activities)
            {
                if (reviewIds.Contains(activity.itemId))
                {
                    activities.Add(activity);
                }
            }

            if (activities.Count == 0)
            {
                activities.AddRange(level.activities.Take(3));
            }

            return activities;
        }
    }
}
