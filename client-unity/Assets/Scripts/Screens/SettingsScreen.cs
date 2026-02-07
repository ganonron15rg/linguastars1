using LinguaStars.Client.Core;
using LinguaStars.Client.Data;
using UnityEngine;

namespace LinguaStars.Client.Screens
{
    public class SettingsScreen : ScreenBase
    {
        private bool musicEnabled = true;
        private bool sfxEnabled = true;

        public void ToggleMusic()
        {
            musicEnabled = !musicEnabled;
            PlayerPrefs.SetInt("music_enabled", musicEnabled ? 1 : 0);
        }

        public void ToggleSfx()
        {
            sfxEnabled = !sfxEnabled;
            PlayerPrefs.SetInt("sfx_enabled", sfxEnabled ? 1 : 0);
        }

        public void ResetProfile()
        {
            if (PlayerDataStore.Instance == null)
            {
                return;
            }

            PlayerDataStore.Instance.State.profile.name = string.Empty;
            PlayerDataStore.Instance.Save();
            ScreenManager.Instance.NavigateTo(ScreenId.Onboarding, null, clearStack: true);
        }

        public void BackHome()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.Home, null, clearStack: true);
        }
    }
}
