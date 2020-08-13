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
using Voxels.Map;

namespace Voxelfield.Interface.Designer
{
    public class ModelSelectInterface : SessionInterfaceBehavior
    {
        [SerializeField] private Button m_ButtonPrefab = default;
        private int? m_WantedModelId;

        private void Start()
        {
            for (var modelId = 0; modelId < MapManager.ModelPrefabs.Length; modelId++)
            {
                Button button = Instantiate(m_ButtonPrefab, transform);
                var text = button.GetComponentInChildren<TextMeshProUGUI>();
                text.SetText(MapManager.ModelPrefabs[modelId].ModelName);
                int _modelId = modelId;
                button.onClick.AddListener(() => m_WantedModelId = _modelId);
            }
        }

        public override void Render(in SessionContext context)
            => SetInterfaceActive(NoInterrupting && HasItemEquipped(context, ModeIdProperty.Designer, ItemId.ModelWand)
                                                 && InputProvider.GetInput(InputType.OpenContext));

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            if (m_WantedModelId.HasValue)
            {
                Container localCommands = session.GetLocalCommands();
                localCommands.Require<DesignerPlayerComponent>().selectedModelId.Value = (ushort) m_WantedModelId.Value;
            }
            m_WantedModelId = null;
        }
    }
}