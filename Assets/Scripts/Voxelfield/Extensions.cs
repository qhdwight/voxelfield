using System;
using System.Collections.Generic;
using Swihoni.Sessions;
using Voxelfield.Session;
using Voxels;
using Voxels.Map;

namespace Voxelfield
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (T item in enumerable)
                action(item);
        }

        public static MapManager GetMapManager(this in SessionContext context) => context.session.GetMapManager();
        
        public static ChunkManager GetChunkManager(this in SessionContext context) => context.session.GetChunkManager();

        public static MapManager GetMapManager(this SessionBase session) => ((Injector) session.Injector).MapManager;
        
        public static ChunkManager GetChunkManager(this SessionBase session) => ((Injector) session.Injector).ChunkManager;
    }
}