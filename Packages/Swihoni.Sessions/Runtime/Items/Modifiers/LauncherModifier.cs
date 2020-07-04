using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    [CreateAssetMenu(fileName = "Launcher", menuName = "Item/Launcher", order = 0)]
    public class LauncherModifier : GunWithMagazineModifier
    {
        [SerializeField] protected ThrowableModifierBehavior m_ThrowablePrefab = default;
        [SerializeField] protected float m_LaunchForce = 10.0f;
        
        protected override byte? FinishStatus(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            if (item.status.id == ItemStatusId.PrimaryUsing)
            {
                Release(session, playerId, item);
            }
            return base.FinishStatus(session, playerId, item, inventory, inputs);
        }

        protected virtual void Release(SessionBase session, int playerId, ItemComponent item)
        {
            Container player = session.GetPlayerFromId(playerId);
            if (player.Without<ServerTag>()) return;

            Ray ray = SessionBase.GetRayForPlayer(player);
            var modifier = (EntityModifierBehavior) session.EntityManager.ObtainNextModifier(session.GetLatestSession(), m_ThrowablePrefab.id);
            if (modifier is ThrowableModifierBehavior throwableModifier)
            {
                throwableModifier.Name = itemName;
                modifier.transform.SetPositionAndRotation(ray.origin + ray.direction * 1.1f, Quaternion.LookRotation(ray.direction));
                throwableModifier.ThrowerId = playerId;
                Vector3 force = ray.direction * m_LaunchForce;
                if (player.With(out MoveComponent move)) force += move.velocity.Value * 0.1f;
                throwableModifier.Rigidbody.AddForce(force, ForceMode.Impulse);
            }

            item.ammoInMag.Value--;
        }
    }
}