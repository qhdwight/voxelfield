using System.Text;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using Voxelfield.Session;

namespace Voxelfield.Interface.Designer
{
    public class DesignerSidebarInterface : SessionInterfaceBehavior
    {
        [SerializeField] private BufferedTextGui m_InformationText = default;

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isVisible = sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Designer
                          && sessionContainer.Require<LocalPlayerId>().WithValue;
            Container localPlayer = default;
            byte equippedItemId = ItemId.None;
            if (isVisible)
            {
                int localPlayerId = sessionContainer.Require<LocalPlayerId>();
                localPlayer = session.GetPlayerFromId(localPlayerId, sessionContainer);
                if (localPlayer.Require<InventoryComponent>().WithItemEquipped(out ItemComponent item)) equippedItemId = item.id;
            }
            if (equippedItemId == ItemId.None || equippedItemId != ItemId.VoxelWand && equippedItemId != ItemId.ModelWand) isVisible = false;
            if (isVisible)
            {
                var designer = localPlayer.Require<DesignerPlayerComponent>();
                m_InformationText.BuildText(builder =>
                {
                    switch (equippedItemId)
                    {
                        case ItemId.VoxelWand:
                            AppendProperty("P1: ", designer.positionOne, builder).Append("\n");
                            AppendProperty("P2: ", designer.positionTwo, builder).Append("\n");
                            AppendProperty("Selected: ", designer.selectedBlockId, builder);
                            break;
                        case ItemId.ModelWand:
                            AppendProperty("Selected: ", designer.selectedModelId, builder);
                            break;
                    }
                });
            }
            SetInterfaceActive(isVisible);
        }

        private static StringBuilder AppendProperty<T>(string prefix, PropertyBase<T> position, StringBuilder builder) where T : struct
        {
            builder.Append(prefix);
            if (position.WithValue) builder.Append(position.Value);
            else builder.Append("Not set");
            return builder;
        }
    }
}