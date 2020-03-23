using UnityEngine;

[DisallowMultipleComponent]
public class SingletonBehavior<TSingleton> : MonoBehaviour where TSingleton : MonoBehaviour
{
    public static TSingleton Singleton { get; private set; }

    protected virtual void Awake()
    {
        if (Singleton)
            Destroy(gameObject);
        else
            Singleton = FindObjectOfType<TSingleton>();
    }
}