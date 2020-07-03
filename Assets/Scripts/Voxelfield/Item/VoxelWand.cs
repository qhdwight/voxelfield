using System;
using System.Collections.Generic;
using Console;
using Swihoni.Components;
using Swihoni.Sessions;
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
        
        protected override void Swing(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            if (WithoutServerHit(session, playerId, out RaycastHit hit)
             || WithoutInnerVoxel(hit, out Position3Int position, out Voxel.Voxel voxel)) return;
            if (voxel.renderType == VoxelRenderType.Block)
            {
                var designer = session.GetPlayerFromId(playerId).Require<DesignerPlayerComponent>();
                if (designer.positionOne.WithoutValue)
                {
                    Debug.Log($"Set position one: {position}");
                    designer.positionOne.Value = position;
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
            designer.positionOne.Clear();
            designer.positionTwo.Clear();
        }

        protected void InterpretCommand(SessionBase session, string stringCommand, int playerId, Container player, Container sessionContainer)
        {
            string[] args = ConsoleCommandExecutor.Split(stringCommand);
            if (args[0] == "set")
                SetDimension(session, player.Require<DesignerPlayerComponent>());
        }
    }
}