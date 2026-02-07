using LinguaStars.Client.Core;
using LinguaStars.Client.Data;

namespace LinguaStars.Client.Screens
{
    public class ProgressScreen : ScreenBase
    {
        public ProgressState GetProgress()
        {
            return PlayerDataStore.Instance != null ? PlayerDataStore.Instance.State.progress : new ProgressState();
        }

        public void BackToHome()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.Home, null, clearStack: true);
        }
    }
}
