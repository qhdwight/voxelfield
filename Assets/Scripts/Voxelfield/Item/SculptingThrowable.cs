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
        private static readonly Color32 Sand = new(253, 255, 224, 255);

        protected override void JustPopped(in SessionContext context, ThrowableComponent throwable)
        {
            var server = (ServerInjector) context.session.Injector;

            var center = (Position3Int) (transform.position + new Vector3 {y = m_Radius * 0.75f});
            var change = new VoxelChange
            {
                position = center, form = VoxelVolumeForm.Cylindrical,
                color = Sand, magnitude = m_Radius, texture = VoxelTexture.Speckled, natural = false, isBreathable = true
            };
            server.ApplyVoxelChanges(change);
        }
    }
}