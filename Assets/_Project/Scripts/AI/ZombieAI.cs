using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;

namespace ProjectSurvival.AI
{
    public enum ZombieType
    {
        Standard,
        Runner,
        Screamer,
        Tank
    }

    public enum ZombieState
    {
        Idle,
        Patrol,
        Investigate,
        Chase,
        Attack,
        Scream
    }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(ZombieSensor))]
    public class ZombieAI : ZombieBrain
    {
        [Header("Screamer Specifics")]
        [SerializeField] private float screamRadius = 25f;
        [SerializeField] private float screamDuration = 2.0f;
        [SerializeField] private float screamCooldown = 15f;

        public float ScreamDuration => screamDuration;

        private LineRenderer screamShockwaveRenderer;
        private float lastScreamTime = -99f;
        private Renderer[] cachedRenderers;
        private Coroutine hitFlashCoroutine;
        private Coroutine staggerRoutine;

        protected override void Awake()
        {
            base.Awake();
            cachedRenderers = GetComponentsInChildren<Renderer>();

            if (zombieType == ZombieType.Screamer)
            {
                SetupScreamRenderer();
            }
        }

        private void SetupScreamRenderer()
        {
            GameObject shockwaveChild = new GameObject("ScreamShockwave");
            shockwaveChild.transform.SetParent(transform, false);
            screamShockwaveRenderer = shockwaveChild.AddComponent<LineRenderer>();
            screamShockwaveRenderer.useWorldSpace = true;
            screamShockwaveRenderer.loop = true;
            screamShockwaveRenderer.positionCount = 36;
            screamShockwaveRenderer.startWidth = 0.15f;
            screamShockwaveRenderer.endWidth = 0.15f;

            Color shockColor = new Color(1f, 0.1f, 0.3f, 0.8f);
            screamShockwaveRenderer.startColor = shockColor;
            screamShockwaveRenderer.endColor = shockColor;

            Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (s != null)
            {
                Material m = new Material(s);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", shockColor);
                screamShockwaveRenderer.material = m;
            }

            screamShockwaveRenderer.enabled = false;
        }

        protected override void Update()
        {
            base.Update();
            if (isDead) return;

            if (currentState == ZombieState.Scream)
            {
                UpdateScream();
            }
            else if (zombieType == ZombieType.Screamer && currentState == ZombieState.Chase)
            {
                if (Time.time >= lastScreamTime + screamCooldown)
                {
                    SetState(ZombieState.Scream);
                }
            }
        }

        private bool isScreamTelegraphing = false;
        private const float ScreamTelegraphTime = 1.0f;

        public override void SetState(ZombieState newState)
        {
            base.SetState(newState);

            if (newState == ZombieState.Scream)
            {
                if (agent == null) agent = GetComponent<NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.speed = 0f;
                }
                lastScreamTime = Time.time;
                isScreamTelegraphing = true;
                stateTimer = 0f;
                if (locomotionEngine != null) locomotionEngine.TriggerScreamerTelegraph();
                TriggerPurpleGlow(true);
            }
            else
            {
                if (isScreamTelegraphing)
                {
                    CancelScream();
                }
            }
        }

        private void UpdateScream()
        {
            stateTimer += Time.deltaTime;

            if (chaseTarget != null)
            {
                Vector3 lookDir = (chaseTarget.position - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 6f);
                }
            }

            // 1.0s Telegraph wind-up phase
            if (isScreamTelegraphing)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(stateTimer * 16f);
                SetPurpleGlowColor(new Color(0.85f * pulse, 0.15f * pulse, 0.95f * pulse, 1f));

                float effectiveTelegraphTime = HordeAlertSystem.Instance != null ? HordeAlertSystem.Instance.TelegraphDuration : (screamDuration > 0f ? screamDuration : ScreamTelegraphTime);
                if (stateTimer >= effectiveTelegraphTime)
                {
                    isScreamTelegraphing = false;
                    TriggerPurpleGlow(false);
                    if (locomotionEngine != null) locomotionEngine.CancelScreamerTelegraph();

                    // Unleash shriek via HordeAlertSystem
                    if (HordeAlertSystem.Instance != null)
                    {
                        HordeAlertSystem.Instance.ExecuteScreamerShriek(transform.position, chaseTarget);
                    }
                    else
                    {
                        ProceduralAudioManager.Instance?.PlayScreamerShriek();
                        GameEvents.TriggerHordeAlertTriggered(transform.position, screamRadius);
                        GameEvents.TriggerNoiseEmitted(transform.position, screamRadius);
                    }

                    animationBridge?.TriggerScream();
                    SetState(ZombieState.Chase);
                }
            }
        }

        public void CancelScream()
        {
            if (!isScreamTelegraphing) return;
            isScreamTelegraphing = false;
            TriggerPurpleGlow(false);
            if (locomotionEngine != null) locomotionEngine.CancelScreamerTelegraph();
            SetState(ZombieState.Chase);
        }

        private void TriggerPurpleGlow(bool enable)
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                cachedRenderers = GetComponentsInChildren<Renderer>();
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            if (enable)
            {
                block.SetColor("_BaseColor", new Color(0.85f, 0.2f, 0.95f, 1f));
            }
            else
            {
                block.Clear();
            }

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null) cachedRenderers[i].SetPropertyBlock(block);
            }
        }

        private void SetPurpleGlowColor(Color c)
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0) return;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", c);
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null) cachedRenderers[i].SetPropertyBlock(block);
            }
        }

        private void DrawShockwave(float radius)
        {
            if (screamShockwaveRenderer == null) return;

            int segs = 36;
            float step = 360f / segs;
            Vector3 center = transform.position;

            for (int i = 0; i < segs; i++)
            {
                float ang = i * step * Mathf.Deg2Rad;
                float x = center.x + Mathf.Cos(ang) * radius;
                float z = center.z + Mathf.Sin(ang) * radius;
                screamShockwaveRenderer.SetPosition(i, new Vector3(x, center.y + 0.1f, z));
            }
        }

        public void SetTarget(Transform target)
        {
            if (isDead || target == null) return;
            chaseTarget = target;
            if (currentState != ZombieState.Chase && currentState != ZombieState.Attack && currentState != ZombieState.Scream)
            {
                SetState(ZombieState.Chase);
            }
        }

        public void TakeDamage(float amount, Transform attacker)
        {
            if (attacker != null)
            {
                chaseTarget = attacker;
                SetState(ZombieState.Chase);
            }
            Vector3 hitDir = attacker != null ? (transform.position - attacker.position).normalized : Vector3.forward;
            ProcessHitZoneDamage(amount, transform.position + Vector3.up * 1.0f, hitDir);
        }

        /// <summary>
        /// Tactical Hit-Zone combat resolution:
        /// - Headshot (>= 0.82h): 2.5x critical damage, crimson hit flash, 0.25s head-snap stagger, Screamer cancellation.
        /// - Torso (0.38h - 0.82h): 1.0x damage, 1.1m directional knockback, melee attack timer reset.
        /// - Legs (< 0.38h): 0.85x damage, leg trauma accumulation -> Cripple state at 35 trauma (55% speed cut, hip drop, drag limp).
        /// </summary>
        public HitZoneDamageResult ProcessHitZoneDamage(float baseDamage, Vector3 hitPoint, Vector3 hitDir)
        {
            if (isDead) return default;
            if (col == null) col = GetComponent<Collider>();
            if (locomotionEngine == null) locomotionEngine = GetComponent<ZombieLocomotionEngine>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();

            float agentHeight = 1.8f;
            if (col is CapsuleCollider cc) agentHeight = cc.height * transform.lossyScale.y;
            else if (zombieType == ZombieType.Tank) agentHeight = 2.25f;

            HitZoneDamageResult result = ZombieHitZoneResolver.ResolveHit(baseDamage, hitPoint, transform.position, agentHeight);

            if (result.Zone == HitZone.Head)
            {
                if (currentState == ZombieState.Scream)
                {
                    CancelScream();
                }
                ApplyHeadSnapStagger(ZombieHitZoneResolver.HeadSnapStaggerDuration);
            }
            else if (result.Zone == HitZone.Torso)
            {
                lastAttackTime = Time.time; // Reset melee attack timer
                if (zombieType != ZombieType.Tank)
                {
                    ApplyKnockback(hitDir, ZombieHitZoneResolver.TorsoKnockbackDistance);
                }
            }
            else if (result.Zone == HitZone.Legs)
            {
                float traumaAmount = result.FinalDamage;
                if (ProjectSurvival.Progression.SurvivorProgressionManager.Instance != null && ProjectSurvival.Progression.SurvivorProgressionManager.Instance.HasHeavyImpact())
                {
                    traumaAmount *= 1.30f;
                }
                legTrauma += traumaAmount;
                if (legTrauma >= ZombieHitZoneResolver.LegTraumaThreshold && !isCrippled)
                {
                    SetCrippled(true);
                }
            }

            SetLastHitDetails(hitDir, hitPoint, result.IsHeadshot ? "Headshot" : "Pistol", result.IsHeadshot);
            base.TakeDamage(result.FinalDamage, hitPoint, -hitDir);
            TriggerHitFlash(result.IsHeadshot);
            if (result.IsHeadshot)
            {
                ProjectSurvival.Progression.SurvivorProgressionManager.Instance?.AwardHeadshotBonus();
            }
            GameEvents.TriggerCameraShakeRequested(result.IsHeadshot ? 0.32f : 0.22f, 0.15f);

            return result;
        }

        public void ApplyHeadSnapStagger(float duration)
        {
            if (isDead) return;
            Stun(duration);
            if (locomotionEngine != null) locomotionEngine.TriggerHeadSnap(duration);
        }

        public override void TakeDamage(float damage, Vector3 hitPoint = default, Vector3 hitNormal = default)
        {
            if (currentState == ZombieState.Scream)
            {
                CancelScream();
            }

            base.TakeDamage(damage, hitPoint, hitNormal);
            TriggerHitFlash(false);
            GameEvents.TriggerCameraShakeRequested(0.22f, 0.15f);

            // Knockback impulse away from player if not tank
            if (zombieType != ZombieType.Tank)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    Vector3 knockDir = (transform.position - player.transform.position).normalized;
                    ApplyKnockback(knockDir, 1.1f);
                }
            }
        }

        private void TriggerHitFlash(bool isHeadshot = false)
        {
            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(HitFlashRoutine(isHeadshot));
        }

        private IEnumerator HitFlashRoutine(bool isHeadshot = false)
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                cachedRenderers = GetComponentsInChildren<Renderer>();
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            Color flashCol = isHeadshot ? new Color(1f, 0.05f, 0.12f, 1f) : new Color(1f, 0.25f, 0.25f, 1f);
            block.SetColor("_BaseColor", flashCol);
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null) cachedRenderers[i].SetPropertyBlock(block);
            }

            yield return new WaitForSeconds(0.12f);

            block.Clear();
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null) cachedRenderers[i].SetPropertyBlock(block);
            }
        }

        public void ApplyKnockback(Vector3 direction, float distance)
        {
            if (isDead) return;
            Stun(0.8f);
            direction.y = 0;
            if (staggerRoutine != null) StopCoroutine(staggerRoutine);
            staggerRoutine = StartCoroutine(StaggerMotion(direction.normalized, distance));
        }

        private IEnumerator StaggerMotion(Vector3 dir, float dist)
        {
            float elapsed = 0f;
            float duration = 0.22f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + dir * dist;

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, dist + 0.5f, NavMesh.AllAreas))
            {
                targetPos = hit.position;
            }

            Quaternion startRot = transform.rotation;
            Quaternion flinchRot = Quaternion.Euler(UnityEngine.Random.Range(-12f, -4f), transform.eulerAngles.y + UnityEngine.Random.Range(-15f, 15f), 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);

                Vector3 newPos = Vector3.Lerp(startPos, targetPos, easeOut);
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.Warp(newPos);
                }
                else
                {
                    transform.position = newPos;
                }

                transform.rotation = Quaternion.Slerp(startRot, flinchRot, easeOut);
                yield return null;
            }

            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            staggerRoutine = null;
        }

        private Vector3 lastHitDir = Vector3.back;
        private Vector3 lastHitPoint = Vector3.zero;
        private string lastDamageType = "Pistol";
        private bool lastWasHeadshot = false;

        public void SetLastHitDetails(Vector3 dir, Vector3 point, string dmgType, bool isHeadshot = false)
        {
            lastHitDir = dir;
            lastHitPoint = point;
            lastDamageType = dmgType;
            lastWasHeadshot = isHeadshot;
        }

        protected override void Die()
        {
            base.Die();
            if (screamShockwaveRenderer != null) screamShockwaveRenderer.enabled = false;

            // Phase 6: Trigger Directional Ragdoll if controller attached
            var ragdoll = GetComponent<ZombieRagdollController>();
            if (ragdoll != null)
            {
                var rootCol = GetComponent<Collider>();
                if (rootCol != null) rootCol.enabled = false;

                ragdoll.ApplyDamageImpulse(lastDamageType, lastHitDir, lastHitPoint, lastWasHeadshot);
            }
            else
            {
                // Fallback: Disable character / child colliders so player can walk freely
                Collider[] cols = GetComponentsInChildren<Collider>();
                for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
            }

            DropDeathLoot();

            GameEvents.TriggerZombieAlertStateChanged(gameObject, "");
            GameEvents.TriggerZombieKilled(gameObject);
            Debug.Log($"[{gameObject.name}] Zombie perished & dropped loot.");

            // Phase 4: Register corpse for stealth dragging & concealment (45s lifetime)
            if (Player.BodyConcealmentSystem.Instance != null)
            {
                Player.BodyConcealmentSystem.Instance.RegisterCorpse(gameObject);
            }
            else
            {
                Invoke(nameof(Recycle), 3.0f);
            }
        }

        private void DropDeathLoot()
        {
            GameObject lootObj = new GameObject("Zombie_Drop_" + gameObject.name);
            lootObj.transform.position = transform.position + Vector3.up * 0.15f;

            GameObject pouch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pouch.transform.SetParent(lootObj.transform);
            pouch.transform.localPosition = Vector3.zero;
            pouch.transform.localScale = new Vector3(0.35f, 0.18f, 0.35f);
            Material pouchMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            pouchMat.color = new Color(0.85f, 0.70f, 0.20f);
            pouch.GetComponent<Renderer>().sharedMaterial = pouchMat;

            SphereCollider sc = lootObj.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.2f;

            LootContainer lc = lootObj.AddComponent<LootContainer>();
            float roll = UnityEngine.Random.value;
            string dropId = "ammo_9mm";
            int qty = 8;
            if (roll < 0.40f) { dropId = "ammo_9mm"; qty = UnityEngine.Random.Range(6, 15); }
            else if (roll < 0.65f) { dropId = "ammo_shells"; qty = UnityEngine.Random.Range(2, 5); }
            else if (roll < 0.85f) { dropId = "item_scrap"; qty = UnityEngine.Random.Range(1, 3); }
            else { dropId = "item_cloth"; qty = UnityEngine.Random.Range(1, 3); }

            ItemData dropItem = Resources.Load<ItemData>($"Items/{dropId}");
            if (dropItem != null)
            {
                lc.AddItem(dropItem, qty);
            }
        }

        private void Recycle()
        {
            string poolTag = $"Zombie_{zombieType}";
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
