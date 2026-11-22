using UnityEngine;

namespace ProjectSurvival.Player
{
    /// <summary>
    /// Phase 2 (Step 2A): Unified FPS / TPS Camera Controller Architecture.
    /// Provides alias interface to ThirdPersonCamera ensuring strict single-authority
    /// camera ownership in LateUpdate, smooth 0.22s perspective transitions,
    /// wall collision avoidance, and clean viewmodel masking.
    /// </summary>
    public class TPSCameraController : ThirdPersonCamera
    {
    }
}
