using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectSurvival.Core;
using ProjectSurvival.Missions;

namespace ProjectSurvival.UI
{
    public class CompassBarUI : MonoBehaviour
    {
        public static CompassBarUI Instance { get; private set; }

        [Header("Compass UI Elements")]
        [SerializeField] private RectTransform compassContainer;
        [SerializeField] private RectTransform cardinalMarkersParent;
        [SerializeField] private RectTransform waypointMarker;
        [SerializeField] private Text waypointDistanceText;
        [SerializeField] private RectTransform customWaypointMarker;
        [SerializeField] private Text customWaypointDistanceText;
        [SerializeField] private float compassWidth = 360f;
        [SerializeField] private float fovDegrees = 160f;

        [Header("Active District Indicator")]
        [SerializeField] private Text activeDistrictText;
        private Coroutine districtFadeRoutine;

        private Camera mainCamera;
        private Transform playerTransform;
        private Vector3? customWaypointPos = null;

        public void SetActiveDistrict(string districtName)
        {
            if (activeDistrictText == null)
            {
                CreateDistrictTextIfMissing();
            }

            if (activeDistrictText != null)
            {
                activeDistrictText.text = districtName.ToUpper();
                if (districtFadeRoutine != null) StopCoroutine(districtFadeRoutine);
                districtFadeRoutine = StartCoroutine(PulseDistrictLabel());
            }
        }

        private void CreateDistrictTextIfMissing()
        {
            if (compassContainer == null) compassContainer = GetComponent<RectTransform>();
            if (compassContainer == null) return;

            GameObject dObj = new GameObject("ActiveDistrict_Text", typeof(RectTransform));
            dObj.transform.SetParent(compassContainer, false);
            RectTransform dr = dObj.GetComponent<RectTransform>();
            dr.anchorMin = new Vector2(0.5f, 0f);
            dr.anchorMax = new Vector2(0.5f, 0f);
            dr.pivot = new Vector2(0.5f, 1f);
            dr.anchoredPosition = new Vector2(0f, -6f);
            dr.sizeDelta = new Vector2(300f, 20f);

            activeDistrictText = dObj.AddComponent<Text>();
            activeDistrictText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            activeDistrictText.fontSize = 11;
            activeDistrictText.fontStyle = FontStyle.Bold;
            activeDistrictText.alignment = TextAnchor.MiddleCenter;
            activeDistrictText.color = new Color(0.95f, 0.78f, 0.25f, 0.95f);
            activeDistrictText.raycastTarget = false;
        }

        private System.Collections.IEnumerator PulseDistrictLabel()
        {
            if (activeDistrictText == null) yield break;

            Color baseCol = activeDistrictText.color;
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(t / 0.4f);
                activeDistrictText.color = new Color(baseCol.r, baseCol.g, baseCol.b, alpha);
                yield return null;
            }
        }

        public void SetCustomWaypoint(Vector3 worldPos)
        {
            customWaypointPos = worldPos;
            if (customWaypointMarker != null) customWaypointMarker.gameObject.SetActive(true);
        }

        public void ClearCustomWaypoint()
        {
            customWaypointPos = null;
            if (customWaypointMarker != null) customWaypointMarker.gameObject.SetActive(false);
        }

        private struct CardinalDef
        {
            public float angle;
            public string label;
            public Text textComponent;
        }

        private readonly List<CardinalDef> cardinals = new List<CardinalDef>();
        private float pixelsPerDegree;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            mainCamera = Camera.main;
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;

            pixelsPerDegree = compassWidth / fovDegrees;

            SetupCardinalLabels();
        }

        private void SetupCardinalLabels()
        {
            if (cardinalMarkersParent == null) return;

            // Define 8 cardinal/intercardinal directions in Turkish:
            // K (Kuzey), KD (Kuzeydoğu), D (Doğu), GD (Güneydoğu), G (Güney), GB (Güneybatı), B (Batı), KB (Kuzeybatı)
            (float, string)[] defs = new (float, string)[]
            {
                (0f, "K"),
                (45f, "KD"),
                (90f, "D"),
                (135f, "GD"),
                (180f, "G"),
                (225f, "GB"),
                (270f, "B"),
                (315f, "KB")
            };

            for (int i = 0; i < defs.Length; i++)
            {
                float angle = defs[i].Item1;
                string label = defs[i].Item2;

                GameObject obj = new GameObject("Cardinal_" + label);
                obj.transform.SetParent(cardinalMarkersParent, false);

                RectTransform rt = obj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(36f, 24f);

                Text txt = obj.AddComponent<Text>();
                txt.text = label;
                txt.fontSize = (label.Length == 1) ? 14 : 11;
                txt.fontStyle = (label.Length == 1) ? FontStyle.Bold : FontStyle.Normal;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = (label == "K") ? new Color(1f, 0.35f, 0.35f, 0.95f) : new Color(0.85f, 0.9f, 0.95f, 0.85f);
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.raycastTarget = false;

                cardinals.Add(new CardinalDef
                {
                    angle = angle,
                    label = label,
                    textComponent = txt
                });
            }
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }
            if (playerTransform == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            float camYaw = mainCamera.transform.eulerAngles.y;
            float halfFov = fovDegrees * 0.5f;

            // 1. Update Cardinal Direction Labels
            for (int i = 0; i < cardinals.Count; i++)
            {
                var card = cardinals[i];
                if (card.textComponent == null) continue;

                float delta = Mathf.DeltaAngle(camYaw, card.angle);
                if (Mathf.Abs(delta) <= halfFov)
                {
                    card.textComponent.gameObject.SetActive(true);
                    float posX = delta * pixelsPerDegree;
                    card.textComponent.rectTransform.anchoredPosition = new Vector2(posX, 0f);

                    // Fade alpha towards edges
                    float edgeRatio = Mathf.Abs(delta) / halfFov;
                    float alpha = 1f - (edgeRatio * edgeRatio);
                    Color c = card.textComponent.color;
                    c.a = alpha * 0.9f;
                    card.textComponent.color = c;
                }
                else
                {
                    card.textComponent.gameObject.SetActive(false);
                }
            }

            // 2. Update Active Objective Waypoint Marker
            UpdateWaypoint(camYaw, halfFov);

            // 3. Update Custom Waypoint Marker
            UpdateCustomWaypoint(camYaw, halfFov);
        }

        private void UpdateCustomWaypoint(float camYaw, float halfFov)
        {
            if (customWaypointMarker == null) return;

            if (customWaypointPos.HasValue && playerTransform != null)
            {
                customWaypointMarker.gameObject.SetActive(true);

                Vector3 toTarget = customWaypointPos.Value - playerTransform.position;
                float dist = toTarget.magnitude;
                toTarget.y = 0;

                float targetAngle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                float delta = Mathf.DeltaAngle(camYaw, targetAngle);

                float clampedDelta = Mathf.Clamp(delta, -halfFov, halfFov);
                float posX = clampedDelta * pixelsPerDegree;
                customWaypointMarker.anchoredPosition = new Vector2(posX, 0f);

                if (customWaypointDistanceText != null)
                {
                    customWaypointDistanceText.text = $"{dist:F0}m";
                }

                // Subtle pulse animation
                float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f);
                customWaypointMarker.localScale = new Vector3(pulse, pulse, 1f);
            }
            else
            {
                customWaypointMarker.gameObject.SetActive(false);
            }
        }

        private void UpdateWaypoint(float camYaw, float halfFov)
        {
            if (waypointMarker == null) return;

            Transform targetTransform = null;
            if (MissionManager.Instance != null && MissionManager.Instance.HasActiveMission)
            {
                var obj = MissionManager.Instance.ActiveObjective;
                if (!string.IsNullOrEmpty(obj.data.targetZoneId))
                {
                    var zoneObj = GameObject.Find(obj.data.targetZoneId);
                    if (zoneObj != null) targetTransform = zoneObj.transform;
                }
            }

            if (targetTransform != null && playerTransform != null)
            {
                waypointMarker.gameObject.SetActive(true);

                Vector3 toTarget = targetTransform.position - playerTransform.position;
                float dist = toTarget.magnitude;
                toTarget.y = 0;

                float targetAngle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                float delta = Mathf.DeltaAngle(camYaw, targetAngle);

                float clampedDelta = Mathf.Clamp(delta, -halfFov, halfFov);
                float posX = clampedDelta * pixelsPerDegree;
                waypointMarker.anchoredPosition = new Vector2(posX, 0f);

                if (waypointDistanceText != null)
                {
                    waypointDistanceText.text = $"{dist:F0}m";
                }
            }
            else
            {
                waypointMarker.gameObject.SetActive(false);
            }
        }
    }
}
