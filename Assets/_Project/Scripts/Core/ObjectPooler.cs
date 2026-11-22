using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectSurvival.Core
{
    [Serializable]
    public class PoolItem
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
        public bool expandable = true;
    }

    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        [Header("Pool Definitions")]
        [SerializeField] private List<PoolItem> poolDefinitions = new List<PoolItem>();

        private readonly Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, PoolItem> configDictionary = new Dictionary<string, PoolItem>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var item in poolDefinitions)
            {
                if (item.prefab == null || string.IsNullOrEmpty(item.tag))
                    continue;

                configDictionary[item.tag] = item;
                Queue<GameObject> objectQueue = new Queue<GameObject>();

                for (int i = 0; i < item.initialSize; i++)
                {
                    GameObject obj = Instantiate(item.prefab, transform);
                    obj.SetActive(false);
                    objectQueue.Enqueue(obj);
                }

                poolDictionary[item.tag] = objectQueue;
            }
        }

        public GameObject SpawnFromPool(string poolTag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(poolTag))
            {
                Debug.LogWarning($"[ObjectPooler] Pool tag '{poolTag}' not found!");
                return null;
            }

            Queue<GameObject> queue = poolDictionary[poolTag];
            GameObject objToSpawn = null;

            if (queue.Count > 0)
            {
                objToSpawn = queue.Dequeue();
            }
            else if (configDictionary[poolTag].expandable)
            {
                objToSpawn = Instantiate(configDictionary[poolTag].prefab, transform);
            }
            else
            {
                Debug.LogWarning($"[ObjectPooler] Pool '{poolTag}' is empty and not expandable.");
                return null;
            }

            objToSpawn.transform.position = position;
            objToSpawn.transform.rotation = rotation;
            objToSpawn.SetActive(true);

            IPooledObject pooled = objToSpawn.GetComponent<IPooledObject>();
            pooled?.OnObjectSpawn();

            return objToSpawn;
        }

        public void ReturnToPool(string poolTag, GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(transform);

            if (poolDictionary.ContainsKey(poolTag))
            {
                poolDictionary[poolTag].Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
    }

    public interface IPooledObject
    {
        void OnObjectSpawn();
    }
}
