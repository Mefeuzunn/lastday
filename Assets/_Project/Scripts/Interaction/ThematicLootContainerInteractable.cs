using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.Interaction
{
    public enum ThematicLootContainerType
    {
        // Sector -1, 0: West Urban Forest Park
        RangerSupplyCrate,
        HikerBackpack,
        PicnicCooler,

        // Sector 1, -1: Southeast Power Substation
        ElectricianToolbox,
        HighVoltageLocker,
        MaintenanceVanStorage
    }

    /// <summary>
    /// Pre-Phase 3 Optimization: Thematic Outer Sector Loot Container.
    /// Provides physical, persistent, scavengeable containers for under-stocked outer sectors.
    /// West Urban Forest Park (12 containers): Ranger supply crates, hiker backpacks, picnic coolers.
    /// Southeast Power Substation (10 containers): Toolboxes, high-voltage lockers, maintenance van stashes.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ThematicLootContainerInteractable : WorldInteractableObject
    {
        [Header("Container Configuration")]
        [SerializeField] private ThematicLootContainerType containerType = ThematicLootContainerType.RangerSupplyCrate;
        [SerializeField] private Transform lidTransform;
        [SerializeField] private Vector3 lidOpenRotation = new Vector3(-75f, 0f, 0f);
        [SerializeField] private Vector3 lidOpenOffset = new Vector3(0f, 0.25f, 0f);

        private Quaternion initialLidRot;
        private Vector3 initialLidPos;
        private Coroutine openRoutine;

        public ThematicLootContainerType ContainerType => containerType;
        public override string ObjectSubtitle => hasBeenSearched ? "BOŞALTILDI" : "ARANABİLİR";

        protected override void Awake()
        {
            base.Awake();
            DetectContainerDetails();

            if (lidTransform == null)
            {
                lidTransform = transform.Find("Lid") ?? transform.Find("Hinge") ?? (transform.childCount > 0 ? transform.GetChild(0) : null);
            }

            if (lidTransform != null)
            {
                initialLidRot = lidTransform.localRotation;
                initialLidPos = lidTransform.localPosition;
            }

            if (isOpen && lidTransform != null)
            {
                lidTransform.localRotation = initialLidRot * Quaternion.Euler(lidOpenRotation);
            }
        }

        public void SetContainerType(ThematicLootContainerType type)
        {
            containerType = type;
            DetectContainerDetails();
        }

        private void DetectContainerDetails()
        {
            switch (containerType)
            {
                case ThematicLootContainerType.RangerSupplyCrate:
                    objectTitle = "Orman Korucu Tedarik Sandığı";
                    objectSubtitle = "BOTANİK BİTKİLER & AV MÜHİMMATI";
                    break;
                case ThematicLootContainerType.HikerBackpack:
                    objectTitle = "Terk Edilmiş Dağcı Çantası";
                    objectSubtitle = "DOĞADA HAYATTA KALMA EKİPMANI";
                    break;
                case ThematicLootContainerType.PicnicCooler:
                    objectTitle = "Taşınabilir Piknik Soğutucusu";
                    objectSubtitle = "YİYECEK & TEMİZ SU KAYNAĞI";
                    break;
                case ThematicLootContainerType.ElectricianToolbox:
                    objectTitle = "Elektrikçi Takım Çantası";
                    objectSubtitle = "ENDÜSTRİYEL HURDA & PİLLER";
                    break;
                case ThematicLootContainerType.HighVoltageLocker:
                    objectTitle = "Yüksek Gerilim Dağıtım Dolabı";
                    objectSubtitle = "TRAFO EKİPMANI & YAKIT";
                    break;
                case ThematicLootContainerType.MaintenanceVanStorage:
                    objectTitle = "Teknik Servis Malzeme Kasası";
                    objectSubtitle = "ONARIM PARÇALARI & AKÜ";
                    break;
            }
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();
            if (!hasBeenSearched)
            {
                verbs.Add(new InteractionVerb("search", "KAPAĞI AÇ VE YAĞMALA", 1.2f, null, null, 10f));
            }
            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "search" && !hasBeenSearched)
            {
                ExecuteSearch();
            }
        }

        private void ExecuteSearch()
        {
            hasBeenSearched = true;
            isOpen = true;

            AnimateLidOpen();
            DispenseThematicLoot();

            ProjectSurvival.Progression.SurvivorProgressionManager.Instance?.RegisterScavengeAction(objectTitle);
            ProceduralAudioManager.Instance?.PlayItemPickup();
            GameEvents.TriggerObjectiveToastRequested($"{objectTitle.ToUpper()} YAĞMALANDI", 2.2f);
            MobileHapticManager.Instance?.TriggerLight();
        }

        private void DispenseThematicLoot()
        {
            if (InventorySystem.Instance == null) return;

            switch (containerType)
            {
                case ThematicLootContainerType.RangerSupplyCrate:
                    InventorySystem.Instance.AddItem(LoadItem("item_herbal_tea"), 2); // Botanical Herbs
                    InventorySystem.Instance.AddItem(LoadItem("item_bandage"), 2);
                    InventorySystem.Instance.AddItem(LoadItem("ammo_9mm"), 12);
                    break;

                case ThematicLootContainerType.HikerBackpack:
                    InventorySystem.Instance.AddItem(LoadItem("item_canned_food"), 2);
                    InventorySystem.Instance.AddItem(LoadItem("item_water_bottle"), 2);
                    InventorySystem.Instance.AddItem(LoadItem("item_bandage"), 1);
                    break;

                case ThematicLootContainerType.PicnicCooler:
                    InventorySystem.Instance.AddItem(LoadItem("item_canned_food"), 2);
                    InventorySystem.Instance.AddItem(LoadItem("item_water_bottle"), 2);
                    break;

                case ThematicLootContainerType.ElectricianToolbox:
                    InventorySystem.Instance.AddItem(LoadItem("item_scrap"), 3);
                    InventorySystem.Instance.AddItem(LoadItem("item_battery"), 2);
                    InventorySystem.Instance.AddItem(LoadItem("item_splint"), 1);
                    break;

                case ThematicLootContainerType.HighVoltageLocker:
                    InventorySystem.Instance.AddItem(LoadItem("item_battery"), 2);
                    InventorySystem.Instance.AddItem(LoadItem("item_alcohol"), 1);
                    InventorySystem.Instance.AddItem(LoadItem("item_fuel"), 1);
                    InventorySystem.Instance.AddItem(LoadItem("item_scrap"), 2);
                    break;

                case ThematicLootContainerType.MaintenanceVanStorage:
                    InventorySystem.Instance.AddItem(LoadItem("item_scrap"), 3);
                    InventorySystem.Instance.AddItem(LoadItem("item_fuel"), 1);
                    InventorySystem.Instance.AddItem(LoadItem("item_battery"), 1);
                    InventorySystem.Instance.AddItem(LoadItem("item_splint"), 1);
                    break;
            }
        }

        private ItemData LoadItem(string itemId)
        {
            return Resources.Load<ItemData>($"Items/{itemId}") ?? Resources.Load<ItemData>($"Data/Items/{itemId}") ?? Resources.Load<ItemData>(itemId);
        }

        private void AnimateLidOpen()
        {
            if (lidTransform == null) return;
            if (openRoutine != null) StopCoroutine(openRoutine);
            openRoutine = StartCoroutine(LidOpenCoroutine());
        }

        private IEnumerator LidOpenCoroutine()
        {
            float elapsed = 0f;
            float duration = 0.35f;
            Quaternion targetRot = initialLidRot * Quaternion.Euler(lidOpenRotation);
            Vector3 targetPos = initialLidPos + lidOpenOffset;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                lidTransform.localRotation = Quaternion.Slerp(initialLidRot, targetRot, t);
                lidTransform.localPosition = Vector3.Lerp(initialLidPos, targetPos, t);
                yield return null;
            }

            lidTransform.localRotation = targetRot;
            lidTransform.localPosition = targetPos;
        }

        public override string GetSerializedState()
        {
            return hasBeenSearched ? "searched" : (isOpen ? "open" : "closed");
        }

        public override void LoadSerializedState(string state)
        {
            if (state == "searched")
            {
                hasBeenSearched = true;
                isOpen = true;
            }
            else if (state == "open")
            {
                isOpen = true;
            }

            if (isOpen && lidTransform != null)
            {
                lidTransform.localRotation = initialLidRot * Quaternion.Euler(lidOpenRotation);
                lidTransform.localPosition = initialLidPos + lidOpenOffset;
            }
        }
    }
}
