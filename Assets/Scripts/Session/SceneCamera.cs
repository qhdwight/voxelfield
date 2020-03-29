using UnityEngine;
using Util;

namespace Session
{
    [RequireComponent(typeof(Camera))]
    public class SceneCamera : SingletonBehavior<SceneCamera>
    {
        private Camera m_Camera;
        private AudioListener m_AudioListener;

        protected override void Awake()
        {
            base.Awake();
            m_Camera = GetComponent<Camera>();
            m_AudioListener = GetComponent<AudioListener>();
        }

        public void SetEnabled(bool isEnabled)
        {
            m_Camera.enabled = isEnabled;
            m_AudioListener.enabled = isEnabled;
        }
    }
}