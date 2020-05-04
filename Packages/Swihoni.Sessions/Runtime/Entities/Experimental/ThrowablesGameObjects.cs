// using System;
// using Swihoni.Components;
// using Swihoni.Sessions.Mapper;
// using UnityEngine;
//
// namespace Swihoni.Sessions.Entities
// {
//     public class ThrowableVisuals : EntityVisuals
//     {
//         public override void Render(Container root, int index, EntityContainer player)
//         {
//             base.Render(root, index, player);
//
//             if (player.Without(out ThrowableComponent throwable)) return;
//
//             transform.SetPositionAndRotation(throwable.position, throwable.rotation);
//         }
//     }
//
//     [RequireComponent(typeof(Rigidbody), typeof(Collider))]
//     public class ThrowableModifier : EntityModifier
//     {
//         public override void Modify(SessionBase session, Container root, int index, EntityContainer entity, Container commands, float duration)
//         {
//             if (entity.Without(out ThrowableComponent throwable)) return;
//
//             Transform t = transform;
//             throwable.position.Value = t.position;
//             throwable.rotation.Value = t.rotation;
//         }
//
//         public override void Synchronize(EntityContainer container) { }
//
//         public override void ModifyCommands(SessionBase session, Container commands) { throw new NotImplementedException(); }
//     }
// }