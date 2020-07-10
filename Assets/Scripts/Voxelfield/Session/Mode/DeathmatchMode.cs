using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;
using Voxel.Map;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Warmup", menuName = "Session/Mode/Warmup", order = 0)]
    public class DeathmatchMode : DeathmatchModeBase
    {
        protected override float CalculateWeaponDamage(SessionBase session, Container hitPlayer, Container inflictingPlayer, PlayerHitbox hitbox, WeaponModifierBase weapon,
                                                       in RaycastHit hit)
        {
            float baseDamage = base.CalculateWeaponDamage(session, hitPlayer, inflictingPlayer, hitbox, weapon, in hit);
            return ShowdownMode.CalculateDamageWithMovement(session, inflictingPlayer, weapon, baseDamage);
        }
        
        protected override Vector3 GetSpawnPosition(Container player, int playerId, SessionBase session, Container sessionContainer) => GetRandomSpawn();

        public static Vector3 GetRandomSpawn()
        {
            int chunkSize = ChunkManager.Singleton.ChunkSize;
            DimensionComponent dimension = MapManager.Singleton.Map.dimension;
            Position3Int lower = dimension.lowerBound, upper = dimension.upperBound;
            var position = new Vector3
            {
                x = Random.Range(lower.x * chunkSize, (upper.x + 1) * chunkSize),
                y = 1000.0f,
                z = Random.Range(lower.z * chunkSize, (upper.z + 1) * chunkSize)
            };
            for (var _ = 0; _ < 32; _++)
            {
                if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, float.PositiveInfinity))
                    return hit.point + new Vector3 {y = 0.1f};
            }
            return new Vector3 {y = 8.0f};
        }
    }
}