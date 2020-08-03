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

        public WantedItemIdsComponent WantedItems { get; set; } = new WantedItemIdsComponent();

        private void Start()
        {
            m_LoadOutButtons = transform.Cast<Transform>()
                                        .Select((horizontal, slotIndex) => horizontal.Cast<Transform>()
                                                                                     .Select(vertical => vertical.GetComponent<LoadOutButton>())
                                                                                     .Where(button =>
                                                                                      {
                                                                                          bool hasComponent = button;
                                                                                          if (hasComponent)
                                                                                              button.OnClick.AddListener(() =>
                                                                                              {
                                                                                                  var index = (byte) (slotIndex + 1);
                                                                                                  WantedItems[index].Value = button.ItemId;
                                                                                              });
                                                                                          return hasComponent;
                                                                                      }).ToArray()).ToArray();
        }

        public override void SessionStateChange(bool isActive)
        {
            base.SessionStateChange(isActive);
            if (!isActive) WantedItems.Clear();
        }

        internal void Render(InventoryComponent inventory)
        {
            if (NoInterrupting && InputProvider.GetInputDown(InputType.Buy)) ToggleInterfaceActive();

            for (var i = 1; i <= m_LoadOutButtons.Length; i++)
            {
                ItemComponent item = inventory[i];
                foreach (LoadOutButton loadOutButton in m_LoadOutButtons[i - 1])
                    loadOutButton.SetChecked(item.id == loadOutButton.ItemId);
            }
        }

        public override void Render(SessionBase session, Container sessionContainer) {  }

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
            => commands.Require<WantedItemIdsComponent>().SetTo(WantedItems);
    }
}