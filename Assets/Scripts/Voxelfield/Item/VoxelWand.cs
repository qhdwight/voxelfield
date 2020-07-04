using System;
using Console;
using Input;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;
using Voxelfield.Session;

namespace Voxelfield.Item
{
    public class VoxelWand : SculptingItem
    {
        private readonly VoxelChangeTransaction m_Transaction = new VoxelChangeTransaction();

        protected override void OnEquip(SessionBase session, int playerId, ItemComponent itemComponent, uint durationUs)
            => ConsoleCommandExecutor.SetCommand("set", args => session.StringCommand(playerId, string.Join(" ", args)));

        protected override void OnUnequip(SessionBase session, int playerId, ItemComponent itemComponent, uint durationUs)
            => ConsoleCommandExecutor.RemoveCommand("set");

        protected override void Swing(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            if (WithoutServerHit(session, playerId, out RaycastHit hit)
             || WithoutInnerVoxel(hit, out Position3Int position, out Voxel.Voxel voxel)) return;
            if (voxel.renderType == VoxelRenderType.Block)
            {
                var designer = session.GetPlayerFromId(playerId).Require<DesignerPlayerComponent>();
                if (designer.positionOne.WithoutValue || designer.positionTwo.WithValue)
                {
                    Debug.Log($"Set position one: {position}");
                    designer.positionOne.Value = position;
                    designer.positionTwo.Clear();
                }
                else
                {
                    Debug.Log($"Set position two: {position}");
                    designer.positionTwo.Value = position;
                }
            }
        }

        protected override void SecondaryUse(SessionBase session, int playerId, uint durationUs)
        {
            var designer = session.GetPlayerFromId(playerId).Require<DesignerPlayerComponent>();
            SetDimension(session, designer);
        }

        public override void ModifyChecked(SessionBase session, int playerId, Container player, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs,
                                           uint durationUs)
        {
            base.ModifyChecked(session, playerId, player, item, inventory, inputs, durationUs);
            
            if (player.WithoutPropertyOrWithoutValue(out StringCommandProperty command)) return;

            string[] split = command.Builder.ToString().Split();
            if (split[0] == "set")
            {
                var designer = player.Require<DesignerPlayerComponent>();
                if (split.Length > 1 && byte.TryParse(split[1], out byte blockId))
                    designer.selectedBlockId.Value = blockId;
                SetDimension(session, designer);
            }
        }

        private void SetDimension(SessionBase session, DesignerPlayerComponent designer)
        {
            if (designer.positionOne.WithValue && designer.positionTwo.WithValue)
            {
                var voxelInjector = (VoxelInjector) session.Injector;
                Position3Int p1 = designer.positionOne, p2 = designer.positionTwo;
                for (int x = Math.Min(p1.x, p2.x); x <= Math.Max(p1.x, p2.x); x++)
                for (int y = Math.Min(p1.y, p2.y); y <= Math.Max(p1.y, p2.y); y++)
                for (int z = Math.Min(p1.z, p2.z); z <= Math.Max(p1.z, p2.z); z++)
                    m_Transaction.AddChange(new Position3Int(x, y, z), new VoxelChangeData {texture = designer.selectedBlockId, renderType = VoxelRenderType.Block});
                Debug.Log($"Set {p1} to {p2}");
                voxelInjector.VoxelTransaction(m_Transaction);
            }
        }
    }
}