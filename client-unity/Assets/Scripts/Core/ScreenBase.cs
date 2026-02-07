using UnityEngine;

namespace LinguaStars.Client.Core
{
    public abstract class ScreenBase : MonoBehaviour
    {
        [SerializeField] private ScreenId screenId;

        public ScreenId ScreenId => screenId;

        public virtual void OnShow(object context)
        {
            gameObject.SetActive(true);
        }

        public virtual void OnHide()
        {
            gameObject.SetActive(false);
        }
    }
}
