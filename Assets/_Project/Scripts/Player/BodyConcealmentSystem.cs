using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using ProjectSurvival.AI;
using ProjectSurvival.Core;
using ProjectSurvival.Environment;
using ProjectSurvival.Progression;
using ProjectSurvival.Survival;

namespace ProjectSurvival.Player
{
    /// <summary>
    /// Active Phase 4: Corpse Dragging & Foliage/Dumpster/Interior Concealment System.
    /// Manages physical body lifetime (45s), unhidden corpse detection by patrol zombies (< 11m),
    /// tactile dragging behind player (1.15m height-clamped, -35% speed),
    /// and concealment detection in bushes/dumpsters/interiors (+15 Stealth XP).
    /// </summary>
    public class BodyConcealmentSystem : MonoBehaviour
    {
        private static BodyConcealmentSystem _instance;
        public static BodyConcealmentSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<BodyConcealmentSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("BodyConcealmentSystem");
                        _instance = go.AddComponent<BodyConcealmentSystem>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (_instance == null)
            {
                var inst = Instance;
            }
        }

        [System.Serializable]
        public class CorpseRecord
        {
            public GameObject corpseObject;
            public Transform corpseTransform;
            public bool isConcealed = false;
            public float decayTimer = 45.0f;
            public bool isBeingDragged = false;
            public Vector3 lastGroundedPos;
        }

        [Header("Tuning Settings")]
        [SerializeField] private float corpseDetectionRange = 11.0f;
        [SerializeField] private float dragInteractionDistance = 2.2f;
        [SerializeField] private float dragHoldDuration = 0.35f;
        [SerializeField] private float dragFollowDistance = 1.15f;
        [SerializeField] private float concealmentRadius = 2.6f;

        [Header("State")]
        [SerializeField] private bool isDraggingCorpse = false;
        private CorpseRecord currentlyDraggedCorpse = null;
        private readonly List<CorpseRecord> activeCorpses = new List<CorpseRecord>();

        private Transform playerTransform;
        private Camera mainCamera;
        private float holdTimer = 0f;
        private float patrolScanTimer = 0f;

        // Contextual Drag / Drop UI
        private GameObject dragDropHUDPrompt;
        private Text dragDropText;

        public bool IsDraggingCorpse => isDraggingCorpse;
        public CorpseRecord CurrentlyDraggedCorpse => currentlyDraggedCorpse;
        public IReadOnlyList<CorpseRecord> ActiveCorpses => activeCorpses;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            LocatePlayer();
            CreateDragDropPromptUI();
        }

        private void LocatePlayer()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Root");
                if (player != null) playerTransform = player.transform;
            }
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        public void RegisterCorpse(GameObject zombieObject)
        {
            if (zombieObject == null) return;

            // Check if already registered
            for (int i = 0; i < activeCorpses.Count; i++)
            {
                if (activeCorpses[i].corpseObject == zombieObject) return;
            }

            CorpseRecord record = new CorpseRecord
            {
                corpseObject = zombieObject,
                corpseTransform = zombieObject.transform,
                isConcealed = false,
                decayTimer = 45.0f,
                isBeingDragged = false,
                lastGroundedPos = zombieObject.transform.position
            };

            activeCorpses.Add(record);
            Debug.Log($"[BodyConcealmentSystem] Registered corpse '{zombieObject.name}'. Remaining on ground for 45s.");
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            LocatePlayer();
            UpdateCorpseDecay();

            if (isDraggingCorpse)
            {
                UpdateCorpseDraggingMotion();
                HandleDropInput();
            }
            else
            {
                HandleDragInteraction();
                UpdatePatrolZombieCorpseScan();
            }
        }

        private void UpdateCorpseDecay()
        {
            for (int i = activeCorpses.Count - 1; i >= 0; i--)
            {
                CorpseRecord rec = activeCorpses[i];
                if (rec == null || rec.corpseObject == null)
                {
                    activeCorpses.RemoveAt(i);
                    continue;
                }

                // If currently being dragged, pause decay
                if (rec.isBeingDragged) continue;

                rec.decayTimer -= Time.deltaTime;
                if (rec.decayTimer <= 0f)
                {
                    // Cleanup corpse
                    Destroy(rec.corpseObject);
                    activeCorpses.RemoveAt(i);
                }
            }
        }

        private void UpdateCorpseDraggingMotion()
        {
            if (currentlyDraggedCorpse == null || currentlyDraggedCorpse.corpseTransform == null || playerTransform == null)
            {
                DropCorpse();
                return;
            }

            // Target position: 1.15m directly behind player
            Vector3 targetPos = playerTransform.position - (playerTransform.forward * dragFollowDistance);

            // Ground height clamping with Raycast so corpse never clips through floors
            int groundMask = ~((1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("UI")));
            int pLayer = LayerMask.NameToLayer("Player");
            if (pLayer != -1) groundMask &= ~(1 << pLayer);

            Vector3 rayStart = targetPos + Vector3.up * 1.5f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 4.0f, groundMask))
            {
                targetPos.y = hit.point.y + 0.12f;
            }
            else if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            {
                targetPos.y = navHit.position.y + 0.12f;
            }

            Transform corpseTrans = currentlyDraggedCorpse.corpseTransform;
            corpseTrans.position = Vector3.Lerp(corpseTrans.position, targetPos, Time.deltaTime * 12f);

            // Align rotation with drag direction
            Vector3 dragDir = (playerTransform.position - corpseTrans.position).normalized;
            dragDir.y = 0;
            if (dragDir.sqrMagnitude > 0.01f)
            {
                corpseTrans.rotation = Quaternion.Slerp(corpseTrans.rotation, Quaternion.LookRotation(dragDir), Time.deltaTime * 8f);
            }

            currentlyDraggedCorpse.lastGroundedPos = corpseTrans.position;
        }

        private void HandleDragInteraction()
        {
            if (playerTransform == null) return;

            CorpseRecord nearest = GetNearestInteractableCorpse(dragInteractionDistance);
            if (nearest != null)
            {
                ShowPrompt(true, "[E] CESEDİ SÜRÜKLE (Basılı tut)");

                // Hold interaction check (E key or Hold on mobile)
                if (Input.GetKey(KeyCode.E))
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= dragHoldDuration)
                    {
                        holdTimer = 0f;
                        StartDraggingCorpse(nearest);
                    }
                }
                else
                {
                    holdTimer = 0f;
                }
            }
            else
            {
                ShowPrompt(false, string.Empty);
                holdTimer = 0f;
            }
        }

        private void HandleDropInput()
        {
            ShowPrompt(true, "[E] CESEDİ BIRAK");

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                DropCorpse();
            }
        }

        public void StartDraggingCorpse(CorpseRecord corpse)
        {
            if (corpse == null || corpse.corpseObject == null) return;

            isDraggingCorpse = true;
            currentlyDraggedCorpse = corpse;
            corpse.isBeingDragged = true;

            // Holster weapon immediately
            WeaponSystem.Instance?.HolsterWeapon();

            ProceduralAudioManager.Instance?.PlayBandageWrap();
            MobileHapticManager.TriggerLight();
            GameEvents.TriggerObjectiveToastRequested("[CESET SÜRÜKLENİYOR] Çalılıklara, çöp konteynerine veya bina içine gizleyin (-%35 Hız).", 3.5f);
        }

        public void DropCorpse()
        {
            if (!isDraggingCorpse || currentlyDraggedCorpse == null) return;

            CorpseRecord corpse = currentlyDraggedCorpse;
            corpse.isBeingDragged = false;
            isDraggingCorpse = false;
            currentlyDraggedCorpse = null;

            Vector3 dropPos = (corpse.corpseTransform != null) ? corpse.corpseTransform.position : playerTransform.position;

            // Concealment Zone Check: Foliage, Dumpsters, or Indoors
            bool isConcealed = CheckConcealmentZone(dropPos);
            if (isConcealed && !corpse.isConcealed)
            {
                corpse.isConcealed = true;
                ProceduralAudioManager.Instance?.PlayBandageWrap();
                MobileHapticManager.TriggerLight();

                // Award +15 Stealth XP
                SurvivorProgressionManager.Instance?.AddXP(15, "Ceset Gizlendi (+15 XP)");
                GameEvents.TriggerObjectiveToastRequested("[GİZLENDİ] Ceset başarıyla gizlendi! (+15 Gizlilik XP)", 3.5f);
            }
            else
            {
                ProceduralAudioManager.Instance?.PlayItemPickup();
                GameEvents.TriggerObjectiveToastRequested("[CESET BIRAKILDI] Devriye zombiler açıkta kalan cesetleri fark edebilir!", 3.0f);
            }

            ShowPrompt(false, string.Empty);
        }

        public bool CheckConcealmentZone(Vector3 pos)
        {
            // 1. Closed Building Interior
            if (DynamicWeatherManager.Instance != null && DynamicWeatherManager.Instance.IsIndoors)
            {
                return true;
            }

            // 2. Foliage Bushes or Dumpsters
            var spots = FindObjectsByType<HidingSpot>(FindObjectsSortMode.None);
            for (int i = 0; i < spots.Length; i++)
            {
                if (spots[i] == null) continue;
                if (spots[i].SpotType == HidingSpotType.FoliageBush || spots[i].SpotType == HidingSpotType.Dumpster)
                {
                    if ((spots[i].transform.position - pos).sqrMagnitude <= (concealmentRadius * concealmentRadius))
                    {
                        return true;
                    }
                }
            }

            // 3. Overlap with foliage or dumpster colliders
            Collider[] hits = Physics.OverlapSphere(pos + Vector3.up * 0.4f, 1.2f);
            for (int i = 0; i < hits.Length; i++)
            {
                string name = hits[i].name.ToLower();
                if (name.Contains("bush") || name.Contains("dumpster") || name.Contains("trash") || name.Contains("container"))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdatePatrolZombieCorpseScan()
        {
            patrolScanTimer += Time.deltaTime;
            if (patrolScanTimer < 0.4f) return;
            patrolScanTimer = 0f;

            if (activeCorpses.Count == 0) return;
            var activeZombies = ZombieAI.ActiveZombies;
            if (activeZombies == null || activeZombies.Count == 0) return;

            int obstacleMask = ~((1 << LayerMask.NameToLayer("Ignore Raycast")) | (1 << LayerMask.NameToLayer("UI")));
            int pLayer = LayerMask.NameToLayer("Player");
            if (pLayer != -1) obstacleMask &= ~(1 << pLayer);

            for (int c = 0; c < activeCorpses.Count; c++)
            {
                CorpseRecord corpse = activeCorpses[c];
                if (corpse == null || corpse.isConcealed || corpse.isBeingDragged || corpse.corpseTransform == null) continue;

                Vector3 corpsePos = corpse.corpseTransform.position + Vector3.up * 0.25f;

                for (int z = 0; z < activeZombies.Count; z++)
                {
                    ZombieAI zombie = activeZombies[z];
                    if (zombie == null || zombie.IsDead) continue;
                    if (zombie.CurrentState == ZombieState.Chase || zombie.CurrentState == ZombieState.Attack) continue;

                    Vector3 eyePos = zombie.transform.position + Vector3.up * 1.6f;
                    Vector3 toCorpse = corpsePos - eyePos;
                    float dist = toCorpse.magnitude;

                    if (dist <= corpseDetectionRange)
                    {
                        // Check line of sight
                        if (!Physics.Raycast(eyePos, toCorpse.normalized, dist - 0.2f, obstacleMask))
                        {
                            // Zombie spots unconcealed corpse and investigates
                            zombie.OnNoiseDetected(corpsePos);
                            ProceduralAudioManager.Instance?.PlayZombieQuestioningGroan();

                            // Suppress HUD toast alert during opening prologue / indoors
                            bool isPrologueActive = Narrative.StarterPrologueDirector.Instance != null && !Narrative.StarterPrologueDirector.Instance.IsPrologueCompleted;
                            if (!isPrologueActive)
                            {
                                GameEvents.TriggerObjectiveToastRequested("[DİKKAT] Zombi açıkta kalan cesedi fark etti!", 3.0f);
                            }
                            break;
                        }
                    }
                }
            }
        }

        private CorpseRecord GetNearestInteractableCorpse(float maxDist)
        {
            if (playerTransform == null) return null;

            Vector3 playerPos = playerTransform.position;
            CorpseRecord nearest = null;
            float minSqrDist = maxDist * maxDist;

            for (int i = 0; i < activeCorpses.Count; i++)
            {
                CorpseRecord rec = activeCorpses[i];
                if (rec == null || rec.corpseTransform == null || rec.isBeingDragged) continue;

                float sqrDist = (rec.corpseTransform.position - playerPos).sqrMagnitude;
                if (sqrDist <= minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = rec;
                }
            }

            return nearest;
        }

        private void CreateDragDropPromptUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            dragDropHUDPrompt = new GameObject("Prompt_CorpseDragDrop");
            dragDropHUDPrompt.transform.SetParent(canvas.transform, false);

            RectTransform rt = dragDropHUDPrompt.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 150f);
            rt.sizeDelta = new Vector2(240f, 44f);

            Image bg = dragDropHUDPrompt.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.10f, 0.16f, 0.88f);

            Outline outline = dragDropHUDPrompt.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.72f, 0.01f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            Button btn = dragDropHUDPrompt.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (isDraggingCorpse) DropCorpse();
                else
                {
                    CorpseRecord nearest = GetNearestInteractableCorpse(dragInteractionDistance);
                    if (nearest != null) StartDraggingCorpse(nearest);
                }
            });

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(dragDropHUDPrompt.transform, false);
            RectTransform tRt = txtGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            dragDropText = txtGo.AddComponent<Text>();
            dragDropText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dragDropText.text = "CESEDİ SÜRÜKLE";
            dragDropText.fontSize = 14;
            dragDropText.fontStyle = FontStyle.Bold;
            dragDropText.alignment = TextAnchor.MiddleCenter;
            dragDropText.color = Color.white;

            dragDropHUDPrompt.SetActive(false);
        }

        private void ShowPrompt(bool show, string text)
        {
            if (dragDropHUDPrompt == null) return;
            if (dragDropHUDPrompt.activeSelf != show) dragDropHUDPrompt.SetActive(show);
            if (show && dragDropText != null) dragDropText.text = text;
        }
    }
}
