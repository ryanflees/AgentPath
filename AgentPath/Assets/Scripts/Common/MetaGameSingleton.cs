using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CR
{
    public abstract class MetaGameSingleton<T> : MonoBehaviour where T : MetaGameSingleton<T>
    {
        public static T GetInstanceByForce()
        {
            if (Instance == null)
            {
                Instance = GameObject.FindAnyObjectByType<T>();
            }
            return Instance;
        }
        internal static T Instance;

        private void Awake()
        {
            if ((Object)Instance != (Object)null)
            {
                Object.Destroy(base.gameObject);
            }
            else
            {
                Instance = (T)this;
                if ((Object)base.transform.parent == (Object)null)
                {
                    Object.DontDestroyOnLoad(base.gameObject);
                }
                OnAwake();
            }
        }

        protected virtual void OnAwake()
        {
        }
    }
}
