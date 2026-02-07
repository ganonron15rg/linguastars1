using System;
using System.Collections.Generic;
using UnityEngine;

namespace LinguaStars.Client.Core
{
    public class ScreenManager : MonoBehaviour
    {
        [SerializeField] private ScreenBase[] screens;
        [SerializeField] private ScreenId startScreen = ScreenId.Splash;
        [SerializeField] private UISafeArea safeArea;

        private readonly Dictionary<ScreenId, ScreenBase> registry = new Dictionary<ScreenId, ScreenBase>();
        private readonly UIStack stack = new UIStack();

        public static ScreenManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildRegistry();
        }

        private void Start()
        {
            NavigateTo(startScreen, null, clearStack: true);
        }

        private void BuildRegistry()
        {
            registry.Clear();
            if (screens == null)
            {
                return;
            }

            foreach (ScreenBase screen in screens)
            {
                if (screen == null)
                {
                    continue;
                }

                if (!registry.ContainsKey(screen.ScreenId))
                {
                    registry.Add(screen.ScreenId, screen);
                    screen.gameObject.SetActive(false);
                }
            }
        }

        public void NavigateTo(ScreenId screenId, object context, bool clearStack = false)
        {
            if (!registry.TryGetValue(screenId, out ScreenBase target))
            {
                Debug.LogWarning($"Screen {screenId} not found in registry.");
                return;
            }

            if (clearStack)
            {
                while (stack.Count > 0)
                {
                    ScreenBase current = stack.Pop();
                    current?.OnHide();
                }
            }
            else
            {
                ScreenBase current = stack.Peek();
                current?.OnHide();
            }

            stack.Push(target);
            target.OnShow(context);
        }

        public bool TryNavigate(ScreenId screenId, object context, bool clearStack = false)
        {
            if (!registry.ContainsKey(screenId))
            {
                return false;
            }

            NavigateTo(screenId, context, clearStack);
            return true;
        }

        public void Back()
        {
            if (stack.Count <= 1)
            {
                return;
            }

            ScreenBase current = stack.Pop();
            current?.OnHide();
            ScreenBase next = stack.Peek();
            next?.OnShow(null);
        }

        public void Replace(ScreenId screenId, object context)
        {
            if (stack.Count > 0)
            {
                ScreenBase current = stack.Pop();
                current?.OnHide();
            }

            NavigateTo(screenId, context, clearStack: false);
        }

        public void EnsureSafeArea()
        {
            if (safeArea != null)
            {
                safeArea.enabled = true;
            }
        }
    }
}
