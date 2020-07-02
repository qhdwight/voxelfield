using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    [CreateAssetMenu(fileName = "Launcher", menuName = "Item/Launcher", order = 0)]
    public class LauncherModifier : GunWithMagazineModifier
    {
        [SerializeField] protected ThrowableModifierBehavior m_ThrowablePrefab = default;
        
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
            
        }
    }
}