using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using UnityEngine;
using UnityEngine.Rendering;
using Voxelfield.Item;
using Voxelfield.Session;

namespace Voxelfield.Interface.Designer
{
    public class DesignerHudInterface : SessionInterfaceBehavior
    {
        [SerializeField] private Mesh m_SphereMesh = default;
        [SerializeField] private Material m_Material = default;
        private SculptingItem m_SculptingItem;

        public override void Initialize()
        {
            m_SculptingItem = (SculptingItem) ItemAssetLink.GetModifier(ItemId.SuperPickaxe);
            base.Initialize();
        }

        private readonly RaycastHit[] m_CachedHits = new RaycastHit[1];

        public override void Render(in SessionContext context)
        {
            Container localPlayer = default;
            bool isVisible = context.sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Designer && context.IsValidLocalPlayer(out localPlayer, out byte _);
            FloatProperty editRadius = default;
            if (isVisible)
            {
                editRadius = localPlayer.Require<DesignerPlayerComponent>().editRadius;
                if (!localPlayer.Require<InventoryComponent>().WithItemEquipped(out ItemComponent item) || item.id != ItemId.SuperPickaxe) isVisible = false;
            }
            if (isVisible)
            {
                int count = Physics.RaycastNonAlloc(localPlayer.GetRayForPlayer(), m_CachedHits, m_SculptingItem.EditDistance, m_SculptingItem.ChunkMask);
                if (m_CachedHits.TryClosest(count, out RaycastHit hit))
                {
                    Matrix4x4 matrix = Matrix4x4.TRS(hit.point, Quaternion.identity, Vector3.one * editRadius * 1.5f);
                    Graphics.DrawMesh(m_SphereMesh, matrix, m_Material, 0, SessionBase.ActiveCamera, 0, null, ShadowCastingMode.Off, false);
                }
            }
            SetInterfaceActive(isVisible);
        }

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            if (session.GetLatestSession().Require<ModeIdProperty>() != ModeIdProperty.Designer) return;

            float wheel = InputProvider.GetMouseScrollWheel() * 2.5f;
            FloatProperty editRadius = commands.Require<DesignerPlayerComponent>().editRadius;
            editRadius.Value = Mathf.Clamp(editRadius.Else(2.0f) + wheel, 0.0f, 10.0f);
        }
    }
}