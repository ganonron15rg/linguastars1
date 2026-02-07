using LinguaStars.Client.Core;
using UnityEngine;

namespace LinguaStars.Client.Screens
{
    public class StageMapScreen : ScreenBase
    {
        private int currentWorld = 1;

        public override void OnShow(object context)
        {
            base.OnShow(context);
            if (context is int worldIndex)
            {
                currentWorld = Mathf.Max(1, worldIndex);
            }
        }

        public void SelectStage(int stageIndex)
        {
            int clampedStage = Mathf.Max(1, stageIndex);
            string levelId = $"world{currentWorld}-stage{clampedStage}-level1";
            ScreenManager.Instance.NavigateTo(ScreenId.LevelIntro, levelId, clearStack: false);
        }
    }
}
