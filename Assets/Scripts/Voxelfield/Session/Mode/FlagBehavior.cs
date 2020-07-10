using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel.Map;

namespace Voxelfield.Session.Mode
{
    public class FlagBehavior : ModelBehavior
    {
        private Cloth m_FlagCloth;
        private SkinnedMeshRenderer m_Renderer;
        private Material m_Material, m_SpriteMaterial;
        private SpriteRenderer m_SpriteRenderer;

        [SerializeField] private float m_TakingBlinkRate = 10.0f;

        private void Awake()
        {
            m_FlagCloth = GetComponentInChildren<Cloth>();
            m_Renderer = m_FlagCloth.GetComponent<SkinnedMeshRenderer>();
            m_SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            m_Material = m_Renderer.material;
            m_SpriteMaterial = m_SpriteRenderer.material;
        }

        public void Render(SessionBase session, Container sessionContainer, FlagComponent flag)
        {
            Vector3 spritePosition;
            bool isFlagTaken = flag.captureElapsedTimeUs.WithValue && flag.captureElapsedTimeUs > CtfMode.TakeFlagDurationUs;
            if (isFlagTaken)
            {
                Container capturingPlayer = session.GetPlayerFromId(flag.capturingPlayerId);
                spritePosition = capturingPlayer.Require<MoveComponent>().position + new Vector3 {y = 2.7f};
            }
            else spritePosition = transform.position + new Vector3 {y = 2.8f};

            var isIconVisible = true;
            if (session.IsValidLocalPlayer(sessionContainer, out Container localPlayer))
            {
                Vector3 localPosition = localPlayer.Require<MoveComponent>().position + new Vector3 {y = 1.8f};
                m_SpriteRenderer.transform.LookAt(localPosition);
                float distanceMultiplier = Mathf.Clamp(Vector3.Distance(localPosition, spritePosition) * 0.05f, 1.0f, 5.0f);
                spritePosition += new Vector3 {y = distanceMultiplier};
                m_SpriteRenderer.transform.localScale = Vector3.one * distanceMultiplier;
                if (isFlagTaken && sessionContainer.Require<LocalPlayerId>() == flag.capturingPlayerId) isIconVisible = false;
            }
            m_SpriteRenderer.transform.position = spritePosition;

            Color color = Container.Require<TeamProperty>() == CtfMode.BlueTeam ? Color.blue : Color.red;
            float cosine = Mathf.Cos(flag.captureElapsedTimeUs.Else(0u) * TimeConversions.MicrosecondToSecond * m_TakingBlinkRate);
            color.a = cosine.Remap(-1.0f, 1.0f, 0.8f, 1.0f);
            m_Material.color = color;

            color.a = cosine.Remap(-1.0f, 1.0f, 0.2f, 1.0f);
            m_SpriteMaterial.color = color;
            m_SpriteRenderer.enabled = isIconVisible;
            
            gameObject.SetActive(true);
        }

        public override void SetVisibility(Container container) => gameObject.SetActive(IsModeOrDesigner(container, ModeIdProperty.Ctf));
    }
}