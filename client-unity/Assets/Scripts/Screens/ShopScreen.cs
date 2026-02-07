using System.Collections.Generic;
using LinguaStars.Client.Core;
using LinguaStars.Client.Content;
using LinguaStars.Client.Data;
using UnityEngine;

namespace LinguaStars.Client.Screens
{
    public class ShopScreen : ScreenBase
    {
        [System.Serializable]
        public class CosmeticOffer
        {
            public string itemId;
            public int price;
            public string displayName;
        }

        [SerializeField] private List<CosmeticOffer> offers = new List<CosmeticOffer>
        {
            new CosmeticOffer { itemId = "hat-star", price = 50, displayName = "Star Hat" },
            new CosmeticOffer { itemId = "cape-blue", price = 75, displayName = "Blue Cape" },
            new CosmeticOffer { itemId = "badge-moon", price = 30, displayName = "Moon Badge" }
        };

        public IReadOnlyList<CosmeticOffer> Offers => offers;

        public override void OnShow(object context)
        {
            base.OnShow(context);
            LoadOffersFromContent();
        }

        public bool Purchase(string itemId)
        {
            if (PlayerDataStore.Instance == null)
            {
                return false;
            }

            CosmeticOffer offer = offers.Find(entry => entry.itemId == itemId);
            if (offer == null)
            {
                return false;
            }

            bool paid = PlayerDataStore.Instance.SpendCoins(offer.price, "shop_purchase");
            if (!paid)
            {
                return false;
            }

            PlayerDataStore.Instance.AddInventoryItem(offer.itemId, 1);
            return true;
        }

        public void OpenInventory()
        {
            ScreenManager.Instance.NavigateTo(ScreenId.Inventory, null, clearStack: false);
        }

        private void LoadOffersFromContent()
        {
            IReadOnlyList<ShopItem> items = ContentAgent.Instance.LoadShopItems();
            if (items == null || items.Count == 0)
            {
                return;
            }

            offers = new List<CosmeticOffer>();
            foreach (ShopItem item in items)
            {
                offers.Add(new CosmeticOffer
                {
                    itemId = item.id,
                    price = item.price_coins,
                    displayName = item.name_en
                });
            }
        }
    }
}
