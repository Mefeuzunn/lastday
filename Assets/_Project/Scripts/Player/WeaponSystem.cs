using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.AI;
using ProjectSurvival.Combat;

namespace ProjectSurvival.Player
{
    public enum WeaponType
    {
        None,
        Axe,
        Pistol,
        Shotgun,
        Crossbow
    }

    public class WeaponSystem : MonoBehaviour
    {
        public static WeaponSystem Instance { get; private set; }

        [Header("Equipped Weapon")]
        [SerializeField] private WeaponType currentWeapon = WeaponType.None;
        [SerializeField] private int pistolClip = 12;
        [SerializeField] private int pistolMaxClip = 12;
        [SerializeField] private int shotgunClip = 6;
        [SerializeField] private int shotgunMaxClip = 6;
        [SerializeField] private int crossbowClip = 1;
        [SerializeField] private int crossbowMaxClip = 1;

        [Header("Damage Stats")]
        [SerializeField] private float axeDamage = 55f;
        [SerializeField] private float axeRange = 2.2f;
        [SerializeField] private float pistolDamage = 35f;
        [SerializeField] private float pistolRange = 50f;
        [SerializeField] private float shotgunPelletDamage = 18f;
        [SerializeField] private int shotgunPelletCount = 6;
        [SerializeField] private float shotgunRange = 25f;
        [SerializeField] private float headshotMultiplier = 2.5f;
        [SerializeField] private float crossbowDamage = 95f;
        [SerializeField] private float crossbowRange = 65f;
        [SerializeField] private float crossbowHeadshotMultiplier = 2.75f;
        [SerializeField] private float crossbowNoiseRadius = 3.5f;
        [SerializeField] private float crossbowReloadDuration = 1.35f;

        [Header("Weapon Sockets & Visuals")]
        [SerializeField] private Transform weaponSocket;
        [SerializeField] private GameObject visualAxe;
        [SerializeField] private GameObject visualPistol;
        [SerializeField] private GameObject visualShotgun;
        [SerializeField] private GameObject visualCrossbow;
        [SerializeField] private Transform muzzlePoint;

        [Header("Effects & Layers")]
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private Light muzzleLight;
        [SerializeField] private LineRenderer laserSight;
        [SerializeField] private Material tracerMaterial;

        [Header("Audio / Noise")]
        [SerializeField] private float pistolNoiseRadius = 22f;
        [SerializeField] private float shotgunNoiseRadius = 35f;
        [SerializeField] private float axeNoiseRadius = 3.5f;

        private bool isAiming = false;
        private bool isReloading = false;
        private float lastFireTime = 0f;
        private float fireCooldown = 0.25f;
        private Camera mainCamera;
        private NoiseEmitter noiseEmitter;
        private InventorySystem inventory;
        private float currentCrosshairSpread = 1f;

        private readonly HashSet<WeaponType> unlockedWeapons = new HashSet<WeaponType> { WeaponType.None };
        private WeaponType lastEquippedWeapon = WeaponType.Axe;
        private readonly Collider[] _meleeHitBuffer = new Collider[24];

        public WeaponType CurrentWeapon => currentWeapon;
        public bool IsAiming => isAiming;
        public bool IsReloading => isReloading;
        public float HeadshotMultiplier => headshotMultiplier;
        public float CrossbowDamage => crossbowDamage;
        public float CrossbowRange => crossbowRange;
        public float CrossbowHeadshotMultiplier => crossbowHeadshotMultiplier;
        public float CrossbowNoiseRadius => crossbowNoiseRadius;
        public float CrossbowReloadDuration => crossbowReloadDuration;
        public int CurrentClip => currentWeapon == WeaponType.Pistol ? pistolClip : (currentWeapon == WeaponType.Shotgun ? shotgunClip : (currentWeapon == WeaponType.Crossbow ? crossbowClip : 0));
        public int MaxClip
        {
            get
            {
                int baseMax = currentWeapon == WeaponType.Pistol ? pistolMaxClip : (currentWeapon == WeaponType.Shotgun ? shotgunMaxClip : (currentWeapon == WeaponType.Crossbow ? crossbowMaxClip : 0));
                return WeaponAttachmentManager.Instance != null ? WeaponAttachmentManager.Instance.GetMaxClip(currentWeapon, baseMax) : baseMax;
            }
        }

        public bool IsWeaponAvailable(WeaponType type)
        {
            if (type == WeaponType.None) return true;
            if (unlockedWeapons.Contains(type)) return true;
            if (inventory != null)
            {
                if (type == WeaponType.Axe && inventory.HasItem("weapon_axe")) return true;
                if (type == WeaponType.Pistol && inventory.HasItem("weapon_pistol")) return true;
                if (type == WeaponType.Shotgun && inventory.HasItem("weapon_shotgun")) return true;
                if (type == WeaponType.Crossbow && inventory.HasItem("weapon_crossbow")) return true;
            }
            return false;
        }

        public List<WeaponType> GetAvailableWeapons()
        {
            List<WeaponType> list = new List<WeaponType> { WeaponType.None };
            if (IsWeaponAvailable(WeaponType.Axe)) list.Add(WeaponType.Axe);
            if (IsWeaponAvailable(WeaponType.Pistol)) list.Add(WeaponType.Pistol);
            if (IsWeaponAvailable(WeaponType.Shotgun)) list.Add(WeaponType.Shotgun);
            if (IsWeaponAvailable(WeaponType.Crossbow)) list.Add(WeaponType.Crossbow);
            return list;
        }

        public void HolsterWeapon()
        {
            if (currentWeapon != WeaponType.None)
            {
                lastEquippedWeapon = currentWeapon;
                EquipWeapon(WeaponType.None);
            }
            else
            {
                if (IsWeaponAvailable(lastEquippedWeapon) && lastEquippedWeapon != WeaponType.None)
                {
                    EquipWeapon(lastEquippedWeapon);
                }
                else
                {
                    List<WeaponType> available = GetAvailableWeapons();
                    if (available.Count > 1)
                    {
                        EquipWeapon(available[1]);
                    }
                    else
                    {
                        GameEvents.TriggerObjectiveToastRequested("Kuşanılacak Silah Yok", 2f);
                        ProceduralAudioManager.Instance?.PlaySoftError();
                    }
                }
            }
        }

        public void CycleNextWeapon()
        {
            List<WeaponType> available = GetAvailableWeapons();
            if (available.Count <= 1)
            {
                GameEvents.TriggerObjectiveToastRequested("Başka Silah Bulunmuyor", 2f);
                ProceduralAudioManager.Instance?.PlaySoftError();
                return;
            }

            int currentIndex = available.IndexOf(currentWeapon);
            if (currentIndex < 0) currentIndex = 0;
            int nextIndex = (currentIndex + 1) % available.Count;
            EquipWeapon(available[nextIndex]);
            ProceduralAudioManager.Instance?.PlayButtonClick();
        }

        public void RestoreWeaponState(int weaponTypeIndex, bool isHolstered)
        {
            WeaponType targetType = (WeaponType)weaponTypeIndex;
            if (targetType != WeaponType.None)
            {
                unlockedWeapons.Add(targetType);
                lastEquippedWeapon = targetType;
            }

            if (isHolstered || targetType == WeaponType.None)
            {
                currentWeapon = WeaponType.None;
                SetAiming(false);
                if (laserSight != null) laserSight.enabled = false;
                ThirdPersonCamera.Instance?.SetAiming(false);
                UpdateWeaponVisuals();
                BroadcastWeaponState();
            }
            else
            {
                EquipWeapon(targetType);
            }
            Debug.Log($"[WeaponSystem] Restored weapon state: {targetType}, Holstered: {isHolstered}");
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            noiseEmitter = GetComponent<NoiseEmitter>();
            inventory = GetComponent<InventorySystem>();
            mainCamera = Camera.main;

            // Filter out Player and UI layers from hitLayers
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) hitLayers &= ~(1 << playerLayer);
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1) hitLayers &= ~(1 << uiLayer);

            SetupLaserSight();
        }

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            UpdateWeaponVisuals();
            BroadcastWeaponState();

            if (WeaponFeelController.Instance != null && weaponSocket != null)
            {
                WeaponFeelController.Instance.SetWeaponSocket(weaponSocket);
            }
        }

        private void SetupLaserSight()
        {
            if (laserSight == null)
            {
                GameObject laserObj = new GameObject("LaserSight");
                laserObj.transform.SetParent(transform);
                laserSight = laserObj.AddComponent<LineRenderer>();
                laserSight.useWorldSpace = true;
                laserSight.startWidth = 0.015f;
                laserSight.endWidth = 0.015f;
                laserSight.positionCount = 2;

                Material laserMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                laserMat.color = new Color(1f, 0.1f, 0.1f, 0.85f);
                laserSight.sharedMaterial = laserMat;
                laserSight.enabled = false;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (GameManager.Instance != null && (GameManager.Instance.CurrentState == GameState.Dialogue || GameManager.Instance.CurrentState == GameState.MainMenu)) return;

            HandleWeaponInput();
            UpdateCrosshairSpread();
            UpdateLaserSight();
        }

        private void HandleWeaponInput()
        {
            // Dedicated Holster & Switch shortcuts
            if (Input.GetKeyDown(KeyCode.H)) HolsterWeapon();
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Tab)) CycleNextWeapon();

            // Right Click to Aim (toggle or hold)
            if (Input.GetMouseButtonDown(1))
            {
                SetAiming(true);
            }
            else if (Input.GetMouseButtonUp(1))
            {
                SetAiming(false);
            }

            // Left Click to Fire
            if (Input.GetMouseButtonDown(0))
            {
                // Prevent shooting if clicking over UI
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    Fire();
                }
            }

            // R to Reload
            if (Input.GetKeyDown(KeyCode.R))
            {
                Reload();
            }
        }

        public void SetAiming(bool aiming)
        {
            if (currentWeapon == WeaponType.None || currentWeapon == WeaponType.Axe)
            {
                isAiming = false;
                ThirdPersonCamera.Instance?.SetAiming(false);
                if (laserSight != null) laserSight.enabled = false;
                return;
            }

            isAiming = aiming;
            ThirdPersonCamera.Instance?.SetAiming(aiming);
            if (laserSight != null) laserSight.enabled = isAiming;
        }

        public void ToggleAim()
        {
            SetAiming(!isAiming);
        }

        public void EquipWeapon(WeaponType type)
        {
            currentWeapon = type;
            if (type != WeaponType.None)
            {
                unlockedWeapons.Add(type);
                lastEquippedWeapon = type;
            }
            fireCooldown = type == WeaponType.Axe ? 0.65f : (type == WeaponType.Pistol ? 0.3f : (type == WeaponType.Shotgun ? 0.85f : 0.5f));
            SetAiming(false);
            if (laserSight != null) laserSight.enabled = false;
            ThirdPersonCamera.Instance?.SetAiming(false);
            UpdateWeaponVisuals();
            BroadcastWeaponState();
            Debug.Log($"[WeaponSystem] Equipped weapon: {type}");
        }

        public void EquipWeaponById(string itemId)
        {
            if (itemId == "weapon_axe") EquipWeapon(WeaponType.Axe);
            else if (itemId == "weapon_pistol") EquipWeapon(WeaponType.Pistol);
            else if (itemId == "weapon_shotgun") EquipWeapon(WeaponType.Shotgun);
            else if (itemId == "weapon_crossbow") EquipWeapon(WeaponType.Crossbow);
        }

        public void Fire()
        {
            if (currentWeapon == WeaponType.None) return;
            if (isReloading) return;
            if (Time.time < lastFireTime + fireCooldown) return;

            switch (currentWeapon)
            {
                case WeaponType.Axe:
                    FireAxe();
                    break;
                case WeaponType.Pistol:
                    FirePistol();
                    break;
                case WeaponType.Shotgun:
                    FireShotgun();
                    break;
                case WeaponType.Crossbow:
                    FireCrossbow();
                    break;
            }
        }

        private void FireAxe()
        {
            lastFireTime = Time.time;
            currentCrosshairSpread += 0.4f;

            float baseStamina = 14f;
            float finalStamina = WeaponAttachmentManager.Instance != null ? WeaponAttachmentManager.Instance.GetMeleeStaminaCost(baseStamina) : baseStamina;
            SurvivalStats.Instance?.DrainStamina(finalStamina);

            // SphereCast ahead
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 dir = transform.forward;
            float effectiveDamage = WeaponAttachmentManager.Instance != null ? WeaponAttachmentManager.Instance.GetMeleeDamage(axeDamage) : axeDamage;

            if (Physics.SphereCast(origin, 0.45f, dir, out RaycastHit hit, axeRange, hitLayers, QueryTriggerInteraction.Ignore))
            {
                ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
                if (zombie != null && !zombie.IsDead)
                {
                    float hitDmg = effectiveDamage;
                    if (ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null &&
                        zombie.CurrentState != ZombieState.Chase && zombie.CurrentState != ZombieState.Attack)
                    {
                        hitDmg *= ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetSneakMeleeDamageMultiplier();
                    }

                    var res = zombie.ProcessHitZoneDamage(hitDmg, hit.point, dir);
                    zombie.SetLastHitDetails(dir, hit.point, "Axe", res.IsHeadshot);
                    SpawnImpactEffect(hit.point, hit.normal, true);

                    if (WeaponAttachmentManager.Instance != null && WeaponAttachmentManager.Instance.HasReinforcedAxe())
                    {
                        zombie.ApplyStaggerKnockback(dir * 3.8f);
                    }

                    if (ProjectSurvival.Combat.CombatJuiceController.Instance != null)
                        ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerMeleeImpactJuice(res.IsCritical, hit.point, hit.normal);
                    else if (WeaponFeelController.Instance != null)
                        WeaponFeelController.Instance.TriggerHitJuice(res.IsCritical, hit.point, hit.normal, true);
                    else
                        GameEvents.TriggerHitConfirmed(res.IsCritical, hit.point);
                    GameEvents.TriggerCameraShakeRequested(res.IsCritical ? 0.35f : 0.25f, 0.15f);
                }
                else
                {
                    var raider = hit.collider.GetComponentInParent<ProjectSurvival.AI.HumanRaiderAI>();
                    if (raider != null && !raider.IsDead)
                    {
                        raider.TakeDamage(effectiveDamage, hit.point, dir);
                        SpawnImpactEffect(hit.point, hit.normal, true);
                        if (ProjectSurvival.Combat.CombatJuiceController.Instance != null)
                            ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerHitmarker(false, raider.IsDead, hit.point);
                        else
                            GameEvents.TriggerHitConfirmed(false, hit.point);
                        GameEvents.TriggerCameraShakeRequested(0.3f, 0.15f);
                    }
                    else
                    {
                        ProjectSurvival.AI.MutantBossAI boss = hit.collider.GetComponentInParent<ProjectSurvival.AI.MutantBossAI>();
                        if (boss != null && !boss.IsDead)
                        {
                            boss.TakeDamage(effectiveDamage, hit.point, dir);
                            SpawnImpactEffect(hit.point, hit.normal, true);
                            GameEvents.TriggerHitConfirmed(true, hit.point);
                            GameEvents.TriggerCameraShakeRequested(0.35f, 0.2f);
                        }
                        else
                        {
                            var animal = hit.collider.GetComponentInParent<ProjectSurvival.AI.WildlifeAnimalAI>();
                            if (animal != null && !animal.IsDead)
                            {
                                animal.TakeDamage(effectiveDamage, hit.point, dir);
                                SpawnImpactEffect(hit.point, hit.normal, true);
                                GameEvents.TriggerHitConfirmed(false, hit.point);
                                GameEvents.TriggerCameraShakeRequested(0.25f, 0.12f);
                            }
                            else
                            {
                                var prop = hit.collider.GetComponentInParent<ProjectSurvival.Environment.DestructibleProp>();
                            if (prop != null && !prop.IsDestroyed)
                            {
                                prop.TakeDamage(effectiveDamage, hit.point, dir);
                                SpawnImpactEffect(hit.point, hit.normal, false);
                            }
                            var hazard = hit.collider.GetComponentInParent<ProjectSurvival.Environment.ExplosiveFuelHazard>();
                            if (hazard != null && !hazard.IsDetonated)
                            {
                                hazard.TakeDamage(effectiveDamage, hit.point, dir);
                                SpawnImpactEffect(hit.point, hit.normal, false);
                            }
                        }
                    }
                }
            }
        }

            // Heavy Impact cleave arc (Zero-allocation NonAlloc buffer)
            if (ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null && ProjectSurvival.Progression.SurvivorProgressionManager.Instance.HasHeavyImpact())
            {
                int hitCount = Physics.OverlapSphereNonAlloc(origin + dir * 1.4f, 1.6f, _meleeHitBuffer, hitLayers);
                for (int i = 0; i < hitCount; i++)
                {
                    ZombieAI otherZ = _meleeHitBuffer[i].GetComponentInParent<ZombieAI>();
                    if (otherZ != null && !otherZ.IsDead && otherZ.gameObject != hit.collider?.gameObject)
                    {
                        var cleaveRes = otherZ.ProcessHitZoneDamage(effectiveDamage * 0.65f, otherZ.transform.position + Vector3.up, dir);
                        otherZ.SetLastHitDetails(dir, otherZ.transform.position, "Axe", cleaveRes.IsHeadshot);
                    }
                }
            }

            bool isIndoors = WeaponFeelController.Instance != null && WeaponFeelController.Instance.IsIndoors(transform.position);
            float effectiveNoise = axeNoiseRadius * (isIndoors ? 0.6f : 1.0f);
            noiseEmitter?.EmitCustomImpulse(effectiveNoise);
            WeaponFeelController.Instance?.OnWeaponFired(WeaponType.Axe, muzzlePoint);
            GameEvents.TriggerWeaponFired();
        }

        private void FirePistol()
        {
            if (pistolClip <= 0)
            {
                Reload();
                return;
            }

            bool preserveAmmo = ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null && ProjectSurvival.Progression.SurvivorProgressionManager.Instance.ShouldPreserveAmmo();
            if (!preserveAmmo)
            {
                pistolClip--;
            }
            lastFireTime = Time.time;

            float spreadMult = WeaponAttachmentManager.Instance != null ? WeaponAttachmentManager.Instance.GetSpreadMultiplier(WeaponType.Pistol) : 1f;
            float noiseMult = WeaponAttachmentManager.Instance != null ? WeaponAttachmentManager.Instance.GetNoiseRadiusMultiplier(WeaponType.Pistol) : 1f;
            bool isSuppressed = WeaponAttachmentManager.Instance != null && WeaponAttachmentManager.Instance.HasSuppressor(WeaponType.Pistol);

            float recoilMult = ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null ? ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetRecoilMultiplier() : 1.0f;
            currentCrosshairSpread += 0.5f * spreadMult * recoilMult;

            BroadcastWeaponState();
            TriggerMuzzleFlash();
            GameEvents.TriggerCameraShakeRequested(0.2f * recoilMult, 0.12f);

            bool isIndoors = WeaponFeelController.Instance != null && WeaponFeelController.Instance.IsIndoors(transform.position);
            float effectiveNoise = pistolNoiseRadius * noiseMult * (isIndoors ? 0.6f : 1.0f);
            noiseEmitter?.EmitCustomImpulse(effectiveNoise);

            if (isSuppressed)
            {
                ProceduralAudioManager.Instance?.PlaySuppressedGunfire(false);
            }
            else
            {
                WeaponFeelController.Instance?.OnWeaponFired(WeaponType.Pistol, muzzlePoint);
            }
            GameEvents.TriggerWeaponFired();

            // Raycast from Camera Center
            Ray ray = GetCameraAimRay((isAiming ? 0.005f : 0.035f) * spreadMult);
            Vector3 tracerEnd = ray.origin + ray.direction * pistolRange;

            float apPenMult = ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null ? ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetFirearmDamagePenetrationMultiplier() : 1.0f;
            float finalPistolDmg = pistolDamage * apPenMult;

            if (Physics.Raycast(ray, out RaycastHit hit, pistolRange, hitLayers, QueryTriggerInteraction.Ignore))
            {
                tracerEnd = hit.point;
                ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
                if (zombie != null && !zombie.IsDead)
                {
                    var res = zombie.ProcessHitZoneDamage(finalPistolDmg, hit.point, ray.direction);
                    zombie.SetLastHitDetails(ray.direction, hit.point, res.IsHeadshot ? "Headshot" : "Pistol", res.IsHeadshot);
                    SpawnImpactEffect(hit.point, hit.normal, true);
                    if (res.IsHeadshot && ProjectSurvival.Combat.CombatJuiceController.Instance != null)
                        ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerCriticalHeadshotJuice(hit.point, hit.normal, zombie.IsDead);
                    else if (ProjectSurvival.Combat.CombatJuiceController.Instance != null)
                        ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerHitmarker(res.IsCritical, zombie.IsDead, hit.point);
                    else if (WeaponFeelController.Instance != null)
                        WeaponFeelController.Instance.TriggerHitJuice(res.IsCritical, hit.point, hit.normal, true);
                    else
                        GameEvents.TriggerHitConfirmed(res.IsCritical, hit.point);
                }
                else
                {
                    var raider = hit.collider.GetComponentInParent<ProjectSurvival.AI.HumanRaiderAI>();
                    if (raider != null && !raider.IsDead)
                    {
                        raider.TakeDamage(finalPistolDmg, hit.point, ray.direction);
                        SpawnImpactEffect(hit.point, hit.normal, true);
                        if (ProjectSurvival.Combat.CombatJuiceController.Instance != null)
                            ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerHitmarker(false, raider.IsDead, hit.point);
                        else
                            GameEvents.TriggerHitConfirmed(false, hit.point);
                    }
                    else
                    {
                        ProjectSurvival.AI.MutantBossAI boss = hit.collider.GetComponentInParent<ProjectSurvival.AI.MutantBossAI>();
                        if (boss != null && !boss.IsDead)
                        {
                            boss.TakeDamage(finalPistolDmg, hit.point, ray.direction);
                            SpawnImpactEffect(hit.point, hit.normal, true);
                            GameEvents.TriggerHitConfirmed(true, hit.point);
                        }
                        else
                        {
                            var animal = hit.collider.GetComponentInParent<ProjectSurvival.AI.WildlifeAnimalAI>();
                            if (animal != null && !animal.IsDead)
                            {
                                float distFromHead = Vector3.Distance(hit.point, animal.transform.position + Vector3.up * 1.1f);
                                bool isHeadshot = distFromHead <= 0.45f || hit.collider.name.ToLower().Contains("head");
                                float dmg = isHeadshot ? finalPistolDmg * headshotMultiplier : finalPistolDmg;
                                animal.TakeDamage(dmg, hit.point, ray.direction, isHeadshot);
                                SpawnImpactEffect(hit.point, hit.normal, true);
                                if (ProjectSurvival.Combat.CombatJuiceController.Instance != null)
                                    ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerHitmarker(isHeadshot, animal.IsDead, hit.point);
                                else
                                    GameEvents.TriggerHitConfirmed(isHeadshot, hit.point);
                            }
                            else
                            {
                                var prop = hit.collider.GetComponentInParent<ProjectSurvival.Environment.DestructibleProp>();
                            if (prop != null && !prop.IsDestroyed)
                            {
                                prop.TakeDamage(finalPistolDmg, hit.point, ray.direction);
                            }
                            var hazard = hit.collider.GetComponentInParent<ProjectSurvival.Environment.ExplosiveFuelHazard>();
                            if (hazard != null && !hazard.IsDetonated)
                            {
                                hazard.TakeDamage(finalPistolDmg, hit.point, ray.direction);
                            }

                            SpawnImpactEffect(hit.point, hit.normal, false);
                            WeaponFeelController.Instance?.TriggerHitJuice(false, hit.point, hit.normal, false);
                        }
                    }
                }
            }
        }

            Vector3 muzzlePos = muzzlePoint != null ? muzzlePoint.position : (transform.position + transform.forward * 0.5f + Vector3.up * 1.3f);
            SpawnTracer(muzzlePos, tracerEnd);

            if (pistolClip <= 0 && GetReserveAmmo("ammo_9mm") > 0)
            {
                Reload();
            }
        }

        private void FireShotgun()
        {
            if (shotgunClip <= 0)
            {
                Reload();
                return;
            }

            bool preserveShotgunAmmo = ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null && ProjectSurvival.Progression.SurvivorProgressionManager.Instance.ShouldPreserveAmmo();
            if (!preserveShotgunAmmo)
            {
                shotgunClip--;
            }
            lastFireTime = Time.time;

            float spreadMult = WeaponAttachmentManager.Instance != null ? WeaponAttachmentManager.Instance.GetSpreadMultiplier(WeaponType.Shotgun) : 1f;
            float noiseMult = WeaponAttachmentManager.Instance != null ? WeaponAttachmentManager.Instance.GetNoiseRadiusMultiplier(WeaponType.Shotgun) : 1f;
            bool isSuppressed = WeaponAttachmentManager.Instance != null && WeaponAttachmentManager.Instance.HasSuppressor(WeaponType.Shotgun);

            float shotgunRecoilMult = ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null ? ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetRecoilMultiplier() : 1.0f;
            currentCrosshairSpread += 0.8f * spreadMult * shotgunRecoilMult;

            BroadcastWeaponState();
            TriggerMuzzleFlash();
            GameEvents.TriggerCameraShakeRequested(0.38f * shotgunRecoilMult, 0.22f);

            bool isIndoors = WeaponFeelController.Instance != null && WeaponFeelController.Instance.IsIndoors(transform.position);
            float effectiveNoise = shotgunNoiseRadius * noiseMult * (isIndoors ? 0.6f : 1.0f);
            noiseEmitter?.EmitCustomImpulse(effectiveNoise);

            if (isSuppressed)
            {
                ProceduralAudioManager.Instance?.PlaySuppressedGunfire(true);
            }
            else
            {
                WeaponFeelController.Instance?.OnWeaponFired(WeaponType.Shotgun, muzzlePoint);
            }
            GameEvents.TriggerWeaponFired();

            Vector3 muzzlePos = muzzlePoint != null ? muzzlePoint.position : (transform.position + transform.forward * 0.5f + Vector3.up * 1.3f);

            float apPenShotgunMult = ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null ? ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetFirearmDamagePenetrationMultiplier() : 1.0f;
            float finalPelletDamage = shotgunPelletDamage * apPenShotgunMult;

            for (int i = 0; i < shotgunPelletCount; i++)
            {
                Ray pelletRay = GetCameraAimRay((isAiming ? 0.04f : 0.08f) * spreadMult);
                Vector3 tracerEnd = pelletRay.origin + pelletRay.direction * shotgunRange;

                if (Physics.Raycast(pelletRay, out RaycastHit hit, shotgunRange, hitLayers, QueryTriggerInteraction.Ignore))
                {
                    tracerEnd = hit.point;
                    ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
                    if (zombie != null && !zombie.IsDead)
                    {
                        var res = zombie.ProcessHitZoneDamage(finalPelletDamage, hit.point, pelletRay.direction);
                        zombie.SetLastHitDetails(pelletRay.direction, hit.point, "Shotgun", res.IsHeadshot);
                        SpawnImpactEffect(hit.point, hit.normal, true);
                        if (res.IsHeadshot && ProjectSurvival.Combat.CombatJuiceController.Instance != null)
                            ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerCriticalHeadshotJuice(hit.point, hit.normal, zombie.IsDead);
                        else if (ProjectSurvival.Combat.CombatJuiceController.Instance != null)
                            ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerHitmarker(res.IsCritical, zombie.IsDead, hit.point);
                        else if (WeaponFeelController.Instance != null)
                            WeaponFeelController.Instance.TriggerHitJuice(res.IsCritical, hit.point, hit.normal, true);
                        else
                            GameEvents.TriggerHitConfirmed(res.IsCritical, hit.point);
                    }
                    else
                    {
                        var raider = hit.collider.GetComponentInParent<ProjectSurvival.AI.HumanRaiderAI>();
                        if (raider != null && !raider.IsDead)
                        {
                            raider.TakeDamage(finalPelletDamage, hit.point, pelletRay.direction);
                            SpawnImpactEffect(hit.point, hit.normal, true);
                            if (i == 0)
                            {
                                if (ProjectSurvival.Combat.CombatJuiceController.Instance != null)
                                    ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerHitmarker(false, raider.IsDead, hit.point);
                                else
                                    GameEvents.TriggerHitConfirmed(false, hit.point);
                            }
                        }
                        else
                        {
                            ProjectSurvival.AI.MutantBossAI boss = hit.collider.GetComponentInParent<ProjectSurvival.AI.MutantBossAI>();
                            if (boss != null && !boss.IsDead)
                            {
                                boss.TakeDamage(finalPelletDamage, hit.point, pelletRay.direction);
                                SpawnImpactEffect(hit.point, hit.normal, true);
                                if (i == 0) GameEvents.TriggerHitConfirmed(true, hit.point);
                            }
                            else
                            {
                                var animal = hit.collider.GetComponentInParent<ProjectSurvival.AI.WildlifeAnimalAI>();
                                if (animal != null && !animal.IsDead)
                                {
                                    animal.TakeDamage(finalPelletDamage, hit.point, pelletRay.direction);
                                    SpawnImpactEffect(hit.point, hit.normal, true);
                                    if (i == 0)
                                    {
                                        if (ProjectSurvival.Combat.CombatJuiceController.Instance != null)
                                            ProjectSurvival.Combat.CombatJuiceController.Instance.TriggerHitmarker(false, animal.IsDead, hit.point);
                                        else
                                            GameEvents.TriggerHitConfirmed(false, hit.point);
                                    }
                                }
                                else
                                {
                                    var prop = hit.collider.GetComponentInParent<ProjectSurvival.Environment.DestructibleProp>();
                                if (prop != null && !prop.IsDestroyed)
                                {
                                    prop.TakeDamage(finalPelletDamage, hit.point, pelletRay.direction);
                                }
                                var hazard = hit.collider.GetComponentInParent<ProjectSurvival.Environment.ExplosiveFuelHazard>();
                                if (hazard != null && !hazard.IsDetonated)
                                {
                                    hazard.TakeDamage(finalPelletDamage, hit.point, pelletRay.direction);
                                }

                                SpawnImpactEffect(hit.point, hit.normal, false);
                                if (i == 0) WeaponFeelController.Instance?.TriggerHitJuice(false, hit.point, hit.normal, false);
                            }
                        }
                    }
                }
            }

                SpawnTracer(muzzlePos, tracerEnd);
            }

            if (shotgunClip <= 0 && GetReserveAmmo("ammo_shells") > 0)
            {
                Reload();
            }
        }

        private void FireCrossbow()
        {
            if (crossbowClip <= 0)
            {
                Reload();
                return;
            }

            bool preserveAmmo = ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null && ProjectSurvival.Progression.SurvivorProgressionManager.Instance.ShouldPreserveAmmo();
            if (!preserveAmmo)
            {
                crossbowClip--;
            }
            lastFireTime = Time.time;

            float spreadMult = WeaponAttachmentManager.Instance != null ? WeaponAttachmentManager.Instance.GetSpreadMultiplier(WeaponType.Crossbow) : 1f;
            currentCrosshairSpread += 0.15f * spreadMult;

            BroadcastWeaponState();
            GameEvents.TriggerCameraShakeRequested(0.12f, 0.08f);

            bool isIndoors = WeaponFeelController.Instance != null && WeaponFeelController.Instance.IsIndoors(transform.position);
            float effectiveNoise = crossbowNoiseRadius * (isIndoors ? 0.6f : 1.0f);
            noiseEmitter?.EmitCustomImpulse(effectiveNoise);

            ProceduralAudioManager.Instance?.PlayBowstringRelease(transform.position);
            WeaponFeelController.Instance?.OnWeaponFired(WeaponType.Crossbow, muzzlePoint);
            GameEvents.TriggerWeaponFired();

            Vector3 spawnOrigin = muzzlePoint != null ? muzzlePoint.position : (transform.position + transform.forward * 0.5f + Vector3.up * 1.3f);
            Ray aimRay = GetCameraAimRay((isAiming ? 0.002f : 0.015f) * spreadMult);

            RetrievableBoltProjectile.SpawnBolt(spawnOrigin, aimRay.direction, 68f, crossbowDamage, crossbowHeadshotMultiplier, hitLayers);

            if (crossbowClip <= 0 && GetReserveAmmo("ammo_crossbow_bolt") > 0)
            {
                Reload();
            }
        }

        private Ray GetCameraAimRay(float spreadAngle)
        {
            if (mainCamera == null) mainCamera = Camera.main;

            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            Ray baseRay = mainCamera.ScreenPointToRay(screenCenter);

            if (spreadAngle > 0.001f)
            {
                Vector2 spreadCircle = UnityEngine.Random.insideUnitCircle * spreadAngle;
                Vector3 spreadDir = baseRay.direction + (mainCamera.transform.right * spreadCircle.x) + (mainCamera.transform.up * spreadCircle.y);
                return new Ray(baseRay.origin, spreadDir.normalized);
            }

            return baseRay;
        }

        public void Reload()
        {
            if (isReloading) return;
            if (currentWeapon != WeaponType.Pistol && currentWeapon != WeaponType.Shotgun && currentWeapon != WeaponType.Crossbow) return;

            int needed = MaxClip - CurrentClip;
            if (needed <= 0) return;

            string ammoId = currentWeapon == WeaponType.Pistol ? "ammo_9mm" : (currentWeapon == WeaponType.Shotgun ? "ammo_shells" : "ammo_crossbow_bolt");
            int reserve = GetReserveAmmo(ammoId);
            if (reserve <= 0)
            {
                Debug.Log("[WeaponSystem] No reserve ammo for reload!");
                return;
            }

            StartCoroutine(ReloadRoutine(ammoId, needed));
        }

        private IEnumerator ReloadRoutine(string ammoId, int needed)
        {
            isReloading = true;
            Debug.Log($"[WeaponSystem] Reloading {currentWeapon}...");

            float baseDuration = currentWeapon == WeaponType.Crossbow ? crossbowReloadDuration : 1.6f;
            float reloadSpeed = WeaponAttachmentManager.Instance != null ? WeaponAttachmentManager.Instance.GetReloadSpeedMultiplier(currentWeapon) : 1.0f;
            if (ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null)
            {
                reloadSpeed *= ProjectSurvival.Progression.SurvivorProgressionManager.Instance.GetReloadSpeedMultiplier();
            }
            yield return new WaitForSeconds(baseDuration / Mathf.Max(0.1f, reloadSpeed));

            int reserve = GetReserveAmmo(ammoId);
            int toLoad = Mathf.Min(needed, reserve);

            ConsumeReserveAmmo(ammoId, toLoad);

            if (currentWeapon == WeaponType.Pistol) pistolClip += toLoad;
            else if (currentWeapon == WeaponType.Shotgun) shotgunClip += toLoad;
            else if (currentWeapon == WeaponType.Crossbow)
            {
                crossbowClip += toLoad;
                ProceduralAudioManager.Instance?.PlayQuiverClick(transform.position);
            }

            isReloading = false;
            BroadcastWeaponState();
            Debug.Log($"[WeaponSystem] Reload complete: {CurrentClip}/{GetReserveAmmo(ammoId)}");
        }

        private int GetReserveAmmo(string ammoId)
        {
            if (inventory == null) return 0;
            int count = 0;
            foreach (var slot in inventory.Slots)
            {
                if (slot.item != null && slot.item.itemId == ammoId)
                {
                    count += slot.quantity;
                }
            }
            return count;
        }

        private void ConsumeReserveAmmo(string ammoId, int count)
        {
            if (inventory == null) return;
            int remaining = count;
            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                var slot = inventory.Slots[i];
                if (slot.item != null && slot.item.itemId == ammoId)
                {
                    if (slot.quantity <= remaining)
                    {
                        remaining -= slot.quantity;
                        slot.Clear();
                    }
                    else
                    {
                        slot.quantity -= remaining;
                        remaining = 0;
                        break;
                    }
                }
            }
            GameEvents.TriggerInventoryUpdated();
        }

        private void UpdateCrosshairSpread()
        {
            float targetSpread = 1.0f;
            var cc = GetComponent<CharacterController>();
            if (cc != null && cc.velocity.sqrMagnitude > 0.1f)
            {
                targetSpread = 1.7f;
            }
            if (isAiming)
            {
                targetSpread *= 0.5f;
            }

            currentCrosshairSpread = Mathf.MoveTowards(currentCrosshairSpread, targetSpread, Time.deltaTime * 3.5f);
            GameEvents.TriggerCrosshairSpreadChanged(currentCrosshairSpread);
        }

        private void UpdateLaserSight()
        {
            if (laserSight == null || !laserSight.enabled) return;

            Vector3 startPos = muzzlePoint != null ? muzzlePoint.position : (transform.position + Vector3.up * 1.3f + transform.forward * 0.4f);
            Ray aimRay = GetCameraAimRay(0f);
            Vector3 endPos = aimRay.origin + aimRay.direction * 35f;

            if (Physics.Raycast(aimRay, out RaycastHit hit, 35f, hitLayers, QueryTriggerInteraction.Ignore))
            {
                endPos = hit.point;
            }

            laserSight.SetPosition(0, startPos);
            laserSight.SetPosition(1, endPos);
        }

        private void TriggerMuzzleFlash()
        {
            if (muzzleLight != null)
            {
                StartCoroutine(MuzzleFlashRoutine());
            }
        }

        private IEnumerator MuzzleFlashRoutine()
        {
            muzzleLight.enabled = true;
            yield return new WaitForSeconds(0.045f);
            muzzleLight.enabled = false;
        }

        private void SpawnTracer(Vector3 start, Vector3 end)
        {
            GameObject tracerObj = new GameObject("BulletTracer");
            LineRenderer lr = tracerObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = 0.035f;
            lr.endWidth = 0.02f;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            Material mat = tracerMaterial;
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                mat.color = new Color(1f, 0.9f, 0.4f, 0.9f);
            }
            lr.sharedMaterial = mat;

            Destroy(tracerObj, 0.065f);
        }

        private void SpawnImpactEffect(Vector3 pos, Vector3 normal, bool isFlesh)
        {
            if (ProjectSurvival.Combat.ImpactDecalPoolManager.Instance != null)
            {
                ProjectSurvival.Combat.ImpactDecalPoolManager.Instance.SpawnImpact(pos, normal, isFlesh);
                return;
            }

            Color fxColor = isFlesh ? new Color(0.85f, 0.08f, 0.08f, 0.95f) : new Color(1f, 0.85f, 0.2f, 0.95f);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material m = new Material(shader);
            m.color = fxColor;

            // Flash core
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = isFlesh ? "BloodImpact" : "SparkImpact";
            core.transform.position = pos;
            core.transform.localScale = Vector3.one * (isFlesh ? 0.22f : 0.14f);
            Collider cc = core.GetComponent<Collider>();
            if (cc != null) Destroy(cc);
            Renderer cr = core.GetComponent<Renderer>();
            if (cr != null) cr.sharedMaterial = m;
            Destroy(core, 0.15f);

            // Scattering droplets / sparks
            int count = isFlesh ? 4 : 3;
            for (int i = 0; i < count; i++)
            {
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                p.transform.position = pos + UnityEngine.Random.insideUnitSphere * 0.05f;
                p.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.04f, 0.08f);
                Collider pc = p.GetComponent<Collider>();
                if (pc != null) Destroy(pc);
                Renderer pr = p.GetComponent<Renderer>();
                if (pr != null) pr.sharedMaterial = m;

                Rigidbody rb = p.AddComponent<Rigidbody>();
                rb.mass = 0.05f;
                rb.linearVelocity = (normal + UnityEngine.Random.insideUnitSphere * 0.8f).normalized * UnityEngine.Random.Range(2.5f, 5.5f);
                Destroy(p, UnityEngine.Random.Range(0.25f, 0.45f));
            }
        }

        private void UpdateWeaponVisuals()
        {
            if (visualAxe != null) visualAxe.SetActive(currentWeapon == WeaponType.Axe);
            if (visualPistol != null) visualPistol.SetActive(currentWeapon == WeaponType.Pistol);
            if (visualShotgun != null) visualShotgun.SetActive(currentWeapon == WeaponType.Shotgun);
            if (visualCrossbow != null) visualCrossbow.SetActive(currentWeapon == WeaponType.Crossbow);
            else if (currentWeapon == WeaponType.Crossbow)
            {
                EnsureCrossbowVisual();
            }
        }

        public void SetVisualReferences(GameObject axe, GameObject pistol, GameObject shotgun, Transform muzzle, Light mLight, GameObject crossbow = null)
        {
            visualAxe = axe;
            visualPistol = pistol;
            visualShotgun = shotgun;
            if (crossbow != null) visualCrossbow = crossbow;
            muzzlePoint = muzzle;
            muzzleLight = mLight;
            UpdateWeaponVisuals();
        }

        public GameObject EnsureCrossbowVisual(Transform handSocket = null)
        {
            if (visualCrossbow != null)
            {
                visualCrossbow.SetActive(currentWeapon == WeaponType.Crossbow);
                return visualCrossbow;
            }

            Transform parentSocket = handSocket != null ? handSocket : weaponSocket;
            if (parentSocket == null)
            {
                Transform found = transform.Find("Visual_Model/Body_Hips/Torso/Chest/RightArm/RightForearm/RightHand/Weapon_Socket")
                    ?? transform.Find("Visual_Model/Weapon_Socket")
                    ?? transform;
                parentSocket = found;
            }

            Transform existing = parentSocket.Find("Visual_Weapon_Crossbow");
            if (existing != null)
            {
                visualCrossbow = existing.gameObject;
                visualCrossbow.SetActive(currentWeapon == WeaponType.Crossbow);
                return visualCrossbow;
            }

            GameObject cb = new GameObject("Visual_Weapon_Crossbow");
            cb.transform.SetParent(parentSocket);
            cb.transform.localPosition = new Vector3(0f, 0.02f, 0.20f);
            cb.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Stock & Rail
            GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "Stock_Rail";
            stock.transform.SetParent(cb.transform, false);
            stock.transform.localPosition = new Vector3(0f, 0f, 0f);
            stock.transform.localScale = new Vector3(0.045f, 0.055f, 0.42f);
            DestroyImmediate(stock.GetComponent<Collider>());

            // Crossbow Limbs (Prod)
            GameObject limbs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            limbs.name = "Prod_Limbs";
            limbs.transform.SetParent(cb.transform, false);
            limbs.transform.localPosition = new Vector3(0f, 0.02f, 0.18f);
            limbs.transform.localScale = new Vector3(0.36f, 0.035f, 0.025f);
            DestroyImmediate(limbs.GetComponent<Collider>());

            // Stirrup / Foot Step
            GameObject stirrup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stirrup.name = "Stirrup";
            stirrup.transform.SetParent(cb.transform, false);
            stirrup.transform.localPosition = new Vector3(0f, 0f, 0.23f);
            stirrup.transform.localScale = new Vector3(0.08f, 0.02f, 0.05f);
            DestroyImmediate(stirrup.GetComponent<Collider>());

            // Compact Reflex Scope Rail
            GameObject scope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scope.name = "Reflex_Optic";
            scope.transform.SetParent(cb.transform, false);
            scope.transform.localPosition = new Vector3(0f, 0.05f, -0.05f);
            scope.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            scope.transform.localScale = new Vector3(0.028f, 0.06f, 0.028f);
            DestroyImmediate(scope.GetComponent<Collider>());

            // Materials
            Material gunMetal = RetrievableBoltProjectile.GetOrCreateMaterialRuntime("Mat_TacticalPolymer", new Color(0.12f, 0.13f, 0.15f));
            Material steelMat = RetrievableBoltProjectile.GetOrCreateMaterialRuntime("Mat_Weapon_Steel", new Color(0.25f, 0.28f, 0.30f));
            stock.GetComponent<Renderer>().sharedMaterial = gunMetal;
            limbs.GetComponent<Renderer>().sharedMaterial = steelMat;
            stirrup.GetComponent<Renderer>().sharedMaterial = steelMat;
            scope.GetComponent<Renderer>().sharedMaterial = gunMetal;

            visualCrossbow = cb;
            visualCrossbow.SetActive(currentWeapon == WeaponType.Crossbow);
            return visualCrossbow;
        }

        public void BroadcastWeaponState()
        {
            string nameStr = "ELLER BOŞ (SİLAHSIZ)";
            int clip = 0;
            int res = 0;

            if (currentWeapon == WeaponType.Axe)
            {
                nameStr = "İTFAİYE BALTASI";
            }
            else if (currentWeapon == WeaponType.Pistol)
            {
                nameStr = "9MM TABANCA";
                clip = pistolClip;
                res = GetReserveAmmo("ammo_9mm");
            }
            else if (currentWeapon == WeaponType.Shotgun)
            {
                nameStr = "POMPALI TÜFEK";
                clip = shotgunClip;
                res = GetReserveAmmo("ammo_shells");
            }
            else if (currentWeapon == WeaponType.Crossbow)
            {
                nameStr = "TAKTİK TATAR YAYI";
                clip = crossbowClip;
                res = GetReserveAmmo("ammo_crossbow_bolt");
            }

            // Append active attachment badges (e.g. [S] [L] [M] or [R])
            if (WeaponAttachmentManager.Instance != null && currentWeapon != WeaponType.None)
            {
                string badges = WeaponAttachmentManager.Instance.GetActiveBadgesForWeapon(currentWeapon);
                if (!string.IsNullOrEmpty(badges))
                {
                    nameStr = $"{nameStr} {badges}";
                }
            }

            GameEvents.TriggerWeaponChanged(nameStr, clip, res);
        }
    }
}
