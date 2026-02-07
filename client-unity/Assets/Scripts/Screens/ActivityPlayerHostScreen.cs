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
        }

        public void CompleteLevel(int starsEarned)
        {
            if (PlayerDataStore.Instance != null)
            {
                PlayerDataStore.Instance.UpdateProgress(1, 1, levelId, starsEarned, starsEarned * 33.3f, true);
                PlayerDataStore.Instance.AddCoins(10 + starsEarned * 5, "level_complete");
            }

            ScreenManager.Instance.NavigateTo(ScreenId.LevelSummary, levelId, clearStack: false);
        }
    }
}
