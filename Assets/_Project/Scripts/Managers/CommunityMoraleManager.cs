using System;
using UnityEngine;
using ProjectSurvival.AI;

namespace ProjectSurvival.Managers
{
    /// <summary>
    /// Active Phase 5: High-Level Community Morale Manager & Coordinator.
    /// Manages emotional resonance and community cohesion across survivor settlements,
    /// integrating directly with CommunityOutpostManager, radio broadcast effects,
    /// and narrative moral choice impacts.
    /// </summary>
    public class CommunityMoraleManager : MonoBehaviour
    {
        private static CommunityMoraleManager _instance;
        public static CommunityMoraleManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CommunityMoraleManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CommunityMoraleManager");
                        _instance = go.AddComponent<CommunityMoraleManager>();
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private float localMorale = 75f;

        public float CurrentMorale
        {
            get
            {
                if (CommunityOutpostManager.Instance != null)
                {
                    return CommunityOutpostManager.Instance.CommunityMorale;
                }
                return localMorale;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public void AddMorale(float amount)
        {
            ModifyMorale(amount);
        }

        public void ModifyMorale(float delta)
        {
            localMorale = Mathf.Clamp(localMorale + delta, 0f, 100f);
            if (CommunityOutpostManager.Instance != null)
            {
                CommunityOutpostManager.Instance.ModifyMorale(delta);
            }
            Debug.Log($"[CommunityMoraleManager] Morale modified by {delta:+#;-#;0}%. Current Morale: {CurrentMorale:F0}%");
        }
    }
}
