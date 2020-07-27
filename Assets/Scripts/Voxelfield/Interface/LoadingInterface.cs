using System.Linq;
using Swihoni.Sessions;
using Swihoni.Util.Interface;
using UnityEngine.UI;
using Voxels;

namespace Voxelfield.Interface
{
    public class LoadingInterface : InterfaceBehaviorBase
    {
        private BufferedTextGui m_Text;
        private Slider m_ProgressBar;

        protected override void Awake()
        {
            m_ProgressBar = GetComponentInChildren<Slider>();
            m_Text = GetComponentInChildren<BufferedTextGui>();
            base.Awake();
        }

        private void Update()
        {
            bool isVisible = SessionBase.SessionEnumerable.Any(session => session.IsLoading);
            if (isVisible)
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
            }
            SetInterfaceActive(isVisible);
        }
    }
}