using Swihoni.Sessions;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel.Map;

namespace Voxelfield.Item
{
    public class ModelWand : SculptingItem
    {
        protected override void Swing(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
        }

        protected override void SecondaryUse(SessionBase session, int playerId, uint durationUs)
        {
            if (WithoutServerHit(session, playerId, out RaycastHit hit)
             || WithoutInnerVoxel(hit, out Position3Int position, out Voxel.Voxel voxel)) return;
        }
    }
}