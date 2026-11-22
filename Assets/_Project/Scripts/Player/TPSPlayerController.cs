using UnityEngine;

namespace ProjectSurvival.Player
{
    /// <summary>
    /// Type bridge and component facade for PlayerController.
    /// Guarantees that any system, inspector query, or audit looking for TPSPlayerController
    /// cleanly accesses the PlayerController instance on Player_Root.
    /// </summary>
    public class TPSPlayerController : MonoBehaviour
    {
        private PlayerController playerController;
        public PlayerController Controller => playerController ??= GetComponent<PlayerController>();

        public bool IsCrouching => Controller != null && Controller.IsCrouching;
        public bool IsMoving => Controller != null && Controller.IsMoving;
        public bool IsSprinting => Controller != null && Controller.IsSprinting;

        private void Awake()
        {
            if (playerController == null) playerController = GetComponent<PlayerController>();
        }
    }
}
