using UnityEngine;
using System.Collections.Generic;
using Systems.Base;

namespace Systems.Managers
{
    /// <summary>
    /// ������Ʈ Ǯ���� �����ϴ� Ŭ�����Դϴ�.
    /// ������ �ؽ�Ʈ, ����Ʈ �� ���� ����/�����Ǵ� ������Ʈ�� ���˴ϴ�.
    /// </summary>
    public class PoolingManager : AbstractSingleton<PoolingManager>
    {
        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int size;
            // �ν����Ϳ��� �� Ǯ�� �θ� ������ �� �ֵ��� Transform ���� �߰�
            public Transform parent;
        }

        [Header("Pool List")]
        [SerializeField] private List<Pool> pools;

        private Dictionary<string, Queue<GameObject>> _poolDictionary;
        private Dictionary<string, Transform> _poolParents;

        public void Init()
        {
            _poolDictionary = new Dictionary<string, Queue<GameObject>>();
            _poolParents = new Dictionary<string, Transform>();

            foreach (Pool pool in pools)
            {
                Transform poolParent = pool.parent;

                // ���� �ν����Ϳ��� �θ� �������� �ʾҴٸ�, �ڵ����� �����մϴ�.
                if (poolParent == null)
                {
                    GameObject parentObj = new GameObject($"{pool.tag} Pool");
                    poolParent = parentObj.transform;
                    poolParent.SetParent(transform);
                }

                _poolParents.Add(pool.tag, poolParent);

                Queue<GameObject> objectPool = new Queue<GameObject>();
                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = Instantiate(pool.prefab, poolParent);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }
                _poolDictionary.Add(pool.tag, objectPool);
            }

            // ����� �α� �߰�
            foreach (Pool pool in pools)
            {
                Debug.Log($"Pool created: {pool.tag} with {pool.size} objects");
            }
        }

        /// <summary>
        /// Ǯ���� ������Ʈ�� �����ɴϴ�.
        /// </summary>
        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
                return null;
            }

            Queue<GameObject> poolQueue = _poolDictionary[tag];
            GameObject objectToSpawn;

            if (poolQueue.Count > 0)
            {
                objectToSpawn = poolQueue.Dequeue();
            }
            else // Ǯ�� ��������� ���� ����
            {
                Pool pool = pools.Find(p => p.tag == tag);
                // ���� ������ ���� ������ �θ�(_poolParents[tag]) �ؿ� �����ǵ��� ����
                objectToSpawn = Instantiate(pool.prefab, _poolParents[tag]);
            }

            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.SetActive(true);

            IPoolable poolable = objectToSpawn.GetComponent<IPoolable>();
            poolable?.OnObjectSpawn();

            return objectToSpawn;
        }

        /// <summary>
        /// ����� ������Ʈ�� Ǯ�� ��ȯ�մϴ�.
        /// </summary>
        public void ReturnToPool(string tag, GameObject objectToReturn)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
                Destroy(objectToReturn);
                return;
            }

            // ������Ʈ�� �ٽ� ���� Ǯ�� �θ�� �ǵ����ϴ�.
            if (_poolParents.ContainsKey(tag))
            {
                objectToReturn.transform.SetParent(_poolParents[tag]);
            }

            objectToReturn.SetActive(false);
            _poolDictionary[tag].Enqueue(objectToReturn);
        }
    }

    /// <summary>
    /// Ǯ�� ������ ������Ʈ�� �����ؾ� �� �������̽�
    /// </summary>
    public interface IPoolable
    {
        void OnObjectSpawn();
    }
}

