using Swihoni.Sessions;
using Swihoni.Sessions.Entities;
using Swihoni.Util.Math;
using UnityEngine;
using Voxelation;
using Voxelfield.Session;

namespace Voxelfield.Item
{
    public class SculptingThrowable : ThrowableModifierBehavior
    {
        private static readonly Color32 Sand = new Color32(253, 255, 224, 255);

        protected override void JustPopped(in ModifyContext context)
        {
            if (context.session.Injector is Injector injector)
            {
                var center = (Position3Int) transform.position;
                injector.EvaluateVoxelChange(center, new VoxelChange {color = Sand, form = VoxelVolumeForm.Cylindrical, texture = VoxelTexture.Solid});
            }
        }
    }
}