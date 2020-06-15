using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    public static class ThrowableStatusId
    {
        public const byte Cooking = ItemStatusId.Last + 1;
    }

    public class ThrowableModifierBase : ItemModifierBase
    {
        [Header("Throwable"), SerializeField] private ushort m_ThrowForce = default;
        [SerializeField] protected byte m_AmmoCount = default;
        [SerializeField] protected ThrowableModifierBehavior m_ThrowablePrefab = default;

        public byte AmmoCount => m_AmmoCount;
        public ushort ThrowForce => m_ThrowForce;
        public ThrowableModifierBehavior ThrowablePrefab => m_ThrowablePrefab;

        protected override void StatusTick(SessionBase session, int playerId, ItemComponent item, InputFlagProperty inputs, uint durationUs)
        {
            if (item.status.id == ThrowableStatusId.Cooking && !inputs.GetInput(PlayerInput.UseOne))
                StartStatus(session, playerId, item, ItemStatusId.PrimaryUsing, durationUs);
        }

        protected override byte? FinishStatus(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            if (item.status.id == ItemStatusId.PrimaryUsing) Release(session, playerId);
            return base.FinishStatus(session, playerId, item, inventory, inputs);
        }

        protected override byte GetUseStatus(InputFlagProperty inputProperty) => ThrowableStatusId.Cooking;

        protected override bool CanUse(ItemComponent item, InventoryComponent inventory, bool justFinishedUse = false) =>
            base.CanUse(item, inventory, justFinishedUse) && item.status.id != ThrowableStatusId.Cooking;

        protected override void PrimaryUse(SessionBase session, int playerId, ItemComponent item, uint durationUs) { }

        protected override bool HasSecondaryUse() => true;

        protected override void SecondaryUse(SessionBase session, int playerId, uint durationUs)
        {
            var entities = session.GetLatestSession().Require<EntityArrayElement>();
            for (var index = 0; index < entities.Length; index++)
            {
                EntityContainer entity = entities[index];
                if (session.EntityManager.Modifiers[index] is ThrowableModifierBehavior throwableModifier && throwableModifier.ThrowerId == playerId)
                {
                    var throwable = entity.Require<ThrowableComponent>();
                    if (throwable.popTimeUs > throwable.thrownElapsedUs)
                        throwableModifier.PopQueued = true;
                }
            }
        }

        protected virtual void Release(SessionBase session, int playerId)
        {
            Container player = session.GetPlayerFromId(playerId);
            if (player.Without<ServerTag>()) return;

            Ray ray = SessionBase.GetRayForPlayer(player);
            EntityModifierBehavior modifier = session.EntityManager.ObtainModifier(session.GetLatestSession(), m_ThrowablePrefab.id);
            if (modifier is ThrowableModifierBehavior throwableModifier)
            {
                modifier.transform.SetPositionAndRotation(ray.origin + ray.direction * 1.1f, Quaternion.identity);
                throwableModifier.ThrowerId = playerId;
                Vector3 force = ray.direction * m_ThrowForce;
                if (player.With(out MoveComponent move)) force += move.velocity.Value * 0.1f;
                throwableModifier.Rigidbody.AddForce(force, ForceMode.Impulse);
            }
        }

        internal override void OnUnequip(SessionBase session, int playerId, ItemComponent itemComponent, uint durationUs)
        {
            if (itemComponent.status.id == ThrowableStatusId.Cooking)
                StartStatus(session, playerId, itemComponent, ItemStatusId.Idle, durationUs);
        }
    }
}