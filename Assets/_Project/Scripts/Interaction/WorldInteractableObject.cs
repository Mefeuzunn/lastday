using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Base class for all physical interactable objects across the 240x240m city.
    /// Manages persistent state flags, URP focus highlighting (subtle #FFD166 rim/emission),
    /// and provides an extensible multi-verb interaction architecture.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class WorldInteractableObject : MonoBehaviour, IUniversalInteractable
    {
        [Header("Object Identity")]
        [SerializeField] protected string objectTitle = "Terk Edilmiş Eşya";
        [SerializeField] protected string objectSubtitle = "Etkileşilebilir";
        [SerializeField] protected string persistentId = string.Empty;

        [Header("Persistent State Flags")]
        [SerializeField] protected bool hasBeenSearched = false;
        [SerializeField] protected bool isOpen = false;
        [SerializeField] protected bool isBarricaded = false;
        [SerializeField] protected bool isPowered = false;
        [SerializeField] protected bool isDamaged = false;

        [Header("Focus Highlighting")]
        [SerializeField] protected bool enableFocusHighlight = true;
        [SerializeField] protected Color focusEmissionColor = new Color(0.35f, 0.28f, 0.08f, 1f); // #FFD166 warm amber rim

        protected Renderer[] cachedRenderers;
        protected MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        protected bool isFocused = false;
        protected Collider mainCollider;

        public string ObjectTitle => objectTitle;
        public virtual string ObjectSubtitle => objectSubtitle;
        public Transform InteractionTransform => transform;
        public string PersistentId => string.IsNullOrEmpty(persistentId) ? (persistentId = GeneratePersistentId()) : persistentId;
        public void SetPersistentId(string id) => persistentId = id;
        public void SetObjectDetails(string title, string subtitle)
        {
            objectTitle = title;
            objectSubtitle = subtitle;
        }

        public bool HasBeenSearched => hasBeenSearched;
        public bool IsOpen => isOpen;
        public bool IsBarricaded => isBarricaded;
        public bool IsPowered => isPowered;
        public bool IsDamaged => isDamaged;

        protected virtual void Awake()
        {
            mainCollider = GetComponent<Collider>();
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            propBlock = new MaterialPropertyBlock();

            if (string.IsNullOrEmpty(persistentId))
            {
                persistentId = GeneratePersistentId();
            }
        }

        private string GeneratePersistentId()
        {
            Vector3 pos = transform.position;
            return $"{gameObject.name}_{Mathf.RoundToInt(pos.x)}_{Mathf.RoundToInt(pos.z)}";
        }

        public abstract IReadOnlyList<InteractionVerb> GetAvailableVerbs();

        public virtual bool CanInteract(string verbId)
        {
            var verbs = GetAvailableVerbs();
            for (int i = 0; i < verbs.Count; i++)
            {
                if (verbs[i].verbId == verbId)
                {
                    if (!verbs[i].isAvailable) return false;

                    // Check item requirement if any
                    if (!string.IsNullOrEmpty(verbs[i].requiredItemId))
                    {
                        if (InventorySystem.Instance != null && !InventorySystem.Instance.HasItem(verbs[i].requiredItemId))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public abstract void ExecuteVerb(string verbId, GameObject interactor);

        public virtual void SetFocused(bool focused)
        {
            if (isFocused == focused) return;
            isFocused = focused;

            if (!enableFocusHighlight || cachedRenderers == null || cachedRenderers.Length == 0) return;
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer r = cachedRenderers[i];
                if (r == null) continue;

                if (isFocused)
                {
                    r.GetPropertyBlock(propBlock);
                    propBlock.SetColor("_EmissionColor", focusEmissionColor);
                    r.SetPropertyBlock(propBlock);
                }
                else
                {
                    r.GetPropertyBlock(propBlock);
                    propBlock.SetColor("_EmissionColor", Color.black);
                    r.SetPropertyBlock(propBlock);
                }
            }
        }

        public virtual string GetSerializedState()
        {
            return $"{hasBeenSearched}|{isOpen}|{isBarricaded}|{isPowered}|{isDamaged}";
        }

        public virtual void LoadSerializedState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            string[] parts = state.Split('|');
            if (parts.Length >= 5)
            {
                bool.TryParse(parts[0], out hasBeenSearched);
                bool.TryParse(parts[1], out isOpen);
                bool.TryParse(parts[2], out isBarricaded);
                bool.TryParse(parts[3], out isPowered);
                bool.TryParse(parts[4], out isDamaged);
            }
        }
    }
}
