using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Player
{
    public class WeaponFeelController : MonoBehaviour
    {
        public static WeaponFeelController Instance { get; private set; }

        [Header("Weapon Socket Kickback Spring")]
        [SerializeField] private Transform weaponSocket;
        [SerializeField] private float springStiffness = 32f;
        [SerializeField] private float springDamping = 9f;

        [Header("Muzzle Flash Light")]
        [SerializeField] private Light dynamicMuzzleLight;
        [SerializeField] private float muzzleLightDuration = 0.05f;

        [Header("Hitstop & Feel Settings")]
        [SerializeField] private float headshotHitstopDuration = 0.035f; // 35ms micro-hitstop
        [SerializeField] private LayerMask indoorCeilingLayers = ~0;

        private Vector3 targetSocketPosOffset = Vector3.zero;
        private Vector3 currentSocketPosOffset = Vector3.zero;
        private Vector3 socketPosVelocity = Vector3.zero;

        private Vector3 targetSocketRotOffset = Vector3.zero;
        private Vector3 currentSocketRotOffset = Vector3.zero;
        private Vector3 socketRotVelocity = Vector3.zero;

        private Vector3 baseSocketLocalPos = Vector3.zero;
        private Quaternion baseSocketLocalRot = Quaternion.identity;

        private Coroutine muzzleFlashCoroutine;
        private Coroutine hitstopCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (weaponSocket != null)
            {
                baseSocketLocalPos = weaponSocket.localPosition;
                baseSocketLocalRot = weaponSocket.localRotation;
            }
        }

        public void SetWeaponSocket(Transform socket)
        {
            weaponSocket = socket;
            if (weaponSocket != null)
            {
                baseSocketLocalPos = weaponSocket.localPosition;
                baseSocketLocalRot = weaponSocket.localRotation;
            }
        }

        private void Update()
        {
            UpdateSpringPhysics();
        }

        private void UpdateSpringPhysics()
        {
            if (weaponSocket == null) return;

            // Smooth spring damper for position
            currentSocketPosOffset = Vector3.SmoothDamp(currentSocketPosOffset, targetSocketPosOffset, ref socketPosVelocity, 1f / springStiffness);
            targetSocketPosOffset = Vector3.MoveTowards(targetSocketPosOffset, Vector3.zero, Time.deltaTime * springDamping);

            // Smooth spring damper for rotation
            currentSocketRotOffset = Vector3.SmoothDamp(currentSocketRotOffset, targetSocketRotOffset, ref socketRotVelocity, 1f / springStiffness);
            targetSocketRotOffset = Vector3.MoveTowards(targetSocketRotOffset, Vector3.zero, Time.deltaTime * springDamping * 20f);

            weaponSocket.localPosition = baseSocketLocalPos + currentSocketPosOffset;
            weaponSocket.localRotation = baseSocketLocalRot * Quaternion.Euler(currentSocketRotOffset);
        }

        public void OnWeaponFired(WeaponType weapon, Transform muzzlePoint)
        {
            ApplyProceduralRecoil(weapon);
            TriggerMuzzleFlash(muzzlePoint);
            EjectShellCasing(weapon, muzzlePoint);
        }

        private void ApplyProceduralRecoil(WeaponType weapon)
        {
            float pitchRecoil = 1.0f;
            float yawRecoil = 0f;

            switch (weapon)
            {
                case WeaponType.Pistol:
                    targetSocketPosOffset += new Vector3(0f, 0.022f, -0.065f);
                    targetSocketRotOffset += new Vector3(-3.8f, UnityEngine.Random.Range(-0.8f, 0.8f), 0f);
                    pitchRecoil = 1.25f;
                    yawRecoil = UnityEngine.Random.Range(-0.35f, 0.35f);
                    break;

                case WeaponType.Shotgun:
                    targetSocketPosOffset += new Vector3(0f, 0.048f, -0.155f);
                    targetSocketRotOffset += new Vector3(-8.2f, UnityEngine.Random.Range(-1.6f, 1.6f), 0f);
                    pitchRecoil = 3.2f;
                    yawRecoil = UnityEngine.Random.Range(-0.75f, 0.75f);
                    break;

                case WeaponType.Axe:
                    targetSocketPosOffset += new Vector3(0.04f, -0.035f, 0.06f);
                    targetSocketRotOffset += new Vector3(4.5f, -6.5f, 5.0f);
                    pitchRecoil = 0.85f;
                    yawRecoil = UnityEngine.Random.Range(-0.25f, 0.25f);
                    break;

                case WeaponType.Crossbow:
                    targetSocketPosOffset += new Vector3(0f, 0.012f, -0.040f);
                    targetSocketRotOffset += new Vector3(-1.8f, UnityEngine.Random.Range(-0.3f, 0.3f), 0f);
                    pitchRecoil = 0.55f;
                    yawRecoil = UnityEngine.Random.Range(-0.15f, 0.15f);
                    break;
            }

            // Apply camera kick via ThirdPersonCamera
            if (ThirdPersonCamera.Instance != null)
            {
                ThirdPersonCamera.Instance.AddRecoil(pitchRecoil, yawRecoil);
            }
        }

        private void TriggerMuzzleFlash(Transform muzzlePoint)
        {
            if (dynamicMuzzleLight == null)
            {
                GameObject lightObj = new GameObject("Dynamic_MuzzleLight");
                lightObj.transform.SetParent(muzzlePoint != null ? muzzlePoint : transform, false);
                dynamicMuzzleLight = lightObj.AddComponent<Light>();
                dynamicMuzzleLight.type = LightType.Point;
                dynamicMuzzleLight.color = new Color(1.0f, 0.68f, 0.22f); // Warm muzzle flash amber #FFA726
                dynamicMuzzleLight.range = 5.5f;
                dynamicMuzzleLight.intensity = 2.8f;
                dynamicMuzzleLight.shadows = LightShadows.None;
                dynamicMuzzleLight.enabled = false;
            }

            if (dynamicMuzzleLight != null)
            {
                if (muzzlePoint != null) dynamicMuzzleLight.transform.position = muzzlePoint.position;
                if (muzzleFlashCoroutine != null) StopCoroutine(muzzleFlashCoroutine);
                muzzleFlashCoroutine = StartCoroutine(MuzzleFlashRoutine());
            }
        }

        private IEnumerator MuzzleFlashRoutine()
        {
            dynamicMuzzleLight.enabled = true;
            yield return new WaitForSeconds(muzzleLightDuration);
            dynamicMuzzleLight.enabled = false;
        }

        private void EjectShellCasing(WeaponType weapon, Transform muzzlePoint)
        {
            if (weapon != WeaponType.Pistol && weapon != WeaponType.Shotgun) return;

            Vector3 spawnPos = muzzlePoint != null 
                ? (muzzlePoint.position - transform.forward * 0.25f + transform.right * 0.08f)
                : (transform.position + Vector3.up * 1.35f + transform.forward * 0.3f);

            GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            casing.name = "ShellCasing";
            casing.transform.position = spawnPos;

            bool isShotgun = (weapon == WeaponType.Shotgun);
            casing.transform.localScale = isShotgun ? new Vector3(0.024f, 0.052f, 0.024f) : new Vector3(0.015f, 0.032f, 0.015f);

            // Material
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            Material mat = new Material(unlit);
            mat.color = isShotgun ? new Color(0.85f, 0.15f, 0.15f) : new Color(0.88f, 0.75f, 0.25f); // Brass vs Red Shell
            casing.GetComponent<Renderer>().sharedMaterial = mat;

            Collider c = casing.GetComponent<Collider>();
            if (c != null)
            {
                // Disable collision with player
                var playerCol = GetComponent<Collider>();
                if (playerCol != null) Physics.IgnoreCollision(c, playerCol);
            }

            Rigidbody rb = casing.AddComponent<Rigidbody>();
            rb.mass = 0.02f;
            Vector3 ejectDir = (transform.right * 2.8f + transform.up * 1.8f + transform.forward * -0.3f + UnityEngine.Random.insideUnitSphere * 0.35f).normalized;
            rb.linearVelocity = ejectDir * UnityEngine.Random.Range(2.2f, 3.8f);
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 18f;

            Destroy(casing, 1.5f);
        }

        public void TriggerHitJuice(bool isHeadshot, Vector3 hitPoint, Vector3 hitNormal, bool isZombie)
        {
            if (ProjectSurvival.Combat.CombatJuiceController.Instance != null)
            {
                if (isHeadshot)
                    ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerCriticalHeadshotJuice(hitPoint, hitNormal);
                else
                    ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerHitmarker(false, false, hitPoint);
            }

            if (ProjectSurvival.Combat.ImpactDecalPoolManager.Instance != null)
            {
                ProjectSurvival.Combat.ImpactDecalPoolManager.Instance.SpawnImpact(hitPoint, hitNormal, isZombie);
            }
            else
            {
                if (isZombie)
                {
                    SpawnBloodSplatter(hitPoint, hitNormal, isHeadshot);

                    if (isHeadshot)
                    {
                        if (hitstopCoroutine != null) StopCoroutine(hitstopCoroutine);
                        hitstopCoroutine = StartCoroutine(MicroHitstopRoutine(headshotHitstopDuration));
                    }
                }
                else
                {
                    SpawnConcreteSparks(hitPoint, hitNormal);
                }
            }

            // Synthesized tactile hit sound
            ProceduralAudioManager.Instance?.PlayHitMarker(isHeadshot);

            // Trigger Diegetic Hit Marker tick in HUD
            GameEvents.TriggerHitConfirmed(isHeadshot, hitPoint);
        }

        private IEnumerator MicroHitstopRoutine(float duration)
        {
            float prevTimeScale = Time.timeScale;
            Time.timeScale = 0.05f; // Almost freeze for visceral tactile punch
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = prevTimeScale;
        }

        private void SpawnBloodSplatter(Vector3 pos, Vector3 normal, bool isHeadshot)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material bloodMat = new Material(sh);
            bloodMat.color = new Color(0.62f, 0.05f, 0.05f, 0.95f); // Dark crimson

            int count = isHeadshot ? 10 : 6;
            for (int i = 0; i < count; i++)
            {
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                p.name = "BloodDrop";
                p.transform.position = pos + UnityEngine.Random.insideUnitSphere * 0.06f;
                float s = UnityEngine.Random.Range(0.04f, 0.09f);
                p.transform.localScale = Vector3.one * s;

                Collider col = p.GetComponent<Collider>();
                if (col != null) Destroy(col);
                p.GetComponent<Renderer>().sharedMaterial = bloodMat;

                Rigidbody rb = p.AddComponent<Rigidbody>();
                rb.mass = 0.03f;
                rb.linearVelocity = (normal + UnityEngine.Random.insideUnitSphere * 0.85f).normalized * UnityEngine.Random.Range(2.5f, 5.5f);
                Destroy(p, UnityEngine.Random.Range(0.25f, 0.45f));
            }
        }

        private void SpawnConcreteSparks(Vector3 pos, Vector3 normal)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material sparkMat = new Material(sh);
            sparkMat.color = new Color(1.0f, 0.88f, 0.35f, 0.95f); // Bright yellow-orange spark

            for (int i = 0; i < 6; i++)
            {
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                p.name = "ConcreteSpark";
                p.transform.position = pos;
                p.transform.localScale = Vector3.one * 0.035f;

                Collider col = p.GetComponent<Collider>();
                if (col != null) Destroy(col);
                p.GetComponent<Renderer>().sharedMaterial = sparkMat;

                Rigidbody rb = p.AddComponent<Rigidbody>();
                rb.mass = 0.02f;
                rb.linearVelocity = (normal + UnityEngine.Random.insideUnitSphere * 0.7f).normalized * UnityEngine.Random.Range(3.5f, 7.0f);
                Destroy(p, UnityEngine.Random.Range(0.18f, 0.28f));
            }
        }

        public bool IsIndoors(Vector3 position)
        {
            // Cast ray upwards to check for ceiling/roof
            return Physics.Raycast(position + Vector3.up * 0.5f, Vector3.up, 14f, indoorCeilingLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
