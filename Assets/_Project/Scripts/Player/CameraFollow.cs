using UnityEngine;

namespace ProjectSurvival.Player
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private string playerTag = "Player";

        [Header("Isometric Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 13f, -10f);
        [SerializeField] private float pitchAngle = 52f;
        [SerializeField] private float yawAngle = 0f;
        [SerializeField] private float smoothTime = 0.18f;

        [Header("Look-Ahead Settings")]
        [SerializeField] private float lookAheadDistance = 2.4f;
        [SerializeField] private float lookAheadSmoothTime = 0.25f;

        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 currentLookAhead = Vector3.zero;
        private Vector3 lookAheadVelocity = Vector3.zero;
        private Vector3 lastTargetPos = Vector3.zero;

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (target != null)
            {
                lastTargetPos = target.position;
                transform.position = target.position + offset;
            }

            transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                lastTargetPos = target.position;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null)
                {
                    target = player.transform;
                    lastTargetPos = target.position;
                }
                return;
            }

            // Calculate movement direction for look-ahead
            Vector3 targetDelta = (target.position - lastTargetPos);
            lastTargetPos = target.position;

            Vector3 moveDirection = new Vector3(targetDelta.x, 0f, targetDelta.z);
            Vector3 targetLookAhead = Vector3.zero;
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                targetLookAhead = moveDirection.normalized * lookAheadDistance;
            }

            currentLookAhead = Vector3.SmoothDamp(currentLookAhead, targetLookAhead, ref lookAheadVelocity, lookAheadSmoothTime);

            Vector3 desiredPosition = target.position + offset + currentLookAhead;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
            transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        }
    }
}
