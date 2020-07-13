using UnityEngine;

namespace Voxelfield.Interface
{
    [RequireComponent(typeof(Camera))]
    public class MiniMapCamera : MonoBehaviour
    {
        [SerializeField] private Shader m_Replacement = default;
        
        private Camera m_Camera;

        private void Start()
        {
            m_Camera = GetComponent<Camera>();
            if (m_Replacement) m_Camera.SetReplacementShader(m_Replacement, string.Empty);
        }
    }
}