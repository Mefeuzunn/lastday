using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.AI
{
    public class ZombieSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private string poolTag = "Zombie_Standard";
        [SerializeField] private int maxZombies = 8;
        [SerializeField] private float spawnInterval = 10f;
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        private readonly List<GameObject> activeZombies = new List<GameObject>();
        private float nextSpawnTime = 0f;

        private void Start()
        {
            if (spawnPoints.Count == 0)
            {
                spawnPoints.Add(transform);
            }

            // Initial burst
            for (int i = 0; i < Mathf.Min(4, maxZombies); i++)
            {
                SpawnZombie();
            }
        }

        private void Update()
        {
            // Clean up inactive/recycled zombies
            activeZombies.RemoveAll(z => z == null || !z.activeSelf);

            if (activeZombies.Count < maxZombies && Time.time >= nextSpawnTime)
            {
                nextSpawnTime = Time.time + spawnInterval;
                SpawnZombie();
            }
        }

        public GameObject SpawnZombie()
        {
            if (ObjectPooler.Instance == null || spawnPoints.Count == 0) return null;

            Transform pt = spawnPoints[Random.Range(0, spawnPoints.Count)];
            GameObject zombie = ObjectPooler.Instance.SpawnFromPool(poolTag, pt.position, pt.rotation);
            if (zombie != null)
            {
                activeZombies.Add(zombie);
            }
            return zombie;
        }
    }
}
