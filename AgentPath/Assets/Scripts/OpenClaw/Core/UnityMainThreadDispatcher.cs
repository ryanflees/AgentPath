using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace CR.OpenClaw
{
    /// <summary>
    /// Unity Main Thread Dispatcher
    /// Allows safe execution of Unity API calls from background threads
    /// </summary>
    public class UnityMainThreadDispatcher : MetaGameSingleton<UnityMainThreadDispatcher>
    {
        //private static UnityMainThreadDispatcher m_Instance;
        private static readonly ConcurrentQueue<Action> m_ActionQueue = new ConcurrentQueue<Action>();

        #region Public API

        /// <summary>
        /// Enqueue an action to be executed on the main thread
        /// </summary>
        public void Enqueue(Action action)
        {
            if (action == null)
            {
                Debug.LogWarning("[Dispatcher] Cannot enqueue null action");
                return;
            }

            m_ActionQueue.Enqueue(action);
        }

        /// <summary>
        /// Enqueue an action with parameter to be executed on the main thread
        /// </summary>
        public void Enqueue<T>(Action<T> action, T parameter)
        {
            if (action == null)
            {
                Debug.LogWarning("[Dispatcher] Cannot enqueue null action");
                return;
            }

            m_ActionQueue.Enqueue(() => action(parameter));
        }

        /// <summary>
        /// Enqueue a function and get result via callback
        /// </summary>
        public void Enqueue<TResult>(Func<TResult> function, Action<TResult> callback)
        {
            if (function == null || callback == null)
            {
                Debug.LogWarning("[Dispatcher] Cannot enqueue null function or callback");
                return;
            }

            m_ActionQueue.Enqueue(() =>
            {
                TResult result = function();
                callback(result);
            });
        }

        /// <summary>
        /// Execute action immediately if on main thread, otherwise enqueue it
        /// </summary>
        public void ExecuteOnMainThread(Action action)
        {
            if (action == null) return;

            if (IsMainThread())
            {
                action();
            }
            else
            {
                Enqueue(action);
            }
        }

        /// <summary>
        /// Check if current thread is Unity main thread
        /// </summary>
        public static bool IsMainThread()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId == 1;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // Execute all queued actions on the main thread
            while (m_ActionQueue.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Dispatcher] Error executing action: {ex.Message}");
                    Debug.LogException(ex);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // Clear queue
            while (m_ActionQueue.TryDequeue(out _)) { }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Find GameObject by name on main thread
        /// </summary>
        public void FindGameObject(string name, Action<GameObject> callback)
        {
            Enqueue(() =>
            {
                GameObject obj = GameObject.Find(name);
                callback?.Invoke(obj);
            });
        }

        /// <summary>
        /// Find component by type on main thread
        /// </summary>
        public void FindComponent<T>(Action<T> callback) where T : Component
        {
            Enqueue(() =>
            {
                T component = FindObjectOfType<T>();
                callback?.Invoke(component);
            });
        }

        /// <summary>
        /// Instantiate prefab on main thread
        /// </summary>
        public void InstantiatePrefab(GameObject prefab, Vector3 position, Quaternion rotation, Action<GameObject> callback = null)
        {
            Enqueue(() =>
            {
                GameObject instance = Instantiate(prefab, position, rotation);
                callback?.Invoke(instance);
            });
        }

        /// <summary>
        /// Destroy GameObject on main thread
        /// </summary>
        public void DestroyGameObject(GameObject obj, float delay = 0f)
        {
            Enqueue(() =>
            {
                if (obj != null)
                {
                    Destroy(obj, delay);
                }
            });
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for easier main thread execution
    /// </summary>
    public static class MainThreadExtensions
    {
        /// <summary>
        /// Execute action on main thread
        /// </summary>
        public static void RunOnMainThread(this MonoBehaviour monoBehaviour, Action action)
        {
            UnityMainThreadDispatcher.Instance.Enqueue(action);
        }

        /// <summary>
        /// Execute action with parameter on main thread
        /// </summary>
        public static void RunOnMainThread<T>(this MonoBehaviour monoBehaviour, Action<T> action, T parameter)
        {
            UnityMainThreadDispatcher.Instance.Enqueue(action, parameter);
        }

        /// <summary>
        /// Execute function and get result on main thread
        /// </summary>
        public static void RunOnMainThread<TResult>(this MonoBehaviour monoBehaviour, Func<TResult> function, Action<TResult> callback)
        {
            UnityMainThreadDispatcher.Instance.Enqueue(function, callback);
        }
    }
}
