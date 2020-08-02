#if UNITY_EDITOR
using Swihoni.Sessions.Items.Visuals;
using Swihoni.Util.Animation;
using UnityEngine;

namespace Swihoni.Sessions
{
    [ExecuteInEditMode]
    public class ItemAnimatingBehavior : MonoBehaviour
    {
        private ArmIk m_ArmIk;
        private ItemVisualBehavior m_ItemVisuals;

        private void Awake() => gameObject.SetActive(false);

        private void Update()
        {
            var justUpdated = false;
            if (m_ArmIk)
            {
                if (!m_ItemVisuals)
                {
                    m_ItemVisuals = GetComponentInChildren<ItemVisualBehavior>();
                    justUpdated = true;
                }
            }
            else
            {
                m_ArmIk = GetComponentInChildren<ArmIk>();
            }
            if (justUpdated)
            {
                Transform t = m_ItemVisuals.transform;
                m_ArmIk.SetTargets(t.Find("IK.L"), t.Find("IK.R"));
            }
        }
    }
}
#endif