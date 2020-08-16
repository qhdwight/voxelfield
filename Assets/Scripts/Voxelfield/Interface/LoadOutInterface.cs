using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using Voxelfield.Session;

namespace Voxelfield.Interface
{
    public class LoadOutInterface : SessionInterfaceBehavior
    {
        private LoadOutButton[][] m_LoadOutButtons;

        public WantedItemIdArray WantedItems { get; set; } = new WantedItemIdArray();

        public override void Initialize()
        {
            m_LoadOutButtons = transform.Cast<Transform>()
                                        .Select((horizontal, slotIndex) => horizontal.Cast<Transform>()
                                                                                     .Select(vertical => vertical.GetComponent<LoadOutButton>())
                                                                                     .Where(button =>
                                                                                      {
                                                                                          bool hasComponent = button;
                                                                                          if (hasComponent)
                                                                                          {
                                                                                              if (!button.gameObject.activeInHierarchy) return false;
                                                                                              button.OnClick.AddListener(() =>
                                                                                              {
                                                                                                  var index = (byte) slotIndex;
                                                                                                  WantedItems[index].SetToNullable(button.IsChecked ? (byte?) null : button.ItemId);
                                                                                              });
                                                                                          }
                                                                                          return hasComponent;
                                                                                      }).ToArray()).ToArray();
            base.Initialize();
        }

        public override void SessionStateChange(bool isActive)
        {
            base.SessionStateChange(isActive);
            if (!isActive) WantedItems.Clear();
        }

        internal void Render(InventoryComponent inventory)
        {
            if (NoInterrupting && InputProvider.GetInputDown(InputType.Buy)) ToggleInterfaceActive();

            for (var i = 0; i < m_LoadOutButtons.Length; i++)
            {
                ItemComponent item = inventory[i];
                foreach (LoadOutButton loadOutButton in m_LoadOutButtons[i])
                    loadOutButton.SetChecked(item.id.AsNullable == loadOutButton.ItemId);
            }
        }

        public override void Render(in SessionContext context) { }

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
            => commands.Require<WantedItemIdArray>().SetTo(WantedItems);
    }
}