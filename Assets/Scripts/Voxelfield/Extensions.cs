using System;
using Swihoni.Sessions;
using Voxelfield.Session;
using Voxels;
using Voxels.Map;

namespace Voxelfield
{
    public static class Extensions
    {
        public static MapManager GetMapManager(this in SessionContext context) => context.session.GetMapManager();

        public static ChunkManager GetChunkManager(this in SessionContext context) => context.GetMapManager().ChunkManager;

        public static MapManager GetMapManager(this SessionBase session) => ((Injector)session.Injector).MapManager;

        public static ChunkManager GetChunkManager(this SessionBase session) => session.GetMapManager().ChunkManager;
    }
}