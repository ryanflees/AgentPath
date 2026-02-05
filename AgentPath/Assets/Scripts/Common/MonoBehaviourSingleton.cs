using UnityEngine;

namespace CR
{
    public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T>
    {
        public static T Instance;

        public static void CreateModule(bool persistent = false)
        {
            if (Instance == (T)null)
            {
                GameObject obj = new GameObject();
                obj.name = typeof(T).Name;
                obj.AddComponent<T>();
                if (persistent)
                {
                    DontDestroyOnLoad(obj);
                }
            }
        }

        protected void Awake()
        {
            if ((Object)Instance != (Object)null)
            {
                Debug.LogError("Multiple instances of singleton class: " + base.GetType().ToString() + "\n");
                Debug.Break();
            }
            Instance = (T)this;
            OnAwake();
        }

        protected void Start()
        {
            OnStart();
        }

        protected virtual void OnAwake() { }

        protected virtual void OnStart() { }

        protected void OnDestroy()
        {
            OnDestroyOverride();
            if (Instance == (T)this)
            {
                Instance = (T)null;
            }
        }

        protected void OnApplicationQuit()
        {
            Instance = (T)null;
        }

        protected virtual void OnDestroyOverride()
        {

        }

        public virtual void OnUpdate(float dt) { }
        public virtual void OnFixedUpdated(float dt) { }
        public virtual void OnLateUpdate(float dt) { }

    }
}
