using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Voxelfield.Session;
using Voxels;

namespace Voxelfield.Interface.Designer
{
    public class VoxelSelectInterface : SessionInterfaceBehavior
    {
        [SerializeField] private Button m_ButtonPrefab = default;
        [SerializeField] private ColorSelector[] m_ColorValueSelectors = default;

        private int m_WantedTexture = VoxelTexture.Solid;
        private Color32 m_WantedColor = new Color32(255, 255, 255, 255);

        private void Start()
        {
            for (byte modelId = 0; modelId <= VoxelTexture.Last; modelId++)
            {
                Button button = Instantiate(m_ButtonPrefab, transform);
                var text = button.GetComponentInChildren<TextMeshProUGUI>();
                text.SetText(VoxelTexture.Name(modelId));
                int _modelId = modelId;
                button.onClick.AddListener(() => m_WantedTexture = _modelId);
            }
            void SelectorListener(int index, float floatValue)
            {
                var value = (byte) Mathf.RoundToInt(floatValue);
                m_WantedColor[index] = value;
                foreach (ColorSelector selector in m_ColorValueSelectors)
                    selector.SetColor(m_WantedColor);
            }
            for (var colorIndex = 0; colorIndex < m_ColorValueSelectors.Length; colorIndex++)
            {
                ColorSelector selector = m_ColorValueSelectors[colorIndex];
                int _colorIndex = colorIndex;
                selector.OnValueChanged.AddListener(floatValue => SelectorListener(_colorIndex, floatValue));
            }
        }

        public override void Render(SessionBase session, Container sessionContainer)
            => SetInterfaceActive(NoInterrupting && sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Designer &&
                                  InputProvider.GetInput(InputType.OpenVoxelSelect));

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            Container localCommands = session.GetLocalCommands();
            var designer = localCommands.Require<DesignerPlayerComponent>();
            ref VoxelChange voxel = ref designer.selectedVoxel.DirectValue;
            voxel.texture = (byte) m_WantedTexture;
            voxel.color = m_WantedColor;
        }
    }
}