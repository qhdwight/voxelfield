using System;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using UnityEngine;

namespace Voxelfield.Session.Mode
{
    public interface IModeWithBuying
    {
        bool CanBuy(SessionBase session, Container sessionContainer);
    }
    
    public static class BuyingMode
    {
        public static ushort GetCost(byte itemId)
        {
            switch (itemId)
            {
                case ItemId.Rifle:
                    return 2000;
                case ItemId.Shotgun:
                    return 1300;
                case ItemId.Sniper:
                    return 5000;
                case ItemId.Deagle:
                    return 700;
                case ItemId.Grenade:
                    return 150;
                case ItemId.Molotov:
                    return 400;
                case ItemId.C4:
                    return 600;
            }
            throw new ArgumentException("Can't buy this item id");
        }

        public static void HandleBuying(Container player)
        {
            ByteProperty wantedBuyItemId = player.Require<MoneyComponent>().wantedBuyItemId;
            if (wantedBuyItemId.WithValue)
            {
                UShortProperty money = player.Require<MoneyComponent>().count;
                Debug.Log($"Trying to buy requested item: {wantedBuyItemId.Value}");
                ushort cost = GetCost(wantedBuyItemId);
                if (cost < money)
                {
                    var inventory = player.Require<InventoryComponent>();
                    if (PlayerItemManagerModiferBehavior.AddItem(inventory, wantedBuyItemId))
                        money.Value -= cost;
                }
            }
        }
    }
}