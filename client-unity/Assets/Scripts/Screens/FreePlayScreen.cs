using LinguaStars.Client.Core;

namespace LinguaStars.Client.Screens
{
    public class FreePlayScreen : ScreenBase
    {
        public void StartFreePlay()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.ActivityPlayerHost, "freeplay", clearStack: false);
        }
    }
}
