using LinguaStars.Client.Core;

namespace LinguaStars.Client.Screens
{
    public class LevelSummaryScreen : ScreenBase
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

        public void NextLevel()
        {
            string nextLevelId = string.IsNullOrEmpty(levelId) ? "world1-stage1-level2" : levelId + "-next";
            ScreenManager.Instance.NavigateTo(ScreenId.LevelIntro, nextLevelId, clearStack: false);
        }

        public void BackToHome()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.Home, null, clearStack: true);
        }
    }
}
