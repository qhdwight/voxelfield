using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Voxel.Map;
using Voxelfield.Session;

namespace Voxelfield.Interface.Designer
{
    public class ModelSelectInterface : SessionInterfaceBehavior
    {
        [SerializeField] private Button m_ButtonPrefab = default;
        private int? m_WantedModelId;

        private void Start()
        {
            for (var modelId = 0; modelId <= ModelsProperty.Last; modelId++)
            {
                Button button = Instantiate(m_ButtonPrefab, transform);
                var text = button.GetComponentInChildren<TextMeshProUGUI>();
                text.SetText(MapManager.ModelPrefabs[modelId].ModelName);
                int _modelId = modelId;
                button.onClick.AddListener(() => m_WantedModelId = _modelId);
            }
        }

        public override void Render(SessionBase session, Container sessionContainer)
            => SetInterfaceActive(NoInterrupting() && sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Designer &&
                                  InputProvider.GetInput(InputType.OpenModelSelect));

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