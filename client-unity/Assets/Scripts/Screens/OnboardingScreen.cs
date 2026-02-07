using LinguaStars.Client.Core;
using LinguaStars.Client.Data;
using UnityEngine;

namespace LinguaStars.Client.Screens
{
    public class OnboardingScreen : ScreenBase
    {
        private string childName = string.Empty;
        private int childAge = 4;
        private string avatarBaseId = "base-01";

        public void SetName(string nameValue)
        {
            childName = nameValue;
        }

        public void SetAge(string ageValue)
        {
            if (int.TryParse(ageValue, out int parsed))
            {
                childAge = Mathf.Clamp(parsed, 3, 10);
            }
        }

        public void SelectAvatarBase(string avatarId)
        {
            avatarBaseId = avatarId;
        }

        public void CreateProfile()
        {
            if (PlayerDataStore.Instance == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(childName))
            {
                childName = "Star Learner";
            }

            PlayerDataStore.Instance.SaveProfile(childName.Trim(), childAge, avatarBaseId);
            ScreenManager.Instance.NavigateTo(ScreenId.Home, null, clearStack: true);
        }
    }
}
