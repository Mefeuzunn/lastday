using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Subway Service Tunnel Gate.
    /// Situated on eastern and western tunnel walls of the Sunken Metro Station in Sector_0_-1_South.
    /// Functions as an interactive gateway and anchor node for Phase 4 Metro Fast-Travel Transit.
    /// </summary>
    public class SubwayServiceTunnelGate : WorldInteractableObject
    {
        [Header("Metro Transit Anchor")]
        [SerializeField] private string stationId = "south_metro_hub";
        [SerializeField] private string destinationSector = "Sector_0_0_Center";
        [SerializeField] private bool isTransitUnlocked = false;

        public string StationId => stationId;
        public string DestinationSector => destinationSector;
        public bool IsTransitUnlocked => isTransitUnlocked;

        public override string ObjectSubtitle => isTransitUnlocked
            ? "TRANSİT HATTI HAZIR (HIZLI SEYAHAT)"
            : "KİLİTLİ — BÖLGESEL TRANSİT HATTI (FAZ 4 HIZLI SEYAHAT)";

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Metro Servis Tüneli Kapısı";
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            verbs.Add(new InteractionVerb(
                "inspect",
                "Tüneli İncele",
                0.5f,
                null,
                null,
                0f
            ));

            if (isTransitUnlocked)
            {
                verbs.Add(new InteractionVerb(
                    "fast_travel",
                    $"Transiti Kullan ({destinationSector})",
                    1.5f,
                    null,
                    null,
                    0f
                ));
            }
            else
            {
                verbs.Add(new InteractionVerb(
                    "locked_gate",
                    "Kilitli Servis Kapısı (Zorla)",
                    0.8f,
                    null,
                    null,
                    8f
                ));
            }

            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            if (verbId == "inspect")
            {
                GameEvents.TriggerObjectiveToastRequested("Tünel rayları kuzeydeki karantina merkezine uzanıyor. Transit kapısı şu an kilitli.", 3.0f);
            }
            else if (verbId == "locked_gate")
            {
                ProceduralAudioManager.Instance?.PlaySoftError();
                GameEvents.TriggerObjectiveToastRequested("Kapı mekanizması kilitli. Faz 4 Hızlı Seyahat ağı henüz devrede değil.", 3.0f);
            }
            else if (verbId == "fast_travel" && isTransitUnlocked)
            {
                if (ProjectSurvival.OpenWorld.MetroFastTravelSystem.Instance != null)
                {
                    ProjectSurvival.OpenWorld.MetroFastTravelSystem.Instance.ExecuteFastTravel("safehouse_tunnel");
                }
                else
                {
                    GameEvents.TriggerObjectiveToastRequested("Merkez metro istasyonuna aktarılıyor...", 2.5f);
                }
            }
        }

        public void UnlockTransitGate()
        {
            isTransitUnlocked = true;
            Debug.Log($"[SubwayServiceTunnelGate] Transit gate '{stationId}' unlocked for fast travel.");
        }
    }
}
