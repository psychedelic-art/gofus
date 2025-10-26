using UnityEngine;

namespace GOFUS.Core
{
    /// <summary>
    /// Generic Singleton pattern for Unity MonoBehaviours
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static object lockObj = new object();
        private static bool applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again.");
                    return null;
                }

                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = (T)FindAnyObjectByType(typeof(T));

                        if (FindObjectsByType(typeof(T), FindObjectsSortMode.None).Length > 1)
                        {
                            Debug.LogError("[Singleton] Something went really wrong - there should never be more than 1 singleton!");
                            return instance;
                        }

                        if (instance == null)
                        {
                            GameObject singleton = new GameObject();
                            instance = singleton.AddComponent<T>();
                            singleton.name = $"(Singleton) {typeof(T).Name}";

                            DontDestroyOnLoad(singleton);
                            Debug.Log($"[Singleton] An instance of {typeof(T)} is needed in the scene, so '{singleton}' was created with DontDestroyOnLoad.");
                        }
                    }

                    return instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate instance of {typeof(T)} found. Destroying this instance.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }
    }
}