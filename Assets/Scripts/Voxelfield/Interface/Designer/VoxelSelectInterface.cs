using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Items.Modifiers;
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

        private VoxelChange m_Change;

        protected override void Start()
        {
            for (byte id = 0; id <= VoxelTexture.Last; id++)
            {
                Button button = Instantiate(m_ButtonPrefab, transform);
                var text = button.GetComponentInChildren<TextMeshProUGUI>();
                text.SetText(VoxelTexture.Name(id));
                int _id = id;
                button.onClick.AddListener(() => m_Change.texture = (byte) _id);
            }
            void SelectorListener(int index, float floatValue)
            {
                var value = (byte) Mathf.RoundToInt(floatValue);
                Color32 color = m_Change.color.GetValueOrDefault(new Color32 {a = byte.MaxValue});
                color[index] = value;
                m_Change.color = color;
            }
            for (var colorIndex = 0; colorIndex < m_ColorValueSelectors.Length; colorIndex++)
            {
                ColorSelector selector = m_ColorValueSelectors[colorIndex];
                selector.Index = colorIndex;
                selector.OnValueChanged.AddListener(floatValue => SelectorListener(selector.Index, floatValue));
            }
            base.Start();
        }

        public override void Render(in SessionContext context)
            => SetInterfaceActive(NoInterrupting && HasItemEquipped(context, ModeIdProperty.Designer, ItemId.VoxelWand, ItemId.SuperPickaxe)
                                                 && InputProvider.GetInput(InputType.OpenContext));

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            Container localCommands = session.GetLocalCommands();
            var designer = localCommands.Require<DesignerPlayerComponent>();
            designer.selectedVoxel.DirectValue.Merge(m_Change);
            m_Change = default;
            foreach (ColorSelector selector in m_ColorValueSelectors)
                selector.SetColor(designer.selectedVoxel.DirectValue.color);
        }
    }
}