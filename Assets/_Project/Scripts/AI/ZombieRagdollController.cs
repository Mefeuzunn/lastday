using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectSurvival.Core;

namespace ProjectSurvival.AI
{
    [Serializable]
    public class RagdollBoneInfo
    {
        public string boneName;
        public Transform boneTransform;
        public Rigidbody boneRigidbody;
        public Collider boneCollider;
        public CharacterJoint joint;
    }

    /// <summary>
    /// Active Phase 6: Directional, Momentum-Driven Zombie Ragdoll & Knockback Controller.
    /// Manages non-self-colliding articulated ragdoll physics across Pelvis, Torso, Head, and Limbs.
    /// On lethal blow or heavy impact, disables Animator/NavMeshAgent, activates dynamic rigidbodies,
    /// applies directional momentum vectors (9mm 140N, Shotgun 380N, Axe 250N, Vehicle 580N, Blast 480N),
    /// and automatically locks rigidbodies to isKinematic = true after 3.5 seconds (0 ms mobile CPU cost),
    /// preserving BodyConcealmentSystem corpse dragging compatibility.
    /// </summary>
    public class ZombieRagdollController : MonoBehaviour
    {
        [Header("Ragdoll Configuration")]
        [SerializeField] private bool isRagdollActive = false;
        [SerializeField] private bool isFrozen = false;
        [SerializeField] private float freezeDelay = 3.5f;
        [SerializeField] private float sleepVelocityThreshold = 0.08f;

        [Header("Impulse Settings (Newtons)")]
        [SerializeField] private float pistolImpulse = 140f;
        [SerializeField] private float shotgunImpulse = 380f;
        [SerializeField] private float axeImpulse = 250f;
        [SerializeField] private float vehicleImpulse = 580f;
        [SerializeField] private float blastImpulse = 480f;

        [Header("Rig Structure")]
        [SerializeField] private Transform pelvisBone;
        [SerializeField] private List<RagdollBoneInfo> ragdollBones = new List<RagdollBoneInfo>();

        public bool IsRagdollActive => isRagdollActive;
        public bool IsFrozen => isFrozen;
        public Transform PelvisBone => pelvisBone;
        public IReadOnlyList<RagdollBoneInfo> RagdollBones => ragdollBones;

        private Animator anim;
        private NavMeshAgent agent;
        private Collider rootCollider;
        private Coroutine freezeCoroutine;

        private void Awake()
        {
            anim = GetComponentInChildren<Animator>();
            agent = GetComponent<NavMeshAgent>();
            rootCollider = GetComponent<Collider>();

            EnsureRagdollRig();
            SetRagdollKinematic(true);
        }

        public void EnsureRagdollRig()
        {
            if (ragdollBones.Count > 0 && pelvisBone != null) return;

            ragdollBones.Clear();
            Transform visualModel = transform.Find("Visual_Model") ?? transform;

            // Search for existing humanoid bones
            Transform[] allBones = visualModel.GetComponentsInChildren<Transform>(true);
            pelvisBone = FindBoneMatching(allBones, "hips", "pelvis", "root");

            if (pelvisBone == null)
            {
                // Create synthetic articulated ragdoll hierarchy under Visual_Model
                GameObject rigRoot = new GameObject("Ragdoll_Rig");
                rigRoot.transform.SetParent(visualModel, false);
                rigRoot.transform.localPosition = Vector3.zero;

                pelvisBone = CreateBone(rigRoot.transform, "Ragdoll_Pelvis", new Vector3(0f, 0.95f, 0f), new Vector3(0.35f, 0.25f, 0.25f), 25f, null);

                Transform spine = CreateBone(pelvisBone, "Ragdoll_Spine", new Vector3(0f, 0.35f, 0f), new Vector3(0.32f, 0.35f, 0.22f), 20f, pelvisBone.GetComponent<Rigidbody>());
                Transform head = CreateBone(spine, "Ragdoll_Head", new Vector3(0f, 0.40f, 0f), new Vector3(0.22f, 0.25f, 0.22f), 5f, spine.GetComponent<Rigidbody>(), isSphere: true);

                Transform lArm = CreateBone(spine, "Ragdoll_LeftArm", new Vector3(-0.35f, 0.25f, 0f), new Vector3(0.12f, 0.45f, 0.12f), 4f, spine.GetComponent<Rigidbody>());
                Transform rArm = CreateBone(spine, "Ragdoll_RightArm", new Vector3(0.35f, 0.25f, 0f), new Vector3(0.12f, 0.45f, 0.12f), 4f, spine.GetComponent<Rigidbody>());

                Transform lLeg = CreateBone(pelvisBone, "Ragdoll_LeftLeg", new Vector3(-0.18f, -0.45f, 0f), new Vector3(0.14f, 0.55f, 0.14f), 8f, pelvisBone.GetComponent<Rigidbody>());
                Transform rLeg = CreateBone(pelvisBone, "Ragdoll_RightLeg", new Vector3(0.18f, -0.45f, 0f), new Vector3(0.14f, 0.55f, 0.14f), 8f, pelvisBone.GetComponent<Rigidbody>());
            }
            else
            {
                // Inspect bones and attach rigidbodies/colliders
                SetupBoneFromTransform(pelvisBone, "Pelvis", 25f, null);
                Transform spine = FindBoneMatching(allBones, "spine", "chest", "torso");
                if (spine != null) SetupBoneFromTransform(spine, "Spine", 20f, pelvisBone.GetComponent<Rigidbody>());

                Transform head = FindBoneMatching(allBones, "head");
                if (head != null) SetupBoneFromTransform(head, "Head", 5f, spine != null ? spine.GetComponent<Rigidbody>() : pelvisBone.GetComponent<Rigidbody>(), isSphere: true);

                Transform lArm = FindBoneMatching(allBones, "leftarm", "arm_l", "l_arm", "upperarm_l");
                if (lArm != null) SetupBoneFromTransform(lArm, "LeftArm", 4f, spine != null ? spine.GetComponent<Rigidbody>() : pelvisBone.GetComponent<Rigidbody>());

                Transform rArm = FindBoneMatching(allBones, "rightarm", "arm_r", "r_arm", "upperarm_r");
                if (rArm != null) SetupBoneFromTransform(rArm, "RightArm", 4f, spine != null ? spine.GetComponent<Rigidbody>() : pelvisBone.GetComponent<Rigidbody>());

                Transform lLeg = FindBoneMatching(allBones, "leftupleg", "thigh_l", "l_leg", "leg_l");
                if (lLeg != null) SetupBoneFromTransform(lLeg, "LeftLeg", 8f, pelvisBone.GetComponent<Rigidbody>());

                Transform rLeg = FindBoneMatching(allBones, "rightupleg", "thigh_r", "r_leg", "leg_r");
                if (rLeg != null) SetupBoneFromTransform(rLeg, "RightLeg", 8f, pelvisBone.GetComponent<Rigidbody>());
            }
        }

        private Transform FindBoneMatching(Transform[] bones, params string[] patterns)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                string n = bones[i].name.ToLower();
                for (int p = 0; p < patterns.Length; p++)
                {
                    if (n.Contains(patterns[p])) return bones[i];
                }
            }
            return null;
        }

        private Transform CreateBone(Transform parent, string name, Vector3 localPos, Vector3 size, float mass, Rigidbody parentRb, bool isSphere = false)
        {
            GameObject bObj = new GameObject(name);
            bObj.transform.SetParent(parent, false);
            bObj.transform.localPosition = localPos;

            Collider col;
            if (isSphere)
            {
                SphereCollider sc = bObj.AddComponent<SphereCollider>();
                sc.radius = size.x * 0.5f;
                col = sc;
            }
            else
            {
                BoxCollider bc = bObj.AddComponent<BoxCollider>();
                bc.size = size;
                col = bc;
            }

            Rigidbody rb = bObj.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            CharacterJoint joint = null;
            if (parentRb != null)
            {
                joint = bObj.AddComponent<CharacterJoint>();
                joint.connectedBody = parentRb;
                joint.enablePreprocessing = false;
            }

            ragdollBones.Add(new RagdollBoneInfo
            {
                boneName = name,
                boneTransform = bObj.transform,
                boneRigidbody = rb,
                boneCollider = col,
                joint = joint
            });

            return bObj.transform;
        }

        private void SetupBoneFromTransform(Transform bone, string name, float mass, Rigidbody parentRb, bool isSphere = false)
        {
            Collider col = bone.GetComponent<Collider>();
            if (col == null)
            {
                if (isSphere)
                {
                    SphereCollider sc = bone.gameObject.AddComponent<SphereCollider>();
                    sc.radius = 0.16f;
                    col = sc;
                }
                else
                {
                    CapsuleCollider cc = bone.gameObject.AddComponent<CapsuleCollider>();
                    cc.radius = 0.12f;
                    cc.height = 0.40f;
                    col = cc;
                }
            }

            Rigidbody rb = bone.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = bone.gameObject.AddComponent<Rigidbody>();
                rb.mass = mass;
                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            CharacterJoint joint = bone.GetComponent<CharacterJoint>();
            if (joint == null && parentRb != null)
            {
                joint = bone.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = parentRb;
                joint.enablePreprocessing = false;
            }

            ragdollBones.Add(new RagdollBoneInfo
            {
                boneName = name,
                boneTransform = bone,
                boneRigidbody = rb,
                boneCollider = col,
                joint = joint
            });
        }

        public void SetRagdollKinematic(bool kinematic)
        {
            for (int i = 0; i < ragdollBones.Count; i++)
            {
                RagdollBoneInfo info = ragdollBones[i];
                if (info.boneRigidbody != null)
                {
                    info.boneRigidbody.isKinematic = kinematic;
                    if (!kinematic)
                    {
                        info.boneRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    }
                }
                if (info.boneCollider != null)
                {
                    // While alive (kinematic = true), disable limb colliders to avoid character controller friction
                    // When ragdolled (kinematic = false), enable limb colliders
                    info.boneCollider.enabled = !kinematic;
                }
            }
        }

        public void ApplyDamageImpulse(string damageType, Vector3 hitDirection, Vector3 hitPoint, bool isHeadshot = false)
        {
            Vector3 force;
            string targetBone = "Pelvis";

            string dt = damageType != null ? damageType.ToLower() : "pistol";
            if (dt.Contains("shotgun"))
            {
                force = (hitDirection.normalized + Vector3.up * 0.25f) * shotgunImpulse;
                targetBone = "Spine";
            }
            else if (dt.Contains("axe") || dt.Contains("melee"))
            {
                force = (hitDirection.normalized + Vector3.up * 0.15f) * axeImpulse;
                targetBone = "Spine";
            }
            else if (dt.Contains("vehicle") || dt.Contains("ram"))
            {
                force = (hitDirection.normalized + Vector3.up * 0.45f) * vehicleImpulse;
                targetBone = "Pelvis";
            }
            else if (dt.Contains("blast") || dt.Contains("explosive"))
            {
                force = (hitDirection.normalized + Vector3.up * 0.5f) * blastImpulse;
                targetBone = "Pelvis";
            }
            else // Pistol / Default
            {
                float mag = isHeadshot ? pistolImpulse * 1.35f : pistolImpulse;
                force = (hitDirection.normalized + Vector3.up * 0.12f) * mag;
                targetBone = isHeadshot ? "Head" : "Spine";
            }

            TriggerRagdoll(force, hitPoint, targetBone);
        }

        public void TriggerRagdoll(Vector3 impulseForce, Vector3 hitPoint, string targetBoneName = "")
        {
            if (isRagdollActive) return;
            isRagdollActive = true;
            isFrozen = false;

            // 1. Disable Animator & NavMeshAgent
            if (anim != null) anim.enabled = false;
            if (agent != null)
            {
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                agent.enabled = false;
            }

            // 2. Disable root collider to prevent player blocking
            if (rootCollider != null) rootCollider.enabled = false;

            // 3. Activate Ragdoll Physics
            SetRagdollKinematic(false);

            // 4. Apply Directional Impulse
            Rigidbody targetRb = null;
            if (!string.IsNullOrEmpty(targetBoneName))
            {
                for (int i = 0; i < ragdollBones.Count; i++)
                {
                    if (ragdollBones[i].boneName.Equals(targetBoneName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetRb = ragdollBones[i].boneRigidbody;
                        break;
                    }
                }
            }

            if (targetRb == null && pelvisBone != null)
            {
                targetRb = pelvisBone.GetComponent<Rigidbody>();
            }

            if (targetRb != null)
            {
                Vector3 applyPoint = hitPoint != Vector3.zero ? hitPoint : targetRb.worldCenterOfMass;
                targetRb.AddForceAtPosition(impulseForce, applyPoint, ForceMode.Impulse);
            }

            // Also distribute slight impulse across spine/pelvis for natural tumbling
            for (int i = 0; i < ragdollBones.Count; i++)
            {
                if (ragdollBones[i].boneRigidbody != null && ragdollBones[i].boneRigidbody != targetRb)
                {
                    ragdollBones[i].boneRigidbody.AddForce(impulseForce * 0.35f, ForceMode.Impulse);
                }
            }

            // 5. Start Mobile 60 FPS Sleep Freeze Coroutine (3.5s)
            if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);
            freezeCoroutine = StartCoroutine(FreezeRagdollAfterDelay(freezeDelay));
        }

        private IEnumerator FreezeRagdollAfterDelay(float delay)
        {
            float elapsed = 0f;
            while (elapsed < delay)
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;

                // Early sleep check: if all bones have settled (< 0.08 m/s)
                if (elapsed >= 1.0f && AreAllBonesSettled())
                {
                    break;
                }
            }

            FreezeRagdoll();
        }

        private bool AreAllBonesSettled()
        {
            for (int i = 0; i < ragdollBones.Count; i++)
            {
                Rigidbody rb = ragdollBones[i].boneRigidbody;
                if (rb != null && !rb.isKinematic)
                {
                    if (rb.linearVelocity.sqrMagnitude > (sleepVelocityThreshold * sleepVelocityThreshold))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void FreezeRagdoll()
        {
            isFrozen = true;
            for (int i = 0; i < ragdollBones.Count; i++)
            {
                Rigidbody rb = ragdollBones[i].boneRigidbody;
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                }
                // Keep limb colliders active so corpse is targetable for dragging / body concealment
                if (ragdollBones[i].boneCollider != null)
                {
                    ragdollBones[i].boneCollider.enabled = true;
                }
            }

            // Synchronize root transform to pelvis position for BodyConcealmentSystem alignment
            if (pelvisBone != null)
            {
                Vector3 pelPos = pelvisBone.position;
                pelPos.y = Mathf.Max(pelPos.y, transform.position.y);
                transform.position = pelPos;
            }

            Debug.Log($"[{gameObject.name}] Ragdoll rigidbodies frozen to isKinematic = true (0 ms CPU cost).");
        }
    }
}
