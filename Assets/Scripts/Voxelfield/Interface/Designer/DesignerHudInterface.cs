using Input;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using UnityEngine.UI;
using Voxelfield.Interface.Showdown;
using Voxelfield.Session;

namespace Voxelfield.Interface.Designer
{
    public class DesignerHudInterface : SessionInterfaceBehavior
    {
        private Image m_Image;

        protected override void Awake()
        {
            base.Awake();
            m_Image = GetComponentInChildren<Image>();
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isVisible = ShowdownInterface.IsValidLocalPlayer(session, sessionContainer, out Container localPlayer);
            FloatProperty editRadius = default;
            if (isVisible)
            {
                editRadius = localPlayer.Require<DesignerPlayerComponent>().editRadius;
                if (!localPlayer.Require<InventoryComponent>().WithItemEquipped(out ItemComponent item) || item.id != ItemId.VoxelWand) isVisible = false;
            }
            if (isVisible)
            {
                m_Image.rectTransform.localScale = Vector3.one * editRadius;
            }
            SetInterfaceActive(isVisible);
        }

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            float wheel = InputProvider.GetMouseScrollWheel();
            FloatProperty editRadius = commands.Require<DesignerPlayerComponent>().editRadius;
            editRadius.Value = Mathf.Clamp(editRadius.Else(1.7f) + wheel, 0.0f, 10.0f);
        }
    }
}