using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.Interaction
{
    public enum FurnitureType
    {
        DrawerDesk,
        KitchenCupboard,
        Refrigerator,
        MedicalCabinet,
        LockerOrWardrobe
    }

    [System.Serializable]
    public class FurnitureLootItem
    {
        public string itemId;
        public int quantity = 1;
    }

    /// <summary>
    /// Interactive Interior Furniture & Appliances Archetype.
    /// Supports drawer sliding, cabinet door swing articulation, contextual room loot,
    /// and interior stealth hiding for lockers and wardrobes.
    /// </summary>
    public class FurnitureInteractable : WorldInteractableObject
    {
        [Header("Furniture Specifics")]
        [SerializeField] private FurnitureType furnitureType = FurnitureType.DrawerDesk;
        [SerializeField] private Transform slidingPart; // Drawer or cabinet door
        [SerializeField] private Vector3 slideOffset = new Vector3(0f, 0f, 0.45f);
        [SerializeField] private Vector3 doorOpenEuler = new Vector3(0f, 90f, 0f);
        [SerializeField] private bool isDoorHinge = true;
        [SerializeField] private Transform peekTransform;
        [SerializeField] private Transform exitTransform;

        [Header("Custom Specific Rations")]
        [SerializeField] private List<FurnitureLootItem> customLoot = new List<FurnitureLootItem>();

        public IReadOnlyList<FurnitureLootItem> CustomLoot => customLoot;

        public void SetCustomLoot(params (string id, int count)[] items)
        {
            customLoot.Clear();
            foreach (var item in items)
            {
                customLoot.Add(new FurnitureLootItem { itemId = item.id, quantity = item.count });
            }
        }

        private Vector3 initialPartPos;
        private Quaternion initialPartRot;
        private bool isPlayerHidden = false;
        private Coroutine animRoutine;

        public override string ObjectSubtitle => hasBeenSearched ? "AÇIK & BOŞ" : (isOpen ? "AÇIK" : "ARANABİLİR");

        protected override void Awake()
        {
            base.Awake();
            DetectTypeFromHierarchy();
            SetupPartDefaults();
        }

        private void DetectTypeFromHierarchy()
        {
            string n = gameObject.name.ToLower();
            if (n.Contains("fridge") || n.Contains("refrigerator"))
            {
                furnitureType = FurnitureType.Refrigerator;
                objectTitle = "Buzdolabı";
                isDoorHinge = true;
            }
            else if (n.Contains("clinic") || n.Contains("medical") || n.Contains("ecza"))
            {
                furnitureType = FurnitureType.MedicalCabinet;
                objectTitle = "Klinik Ecza Dolabı";
                isDoorHinge = true;
            }
            else if (n.Contains("locker") || n.Contains("wardrobe") || n.Contains("gardrop"))
            {
                furnitureType = FurnitureType.LockerOrWardrobe;
                objectTitle = "Metal Soyunma Dolabı";
                isDoorHinge = true;
            }
            else if (n.Contains("kitchen") || n.Contains("cupboard") || n.Contains("tezgah"))
            {
                furnitureType = FurnitureType.KitchenCupboard;
                objectTitle = "Mutfak Dolabı";
                isDoorHinge = true;
            }
            else
            {
                furnitureType = FurnitureType.DrawerDesk;
                objectTitle = "Çekmeceli Çalışma Masası";
                isDoorHinge = false;
            }
        }

        private void SetupPartDefaults()
        {
            if (slidingPart == null)
            {
                slidingPart = transform.Find("Door") ?? transform.Find("Drawer") ?? transform.Find("Lid");
                if (slidingPart == null && transform.childCount > 0)
                {
                    slidingPart = transform.GetChild(0);
                }
            }

            if (slidingPart != null)
            {
                initialPartPos = slidingPart.localPosition;
                initialPartRot = slidingPart.localRotation;
            }

            if (peekTransform == null)
            {
                GameObject peek = new GameObject("Furniture_PeekPoint");
                peek.transform.SetParent(transform, false);
                peek.transform.localPosition = new Vector3(0f, 1.4f, 0.2f);
                peekTransform = peek.transform;
            }

            if (exitTransform == null)
            {
                GameObject exit = new GameObject("Furniture_ExitPoint");
                exit.transform.SetParent(transform, false);
                exit.transform.localPosition = new Vector3(0f, 0f, 1.2f);
                exitTransform = exit.transform;
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (isPlayerHidden)
            {
                verbs.Add(new InteractionVerb("exit_locker", "DOLAPTAN DIŞARI ÇIK", 0f));
                return verbs;
            }

            // Verb 1: Search / Open (holdDuration = 0.6s)
            if (!hasBeenSearched)
            {
                string searchLabel = isDoorHinge ? "KAPAĞI AÇ VE ARA" : "ÇEKMECELERİ ARA";
                verbs.Add(new InteractionVerb("search", searchLabel, 0.6f, null, null, 8f));
            }

            // Verb 2: Hide (Wardrobes/Lockers only)
            if (furnitureType == FurnitureType.LockerOrWardrobe)
            {
                verbs.Add(new InteractionVerb("hide", "DOLABA SAKLAN", 0.6f));
            }

            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            switch (verbId)
            {
                case "search":
                    ExecuteSearch();
                    break;
                case "hide":
                    ExecuteHide(interactor);
                    break;
                case "exit_locker":
                    ExecuteExitLocker(interactor);
                    break;
            }
        }

        private void ExecuteSearch()
        {
            hasBeenSearched = true;
            isOpen = true;
            AnimateOpen();
            ProjectSurvival.Progression.SurvivorProgressionManager.Instance?.RegisterScavengeAction("Dolap/Konteyner");

            if (InventorySystem.Instance != null)
            {
                if (customLoot != null && customLoot.Count > 0)
                {
                    foreach (var loot in customLoot)
                    {
                        if (string.IsNullOrEmpty(loot.itemId) || loot.quantity <= 0) continue;
                        ItemData data = LoadItem(loot.itemId);
                        if (data != null)
                        {
                            InventorySystem.Instance.AddItem(data, loot.quantity);
                        }
                        else
                        {
                            InventoryManager.AddItem(loot.itemId, loot.quantity);
                        }
                        ProjectSurvival.Narrative.StarterPrologueDirector.Instance?.OnItemLooted(loot.itemId, loot.quantity);
                        ProjectSurvival.UI.QuickSlotUI.Instance?.AutoAssignStarterRation(loot.itemId);
                    }
                }
                else
                {
                    switch (furnitureType)
                    {
                        case FurnitureType.Refrigerator:
                        case FurnitureType.KitchenCupboard:
                            InventorySystem.Instance.AddItem(LoadItem("item_canned_food"), 1);
                            InventorySystem.Instance.AddItem(LoadItem("item_water_bottle"), 1);
                            ProjectSurvival.Narrative.StarterPrologueDirector.Instance?.OnItemLooted("item_canned_food", 1);
                            ProjectSurvival.Narrative.StarterPrologueDirector.Instance?.OnItemLooted("item_water_bottle", 1);
                            ProjectSurvival.UI.QuickSlotUI.Instance?.AutoAssignStarterRation("item_canned_food");
                            ProjectSurvival.UI.QuickSlotUI.Instance?.AutoAssignStarterRation("item_water_bottle");
                            break;
                        case FurnitureType.MedicalCabinet:
                            InventorySystem.Instance.AddItem(LoadItem("item_bandage"), 2);
                            InventorySystem.Instance.AddItem(LoadItem("item_splint"), 1);
                            InventorySystem.Instance.AddItem(LoadItem("item_painkillers"), 1);
                            InventorySystem.Instance.AddItem(LoadItem("item_alcohol"), 1);
                            ProjectSurvival.Narrative.StarterPrologueDirector.Instance?.OnItemLooted("item_bandage", 2);
                            ProjectSurvival.UI.QuickSlotUI.Instance?.AutoAssignStarterRation("item_bandage");
                            break;
                        case FurnitureType.LockerOrWardrobe:
                            InventorySystem.Instance.AddItem(LoadItem("item_cloth"), 2);
                            InventorySystem.Instance.AddItem(LoadItem("item_scrap"), 1);
                            break;
                        default: // Desk
                            InventorySystem.Instance.AddItem(LoadItem("item_battery"), 1);
                            InventorySystem.Instance.AddItem(LoadItem("item_scrap"), 2);
                            break;
                    }
                }
            }

            ProceduralAudioManager.Instance?.PlayItemPickup();
            GameEvents.TriggerObjectiveToastRequested($"{objectTitle.ToUpper()} ARANDI — MALZEMELER ALINDI", 2.0f);
        }

        private void ExecuteHide(GameObject player)
        {
            if (player == null) return;
            isPlayerHidden = true;

            var cc = player.GetComponent<CharacterController>();
            var pc = player.GetComponent<PlayerController>();
            var visual = player.transform.Find("Visual_Model");

            if (cc != null) cc.enabled = false;
            if (pc != null) pc.enabled = false;
            if (visual != null) visual.gameObject.SetActive(false);

            player.transform.position = transform.position;
            ThirdPersonCamera.Instance?.EnterHidingPeek(peekTransform);
            GameEvents.TriggerPlayerHidingStateChanged(true);
            GameEvents.TriggerObjectiveToastRequested("DOLAP İÇİNE SAKLANILDI", 2.0f);
        }

        private void ExecuteExitLocker(GameObject player)
        {
            if (player == null) return;
            isPlayerHidden = false;

            Vector3 exitPos = exitTransform != null ? exitTransform.position : (transform.position + transform.forward * 1.2f);
            player.transform.position = exitPos;

            var visual = player.transform.Find("Visual_Model");
            var cc = player.GetComponent<CharacterController>();
            var pc = player.GetComponent<PlayerController>();

            if (visual != null) visual.gameObject.SetActive(true);
            if (cc != null) cc.enabled = true;
            if (pc != null) pc.enabled = true;

            ThirdPersonCamera.Instance?.ExitHidingPeek();
            GameEvents.TriggerPlayerHidingStateChanged(false);
        }

        private void AnimateOpen()
        {
            if (slidingPart == null) return;
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(OpenAnimationRoutine());
        }

        private IEnumerator OpenAnimationRoutine()
        {
            float elapsed = 0f;
            float dur = 0.35f;

            Vector3 targetPos = isDoorHinge ? initialPartPos : initialPartPos + slideOffset;
            Quaternion targetRot = isDoorHinge ? initialPartRot * Quaternion.Euler(doorOpenEuler) : initialPartRot;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                if (isDoorHinge) slidingPart.localRotation = Quaternion.Slerp(initialPartRot, targetRot, t);
                else slidingPart.localPosition = Vector3.Lerp(initialPartPos, targetPos, t);
                yield return null;
            }

            if (isDoorHinge) slidingPart.localRotation = targetRot;
            else slidingPart.localPosition = targetPos;

            animRoutine = null;
        }

        private ItemData LoadItem(string itemId)
        {
            ItemData it = Resources.Load<ItemData>($"Items/{itemId}") ?? Resources.Load<ItemData>($"Data/Items/{itemId}");
#if UNITY_EDITOR
            if (it == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"{itemId} t:ItemData");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    it = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                }
            }
#endif
            return it;
        }
    }
}
