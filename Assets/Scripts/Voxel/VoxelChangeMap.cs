using System.Collections.Generic;
using Swihoni.Util.Math;

namespace Voxel
{
    public class VoxelChangeMap : Dictionary<Position3Int, VoxelChangeData>
    {
        public void Set(Position3Int position, VoxelChangeData change)
        {
            if (TryGetValue(position, out VoxelChangeData existingChange))
            {
                existingChange.Merge(change);
                Remove(position);
                change = existingChange;
            }
            Add(position, change);
        }
    }
}