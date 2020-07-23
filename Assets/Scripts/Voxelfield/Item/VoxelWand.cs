using System;
using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
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

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeCommands() => SessionBase.RegisterSessionCommand("set", "revert", "breakable");

        protected override void Swing(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            if (WithoutClientHit(session, playerId, m_EditDistance, out RaycastHit hit)
             || WithoutInnerVoxel(hit, out Position3Int position, out Voxel.Voxel voxel)) return;

            if (voxel.HasBlock)
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
            DimensionFunction(session, designer, _ => new VoxelChangeData {id = designer.selectedVoxelId, hasBlock = true});
        }

        public override void ModifyChecked(SessionBase session, int playerId, Container player, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs,
                                           uint durationUs)
        {
            base.ModifyChecked(session, playerId, player, item, inventory, inputs, durationUs);

            if (PlayerModifierBehaviorBase.TryServerCommands(player, out IEnumerable<string[]> commands))
            {
                foreach (string[] args in commands)
                {
                    switch (args[0])
                    {
                        case "set":
                        {
                            var designer = player.Require<DesignerPlayerComponent>();
                            if (args.Length > 1 && byte.TryParse(args[1], out byte blockId))
                                designer.selectedVoxelId.ValueOverride = blockId;
                            DimensionFunction(session, designer, _ => new VoxelChangeData {id = designer.selectedVoxelId.AsNullable, hasBlock = true});
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
                            if (args.Length > 1 && bool.TryParse(args[1], out bool parsedBreakable)) breakable = parsedBreakable;
                            DimensionFunction(session, player.Require<DesignerPlayerComponent>(), position => new VoxelChangeData {isBreakable = breakable});
                            break;
                        }
                    }
                }
            }
        }

        private void DimensionFunction(SessionBase session, DesignerPlayerComponent designer, Func<Position3Int, VoxelChangeData> function)
        {
            if (designer.positionOne.WithoutValue || designer.positionTwo.WithoutValue) return;

            var voxelInjector = (Injector) session.Injector;
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