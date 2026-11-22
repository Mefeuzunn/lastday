using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Environment
{
    public class WaypointBeacon : MonoBehaviour
    {
        public static WaypointBeacon Instance { get; private set; }

        public enum BeaconStage
        {
            PoliceCruiserLoot = 0,
            TalkToDrAras = 1,
            SearchClinic = 2,
            ReachSafeHouse = 3,
            Completed = 4
        }

        [Header("State")]
        [SerializeField] private BeaconStage currentStage = BeaconStage.PoliceCruiserLoot;

        [Header("Stage Positions")]
        private readonly Vector3 posPoliceCar = new Vector3(0.5f, 0.4f, -22f);
        private readonly Vector3 posDrAras = new Vector3(-28f, 0.4f, 32f);
        private readonly Vector3 posClinic = new Vector3(-33f, 0.4f, 34f);
        private readonly Vector3 posSafeHouse = new Vector3(-36f, 0.4f, -30f);

        [Header("Visual Components")]
        private LineRenderer lightBeam;
        private Transform groundRing;
        private Transform billboardCanvas;
        private Text titleText;
        private Text distanceText;
        private Transform playerTransform;

        public BeaconStage CurrentStage => currentStage;
        public Vector3 CurrentTargetPosition => transform.position;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildVisuals();
        }

        private void Start()
        {
            FindPlayer();
            SetStage(BeaconStage.PoliceCruiserLoot);
        }

        private void OnEnable()
        {
            GameEvents.OnItemCollected += HandleItemCollected;
            GameEvents.OnDialogueStarted += HandleDialogueStarted;
            GameEvents.OnMissionCompleted += HandleMissionCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnItemCollected -= HandleItemCollected;
            GameEvents.OnDialogueStarted -= HandleDialogueStarted;
            GameEvents.OnMissionCompleted -= HandleMissionCompleted;
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void BuildVisuals()
        {
            // 1. Sky Light Column (Light Beam)
            GameObject beamObj = new GameObject("LightBeam");
            beamObj.transform.SetParent(transform, false);
            lightBeam = beamObj.AddComponent<LineRenderer>();
            lightBeam.useWorldSpace = false;
            lightBeam.positionCount = 2;
            lightBeam.SetPosition(0, Vector3.zero);
            lightBeam.SetPosition(1, Vector3.up * 45f); // 45 meters high into the sky
            lightBeam.startWidth = 0.55f;
            lightBeam.endWidth = 1.6f;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material beamMat = new Material(unlitShader);
            Color beamCol = new Color(0.2f, 0.85f, 1f, 0.45f);
            if (beamMat.HasProperty("_BaseColor")) beamMat.SetColor("_BaseColor", beamCol);
            beamMat.color = beamCol;
            lightBeam.material = beamMat;
            lightBeam.startColor = beamCol;
            lightBeam.endColor = new Color(0.2f, 0.85f, 1f, 0.05f);

            // 2. Pulsing Ground Ring
            GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObj.name = "GroundRing";
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            ringObj.transform.localScale = new Vector3(2.5f, 0.01f, 2.5f);
            Collider col = ringObj.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Material ringMat = new Material(unlitShader);
            Color ringCol = new Color(0.2f, 0.85f, 1f, 0.6f);
            if (ringMat.HasProperty("_BaseColor")) ringMat.SetColor("_BaseColor", ringCol);
            ringMat.color = ringCol;
            ringObj.GetComponent<Renderer>().material = ringMat;
            groundRing = ringObj.transform;

            // 3. Floating World-Space Billboard UI
            GameObject canvasObj = new GameObject("BillboardCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            billboardCanvas = canvasObj.transform;

            Canvas c = canvasObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            RectTransform rtCanvas = canvasObj.GetComponent<RectTransform>();
            rtCanvas.sizeDelta = new Vector2(280f, 100f);
            canvasObj.transform.localScale = Vector3.one * 0.018f;

            // Marker Diamond Icon
            GameObject iconObj = new GameObject("DiamondIcon");
            iconObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtIcon = iconObj.AddComponent<RectTransform>();
            rtIcon.anchoredPosition = new Vector2(0f, 22f);
            rtIcon.sizeDelta = new Vector2(28f, 28f);
            rtIcon.localRotation = Quaternion.Euler(0f, 0f, 45f); // diamond tilt
            Image imgIcon = iconObj.AddComponent<Image>();
            imgIcon.color = new Color(1f, 0.85f, 0.2f, 0.95f);

            // Title Text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchoredPosition = new Vector2(0f, -4f);
            rtTitle.sizeDelta = new Vector2(260f, 26f);
            titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 15;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            // Distance Text
            GameObject distObj = new GameObject("DistanceText");
            distObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rtDist = distObj.AddComponent<RectTransform>();
            rtDist.anchoredPosition = new Vector2(0f, -26f);
            rtDist.sizeDelta = new Vector2(200f, 22f);
            distanceText = distObj.AddComponent<Text>();
            distanceText.font = titleText.font;
            distanceText.fontSize = 14;
            distanceText.fontStyle = FontStyle.Bold;
            distanceText.alignment = TextAnchor.MiddleCenter;
            distanceText.color = new Color(0.2f, 0.9f, 1f, 0.95f);
        }

        private void Update()
        {
            if (currentStage == BeaconStage.Completed) return;

            // Animate light column pulse
            float pulse = Mathf.PingPong(Time.time * 0.8f, 1f);
            if (groundRing != null)
            {
                float s = 2.2f + (pulse * 0.7f);
                groundRing.localScale = new Vector3(s, 0.01f, s);
            }

            // Billboard UI faces camera
            Camera mainCam = Camera.main;
            if (mainCam != null && billboardCanvas != null)
            {
                billboardCanvas.rotation = mainCam.transform.rotation;
            }

            // Update distance
            FindPlayer();
            if (playerTransform != null && distanceText != null)
            {
                float d = Vector3.Distance(transform.position, playerTransform.position);
                distanceText.text = $"{Mathf.RoundToInt(d)}m";

                // Proximity stage auto-transition for PoliceCar if looted
                if (currentStage == BeaconStage.PoliceCruiserLoot && d < 2.5f)
                {
                    CheckIfPlayerArmed();
                }
            }
        }

        public void SetStage(BeaconStage stage)
        {
            currentStage = stage;
            string stageTitle = "";

            switch (stage)
            {
                case BeaconStage.PoliceCruiserLoot:
                    transform.position = posPoliceCar;
                    stageTitle = "POLİS ARACINI YAĞMALA";
                    break;
                case BeaconStage.TalkToDrAras:
                    transform.position = posDrAras;
                    stageTitle = "DR. ARAS İLE KONUŞ";
                    break;
                case BeaconStage.SearchClinic:
                    transform.position = posClinic;
                    stageTitle = "KLİNİĞİ ARAŞTIR";
                    break;
                case BeaconStage.ReachSafeHouse:
                    transform.position = posSafeHouse;
                    stageTitle = "GÜVENLİ SIĞINAK";
                    break;
                case BeaconStage.Completed:
                    gameObject.SetActive(false);
                    return;
            }

            if (titleText != null)
            {
                titleText.text = stageTitle;
            }

            // Update compass waypoint
            GameEvents.TriggerWaypointChanged(transform.position, stageTitle);
            Debug.Log($"[WaypointBeacon] Stage transitioned to {stage}: {stageTitle} at {transform.position}");
        }

        private void HandleItemCollected(string itemId, int count)
        {
            if (currentStage == BeaconStage.PoliceCruiserLoot)
            {
                if (itemId.Contains("pistol") || itemId.Contains("ammo") || itemId.Contains("medkit"))
                {
                    SetStage(BeaconStage.TalkToDrAras);
                }
            }
            else if (currentStage == BeaconStage.SearchClinic)
            {
                if (itemId.Contains("medkit") || itemId.Contains("alcohol") || itemId.Contains("bandage"))
                {
                    SetStage(BeaconStage.ReachSafeHouse);
                }
            }
        }

        private void HandleDialogueStarted(string storyResource, string startNodeId)
        {
            if (currentStage == BeaconStage.TalkToDrAras)
            {
                SetStage(BeaconStage.SearchClinic);
            }
        }

        private void HandleMissionCompleted(string missionId, string nextNode)
        {
            if (currentStage == BeaconStage.PoliceCruiserLoot)
            {
                SetStage(BeaconStage.TalkToDrAras);
            }
            else if (currentStage == BeaconStage.TalkToDrAras)
            {
                SetStage(BeaconStage.SearchClinic);
            }
            else if (currentStage == BeaconStage.SearchClinic)
            {
                SetStage(BeaconStage.ReachSafeHouse);
            }
        }

        private void CheckIfPlayerArmed()
        {
            if (InventorySystem.Instance != null)
            {
                foreach (var slot in InventorySystem.Instance.Slots)
                {
                    if (slot.item != null && (slot.item.itemId == "weapon_pistol" || slot.item.itemId == "ammo_9mm"))
                    {
                        SetStage(BeaconStage.TalkToDrAras);
                        break;
                    }
                }
            }
        }
    }
}
