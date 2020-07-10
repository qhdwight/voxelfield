using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Interfaces;
using Swihoni.Util.Interface;
using UnityEngine.UI;
using Voxel;

namespace Voxelfield.Interface
{
    public class LoadingInterface : SessionInterfaceBehavior
    {
        private BufferedTextGui m_Text;
        private Slider m_ProgressBar;

        protected override void Awake()
        {
            m_ProgressBar = GetComponentInChildren<Slider>();
            m_Text = GetComponentInChildren<BufferedTextGui>();
            base.Awake();
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            MapProgressInfo progressInfo = ChunkManager.Singleton.ProgressInfo;
            switch (progressInfo.stage)
            {
                case MapLoadingStage.CleaningUp:
                    m_Text.SetText("Cleaning up...");
                    break;
                case MapLoadingStage.SettingUp:
                    m_Text.SetText("Setting up...");
                    break;
                case MapLoadingStage.Generating:
                    m_Text.SetText("Generating terrain from save...");
                    break;
                case MapLoadingStage.UpdatingMesh:
                    m_Text.SetText("Generating and applying mesh...");
                    break;
            }
            m_ProgressBar.value = progressInfo.progress;
            SetInterfaceActive(session.IsPaused);
        }
    }
}