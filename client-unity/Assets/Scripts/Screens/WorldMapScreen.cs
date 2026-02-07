using LinguaStars.Client.Core;
using UnityEngine;

namespace LinguaStars.Client.Screens
{
    public class WorldMapScreen : ScreenBase
    {
        [SerializeField] private int maxWorld = 5;

        public void SelectWorld(int worldIndex)
        {
            int clamped = Mathf.Clamp(worldIndex, 1, maxWorld);
            ScreenManager.Instance.NavigateTo(ScreenId.StageMap, clamped, clearStack: false);
        }
    }
}
