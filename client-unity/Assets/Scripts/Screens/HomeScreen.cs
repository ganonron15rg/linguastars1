using LinguaStars.Client.Core;
using LinguaStars.Client.Data;
using UnityEngine;

namespace LinguaStars.Client.Screens
{
    public class HomeScreen : ScreenBase
    {
        [SerializeField] private string continueLevelId = "world1-stage1-level1";

        public void Continue()
        {
            if (PlayerDataStore.Instance != null && !string.IsNullOrEmpty(PlayerDataStore.Instance.State.progress.lastLevelId))
            {
                continueLevelId = PlayerDataStore.Instance.State.progress.lastLevelId;
            }

            ScreenManager.Instance.NavigateTo(ScreenId.LevelIntro, continueLevelId, clearStack: false);
        }

        public void Daily()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.LevelIntro, "daily-01", clearStack: false);
        }

        public void FreePlay()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.FreePlay, null, clearStack: false);
        }

        public void Shop()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.Shop, null, clearStack: false);
        }

        public void Progress()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.Progress, null, clearStack: false);
        }

        public void Settings()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.Settings, null, clearStack: false);
        }

        public void WorldMap()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.WorldMap, null, clearStack: false);
        }
    }
}
