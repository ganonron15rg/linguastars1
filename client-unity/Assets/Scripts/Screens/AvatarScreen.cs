using System.Collections.Generic;
using LinguaStars.Client.Core;
using LinguaStars.Client.Data;

namespace LinguaStars.Client.Screens
{
    public class AvatarScreen : ScreenBase
    {
        public IReadOnlyList<string> GetEquippedItems()
        {
            return PlayerDataStore.Instance != null ? PlayerDataStore.Instance.State.economy.equippedItems : new List<string>();
        }

        public void EquipItem(string itemId)
        {
            if (PlayerDataStore.Instance == null)
            {
                return;
            }

            PlayerDataStore.Instance.EquipItem(itemId);
        }

        public void BackToInventory()
        {
            ScreenManager.Instance.Back();
        }
    }
}
