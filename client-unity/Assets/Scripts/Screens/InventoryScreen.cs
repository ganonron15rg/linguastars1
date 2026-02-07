using System.Collections.Generic;
using LinguaStars.Client.Core;
using LinguaStars.Client.Data;

namespace LinguaStars.Client.Screens
{
    public class InventoryScreen : ScreenBase
    {
        public IReadOnlyList<InventoryItem> GetInventory()
        {
            return PlayerDataStore.Instance != null ? PlayerDataStore.Instance.State.economy.inventory : new List<InventoryItem>();
        }

        public void Equip(string itemId)
        {
            if (PlayerDataStore.Instance == null)
            {
                return;
            }

            PlayerDataStore.Instance.EquipItem(itemId);
        }

        public void OpenAvatar()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.Avatar, null, clearStack: false);
        }
    }
}
