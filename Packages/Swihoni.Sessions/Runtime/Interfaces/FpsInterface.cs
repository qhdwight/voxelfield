using Swihoni.Sessions.Config;
using Swihoni.Util;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    [RequireComponent(typeof(BufferedTextGui))]
    public class FpsInterface : InterfaceBehaviorBase
    {
        private BufferedTextGui m_FramerateText;
        private IntervalAction m_UpdateFramerateAction;

        protected override void Awake()
        {
            base.Awake();
            m_FramerateText = GetComponent<BufferedTextGui>();
            m_UpdateFramerateAction = new IntervalAction(ConfigManagerBase.Active.fpsUpdateRate, UpdateFramerate);
        }

        private void Update() => m_UpdateFramerateAction.Update(Time.deltaTime);

        private void UpdateFramerate()
        {
            float fps = 1.0f / Time.deltaTime;
            m_FramerateText.StartBuild().AppendFormat("FPS: {0:F0}", fps).Commit(m_FramerateText);
        }
    }
}