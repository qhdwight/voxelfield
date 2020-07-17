using System.Linq;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Sessions
{
    [RequireComponent(typeof(Camera))]
    public class SceneCamera : SingletonBehavior<SceneCamera>
    {
        private AudioListener m_AudioListener;
        private Camera m_Camera;

        protected override void Awake()
        {
            base.Awake();
            m_Camera = GetComponent<Camera>();
            m_AudioListener = GetComponent<AudioListener>();
        }

        private void LateUpdate()
        {
            int count = Camera.allCameras.Count(activeCamera => !activeCamera.targetTexture);
            if (count > 1)
                SetEnabled(false);
            else if (count < 1)
                SetEnabled(true);
        }

        private void SetEnabled(bool isEnabled)
        {
            m_Camera.enabled = isEnabled;
            m_AudioListener.enabled = isEnabled;
        }
    }
}