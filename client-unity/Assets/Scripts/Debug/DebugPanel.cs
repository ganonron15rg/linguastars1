#if UNITY_EDITOR || DEVELOPMENT_BUILD
using LinguaStars.Client.Data;
using UnityEngine;

namespace LinguaStars.Client.Debugging
{
    public class DebugPanel : MonoBehaviour
    {
        [SerializeField] private bool showPanel = true;
        [SerializeField] private int addCoinsAmount = 100;

        private Rect windowRect = new Rect(16, 16, 240, 180);

        private void OnGUI()
        {
            if (!showPanel)
            {
                return;
            }

            windowRect = GUILayout.Window(101, windowRect, DrawWindow, "Dev Panel");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Label("Offline Debug");
            if (GUILayout.Button($"Add {addCoinsAmount} Coins"))
            {
                PlayerDataStore.Instance?.AddCoins(addCoinsAmount, "dev_panel");
            }

            if (GUILayout.Button("Reset Progress"))
            {
                PlayerDataStore.Instance?.ResetProgress();
            }

            if (GUILayout.Button("Clear Inventory"))
            {
                if (PlayerDataStore.Instance != null)
                {
                    PlayerDataStore.Instance.State.economy.inventory.Clear();
                    PlayerDataStore.Instance.State.economy.equippedItems.Clear();
                    PlayerDataStore.Instance.Save();
                }
            }

            GUI.DragWindow();
        }
    }
}
#endif
