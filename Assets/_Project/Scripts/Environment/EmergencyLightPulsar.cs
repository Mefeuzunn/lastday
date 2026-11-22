using System.Collections;
using UnityEngine;

namespace ProjectSurvival.Environment
{
    public class EmergencyLightPulsar : MonoBehaviour
    {
        [Header("Light References")]
        [SerializeField] private Light redLight;
        [SerializeField] private Light blueLight;

        [Header("Cadence Settings")]
        [SerializeField] private float pulseDuration = 0.14f;
        [SerializeField] private float interval = 0.12f;
        [SerializeField] private float maxIntensity = 1.4f;

        private void Awake()
        {
            EnsureLights();
        }

        private void EnsureLights()
        {
            if (redLight == null)
            {
                GameObject redObj = new GameObject("Light_Emergency_Red");
                redObj.transform.SetParent(transform, false);
                redObj.transform.localPosition = new Vector3(-0.45f, 1.4f, 0f);
                redLight = redObj.AddComponent<Light>();
                redLight.type = LightType.Point;
                redLight.color = new Color(1f, 0.15f, 0.15f);
                redLight.range = 7f;
                redLight.intensity = 0f;
                redLight.shadows = LightShadows.None; // Mobile optimization
            }

            if (blueLight == null)
            {
                GameObject blueObj = new GameObject("Light_Emergency_Blue");
                blueObj.transform.SetParent(transform, false);
                blueObj.transform.localPosition = new Vector3(0.45f, 1.4f, 0f);
                blueLight = blueObj.AddComponent<Light>();
                blueLight.type = LightType.Point;
                blueLight.color = new Color(0.15f, 0.45f, 1f);
                blueLight.range = 7f;
                blueLight.intensity = 0f;
                blueLight.shadows = LightShadows.None;
            }
        }

        private void Start()
        {
            StartCoroutine(EmergencyBeaconRoutine());
        }

        private IEnumerator EmergencyBeaconRoutine()
        {
            WaitForSeconds waitPulse = new WaitForSeconds(pulseDuration);
            WaitForSeconds waitInter = new WaitForSeconds(interval);

            while (true)
            {
                // Double flash Red
                if (redLight != null) redLight.intensity = maxIntensity;
                yield return waitPulse;
                if (redLight != null) redLight.intensity = 0f;
                yield return waitInter;
                if (redLight != null) redLight.intensity = maxIntensity * 0.75f;
                yield return waitPulse;
                if (redLight != null) redLight.intensity = 0f;
                yield return waitInter;

                // Double flash Blue
                if (blueLight != null) blueLight.intensity = maxIntensity;
                yield return waitPulse;
                if (blueLight != null) blueLight.intensity = 0f;
                yield return waitInter;
                if (blueLight != null) blueLight.intensity = maxIntensity * 0.75f;
                yield return waitPulse;
                if (blueLight != null) blueLight.intensity = 0f;
                yield return waitInter;
            }
        }
    }
}
