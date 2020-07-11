using Input;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Voxel;
using Voxelfield.Session;

namespace Voxelfield.Interface.Designer
{
    public class VoxelSelectInterface : SessionInterfaceBehavior
    {
        [SerializeField] private Button m_ButtonPrefab = default;
        private int? m_WantedId;

        private void Start()
        {
            for (byte id = 1; id <= VoxelId.Last; id++)
            {
                Button button = Instantiate(m_ButtonPrefab, transform);
                var text = button.GetComponentInChildren<TextMeshProUGUI>();
                text.SetText(VoxelId.Name(id));
                int _modelId = id;
                button.onClick.AddListener(() => m_WantedId = _modelId);
            }
        }

        public override void Render(SessionBase session, Container sessionContainer)
            => SetInterfaceActive(NoInterrupting() && sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.Designer &&
                                  InputProvider.Singleton.GetInput(InputType.OpenVoxelSelect));

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            if (m_WantedId.HasValue)
            {
                Container localCommands = session.GetLocalCommands();
                localCommands.Require<DesignerPlayerComponent>().selectedVoxelId.Value = (byte) m_WantedId.Value;
            }
            m_WantedId = null;
        }
    }
}