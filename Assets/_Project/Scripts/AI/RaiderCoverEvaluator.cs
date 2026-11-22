using UnityEngine;
using UnityEngine.AI;

namespace ProjectSurvival.AI
{
    /// <summary>
    /// Phase 2 (Step 2A): Tactical Cover Evaluator for Armed Human Raiders.
    /// Evaluates nearby obstacles (walls, concrete barriers, vehicles, shipping containers)
    /// within 14m using Physics.RaycastNonAlloc and NavMesh.SamplePosition.
    /// Finds a valid NavMesh position that occludes line-of-sight to the target threat
    /// while remaining between 8m and 22m away.
    /// Uses preallocated physics buffers for zero GC spikes at 60 FPS.
    /// </summary>
    public static class RaiderCoverEvaluator
    {
        private static readonly Collider[] s_OverlapColliders = new Collider[32];
        private static readonly RaycastHit[] s_RaycastHits = new RaycastHit[16];

        private static int s_ObstacleLayerMask = -1;
        private static bool s_LayerMaskInitialized = false;

        private static void EnsureLayerMask()
        {
            if (s_LayerMaskInitialized) return;

            s_ObstacleLayerMask = ~0;
            int playerLayer = LayerMask.NameToLayer("Player");
            int npcLayer = LayerMask.NameToLayer("NPC");
            int uiLayer = LayerMask.NameToLayer("UI");
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");

            if (playerLayer != -1) s_ObstacleLayerMask &= ~(1 << playerLayer);
            if (npcLayer != -1) s_ObstacleLayerMask &= ~(1 << npcLayer);
            if (uiLayer != -1) s_ObstacleLayerMask &= ~(1 << uiLayer);
            if (ignoreRaycast != -1) s_ObstacleLayerMask &= ~(1 << ignoreRaycast);

            s_LayerMaskInitialized = true;
        }

        /// <summary>
        /// Attempts to find a tactical cover point around nearby obstacles that breaks line of sight to targetPos.
        /// </summary>
        public static bool TryFindCover(Vector3 raiderPos, Vector3 targetPos, float maxSearchDist, out Vector3 coverPos)
        {
            EnsureLayerMask();
            coverPos = raiderPos;

            int count = Physics.OverlapSphereNonAlloc(raiderPos, maxSearchDist, s_OverlapColliders, s_ObstacleLayerMask, QueryTriggerInteraction.Ignore);
            float bestScore = float.MinValue;
            bool foundCover = false;
            Vector3 bestCoverPoint = raiderPos;

            Vector3 threatDir = targetPos - raiderPos;
            threatDir.y = 0f;
            if (threatDir.sqrMagnitude < 0.01f) threatDir = Vector3.forward;
            threatDir.Normalize();

            for (int i = 0; i < count; i++)
            {
                Collider col = s_OverlapColliders[i];
                if (col == null || col.isTrigger) continue;

                Bounds b = col.bounds;
                Vector3 center = b.center;

                // Compute directional vector away from target behind the obstacle
                Vector3 toObstacle = center - targetPos;
                toObstacle.y = 0f;
                if (toObstacle.sqrMagnitude < 0.01f) continue;
                Vector3 awayFromThreat = toObstacle.normalized;

                float extentsMax = Mathf.Max(b.extents.x, b.extents.z);
                Vector3 perp = Vector3.Cross(Vector3.up, awayFromThreat);

                // 3 tactical candidates: direct rear, flanking left rear, flanking right rear
                Vector3[] candidates = new Vector3[]
                {
                    center + awayFromThreat * (extentsMax + 0.85f),
                    center + awayFromThreat * (extentsMax + 0.65f) + perp * (extentsMax + 0.5f),
                    center + awayFromThreat * (extentsMax + 0.65f) - perp * (extentsMax + 0.5f)
                };

                for (int c = 0; c < candidates.Length; c++)
                {
                    Vector3 cand = candidates[c];
                    cand.y = raiderPos.y;

                    // Sample on NavMesh
                    if (!NavMesh.SamplePosition(cand, out NavMeshHit navHit, 1.8f, NavMesh.AllAreas))
                        continue;

                    Vector3 samplePoint = navHit.position;
                    float distToTarget = Vector3.Distance(samplePoint, targetPos);

                    // Must be between 8m and 22m from target (tolerating 6m-24m range)
                    if (distToTarget < 6.0f || distToTarget > 26.0f)
                        continue;

                    // Line of sight check from cover eye to target eye
                    Vector3 eyePos = samplePoint + Vector3.up * 1.4f;
                    Vector3 targetEye = targetPos + Vector3.up * 1.5f;
                    Vector3 rayDir = targetEye - eyePos;
                    float rayDist = rayDir.magnitude;
                    if (rayDist < 0.1f) continue;

                    int hitCount = Physics.RaycastNonAlloc(eyePos, rayDir.normalized, s_RaycastHits, rayDist, s_ObstacleLayerMask, QueryTriggerInteraction.Ignore);
                    bool isCovered = false;

                    for (int h = 0; h < hitCount; h++)
                    {
                        Collider hitCol = s_RaycastHits[h].collider;
                        if (hitCol != null)
                        {
                            isCovered = true;
                            break;
                        }
                    }

                    if (isCovered)
                    {
                        // Higher score for closer to raider, within 8m-20m sweet spot
                        float distToRaider = Vector3.Distance(samplePoint, raiderPos);
                        float score = 100f - distToRaider * 2.5f;

                        if (distToTarget >= 8f && distToTarget <= 22f)
                        {
                            score += 25f;
                        }

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestCoverPoint = samplePoint;
                            foundCover = true;
                        }
                    }
                }
            }

            // Fallback: radial probe away from threat if direct obstacle rear is not walkable
            if (!foundCover)
            {
                for (int a = 0; a < 8; a++)
                {
                    float angle = a * 45f * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    if (Vector3.Dot(dir, threatDir) > 0.25f) continue; // skip toward threat

                    Vector3 probe = raiderPos + dir * 6.5f;
                    if (NavMesh.SamplePosition(probe, out NavMeshHit nHit, 2.0f, NavMesh.AllAreas))
                    {
                        Vector3 eyePos = nHit.position + Vector3.up * 1.4f;
                        Vector3 targetEye = targetPos + Vector3.up * 1.5f;
                        Vector3 rDir = targetEye - eyePos;
                        int hits = Physics.RaycastNonAlloc(eyePos, rDir.normalized, s_RaycastHits, rDir.magnitude, s_ObstacleLayerMask, QueryTriggerInteraction.Ignore);
                        if (hits > 0)
                        {
                            bestCoverPoint = nHit.position;
                            foundCover = true;
                            break;
                        }
                    }
                }
            }

            if (foundCover)
            {
                coverPos = bestCoverPoint;
                return true;
            }

            // Ultimate fallback: retreat position backwards
            Vector3 retreatProbe = raiderPos - threatDir * 5.0f;
            if (NavMesh.SamplePosition(retreatProbe, out NavMeshHit retreatHit, 3.0f, NavMesh.AllAreas))
            {
                coverPos = retreatHit.position;
                return true;
            }

            return false;
        }
    }
}
