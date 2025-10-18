using UnityEngine;
using System.Collections.Generic;
using Systems.Base;

namespace Systems.Managers
{
    /// <summary>
    /// 오브젝트 풀링을 관리하는 클래스입니다.
    /// 데미지 텍스트, 이펙트 등 자주 생성/삭제되는 오브젝트에 사용됩니다.
    /// </summary>
    public class PoolingManager : AbstractSingleton<PoolingManager>
    {
        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int size;
            // 인스펙터에서 각 풀의 부모를 지정할 수 있도록 Transform 변수 추가
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

                // 만약 인스펙터에서 부모를 지정하지 않았다면, 자동으로 생성합니다.
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

            // 디버그 로그 추가
            foreach (Pool pool in pools)
            {
                Debug.Log($"Pool created: {pool.tag} with {pool.size} objects");
            }
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져옵니다.
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
            else // 풀이 비어있으면 새로 생성
            {
                Pool pool = pools.Find(p => p.tag == tag);
                // 새로 생성할 때도 지정된 부모(_poolParents[tag]) 밑에 생성되도록 수정
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
        /// 사용한 오브젝트를 풀에 반환합니다.
        /// </summary>
        public void ReturnToPool(string tag, GameObject objectToReturn)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
                Destroy(objectToReturn);
                return;
            }

            // 오브젝트를 다시 원래 풀의 부모로 되돌립니다.
            if (_poolParents.ContainsKey(tag))
            {
                objectToReturn.transform.SetParent(_poolParents[tag]);
            }

            objectToReturn.SetActive(false);
            _poolDictionary[tag].Enqueue(objectToReturn);
        }
    }

    /// <summary>
    /// 풀링 가능한 오브젝트가 구현해야 할 인터페이스
    /// </summary>
    public interface IPoolable
    {
        void OnObjectSpawn();
    }
}

