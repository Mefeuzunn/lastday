using System.Collections.Generic;
using UnityEngine;

namespace ProjectSurvival.AI
{
    /// <summary>
    /// Coordinates all roaming zombie packs across the city.
    /// Implements mobile 60 FPS time-slicing by partitioning pack updates and pathfinding checks
    /// across 4 staggered frame buckets (CPU overhead < 0.5ms).
    /// </summary>
    public class ZombiePackManager : MonoBehaviour
    {
        private static ZombiePackManager instance;
        public static ZombiePackManager Instance
        {
            get
            {
                if (instance == null) instance = FindAnyObjectByType<ZombiePackManager>();
                return instance;
            }
            private set => instance = value;
        }

        private readonly List<ZombiePack> registeredPacks = new List<ZombiePack>();

        // 4 time-sliced buckets
        private readonly List<ZombiePack>[] buckets = new List<ZombiePack>[4];

        public IReadOnlyList<ZombiePack> ActivePacks => registeredPacks;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            for (int i = 0; i < 4; i++)
            {
                buckets[i] = new List<ZombiePack>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RegisterPack(ZombiePack pack)
        {
            if (pack == null || registeredPacks.Contains(pack)) return;

            registeredPacks.Add(pack);
            RebalanceBuckets();
        }

        public void UnregisterPack(ZombiePack pack)
        {
            if (pack == null) return;

            registeredPacks.Remove(pack);
            RebalanceBuckets();
        }

        private void RebalanceBuckets()
        {
            for (int i = 0; i < 4; i++)
            {
                buckets[i].Clear();
            }

            for (int i = 0; i < registeredPacks.Count; i++)
            {
                int bucketIndex = i % 4;
                buckets[bucketIndex].Add(registeredPacks[i]);
            }
        }

        public void SetAllPacksAlertDelayMultiplier(float mult)
        {
            if (registeredPacks.Count == 0)
            {
                var scenePacks = FindObjectsByType<ZombiePack>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < scenePacks.Length; i++)
                {
                    if (scenePacks[i] != null) scenePacks[i].AlertDelayMultiplier = mult;
                }
                return;
            }

            for (int i = 0; i < registeredPacks.Count; i++)
            {
                if (registeredPacks[i] != null)
                {
                    registeredPacks[i].AlertDelayMultiplier = mult;
                }
            }
        }

        private void Update()
        {
            if (registeredPacks.Count == 0) return;

            // Mobile 60 FPS Time-Slicing: only update packs in current frame bucket
            int currentBucket = Time.frameCount % 4;
            var activeBucket = buckets[currentBucket];

            // Compensate dt for 4-frame step
            float scaledDt = Time.deltaTime * 4f;

            for (int i = activeBucket.Count - 1; i >= 0; i--)
            {
                ZombiePack pack = activeBucket[i];
                if (pack != null && pack.gameObject.activeInHierarchy)
                {
                    pack.UpdatePack(scaledDt);
                }
                else if (pack == null)
                {
                    activeBucket.RemoveAt(i);
                }
            }
        }
    }
}
