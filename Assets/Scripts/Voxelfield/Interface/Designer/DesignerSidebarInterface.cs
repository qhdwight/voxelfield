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
using Voxels.Map;

namespace Voxelfield.Interface.Designer
{
    public class DesignerSidebarInterface : SessionInterfaceBehavior
    {
        [SerializeField] private BufferedTextGui m_InformationText = default;

        public override void Render(in SessionContext context)
        {
            Container sessionContainer = context.sessionContainer;
            bool isVisible = sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Designer
                          && sessionContainer.Require<LocalPlayerId>().WithValue;
            Container localPlayer = default;
            byte? equippedItemId = null;
            if (isVisible)
            {
                int localPlayerId = sessionContainer.Require<LocalPlayerId>();
                localPlayer = context.GetPlayer(localPlayerId);
                if (localPlayer.Require<InventoryComponent>().WithItemEquipped(out ItemComponent item)) equippedItemId = item.id;
            }
            if (!equippedItemId.HasValue || equippedItemId != ItemId.VoxelWand && equippedItemId != ItemId.ModelWand) isVisible = false;
            if (isVisible)
            {
                var designer = localPlayer.Require<DesignerPlayerComponent>();
                StringBuilder builder = m_InformationText.StartBuild();
                switch (equippedItemId.Value)
                {
                    case ItemId.VoxelWand:
                        AppendProperty("P1: ", designer.positionOne, builder).Append("\n");
                        AppendProperty("P2: ", designer.positionTwo, builder).Append("\n");
                        AppendNullable("Texture: ", designer.selectedVoxel.DirectValue.texture, builder).Append("\n");
                        AppendNullable("Density: ", designer.selectedVoxel.DirectValue.density, builder).Append("\n");
                        AppendNullable("Color: ", designer.selectedVoxel.DirectValue.color, builder);
                        break;
                    case ItemId.ModelWand:
                        builder.Append("Selected: ").Append(designer.selectedModelId.WithValue ? MapManager.ModelPrefabs[designer.selectedModelId].ModelName : "None");
                        break;
                }
                builder.Commit(m_InformationText);
            }
            SetInterfaceActive(isVisible);
        }

        private static StringBuilder AppendNullable<T>(string prefix, T? nullable, StringBuilder builder) where T : struct
        {
            builder.Append(prefix);
            if (nullable.HasValue) builder.Append(nullable.Value);
            else builder.Append("None");
            return builder;
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