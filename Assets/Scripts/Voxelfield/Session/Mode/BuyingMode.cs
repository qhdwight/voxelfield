using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;

namespace Voxelfield.Session.Mode
{
    public interface IModeWithBuying
    {
        bool CanBuy(in SessionContext context, Container sessionLocalPlayer);

        ushort GetCost(int itemId);
    }

    public static class BuyingMode
    {
        public static void HandleBuying(ModeBase mode, Container player, Container commands)
        {
            if (!(mode is IModeWithBuying buyingMode)) return;
            
            ByteProperty wantedBuyItemId = commands.Require<WantedItemComponent>().id;
            if (wantedBuyItemId.WithoutValue) return;
            
            UShortProperty money = player.Require<MoneyComponent>().count;
            ushort cost = buyingMode.GetCost(wantedBuyItemId);
            if (cost <= money)
            {
                var inventory = player.Require<InventoryComponent>();
                if (PlayerItemManagerModiferBehavior.TryAddItem(inventory, wantedBuyItemId, 1, 2).HasValue)
                    money.Value -= cost;
            }
        }
    }
}