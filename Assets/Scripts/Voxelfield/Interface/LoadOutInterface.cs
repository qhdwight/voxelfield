using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using Voxelfield.Session;

namespace Voxelfield.Interface
{
    public class LoadOutInterface : InterfaceBehaviorBase
    {
        private LoadOutButton[][] m_LoadOutButtons;

        public WantedItemComponent WantedItem { get; set; } = new WantedItemComponent();

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
                                                                                                  WantedItem.index.Value = (byte) (slotIndex + 1);
                                                                                                  WantedItem.id.Value = button.ItemId;
                                                                                              });
                                                                                          return hasComponent;
                                                                                      }).ToArray()).ToArray();
        }
        
        internal void Render(InventoryComponent inventory)
        {
            if (InputProvider.Singleton.GetInputDown(InputType.Buy)) ToggleInterfaceActive();

            for (var i = 1; i <= m_LoadOutButtons.Length; i++)
            {
                ItemComponent item = inventory[i];
                foreach (LoadOutButton loadOutButton in m_LoadOutButtons[i - 1])
                    loadOutButton.SetChecked(item.id == loadOutButton.ItemId);
            }
        }
        
        public void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            if (WantedItem.id.WithoutValue || WantedItem.index.WithoutValue) return;
            
            commands.Require<WantedItemComponent>().CopyFrom(WantedItem);
            WantedItem.Clear();
        }
    }
}