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

        protected override void PrimaryUse(SessionBase session, int playerId, ItemComponent item, uint durationUs) => Release(session, playerId, item);

        protected virtual void Release(SessionBase session, int playerId, ItemComponent item)
        {
            checked
            {
                ThrowableModifierBase.Throw(session, playerId, itemName, m_ThrowablePrefab, m_LaunchForce);
                item.ammoInMag.Value--;
            }
        }
    }
}