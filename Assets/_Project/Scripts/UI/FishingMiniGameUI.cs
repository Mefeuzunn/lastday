using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Interaction;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;

namespace ProjectSurvival.UI
{
    /// <summary>
    /// Phase 3 (Step 3B): Tactical Northwest Lake Fishing Mini-Game UI.
    /// Manages tactile bite timing, emerald sweet-spot 1.35s reel window,
    /// SafeArea alignment, ModalWindowManager registration, and mobile haptic feedback.
    /// All static labels have raycastTarget = false; buttons have forgiving -8px padding.
    /// </summary>
    public class FishingMiniGameUI : MonoBehaviour
    {
        public static FishingMiniGameUI Instance { get; private set; }

        public enum FishingPhase
        {
            Inactive,
            WaitingForBite,
            BiteActive,
            Reeling,
            Result
        }

        [Header("State")]
        [SerializeField] private FishingPhase currentPhase = FishingPhase.Inactive;
        [SerializeField] private float sweetSpotWindow = 1.35f;

        private GameObject panelRoot;
        private Image sweetSpotZone;
        private RectTransform needleTransform;
        private Text statusMessageText;
        private Button reelButton;
        private Button cancelButton;
        private LakeFishingSpotInteractable activeSpot;
        private Vector3 activeWaterTarget;

        private float biteWaitTimer = 0f;
        private float biteActiveTimer = 0f;
        private float needleT = 0f;
        private bool needleForward = true;

        public bool IsOpen => currentPhase != FishingPhase.Inactive && panelRoot != null && panelRoot.activeSelf;
        public FishingPhase CurrentPhase => currentPhase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildUIHierarchyIfEmpty();
        }

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (currentPhase == FishingPhase.Inactive) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelMiniGame();
                return;
            }

            // Animate tension needle
            AnimateNeedle();

            switch (currentPhase)
            {
                case FishingPhase.WaitingForBite:
                    biteWaitTimer -= Time.deltaTime;
                    if (biteWaitTimer <= 0f)
                    {
                        TriggerBite();
                    }
                    break;

                case FishingPhase.BiteActive:
                    biteActiveTimer -= Time.deltaTime;
                    if (biteActiveTimer <= 0f)
                    {
                        // Player missed the 1.35s sweet-spot window
                        OnBiteMissed();
                    }
                    break;
            }
        }

        public void StartMiniGame(LakeFishingSpotInteractable spot, Vector3 waterTarget)
        {
            activeSpot = spot;
            activeWaterTarget = waterTarget;

            BuildUIHierarchyIfEmpty();
            if (panelRoot != null) panelRoot.SetActive(true);

            ModalWindowManager.Instance?.NotifyModalOpened(this);

            currentPhase = FishingPhase.WaitingForBite;
            biteWaitTimer = UnityEngine.Random.Range(2.5f, 4.5f);
            biteActiveTimer = 0f;

            if (statusMessageText != null)
            {
                statusMessageText.text = "Yem suda yüzüyor... Balık bekleniyor.";
                statusMessageText.color = new Color(0.85f, 0.90f, 0.95f);
            }

            if (sweetSpotZone != null)
            {
                sweetSpotZone.color = new Color(0.20f, 0.35f, 0.28f, 0.65f); // Dim idle
            }

            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        private void TriggerBite()
        {
            currentPhase = FishingPhase.BiteActive;
            biteActiveTimer = sweetSpotWindow;

            // Plunge bobber visual and emit water splash
            if (activeSpot != null)
            {
                activeSpot.TriggerBobberPlunge(activeWaterTarget);
            }

            MobileHapticManager.TriggerMedium();

            if (statusMessageText != null)
            {
                statusMessageText.text = "ISIRDI! ŞİMDİ ÇEK!";
                statusMessageText.color = new Color(0.20f, 0.95f, 0.55f); // Bright emerald
            }

            if (sweetSpotZone != null)
            {
                sweetSpotZone.color = new Color(0.06f, 0.72f, 0.45f, 0.95f); // Vibrant active emerald
            }
        }

        public void OnReelButtonClicked()
        {
            if (currentPhase == FishingPhase.WaitingForBite)
            {
                // Reeled too early!
                OnEarlyReel();
            }
            else if (currentPhase == FishingPhase.BiteActive)
            {
                // Sweet spot success!
                OnSuccessfulCatch();
            }
        }

        private void OnSuccessfulCatch()
        {
            currentPhase = FishingPhase.Result;

            // Audio & Haptics
            ProceduralAudioManager.Instance?.PlayReelClick(activeWaterTarget);
            ProceduralAudioManager.Instance?.PlayWaterSplash(activeWaterTarget, 0.75f);
            MobileHapticManager.TriggerLight();

            // Reward distribution: 90% Raw Fish, 10% Salvaged Bottle/Scrap
            string rewardItemId = "item_raw_fish";
            string rewardName = "Taze Göl Balığı";
            float roll = UnityEngine.Random.value;
            if (roll < 0.05f)
            {
                rewardItemId = "item_scrap";
                rewardName = "Hurda Metal";
            }
            else if (roll < 0.10f)
            {
                rewardItemId = "item_water_bottle";
                rewardName = "Temiz Su Şişesi";
            }

            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.AddItemById(rewardItemId, 1);
            }

            SurvivorProgressionManager.Instance?.AddXP(25);
            GameEvents.TriggerObjectiveToastRequested($"[BALIK TUTULDU] 1x {rewardName} (+25 XP)", 3.0f);

            if (statusMessageText != null)
            {
                statusMessageText.text = $"YAKALANDI: {rewardName}!";
                statusMessageText.color = new Color(0.25f, 0.95f, 0.45f);
            }

            StartCoroutine(CloseAfterDelay(1.2f));
        }

        private void OnEarlyReel()
        {
            currentPhase = FishingPhase.Result;
            ProceduralAudioManager.Instance?.PlayPush();
            GameEvents.TriggerObjectiveToastRequested("Erken Çektin! Balık Ürktü.", 2.5f);

            if (statusMessageText != null)
            {
                statusMessageText.text = "Erken Çektin — Balık Kaçtı!";
                statusMessageText.color = new Color(0.95f, 0.40f, 0.35f);
            }

            StartCoroutine(CloseAfterDelay(1.0f));
        }

        private void OnBiteMissed()
        {
            currentPhase = FishingPhase.Result;
            ProceduralAudioManager.Instance?.PlaySoftError();
            GameEvents.TriggerObjectiveToastRequested("Geç Kaldın! Balık Yemi Kaptı.", 2.5f);

            if (statusMessageText != null)
            {
                statusMessageText.text = "Geç Kaldın — Balık Kaçtı!";
                statusMessageText.color = new Color(0.95f, 0.40f, 0.35f);
            }

            StartCoroutine(CloseAfterDelay(1.0f));
        }

        private IEnumerator CloseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            CloseMiniGame();
        }

        public void CancelMiniGame()
        {
            GameEvents.TriggerObjectiveToastRequested("Balık Avı İptal Edildi", 2.0f);
            CloseMiniGame();
        }

        public void CloseMiniGame()
        {
            currentPhase = FishingPhase.Inactive;
            if (activeSpot != null)
            {
                activeSpot.EndFishingSession();
                activeSpot = null;
            }

            ModalWindowManager.Instance?.NotifyModalClosed(this);

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void AnimateNeedle()
        {
            if (needleTransform == null) return;

            float speed = currentPhase == FishingPhase.BiteActive ? 2.4f : 1.2f;
            if (needleForward)
            {
                needleT += Time.deltaTime * speed;
                if (needleT >= 1f) { needleT = 1f; needleForward = false; }
            }
            else
            {
                needleT -= Time.deltaTime * speed;
                if (needleT <= 0f) { needleT = 0f; needleForward = true; }
            }

            float xPos = Mathf.Lerp(-160f, 160f, needleT);
            needleTransform.anchoredPosition = new Vector2(xPos, 0f);
        }

        // =========================================================================
        // UI HIERARCHY BUILDER (SafeArea + Non-Blocking Raycasts)
        // =========================================================================

        public void BuildUIHierarchyIfEmpty()
        {
            if (panelRoot != null) return;

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();
            if (parentCanvas == null) return;

            // Root Panel (Aligned to bottom center inside SafeArea)
            panelRoot = new GameObject("FishingMiniGame_PanelRoot");
            panelRoot.transform.SetParent(transform, false);

            RectTransform rootRect = panelRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 32f);
            rootRect.sizeDelta = new Vector2(440f, 210f);

            // Background Card
            Image bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.11f, 0.14f, 0.94f);
            bg.raycastTarget = false;

            // SafeArea Handler
            panelRoot.AddComponent<SafeAreaHandler>();

            // Title
            GameObject titleObj = new GameObject("Title_Text");
            titleObj.transform.SetParent(panelRoot.transform, false);
            RectTransform tr = titleObj.AddComponent<RectTransform>();
            tr.anchoredPosition = new Vector2(0f, 75f);
            tr.sizeDelta = new Vector2(400f, 28f);
            Text tText = titleObj.AddComponent<Text>();
            tText.text = "GÖLDE BALIK AVI";
            tText.fontSize = 15;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.95f, 0.85f, 0.45f);
            tText.raycastTarget = false;

            // Status Message
            GameObject statusObj = new GameObject("Status_Text");
            statusObj.transform.SetParent(panelRoot.transform, false);
            RectTransform sr = statusObj.AddComponent<RectTransform>();
            sr.anchoredPosition = new Vector2(0f, 48f);
            sr.sizeDelta = new Vector2(400f, 24f);
            statusMessageText = statusObj.AddComponent<Text>();
            statusMessageText.text = "Yem suda yüzüyor...";
            statusMessageText.fontSize = 13;
            statusMessageText.alignment = TextAnchor.MiddleCenter;
            statusMessageText.color = new Color(0.85f, 0.90f, 0.95f);
            statusMessageText.raycastTarget = false;

            // Gauge Bar Background (Width 360, Height 28)
            GameObject gaugeObj = new GameObject("Gauge_Bar");
            gaugeObj.transform.SetParent(panelRoot.transform, false);
            RectTransform gr = gaugeObj.AddComponent<RectTransform>();
            gr.anchoredPosition = new Vector2(0f, 10f);
            gr.sizeDelta = new Vector2(360f, 28f);
            Image gImg = gaugeObj.AddComponent<Image>();
            gImg.color = new Color(0.12f, 0.16f, 0.20f, 1f);
            gImg.raycastTarget = false;

            // Emerald Sweet-Spot Zone in Center (Width 120, Height 28)
            GameObject sweetObj = new GameObject("SweetSpot_Zone");
            sweetObj.transform.SetParent(gaugeObj.transform, false);
            RectTransform swr = sweetObj.AddComponent<RectTransform>();
            swr.anchoredPosition = Vector2.zero;
            swr.sizeDelta = new Vector2(120f, 28f);
            sweetSpotZone = sweetObj.AddComponent<Image>();
            sweetSpotZone.color = new Color(0.06f, 0.72f, 0.45f, 0.75f);
            sweetSpotZone.raycastTarget = false;

            // Tension Needle (Width 6, Height 36)
            GameObject needleObj = new GameObject("Tension_Needle");
            needleObj.transform.SetParent(gaugeObj.transform, false);
            needleTransform = needleObj.AddComponent<RectTransform>();
            needleTransform.anchoredPosition = Vector2.zero;
            needleTransform.sizeDelta = new Vector2(6f, 36f);
            Image nImg = needleObj.AddComponent<Image>();
            nImg.color = new Color(1.0f, 0.95f, 0.35f, 1f);
            nImg.raycastTarget = false;

            // Big Reel Button (Height 64px, Touch Friendly with -8px padding)
            GameObject reelObj = new GameObject("Button_ReelIn");
            reelObj.transform.SetParent(panelRoot.transform, false);
            RectTransform rr = reelObj.AddComponent<RectTransform>();
            rr.anchoredPosition = new Vector2(-45f, -55f);
            rr.sizeDelta = new Vector2(230f, 64f);
            Image rImg = reelObj.AddComponent<Image>();
            rImg.color = new Color(0.15f, 0.65f, 0.40f, 1f);
            rImg.raycastTarget = true;
            rImg.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);

            reelButton = reelObj.AddComponent<Button>();
            reelButton.targetGraphic = rImg;
            reelButton.onClick.AddListener(OnReelButtonClicked);

            GameObject rTxtObj = new GameObject("Reel_Text");
            rTxtObj.transform.SetParent(reelObj.transform, false);
            RectTransform rtr = rTxtObj.AddComponent<RectTransform>();
            rtr.sizeDelta = rr.sizeDelta;
            Text rt = rTxtObj.AddComponent<Text>();
            rt.text = "ÇEK! (OLTAYI TOPLA)";
            rt.fontSize = 15;
            rt.fontStyle = FontStyle.Bold;
            rt.alignment = TextAnchor.MiddleCenter;
            rt.color = Color.white;
            rt.raycastTarget = false;

            // Cancel Button
            GameObject cancelObj = new GameObject("Button_Cancel");
            cancelObj.transform.SetParent(panelRoot.transform, false);
            RectTransform cr = cancelObj.AddComponent<RectTransform>();
            cr.anchoredPosition = new Vector2(145f, -55f);
            cr.sizeDelta = new Vector2(110f, 64f);
            Image cImg = cancelObj.AddComponent<Image>();
            cImg.color = new Color(0.35f, 0.38f, 0.42f, 1f);
            cImg.raycastTarget = true;
            cImg.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);

            cancelButton = cancelObj.AddComponent<Button>();
            cancelButton.targetGraphic = cImg;
            cancelButton.onClick.AddListener(CancelMiniGame);

            GameObject cTxtObj = new GameObject("Cancel_Text");
            cTxtObj.transform.SetParent(cancelObj.transform, false);
            RectTransform ctr = cTxtObj.AddComponent<RectTransform>();
            ctr.sizeDelta = cr.sizeDelta;
            Text ct = cTxtObj.AddComponent<Text>();
            ct.text = "İPTAL";
            ct.fontSize = 13;
            ct.fontStyle = FontStyle.Bold;
            ct.alignment = TextAnchor.MiddleCenter;
            ct.color = Color.white;
            ct.raycastTarget = false;
        }
    }
}
