using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
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

        private void Start() => m_SculptingItem = (SculptingItem) ItemAssetLink.GetModifier(ItemId.SuperPickaxe);

        private readonly RaycastHit[] m_CachedHits = new RaycastHit[1];

        public override void Render(SessionBase session, Container sessionContainer)
        {
            Container localPlayer = default;
            bool isVisible = sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Designer && session.IsValidLocalPlayer(sessionContainer, out localPlayer);
            FloatProperty editRadius = default;
            if (isVisible)
            {
                editRadius = localPlayer.Require<DesignerPlayerComponent>().editRadius;
                if (!localPlayer.Require<InventoryComponent>().WithItemEquipped(out ItemComponent item) || item.id != ItemId.SuperPickaxe) isVisible = false;
            }
            if (isVisible)
            {
                if (Physics.RaycastNonAlloc(SessionBase.GetRayForPlayer(localPlayer), m_CachedHits, m_SculptingItem.EditDistance, m_SculptingItem.ChunkMask) > 0)
                {
                    Camera activeCamera = Camera.allCameras.First();
                    Matrix4x4 matrix = Matrix4x4.TRS(m_CachedHits.First().point, Quaternion.identity, Vector3.one * editRadius * 1.5f);
                    Graphics.DrawMesh(m_SphereMesh, matrix, m_Material, 0, activeCamera, 0, null, ShadowCastingMode.Off, false);
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