using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.UI;

namespace ProjectSurvival.Environment
{
    public class CustomWaypointManager : MonoBehaviour
    {
        public static CustomWaypointManager Instance { get; private set; }

        private bool hasWaypoint = false;
        private Vector3 waypointPosition = Vector3.zero;
        private GameObject beaconObject;
        private Transform beaconPillar;
        private Transform groundRing;

        public bool HasCustomWaypoint => hasWaypoint;
        public Vector3 WaypointPosition => waypointPosition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetWaypoint(Vector3 worldPos)
        {
            hasWaypoint = true;
            waypointPosition = new Vector3(worldPos.x, 0f, worldPos.z);

            EnsureBeacon();
            if (beaconObject != null)
            {
                beaconObject.transform.position = waypointPosition;
                beaconObject.SetActive(true);
            }

            if (CompassBarUI.Instance != null)
            {
                CompassBarUI.Instance.SetCustomWaypoint(waypointPosition);
            }

            GameEvents.TriggerWaypointChanged(waypointPosition, "ÖZEL İŞARET");
            Debug.Log($"[CustomWaypointManager] Custom Waypoint placed at {waypointPosition}");
        }

        public void ClearWaypoint()
        {
            hasWaypoint = false;

            if (beaconObject != null)
            {
                beaconObject.SetActive(false);
            }

            if (CompassBarUI.Instance != null)
            {
                CompassBarUI.Instance.ClearCustomWaypoint();
            }

            Debug.Log("[CustomWaypointManager] Custom Waypoint cleared.");
        }

        private void EnsureBeacon()
        {
            if (beaconObject != null) return;

            beaconObject = new GameObject("--- 3D WAYPOINT BEACON ---");
            beaconObject.transform.position = waypointPosition;

            // 1. Vertical Luminous Light Pillar (Cylinder 0.5m x 45m high)
            GameObject pillarObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillarObj.name = "Pillar_Beam";
            pillarObj.transform.SetParent(beaconObject.transform, false);
            pillarObj.transform.localPosition = new Vector3(0f, 22.5f, 0f);
            pillarObj.transform.localScale = new Vector3(0.45f, 22.5f, 0.45f);

            // Remove physics collider
            Collider col = pillarObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Setup glowing amber URP unlit material
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material pillarMat = new Material(unlitShader);
            pillarMat.name = "Mat_Waypoint_Beacon";
            pillarMat.color = new Color(1.0f, 0.72f, 0.01f, 0.65f); // #FFB703
            pillarMat.SetFloat("_Surface", 1f); // Transparent
            pillarMat.SetFloat("_Blend", 0f);   // Alpha
            pillarMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            pillarMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive glow
            pillarMat.SetInt("_ZWrite", 0);
            pillarMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            pillarMat.renderQueue = 3100;

            pillarObj.GetComponent<Renderer>().sharedMaterial = pillarMat;
            beaconPillar = pillarObj.transform;

            // 2. Ground Pulse Disc
            GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObj.name = "Ground_Ring";
            ringObj.transform.SetParent(beaconObject.transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            ringObj.transform.localScale = new Vector3(3.2f, 0.02f, 3.2f);

            Collider ringCol = ringObj.GetComponent<Collider>();
            if (ringCol != null) Destroy(ringCol);

            ringObj.GetComponent<Renderer>().sharedMaterial = pillarMat;
            groundRing = ringObj.transform;

            // 3. Point Light
            GameObject lightObj = new GameObject("Beacon_Light");
            lightObj.transform.SetParent(beaconObject.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.75f, 0.1f);
            l.range = 14f;
            l.intensity = 2.5f;
        }

        private void Update()
        {
            if (!hasWaypoint || beaconObject == null || !beaconObject.activeSelf) return;

            // Subtle pulsing animation on ground ring and vertical pillar
            float pulse = 1.0f + 0.18f * Mathf.Sin(Time.unscaledTime * 5.0f);
            if (groundRing != null)
            {
                groundRing.localScale = new Vector3(3.2f * pulse, 0.02f, 3.2f * pulse);
            }
            if (beaconPillar != null)
            {
                beaconPillar.Rotate(Vector3.up * (45f * Time.unscaledDeltaTime));
            }
        }
    }
}
