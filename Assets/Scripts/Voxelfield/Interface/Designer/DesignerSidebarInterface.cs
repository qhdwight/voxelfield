using System.Text;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using Voxel;
using Voxel.Map;
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
                localPlayer = session.GetModifyingPayerFromId(localPlayerId, sessionContainer);
                if (localPlayer.Require<InventoryComponent>().WithItemEquipped(out ItemComponent item)) equippedItemId = item.id;
            }
            if (equippedItemId == ItemId.None || equippedItemId != ItemId.VoxelWand && equippedItemId != ItemId.ModelWand) isVisible = false;
            if (isVisible)
            {
                var designer = localPlayer.Require<DesignerPlayerComponent>();
                StringBuilder builder = m_InformationText.StartBuild();
                switch (equippedItemId)
                {
                    case ItemId.VoxelWand:
                        AppendProperty("P1: ", designer.positionOne, builder).Append("\n");
                        AppendProperty("P2: ", designer.positionTwo, builder).Append("\n");
                        builder.Append("Selected: ").Append(designer.selectedVoxelId.WithValue ? VoxelTexture.Name(designer.selectedVoxelId) : "None");
                        break;
                    case ItemId.ModelWand:
                        builder.Append("Selected: ").Append(designer.selectedModelId.WithValue ? MapManager.ModelPrefabs[designer.selectedModelId].ModelName : "None");
                        break;
                }
                builder.Commit(m_InformationText);
            }
            SetInterfaceActive(isVisible);
        }

        private static StringBuilder AppendProperty(string prefix, PropertyBase position, StringBuilder builder)
        {
            builder.Append(prefix);
            if (position.WithValue) position.AppendValue(builder);
            else builder.Append("None");
            return builder;
        }
    }
}