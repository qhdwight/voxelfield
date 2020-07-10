using System;
using Console;
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
    [CreateAssetMenu(fileName = "Voxel Wand", menuName = "Item/Voxel Wand", order = 0)]
    public class VoxelWand : SculptingItem
    {
        private readonly VoxelChangeTransaction m_Transaction = new VoxelChangeTransaction();

        private static readonly string[] Commands = {"set", "revert", "breakable"};

        public static void SessionCommand(SessionBase session, int playerId, params string[] commandNames)
        {
            foreach (string commandName in commandNames)
                ConsoleCommandExecutor.SetCommand(commandName, args => session.StringCommand(playerId, string.Join(" ", args)));
        }

        protected override void OnEquip(SessionBase session, int playerId, ItemComponent item, uint durationUs)
            => SessionCommand(session, playerId, Commands);

        protected override void OnUnequip(SessionBase session, int playerId, ItemComponent item, uint durationUs)
            => ConsoleCommandExecutor.RemoveCommands(Commands);

        protected override void Swing(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            if (WithoutClientHit(session, playerId, m_EditDistance, out RaycastHit hit)
             || WithoutInnerVoxel(hit, out Position3Int position, out Voxel.Voxel voxel)) return;

            if (voxel.renderType == VoxelRenderType.Block)
            {
                var designer = session.GetLocalCommands().Require<DesignerPlayerComponent>();
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
            Container player = session.GetModifyingPayerFromId(playerId);
            if (player.Without<ServerTag>()) return;

            var designer = player.Require<DesignerPlayerComponent>();
            DimensionFunction(session, designer, _ => new VoxelChangeData {id = designer.selectedVoxelId, renderType = VoxelRenderType.Block});
        }

        public override void ModifyChecked(SessionBase session, int playerId, Container player, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs,
                                           uint durationUs)
        {
            base.ModifyChecked(session, playerId, player, item, inventory, inputs, durationUs);

            if (player.Without<ServerTag>() || player.WithoutPropertyOrWithoutValue(out StringCommandProperty command) || command.Builder.Length == 0) return;

            string[] split = command.Builder.ToString().Split();
            switch (split[0])
            {
                case "set":
                {
                    var designer = player.Require<DesignerPlayerComponent>();
                    if (split.Length > 1 && byte.TryParse(split[1], out byte blockId))
                        designer.selectedVoxelId.ValueOverride = blockId;
                    DimensionFunction(session, designer, _ => new VoxelChangeData {id = designer.selectedVoxelId.AsNullable, renderType = VoxelRenderType.Block});
                    break;
                }
                case "revert":
                {
                    DimensionFunction(session, player.Require<DesignerPlayerComponent>(), position => ChunkManager.Singleton.GetMapSaveVoxel(position).Value);
                    break;
                }
                case "breakable":
                {
                    var breakable = true;
                    if (split.Length > 1 && bool.TryParse(split[1], out bool parsedBreakable)) breakable = parsedBreakable;
                    DimensionFunction(session, player.Require<DesignerPlayerComponent>(), position => new VoxelChangeData {breakable = breakable});
                    break;
                }
            }
        }

        private void DimensionFunction(SessionBase session, DesignerPlayerComponent designer, Func<Position3Int, VoxelChangeData> function)
        {
            if (designer.positionOne.WithoutValue || designer.positionTwo.WithoutValue) return;

            var voxelInjector = (VoxelInjector) session.Injector;
            Position3Int p1 = designer.positionOne, p2 = designer.positionTwo;
            for (int x = Math.Min(p1.x, p2.x); x <= Math.Max(p1.x, p2.x); x++)
            for (int y = Math.Min(p1.y, p2.y); y <= Math.Max(p1.y, p2.y); y++)
            for (int z = Math.Min(p1.z, p2.z); z <= Math.Max(p1.z, p2.z); z++)
            {
                var worldPosition = new Position3Int(x, y, z);
                m_Transaction.AddChange(worldPosition, function(worldPosition));
            }
            Debug.Log($"Set {p1} to {p2}");
            voxelInjector.VoxelTransaction(m_Transaction);
        }
    }
}