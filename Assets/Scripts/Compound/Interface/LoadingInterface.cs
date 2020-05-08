using System;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Util.Interface;
using UnityEngine;
using UnityEngine.UI;
using Voxel;

namespace Compound.Interface
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
                    m_Text.BuildText(builder => builder.Append("Cleaning up nonsense beforehand..."));
                    break;
                case MapLoadingStage.SettingUp:
                    m_Text.BuildText(builder => builder.Append("Setting up..."));
                    break;
                case MapLoadingStage.Generating:
                    m_Text.BuildText(builder => builder.Append("Generating from noise n' stuff..."));
                    break;
                case MapLoadingStage.UpdatingMesh:
                    m_Text.BuildText(builder => builder.Append("Putting triangles together in meaningful ways..."));
                    break;
            }
            m_ProgressBar.value = progressInfo.progress;
            SetInterfaceActive(session.IsPaused);
        }
    }
}