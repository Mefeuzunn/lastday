using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Player
{
    public class StealthSystem : MonoBehaviour
    {
        [Header("Stealth Parameters")]
        [SerializeField] private bool isCrouched = false;
        [SerializeField] private float crouchVisibilityMultiplier = 0.5f;
        private bool isHidden = false;

        public bool IsCrouched => isCrouched;
        public bool IsHidden => isHidden;
        public float VisibilityMultiplier => isCrouched ? crouchVisibilityMultiplier : 1f;

        private void OnEnable()
        {
            GameEvents.OnPlayerHidingStateChanged += HandleHidingChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHidingStateChanged -= HandleHidingChanged;
        }

        private void HandleHidingChanged(bool hidden)
        {
            isHidden = hidden;
        }

        public void SetHidden(bool hidden)
        {
            isHidden = hidden;
        }

        public void ToggleCrouch()
        {
            SetCrouch(!isCrouched);
        }

        public void SetCrouch(bool crouch)
        {
            if (isCrouched == crouch) return;

            isCrouched = crouch;
            GameEvents.TriggerStealthStateChanged(isCrouched);
        }
    }
}
