using LinguaStars.Client.Core;
using UnityEngine;

namespace LinguaStars.Client.Screens
{
    public class LevelIntroScreen : ScreenBase
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

        public void StartLevel()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.ActivityPlayerHost, levelId, clearStack: false);
        }

        public void BackToMap()
        {
            ScreenManager.Instance.Back();
        }
    }
}
