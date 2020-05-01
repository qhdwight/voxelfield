using UnityEngine;

namespace Swihoni.Util.Interface
{
    [RequireComponent(typeof(BufferedTextGui))]
    public class FpsInterface : InterfaceBehaviorBase
    {
        [SerializeField] private float m_UpdateRate = 0.1f;

        private BufferedTextGui m_FramerateText;
        private IntervalAction m_UpdateFramerateAction;

        protected override void Awake()
        {
            base.Awake();
            m_FramerateText = GetComponent<BufferedTextGui>();
            m_UpdateFramerateAction = new IntervalAction(m_UpdateRate, UpdateFramerate);
        }

        private void Update() { m_UpdateFramerateAction.Update(Time.smoothDeltaTime); }

        private void UpdateFramerate()
        {
            float fps = 1.0f / Time.deltaTime;
            m_FramerateText.Set(builder => builder.AppendFormat("FPS: {0:F0}", fps));
        }
    }
}