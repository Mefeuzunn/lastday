using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.UI;
using ProjectSurvival.Narrative;
using ProjectSurvival.Interaction;

namespace ProjectSurvival.Survival
{
    [Serializable]
    public class RadioBroadcast
    {
        public float frequency;
        public string id;
        public string title;
        public string sender;
        public Vector3 worldPosition;
        [TextArea(3, 6)]
        public string transcript;
        public bool isStorySignal;
        public int associatedChapter;
        public string cacheId;
    }

    /// <summary>
    /// Active Phase 5: Interactive Tactical Radio Frequency Scanner.
    /// Manages frequency tuning (88.0 MHz to 108.0 MHz), dynamic signal-to-noise static calculation,
    /// audio chime locks, signal decoding, secret cache pin dispatch, and story objective synchronization.
    /// </summary>
    public class TacticalRadioScanner : MonoBehaviour
    {
        public static TacticalRadioScanner Instance { get; set; }

        public const float MIN_FREQUENCY = 88.0f;
        public const float MAX_FREQUENCY = 108.0f;
        public const float FREQ_STEP = 0.2f;
        public const float SIGNAL_DETECTION_WINDOW = 0.6f;
        public const float SIGNAL_LOCK_THRESHOLD = 0.05f;

        [Header("Receiver State")]
        [SerializeField] private float currentFrequency = 89.4f;
        [SerializeField] private bool isLockedOnSignal = false;
        [SerializeField] private float currentSignalStrength = 0f; // 0.0 to 1.0
        [SerializeField] private float currentStaticNoise = 1f;    // 0.0 to 1.0

        [Header("Persistence")]
        [SerializeField] private List<float> decodedFrequencies = new List<float>();

        public float CurrentFrequency => currentFrequency;
        public bool IsLockedOnSignal => isLockedOnSignal;
        public float SignalStrength => currentSignalStrength;
        public float StaticNoise => currentStaticNoise;
        public IReadOnlyList<float> DecodedFrequencies => decodedFrequencies;
        public RadioBroadcast ActiveBroadcast { get; private set; }

        public event Action<float> OnFrequencyChanged;
        public event Action<RadioBroadcast> OnSignalLocked;
        public event Action OnSignalLost;
        public event Action<RadioBroadcast> OnSignalDecoded;

        private readonly List<RadioBroadcast> allBroadcasts = new List<RadioBroadcast>();
        private float lastLockChimeTime = -10f;
        private float lastTunedFrequency = -1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeBroadcasts();
        }

        private void OnEnable()
        {
            if (allBroadcasts.Count == 0)
            {
                InitializeBroadcasts();
            }
        }

        public void InitializeBroadcasts()
        {
            allBroadcasts.Clear();

            // 1. Chapter 1 Story Signal (92.4 MHz) — Dr. Selin Kaya's distress loop from Zone Clinic
            allBroadcasts.Add(new RadioBroadcast
            {
                frequency = 92.4f,
                id = "signal_ch1_clinic",
                title = "Acil Yardım Çağrısı — Zone Klinik",
                sender = "Dr. Selin Kaya",
                worldPosition = new Vector3(24f, 0.5f, 15f),
                transcript = "Burası Dr. Selin Kaya... Merkez klinikteki karantina laboratuvarından yayın yapıyorum. Patojenin hücresel RNA şifresini izole etmeyi başardım ancak klinik kuşatma altında. Herhangi bir hayatta kalan bu yayını alıyorsa... kliniğe gelin ve araştırma terminalimdeki verileri kurtarın...",
                isStorySignal = true,
                associatedChapter = 1,
                cacheId = "poi_clinic_terminal"
            });

            // 2. Hidden Side Cache 1 — West Forest (95.2 MHz)
            allBroadcasts.Add(new RadioBroadcast
            {
                frequency = 95.2f,
                id = "cache_side_west_forest",
                title = "Korucu Acil Bırakma Koordinatları",
                sender = "Orman Korucu İstasyonu B-7",
                worldPosition = new Vector3(-260f, 0.45f, 75f),
                transcript = "Tüm devriyelere: Batı Ormanı gözetleme kulesinin güneybatı patikasındaki acil destek sandığı etkinleştirildi. İçerisinde 9mm mühimmat, antiviral aşı ve taktik hurda parçaları bulunmaktadır. Koordinatlar telsiz haritasına aktarıldı.",
                isStorySignal = false,
                associatedChapter = 0,
                cacheId = "cache_west_forest"
            });

            // 3. Chapter 2 Story Signal (98.6 MHz) — Military convoy black box beacon at East Rail Yard
            allBroadcasts.Add(new RadioBroadcast
            {
                frequency = 98.6f,
                id = "signal_ch2_railyard",
                title = "Askeri Konvoy Kara Kutu Sinyali",
                sender = "8. Mekanize Birlik Acil Vericisi",
                worldPosition = new Vector3(280f, 0.5f, 30f),
                transcript = "DİKKAT: 8. Mekanize Lojistik Konvoyu Doğu Tren Garı peronunda pusuya düşürüldü. Zırhlı kargo vagonundaki 'Biyolojik Karantina Şifre Diski' sağlam durumda. Yetkili personelin diski acilen geri alması gerekmektedir.",
                isStorySignal = true,
                associatedChapter = 2,
                cacheId = "poi_military_railcar"
            });

            // 4. Hidden Side Cache 2 — Southwest Suburb (101.4 MHz)
            allBroadcasts.Add(new RadioBroadcast
            {
                frequency = 101.4f,
                id = "cache_side_sw_suburb",
                title = "Batık Eczane Kaçakçı Deposu",
                sender = "Bilinmeyen Kaçakçı Kanalı",
                worldPosition = new Vector3(-210f, 0.3f, -220f),
                transcript = "Güneybatı su basmış eczanenin bodrumundaki su geçirmez acil zula hazır. Tıbbi malzemeler, steril bandajlar ve temiz su şişeleri kilit altında. Koordinatlar frekans üzerinden haritaya işaretlendi.",
                isStorySignal = false,
                associatedChapter = 0,
                cacheId = "cache_sw_pharmacy"
            });

            // 5. Chapter 3 Story Signal (104.2 MHz) — Subterranean bunker telemetry near Northwest Lake
            allBroadcasts.Add(new RadioBroadcast
            {
                frequency = 104.2f,
                id = "signal_ch3_bunker",
                title = "Gölge Protokolü — Yeraltı Sığınak Telemetrisi",
                sender = "Delta Araştırma Tesisi",
                worldPosition = new Vector3(-240f, 0.5f, 270f),
                transcript = "Protokol 44 devrede. Kuzeybatı göl kıyısındaki beton sığınak kapısı acil bypass kodunu bekliyor. Dr. Kaya'nın antiviral anahtarı iç sunucu odasındaki kasada kilitli. Giriş izni verildi.",
                isStorySignal = true,
                associatedChapter = 3,
                cacheId = "poi_nw_bunker"
            });

            // 6. Chapter 4 Finale Signal (107.8 MHz) — North Highway Overpass Relay
            allBroadcasts.Add(new RadioBroadcast
            {
                frequency = 107.8f,
                id = "signal_ch4_relay",
                title = "Kuzey Otoyolu Acil Röle Yayını",
                sender = "Merkezi İletişim Kulesi",
                worldPosition = new Vector3(0f, 7.5f, 220f),
                transcript = "Acil durum ulusal şebekesi son röle kulesine aktarıldı. Kuzey Otoyolu Viyadüğü üzerindeki ana verici devreye girdi. Manuel seçim bekleniyor: Bölgesel Tahliye mi yoksa Küresel Antiviral Yayını mı? Kararınızı bekliyoruz...",
                isStorySignal = true,
                associatedChapter = 4,
                cacheId = "poi_north_relay_tower"
            });
        }

        public void SetFrequency(float freq)
        {
            if (allBroadcasts.Count == 0) InitializeBroadcasts();
            float clamped = Mathf.Clamp(Mathf.Round(freq * 10f) / 10f, MIN_FREQUENCY, MAX_FREQUENCY);
            if (Mathf.Approximately(clamped, currentFrequency)) return;

            currentFrequency = clamped;
            EvaluateTuning(false);
            OnFrequencyChanged?.Invoke(currentFrequency);
        }

        public void StepFrequency(float step)
        {
            SetFrequency(currentFrequency + step);
        }

        private void EvaluateTuning(bool instant)
        {
            RadioBroadcast closest = null;
            float minDiff = float.MaxValue;

            for (int i = 0; i < allBroadcasts.Count; i++)
            {
                float diff = Mathf.Abs(currentFrequency - allBroadcasts[i].frequency);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = allBroadcasts[i];
                }
            }

            if (minDiff <= SIGNAL_DETECTION_WINDOW && closest != null)
            {
                currentSignalStrength = Mathf.Clamp01(1f - (minDiff / SIGNAL_DETECTION_WINDOW));
                currentStaticNoise = Mathf.Clamp01(minDiff / SIGNAL_DETECTION_WINDOW);

                if (minDiff <= SIGNAL_LOCK_THRESHOLD)
                {
                    bool wasLocked = isLockedOnSignal;
                    isLockedOnSignal = true;
                    ActiveBroadcast = closest;

                    if (!wasLocked || Mathf.Abs(lastTunedFrequency - closest.frequency) > 0.05f)
                    {
                        lastTunedFrequency = closest.frequency;
                        if (Time.time - lastLockChimeTime > 0.4f)
                        {
                            lastLockChimeTime = Time.time;
                            ProceduralAudioManager.Instance?.PlayRadioFrequencyLockChime();
                            MobileHapticManager.TriggerLight();
                        }
                        OnSignalLocked?.Invoke(closest);
                    }
                }
                else
                {
                    if (isLockedOnSignal)
                    {
                        isLockedOnSignal = false;
                        ActiveBroadcast = null;
                        OnSignalLost?.Invoke();
                    }
                }
            }
            else
            {
                currentSignalStrength = 0f;
                currentStaticNoise = 1f;
                if (isLockedOnSignal)
                {
                    isLockedOnSignal = false;
                    ActiveBroadcast = null;
                    OnSignalLost?.Invoke();
                }
            }
        }

        public bool IsFrequencyDecoded(float freq)
        {
            for (int i = 0; i < decodedFrequencies.Count; i++)
            {
                if (Mathf.Abs(decodedFrequencies[i] - freq) < 0.05f) return true;
            }
            return false;
        }

        public bool TryDecodeSignal(out RadioBroadcast broadcast)
        {
            broadcast = null;
            if (!isLockedOnSignal || ActiveBroadcast == null) return false;

            broadcast = ActiveBroadcast;
            if (!IsFrequencyDecoded(broadcast.frequency))
            {
                decodedFrequencies.Add(broadcast.frequency);
                ProceduralAudioManager.Instance?.PlayRadioDecodeSuccess();
                MobileHapticManager.TriggerMedium();

                // If side cache, reveal cache in world and pin on tactical/minimap
                if (!broadcast.isStorySignal && !string.IsNullOrEmpty(broadcast.cacheId))
                {
                    RevealSecretCache(broadcast);
                }

                // Notify Story Director
                MainStoryDirector.Instance?.OnFrequencyDecoded(broadcast.frequency);
                OnSignalDecoded?.Invoke(broadcast);
            }

            return true;
        }

        private void RevealSecretCache(RadioBroadcast broadcast)
        {
            Vector2 mapPos = new Vector2(broadcast.worldPosition.x, broadcast.worldPosition.z);
            FullTacticalMapUI.Instance?.RegisterSecretCacheMarker(broadcast.cacheId, broadcast.title, mapPos);
            MinimapHUDWidget.Instance?.RegisterDynamicPoi(broadcast.cacheId, mapPos, new Color(0.2f, 0.98f, 0.5f, 0.95f));

            // Enable physical cache in world if present
            SecretCacheInteractable[] caches = FindObjectsByType<SecretCacheInteractable>(FindObjectsSortMode.None);
            for (int i = 0; i < caches.Length; i++)
            {
                if (caches[i].CacheId == broadcast.cacheId)
                {
                    caches[i].RevealCache();
                    break;
                }
            }

            Debug.Log($"[TacticalRadioScanner] Secret cache revealed: {broadcast.title} at {broadcast.worldPosition}");
        }

        public RadioBroadcast GetBroadcastByFrequency(float freq)
        {
            for (int i = 0; i < allBroadcasts.Count; i++)
            {
                if (Mathf.Abs(allBroadcasts[i].frequency - freq) < 0.05f)
                {
                    return allBroadcasts[i];
                }
            }
            return null;
        }

        public void RestoreDecodedFrequencies(List<float> freqs)
        {
            decodedFrequencies.Clear();
            if (freqs != null)
            {
                for (int i = 0; i < freqs.Count; i++)
                {
                    decodedFrequencies.Add(freqs[i]);
                    // Re-pin revealed side caches on map
                    RadioBroadcast b = GetBroadcastByFrequency(freqs[i]);
                    if (b != null && !b.isStorySignal)
                    {
                        RevealSecretCache(b);
                    }
                }
            }
            EvaluateTuning(true);
        }
    }
}
