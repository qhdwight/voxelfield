using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Entities;
using Swihoni.Util.Math;
using UnityEngine;
using Voxelfield.Session;
using Voxels;

namespace Voxelfield.Item
{
    public class SculptingThrowable : ThrowableModifierBehavior
    {
        private static readonly Color32 Sand = new Color32(253, 255, 224, 255);

        protected override void JustPopped(in SessionContext context, Container entity)
        {
            var server = (ServerInjector) context.session.Injector;

            if (entity.Require<ByteIdProperty>().TryWithValue(out byte entityId))
            {
                var center = (Position3Int) (transform.position + new Vector3 {y = m_Radius * 0.75f});
                switch (entityId)
                {
                    case 6:
                    {
                        var change = new VoxelChange
                        {
                            position = center, form = VoxelVolumeForm.Cylindrical,
                            color = Sand, magnitude = m_Radius, texture = VoxelTexture.Speckled, natural = false, isBreathable = true
                        };
                        server.ApplyVoxelChanges(change);
                        break;
                    }
                    case 8:
                    {
                        var change = new VoxelChange
                            {position = center, magnitude = m_Radius, replace = true, form = VoxelVolumeForm.Spherical, color = Color.red, texture = VoxelTexture.Solid};
                        server.ApplyVoxelChanges(change);
                        break;
                    }
                }
            }
        }
    }
}