using System.Collections;
using LinguaStars.Client.Core;
using LinguaStars.Client.Data;
using UnityEngine;

namespace LinguaStars.Client.Screens
{
    public class SplashScreen : ScreenBase
    {
        [SerializeField] private float delaySeconds = 1.2f;

        public override void OnShow(object context)
        {
            base.OnShow(context);
            StartCoroutine(Transition());
        }

        private IEnumerator Transition()
        {
            yield return new WaitForSeconds(delaySeconds);
            if (PlayerDataStore.Instance == null)
            {
                yield break;
            }

            bool hasProfile = !string.IsNullOrEmpty(PlayerDataStore.Instance.State.profile.name);
            ScreenManager.Instance.NavigateTo(hasProfile ? ScreenId.Home : ScreenId.Onboarding, null, clearStack: true);
        }
    }
}
