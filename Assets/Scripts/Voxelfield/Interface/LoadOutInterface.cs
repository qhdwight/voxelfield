using System.Linq;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Voxelfield.Interface
{
    public class LoadOutInterface : InterfaceBehaviorBase
    {
        private LoadOutButton[][] m_LoadOutButtons;

        protected override void Awake()
        {
            base.Awake();
            m_LoadOutButtons = transform.Cast<Transform>()
                                        .Select(horizontal => horizontal.Cast<Transform>()
                                                                        .Select(vertical => vertical.GetComponent<LoadOutButton>())
                                                                        .Where(loadOut => loadOut).ToArray()).ToArray();
        }
    }
}