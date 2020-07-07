using System;
using Input;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Voxel.Map;

namespace Voxelfield.Interface.Designer
{
    public class ModelSelectInterface : SessionInterfaceBehavior
    {
        [SerializeField] private Button m_ButtonPrefab = default;
        private int? m_WantedModelId;

        private void Start()
        {
            for (var modelId = 0; modelId < ModelsProperty.Last; modelId++)
            {
                Button button = Instantiate(m_ButtonPrefab, transform);
                var text = button.GetComponentInChildren<TextMeshProUGUI>();
                text.SetText(modelId.ToString());
                int _modelId = modelId;
                button.onClick.AddListener(() => m_WantedModelId = _modelId);
            }
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            SetInterfaceActive(sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Designer && InputProvider.Singleton.GetInput(InputType.OpenModelSelect));
        }

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            if (m_WantedModelId.HasValue)
            {
                Container localPlayer = session.GetPlayerFromId(localPlayerId);
                localPlayer.Require<StringCommandProperty>().SetTo(builder => builder.Append("select_model ").Append(m_WantedModelId.Value));
            }
            m_WantedModelId = null;
        }
    }
}