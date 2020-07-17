using LiteNetLib;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Entities;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;
using Voxel.Map;

namespace Voxelfield.Session
{
    public class VoxelInjector : SessionInjectorBase
    {
        protected internal virtual void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData change, Chunk chunk = null, bool updateMesh = true)
            => ChunkManager.Singleton.SetVoxelData(worldPosition, change, chunk, updateMesh);

        protected internal virtual void VoxelTransaction(VoxelChangeTransaction uncommitted) => uncommitted.Commit();

        protected internal virtual void SetVoxelRadius(in Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false,
                                                       bool destroyBlocks = false, bool isAdditive = false, in VoxelChangeData additiveChange = default,
                                                       ChangedVoxelsProperty changedVoxels = null)
            => ChunkManager.Singleton.SetVoxelRadius(worldPosition, radius, replaceGrassWithDirt, destroyBlocks, isAdditive, additiveChange, changedVoxels);

        private readonly NetDataWriter m_RejectionWriter = new NetDataWriter();

        protected override void OnHandleNewConnection(ConnectionRequest request)
        {
            void Reject(string message)
            {
                m_RejectionWriter.Reset();
                m_RejectionWriter.Put(message);
                request.Reject(m_RejectionWriter);
            }
            int nextPeerId = ((NetworkedSessionBase) Manager).Socket.NetworkManager.PeekNextId();
            if (nextPeerId >= SessionBase.MaxPlayers - 1) Reject("Too many players are already connected to the server!");
            else if (request.Data.TryGetString(out string result) && result != Application.version) Reject("Your version does not match that of the server.");
            else request.Accept();
        }

        protected override void OnRenderMode(Container session)
        {
            base.OnRenderMode(session);
            if (!IsLoading(session))
                foreach (ModelBehaviorBase modelBehavior in MapManager.Singleton.Models.Values)
                    modelBehavior.RenderContainer();
        }

        protected override void Stop() => MapManager.Singleton.UnloadMap();

        protected override void OnSettingsTick(Container session)
        {
            MapManager.Singleton.SetMap(session.Require<VoxelMapNameProperty>());
            if (!IsLoading(session))
                foreach (ModelBehaviorBase modelBehavior in MapManager.Singleton.Models.Values)
                    modelBehavior.SetInMode(session);
        }

        public override bool IsLoading(Container session) => session.Require<VoxelMapNameProperty>() != MapManager.Singleton.Map.name
                                                          || ChunkManager.Singleton.ProgressInfo.stage != MapLoadingStage.Completed;

        public override void OnThrowablePopped(ThrowableModifierBehavior throwableBehavior)
        {
            var center = (Position3Int) throwableBehavior.transform.position;
            SetVoxelRadius(center, throwableBehavior.Radius * 0.4f, true, true);
        }
    }
}