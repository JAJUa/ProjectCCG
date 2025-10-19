using System;
using System.Collections.Generic;
using UnityEngine;
using SpiritAge.Core.Interfaces;

namespace SpiritAge.Utility.Pooling
{
    /// <summary>
    /// 오브젝트 풀링 매니저
    /// </summary>
    public class PoolManager : AbstractSingleton<PoolManager>
    {
        [Serializable]
        public class PoolData
        {
            public string key;
            public GameObject prefab;
            public int initialSize = 10;
            public int maxSize = 50;
            public bool autoExpand = true;
        }

        [SerializeField] private List<PoolData> poolConfigs = new List<PoolData>();
        private Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();
        private Transform _poolContainer;

        protected override void OnSingletonAwake()
        {
            InitializePools();
        }

        private void InitializePools()
        {
            _poolContainer = new GameObject("PoolContainer").transform;
            _poolContainer.SetParent(transform);

            foreach (var config in poolConfigs)
            {
                CreatePool(config.key, config.prefab, config.initialSize, config.maxSize, config.autoExpand);
            }
        }

        /// <summary>
        /// 새 풀 생성
        /// </summary>
        public void CreatePool(string key, GameObject prefab, int initialSize = 10, int maxSize = 50, bool autoExpand = true)
        {
            if (_pools.ContainsKey(key))
            {
                Debug.LogWarning($"[PoolManager] Pool '{key}' already exists.");
                return;
            }

            var poolGO = new GameObject($"Pool_{key}");
            poolGO.transform.SetParent(_poolContainer);

            var pool = new ObjectPool(prefab, poolGO.transform, initialSize, maxSize, autoExpand);
            _pools[key] = pool;
        }

        /// <summary>
        /// 오브젝트 가져오기
        /// </summary>
        public GameObject Spawn(string key, Vector3 position = default, Quaternion rotation = default)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogError($"[PoolManager] Pool '{key}' not found.");
                return null;
            }

            return pool.Get(position, rotation);
        }

        /// <summary>
        /// 오브젝트 반환
        /// </summary>
        public void Despawn(string key, GameObject obj, float delay = 0f)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogError($"[PoolManager] Pool '{key}' not found.");
                Destroy(obj);
                return;
            }

            if (delay > 0f)
            {
                StartCoroutine(DespawnDelayed(pool, obj, delay));
            }
            else
            {
                pool.Return(obj);
            }
        }

        private System.Collections.IEnumerator DespawnDelayed(ObjectPool pool, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            pool.Return(obj);
        }

        /// <summary>
        /// 모든 풀 정리
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
        }
    }

    /// <summary>
    /// 개별 오브젝트 풀
    /// </summary>
    public class ObjectPool
    {
        private GameObject _prefab;
        private Transform _container;
        private Queue<GameObject> _pool;
        private HashSet<GameObject> _active;
        private int _maxSize;
        private bool _autoExpand;

        public ObjectPool(GameObject prefab, Transform container, int initialSize, int maxSize, bool autoExpand)
        {
            _prefab = prefab;
            _container = container;
            _maxSize = maxSize;
            _autoExpand = autoExpand;
            _pool = new Queue<GameObject>(initialSize);
            _active = new HashSet<GameObject>();

            for (int i = 0; i < initialSize; i++)
            {
                CreateObject();
            }
        }

        private GameObject CreateObject()
        {
            var obj = GameObject.Instantiate(_prefab, _container);
            obj.SetActive(false);

            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnDespawn();

            return obj;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj = null;

            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else if (_autoExpand || _active.Count < _maxSize)
            {
                obj = CreateObject();
            }

            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
                _active.Add(obj);

                var poolable = obj.GetComponent<IPoolable>();
                poolable?.OnSpawn();
            }

            return obj;
        }

        public void Return(GameObject obj)
        {
            if (!_active.Contains(obj)) return;

            _active.Remove(obj);
            obj.SetActive(false);

            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnDespawn();
            poolable?.ResetState();

            _pool.Enqueue(obj);
        }

        public void Clear()
        {
            foreach (var obj in _active)
            {
                if (obj != null) GameObject.Destroy(obj);
            }

            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null) GameObject.Destroy(obj);
            }

            _active.Clear();
            _pool.Clear();
        }
    }
}