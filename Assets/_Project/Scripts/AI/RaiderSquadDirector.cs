using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.AI
{
    [Serializable]
    public class RaiderSquadGroup
    {
        public string squadId;
        public string squadDisplayName;
        public string targetSectorId;
        public Vector2Int sectorGridCoord;
        public Vector3 squadAnchorPosition;
        public List<HumanRaiderAI> squadMembers = new List<HumanRaiderAI>();
    }

    /// <summary>
    /// Phase 2 (Step 2A): Raider Squad Director.
    /// Manages the 4 distinct 3-person armed raider squads (12 total raiders) deployed in outer sectors:
    /// - East Rail Yard Ambush Crew (Sector_1_0_East)
    /// - North Highway Toll Blockade (Sector_0_1_North)
    /// - Northeast Harbor Smugglers (Sector_1_1_NorthEast)
    /// - Southeast Substation Scavengers (Sector_1_-1_SouthEast)
    /// Enforces zero raiders in Sector_0_0_Center to preserve the starter safezone.
    /// Handles squad-level alert propagation (22m), fallen squadmate reactions, and sector streamer binding.
    /// </summary>
    public class RaiderSquadDirector : MonoBehaviour
    {
        public static RaiderSquadDirector Instance { get; private set; }

        [Header("Squad Configurations (4 Outer Sectors)")]
        [SerializeField] private List<RaiderSquadGroup> squads = new List<RaiderSquadGroup>();

        public IReadOnlyList<RaiderSquadGroup> Squads => squads;
        public int TotalRaiderCount => HumanRaiderAI.ActiveRaiders.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AutoDiscoverSquadsIfEmpty();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void AutoDiscoverSquadsIfEmpty()
        {
            if (squads != null && squads.Count >= 4) return;

            squads = new List<RaiderSquadGroup>
            {
                new RaiderSquadGroup
                {
                    squadId = "Squad_RailYard_East",
                    squadDisplayName = "Doğu Tren Garı Yağmacı Pusu Timi",
                    targetSectorId = "Sector_1_0_East",
                    sectorGridCoord = new Vector2Int(1, 0),
                    squadAnchorPosition = new Vector3(220f, 0f, 15f)
                },
                new RaiderSquadGroup
                {
                    squadId = "Squad_Highway_North",
                    squadDisplayName = "Kuzey Otoyol Gişe Barikatı",
                    targetSectorId = "Sector_0_1_North",
                    sectorGridCoord = new Vector2Int(0, 1),
                    squadAnchorPosition = new Vector3(15f, 0f, 225f)
                },
                new RaiderSquadGroup
                {
                    squadId = "Squad_Harbor_NorthEast",
                    squadDisplayName = "Kuzeydoğu Liman Kaçakçıları",
                    targetSectorId = "Sector_1_1_NorthEast",
                    sectorGridCoord = new Vector2Int(1, 1),
                    squadAnchorPosition = new Vector3(230f, 0f, 230f)
                },
                new RaiderSquadGroup
                {
                    squadId = "Squad_Substation_SouthEast",
                    squadDisplayName = "Güneydoğu Trafo Yağmacıları",
                    targetSectorId = "Sector_1_-1_SouthEast",
                    sectorGridCoord = new Vector2Int(1, -1),
                    squadAnchorPosition = new Vector3(225f, 0f, -225f)
                }
            };

            // Bind existing raiders in scene to their squads
            var allRaiders = FindObjectsByType<HumanRaiderAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int r = 0; r < allRaiders.Length; r++)
            {
                var raider = allRaiders[r];
                for (int s = 0; s < squads.Count; s++)
                {
                    if (squads[s].squadId == raider.SquadId)
                    {
                        if (!squads[s].squadMembers.Contains(raider))
                        {
                            squads[s].squadMembers.Add(raider);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Alerts all living squad members within alertRadius (default 22m) when one spots a threat.
        /// </summary>
        public void AlertSquad(string squadId, Transform threat, HumanRaiderAI sourceRaider, float alertRadius = 22.0f)
        {
            if (threat == null || sourceRaider == null) return;

            Vector3 sourcePos = sourceRaider.transform.position;

            for (int i = 0; i < HumanRaiderAI.ActiveRaiders.Count; i++)
            {
                var raider = HumanRaiderAI.ActiveRaiders[i];
                if (raider == null || raider == sourceRaider || raider.IsDead) continue;

                if (raider.SquadId == squadId)
                {
                    float dist = Vector3.Distance(sourcePos, raider.transform.position);
                    if (dist <= alertRadius)
                    {
                        if (raider.CurrentState == RaiderState.Patrol)
                        {
                            raider.AlertRaider(threat, "Hedef bildirildi! Ateş pozisyonuna!");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Informs surviving squadmates when a member falls, putting nearby members on high alert.
        /// </summary>
        public void NotifySquadmateDown(string squadId, Vector3 deathPos)
        {
            for (int i = 0; i < HumanRaiderAI.ActiveRaiders.Count; i++)
            {
                var raider = HumanRaiderAI.ActiveRaiders[i];
                if (raider == null || raider.IsDead) continue;

                if (raider.SquadId == squadId)
                {
                    float dist = Vector3.Distance(deathPos, raider.transform.position);
                    if (dist <= 26.0f)
                    {
                        GameObject p = GameObject.FindWithTag("Player");
                        Transform player = p != null ? p.transform : null;
                        raider.AlertRaider(player, "Biri yoldaşımızı vurdu! İntikamını alın!");
                    }
                }
            }
        }

        public RaiderSquadGroup GetSquad(string squadId)
        {
            for (int i = 0; i < squads.Count; i++)
            {
                if (squads[i].squadId == squadId) return squads[i];
            }
            return null;
        }
    }
}
