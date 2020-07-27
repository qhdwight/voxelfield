using System;
using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util.Math;
using UnityEngine;
using Voxelfield.Session;
using Voxels;

namespace Voxelfield.Item
{
    [CreateAssetMenu(fileName = "Voxel Wand", menuName = "Item/Voxel Wand", order = 0)]
    public class VoxelWand : SculptingItem
    {
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeCommands() => SessionBase.RegisterSessionCommand("set", "revert", "breakable");

        protected override void Swing(in ModifyContext context, ItemComponent item)
        {
            if (WithoutClientHit(context, m_EditDistance, out RaycastHit hit)
             || WithoutInnerVoxel(hit, out Position3Int position, out Voxel voxel)) return;

            if (voxel.HasBlock)
            {
                var designer = context.session.GetLocalCommands().Require<DesignerPlayerComponent>();
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

        protected override void SecondaryUse(in ModifyContext context)
        {
            Container player = context.player;
            if (player.Without<ServerTag>()) return;

            var designer = player.Require<DesignerPlayerComponent>();
            if (designer.positionOne.WithoutValue || designer.positionTwo.WithoutValue) return;

            VoxelChange change = designer.selectedVoxel;
            change.Merge(new VoxelChange {hasBlock = true, upperBound = designer.positionTwo, form = VoxelVolumeForm.Prism});

            var server = (ServerInjector) context.session.Injector;
            server.EvaluateVoxelChange(designer.positionOne, change, overrideBreakable: true);
        }

        public override void ModifyChecked(in ModifyContext context, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            base.ModifyChecked(context, item, inventory, inputs);

            Container player = context.player;
            SessionBase session = context.session;
            if (PlayerModifierBehaviorBase.TryServerCommands(player, out IEnumerable<string[]> commands))
            {
                foreach (string[] args in commands)
                {
                    var designer = player.Require<DesignerPlayerComponent>();
                    switch (args[0])
                    {
                        case "set":
                        {
                            VoxelChange change = designer.selectedVoxel;
                            change.Merge(new VoxelChange {upperBound = designer.positionTwo, hasBlock = true, form = VoxelVolumeForm.Prism});

                            var server = (ServerInjector) session.Injector;
                            server.EvaluateVoxelChange(designer.positionOne, change, overrideBreakable: true);
                            break;
                        }
                        case "revert":
                        {
                            // ReSharper disable once PossibleInvalidOperationException
                            DimensionFunction(session, designer, position => ChunkManager.Singleton.GetMapSaveVoxel(position).Value);
                            break;
                        }
                        case "breakable":
                        {
                            var breakable = true;
                            if (args.Length > 1 && bool.TryParse(args[1], out bool parsedBreakable)) breakable = parsedBreakable;

                            var change = new VoxelChange {upperBound = designer.positionTwo, isBreakable = breakable, form = VoxelVolumeForm.Prism};

                            var server = (ServerInjector) session.Injector;
                            server.EvaluateVoxelChange(designer.positionOne, change, overrideBreakable: true);
                            break;
                        }
                    }
                }
            }
        }

        private static void DimensionFunction(SessionBase session, DesignerPlayerComponent designer, Func<Position3Int, VoxelChange> function)
        {
            if (designer.positionOne.WithoutValue || designer.positionTwo.WithoutValue) return;

            var server = (ServerInjector) session.Injector;
            Position3Int p1 = designer.positionOne, p2 = designer.positionTwo;
            var touchedChunks = new TouchedChunks();
            for (int x = Math.Min(p1.x, p2.x); x <= Math.Max(p1.x, p2.x); x++)
            for (int y = Math.Min(p1.y, p2.y); y <= Math.Max(p1.y, p2.y); y++)
            for (int z = Math.Min(p1.z, p2.z); z <= Math.Max(p1.z, p2.z); z++)
            {
                var worldPosition = new Position3Int(x, y, z);
                server.EvaluateVoxelChange(worldPosition, function(worldPosition), touchedChunks, true);
            }
            Debug.Log($"Set {p1} to {p2}");
            touchedChunks.UpdateMesh();
        }
    }
}