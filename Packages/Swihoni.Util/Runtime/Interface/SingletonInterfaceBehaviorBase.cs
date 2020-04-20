using UnityEngine;

namespace Swihoni.Util.Interface
{
    [DisallowMultipleComponent]
    public class SingletonInterfaceBehavior<TSingleton> : InterfaceBehaviorBase where TSingleton : InterfaceBehaviorBase
    {
        public static TSingleton Singleton { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (Singleton)
                Destroy(gameObject);
            else
                Singleton = FindObjectOfType<TSingleton>();
        }
    }
}