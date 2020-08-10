using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Visualization;
using Swihoni.Util.Math;
using UnityEngine;
using Voxels;
using Voxels.Map;

namespace Voxelfield.Session.Player
{
    public class VoxelfieldPlayerBodyAnimatorBehavior : PlayerBodyAnimatorBehavior
    {
        [SerializeField] private AudioClip[] m_NaturalClips = default;

        protected override AudioClip GetFootstepAudioClip(MoveComponent move)
        {
            if (ChunkManager.Singleton.GetVoxel((Position3Int) (move.position.Value - new Vector3 {y = 0.5f})) is Voxel voxel
             && MapManager.Singleton.Map.terrainGeneration.grassVoxel.TryWithValue(out VoxelChange grass) && grass.color is Color32 grassColor && voxel.color.SameAs(grassColor))
                return m_NaturalClips[Random.Range(0, m_NaturalClips.Length)];
            return base.GetFootstepAudioClip(move);
        }
    }
}