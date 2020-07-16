using System.Linq;
using Input;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using Voxelfield.Session;

namespace Voxelfield.Interface
{
    public class LoadOutInterface : InterfaceBehaviorBase
    {
        private LoadOutButton[][] m_LoadOutButtons;

        public WantedItemComponent m_WantedItem = new WantedItemComponent();

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
                                                                                                  m_WantedItem.index.Value = (byte) (slotIndex + 1);
                                                                                                  m_WantedItem.index.Value = button.ItemId;
                                                                                              });
                                                                                          return hasComponent;
                                                                                      }).ToArray()).ToArray();
        }

        private void Update()
        {
            if (InputProvider.Singleton.GetInputDown(InputType.Buy))
            {
                ToggleInterfaceActive();
            }
        }

        internal void Render(InventoryComponent inventory)
        {
            foreach (LoadOutButton[] loadOutButtons in m_LoadOutButtons)
            foreach (LoadOutButton loadOutButton in loadOutButtons)
                loadOutButton.SetChecked(false);
            for (var i = 1; i < 10; i++)
            {
                ItemComponent item = inventory[i];
            }
        }
    }
}