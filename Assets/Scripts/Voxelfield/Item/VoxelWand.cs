using System;
using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Sessions;
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
            if (WithoutClientHit(context, m_EditDistance, out RaycastHit hit)) return;

            var position = (Position3Int) (hit.point - hit.normal * 0.1f);
            if (WithoutBreakableVoxel(position, out Voxel voxel) || voxel.OnlySmooth) return;

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

        protected override void SecondaryUse(in ModifyContext context)
        {
            Container player = context.player;
            if (!(context.session.Injector is ServerInjector server)) return;

            var designer = player.Require<DesignerPlayerComponent>();
            if (designer.positionOne.WithoutValue || designer.positionTwo.WithoutValue) return;

            VoxelChange change = designer.selectedVoxel;
            change.Merge(new VoxelChange {position = designer.positionOne, form = VoxelVolumeForm.Prism, hasBlock = true, upperBound = designer.positionTwo});

            server.ApplyVoxelChanges(change, overrideBreakable: true);
        }

        public override void ModifyChecked(in ModifyContext context, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            base.ModifyChecked(context, item, inventory, inputs);

            Container player = context.player;
            SessionBase session = context.session;
            if (PlayerModifierBehaviorBase.TryServerCommands(player, out IEnumerable<string[]> commands))
            {
                foreach (string[] arguments in commands)
                {
                    var designer = player.Require<DesignerPlayerComponent>();
                    switch (arguments[0])
                    {
                        case "set":
                        {
                            VoxelChange change = designer.selectedVoxel;
                            change.Merge(new VoxelChange {position = designer.positionOne, upperBound = designer.positionTwo, hasBlock = true, form = VoxelVolumeForm.Prism});

                            var server = (ServerInjector) session.Injector;
                            server.ApplyVoxelChanges(change, overrideBreakable: true);
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
                            if (arguments.Length > 1 && bool.TryParse(arguments[1], out bool parsedBreakable)) breakable = parsedBreakable;

                            var change = new VoxelChange
                            {
                                position = designer.positionOne, form = VoxelVolumeForm.Prism,
                                upperBound = designer.positionTwo, isBreakable = breakable
                            };

                            var server = (ServerInjector) session.Injector;
                            server.ApplyVoxelChanges(change, overrideBreakable: true);
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
                VoxelChange change = function(worldPosition);
                change.position = worldPosition;
                server.ApplyVoxelChanges(change, touchedChunks, true);
            }
            Debug.Log($"Set {p1} to {p2}");
            touchedChunks.UpdateMesh();
        }
    }
}