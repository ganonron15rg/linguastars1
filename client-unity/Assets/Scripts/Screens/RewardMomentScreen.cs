using LinguaStars.Client.Core;
using UnityEngine;

namespace LinguaStars.Client.Screens
{
    public class RewardMomentContext
    {
        public string LevelId;
        public int CoinsEarned;
        public int StarsEarned;
        public int LivesRemaining;
        public int ActivitiesCompleted;
        public int ActivitiesFailed;
        public int SuccessStreakBonusCoins;
    }

    public class RewardMomentScreen : ScreenBase
    {
        private RewardMomentContext context;

        public override void OnShow(object screenContext)
        {
            base.OnShow(screenContext);
            context = screenContext as RewardMomentContext;
        }

        public void Continue()
        {
            string levelId = context != null ? context.LevelId : string.Empty;
            ScreenManager.Instance.NavigateTo(ScreenId.LevelSummary, levelId, clearStack: false);
        }
    }
}
