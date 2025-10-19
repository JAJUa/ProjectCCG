using UnityEngine;

namespace SpiritAge.Utility
{
    /// <summary>
    /// ¡¶≥◊∏Ø ΩÃ±€≈Ê √ﬂªÛ ≈¨∑°Ω∫
    /// </summary>
    public abstract class AbstractSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));

                        if (FindObjectsOfType(typeof(T)).Length > 1)
                        {
                            Debug.LogError($"[Singleton] Multiple instances of '{typeof(T)}' found!");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singletonGO = new GameObject();
                            _instance = singletonGO.AddComponent<T>();
                            singletonGO.name = $"(Singleton) {typeof(T)}";
                            DontDestroyOnLoad(singletonGO);

                            Debug.Log($"[Singleton] Instance of '{typeof(T)}' created.");
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _applicationIsQuitting = true;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        /// <summary>
        /// ΩÃ±€≈Ê √ ±‚»≠ Ω√ »£√‚
        /// </summary>
        protected virtual void OnSingletonAwake() { }
    }
}