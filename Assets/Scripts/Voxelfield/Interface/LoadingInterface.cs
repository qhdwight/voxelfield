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
            SessionBase session = SessionBase.SessionEnumerable.FirstOrDefault(_session => _session.IsLoading);
            bool isVisible = session != null;
            if (isVisible)
            {
                MapProgressInfo progressInfo = session.GetChunkManager().ProgressInfo;
                m_Text.SetText(progressInfo.stage switch
                {
                    MapLoadingStage.CleaningUp   => "Cleaning up...",
                    MapLoadingStage.SettingUp    => "Setting up...",
                    MapLoadingStage.Generating   => "Generating terrain from save...",
                    MapLoadingStage.UpdatingMesh => "Generating and applying mesh...",
                    _                            => "..."
                });
                m_ProgressBar.value = progressInfo.progress;
            }
            SetInterfaceActive(isVisible);
        }
    }
}