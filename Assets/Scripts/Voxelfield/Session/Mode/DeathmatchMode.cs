using Swihoni.Sessions;
using Swihoni.Sessions.Modes;
using Swihoni.Util.Math;
using UnityEngine;
using Voxels;
using Voxels.Map;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Warmup", menuName = "Session/Mode/Warmup", order = 0)]
    public class DeathmatchMode : DeathmatchModeBase
    {
        protected override float CalculateWeaponDamage(in PlayerHitContext context)
        {
            float baseDamage = base.CalculateWeaponDamage(context);
            return ShowdownMode.CalculateDamageWithMovement(context, baseDamage);
        }

        protected override Vector3 GetSpawnPosition(in SessionContext context) => GetRandomPosition(context);

        public static Vector3 GetRandomPosition(in SessionContext context)
        {
            int chunkSize = context.GetChunkManager().ChunkSize;
            DimensionComponent dimension = context.GetMapManager().Map.dimension;
            Position3Int lower = dimension.lowerBound, upper = dimension.upperBound;
            for (var _ = 0; _ < 32; _++)
            {
                var position = new Vector3
                {
                    x = Random.Range(lower.x * chunkSize, (upper.x + 1) * chunkSize),
                    y = 1000.0f,
                    z = Random.Range(lower.z * chunkSize, (upper.z + 1) * chunkSize)
                };
                if (context.PhysicsScene.Raycast(position, Vector3.down, out RaycastHit hit, float.PositiveInfinity))
                    return hit.point + new Vector3 {y = 0.1f};
            }
            return new Vector3 {y = 8.0f};
        }
    }
}