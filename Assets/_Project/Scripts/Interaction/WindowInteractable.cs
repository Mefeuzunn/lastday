using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;

namespace ProjectSurvival.Interaction
{
    /// <summary>
    /// Interactive Window & Storefront Archetype.
    /// Provides 4 tactical options for ground-floor building windows:
    /// 1. "peek": Safe visual reconnaissance against glass pane.
    /// 2. "close_blinds": Shuts blinds/shutters, blocking zombie vision into the building interior.
    /// 3. "barricade": Consumes 1 Scrap/Wood to nail wooden planks (+100 HP) across window frame.
    /// 4. "vault": Shatters glass (loud crash audio + 28m noise) and vaults player through window.
    /// </summary>
    public class WindowInteractable : WorldInteractableObject
    {
        [Header("Window Meshes & Transforms")]
        [SerializeField] private GameObject glassPane;
        [SerializeField] private GameObject woodenPlanks;
        [SerializeField] private GameObject blindsOrShutter;
        [SerializeField] private Transform peekPoint;
        [SerializeField] private Transform vaultLandingPoint;

        [Header("Structural Integrity")]
        [SerializeField] private float windowHealth = 50f;
        [SerializeField] private bool isShattered = false;
        [SerializeField] private bool isBlindsClosed = false;

        private bool isPeeking = false;

        public override string ObjectSubtitle => isBarricaded ? "GÜÇLENDİRİLMİŞ" : (isShattered ? "KIRIK CAM" : (isBlindsClosed ? "KEPENGİ KAPALI" : "PENCERE"));

        protected override void Awake()
        {
            base.Awake();
            objectTitle = "Bina Penceresi";
            EnsureWindowParts();
        }

        private void EnsureWindowParts()
        {
            if (glassPane == null)
            {
                Transform g = transform.Find("Glass") ?? transform.Find("Pane");
                if (g != null) glassPane = g.gameObject;
            }

            if (peekPoint == null)
            {
                GameObject peek = new GameObject("Window_PeekPoint");
                peek.transform.SetParent(transform, false);
                peek.transform.localPosition = new Vector3(0f, 0.2f, -0.6f);
                peekPoint = peek.transform;
            }

            if (vaultLandingPoint == null)
            {
                GameObject vault = new GameObject("Window_VaultPoint");
                vault.transform.SetParent(transform, false);
                vault.transform.localPosition = new Vector3(0f, 0f, 1.8f);
                vaultLandingPoint = vault.transform;
            }

            if (woodenPlanks == null)
            {
                Transform wp = transform.Find("WoodenPlanks");
                if (wp != null) woodenPlanks = wp.gameObject;
            }
            if (woodenPlanks != null) woodenPlanks.SetActive(isBarricaded);

            if (blindsOrShutter == null)
            {
                Transform b = transform.Find("Blinds");
                if (b != null) blindsOrShutter = b.gameObject;
            }
            if (blindsOrShutter != null) blindsOrShutter.SetActive(isBlindsClosed);
        }

        public override IReadOnlyList<InteractionVerb> GetAvailableVerbs()
        {
            List<InteractionVerb> verbs = new List<InteractionVerb>();

            if (isPeeking)
            {
                verbs.Add(new InteractionVerb("exit_peek", "GÖZETLEMEYİ BIRAK", 0f));
                return verbs;
            }

            // Verb 1: Peek
            if (!isBlindsClosed && !isBarricaded)
            {
                verbs.Add(new InteractionVerb("peek", "İÇERİYİ/DIŞARIYI GÖZETLE", 0f));
            }

            // Verb 2: Close / Open Blinds
            if (!isShattered && !isBarricaded)
            {
                string blindLabel = isBlindsClosed ? "KEPENGİ / PERDEYİ AÇ" : "KEPENGİ / PERDEYİ KAPAT";
                verbs.Add(new InteractionVerb("toggle_blinds", blindLabel, 0.8f));
            }

            // Verb 3: Barricade
            if (!isBarricaded)
            {
                verbs.Add(new InteractionVerb("barricade", "TAHTAYLA GÜÇLENDİR (+100 HP)", 2.0f, "item_scrap", "Hurda/Tahta"));
            }

            // Verb 4: Shatter & Vault
            verbs.Add(new InteractionVerb("vault", isShattered ? "PENCEREDEN ATLA" : "CAMI KIR VE ATLA", isShattered ? 0.3f : 0.7f, null, null, 28f));

            return verbs;
        }

        public override void ExecuteVerb(string verbId, GameObject interactor)
        {
            switch (verbId)
            {
                case "peek":
                    ExecutePeek();
                    break;
                case "exit_peek":
                    ExecuteExitPeek();
                    break;
                case "toggle_blinds":
                    ExecuteToggleBlinds();
                    break;
                case "barricade":
                    ExecuteBarricade();
                    break;
                case "vault":
                    ExecuteVault(interactor);
                    break;
            }
        }

        private void ExecutePeek()
        {
            isPeeking = true;
            ThirdPersonCamera.Instance?.EnterHidingPeek(peekPoint);
            GameEvents.TriggerObjectiveToastRequested("PENCEREDEN GÖZETLEME MODU (GÜVENLİ KEŞİF)", 2.0f);
        }

        private void ExecuteExitPeek()
        {
            isPeeking = false;
            ThirdPersonCamera.Instance?.ExitHidingPeek();
        }

        private void ExecuteToggleBlinds()
        {
            isBlindsClosed = !isBlindsClosed;
            if (blindsOrShutter != null) blindsOrShutter.SetActive(isBlindsClosed);
            ProceduralAudioManager.Instance?.PlayButtonClick();
            string msg = isBlindsClosed ? "KEPENK KAPATILDI (GÖRÜŞ KESİLDİ)" : "KEPENK AÇILDI";
            GameEvents.TriggerObjectiveToastRequested(msg, 2.0f);
        }

        private void ExecuteBarricade()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.RemoveItem("item_scrap", 1);
            }

            isBarricaded = true;
            windowHealth += 100f;
            if (woodenPlanks != null) woodenPlanks.SetActive(true);
            else CreateProceduralPlanks();

            ProceduralAudioManager.Instance?.PlayDoorPoundImpact(transform.position, false);
            GameEvents.TriggerObjectiveToastRequested("PENCERE GÜÇLENDİRİLDİ (+100 HP)", 2.5f);
        }

        private void CreateProceduralPlanks()
        {
            GameObject pRoot = new GameObject("WoodenPlanks");
            pRoot.transform.SetParent(transform, false);
            woodenPlanks = pRoot;

            Material woodMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            woodMat.color = new Color(0.45f, 0.28f, 0.15f);

            for (int i = -1; i <= 1; i++)
            {
                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"Plank_{i}";
                plank.transform.SetParent(pRoot.transform, false);
                plank.transform.localPosition = new Vector3(0f, i * 0.32f, 0f);
                plank.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-4f, 4f));
                plank.transform.localScale = new Vector3(1.4f, 0.14f, 0.05f);
                DestroyImmediate(plank.GetComponent<Collider>());
                plank.GetComponent<Renderer>().sharedMaterial = woodMat;
            }
        }

        private void ExecuteVault(GameObject player)
        {
            if (!isShattered)
            {
                isShattered = true;
                if (glassPane != null) glassPane.SetActive(false);
                ProceduralAudioManager.Instance?.PlayGlassShatter();
                GameEvents.TriggerNoiseEmitted(transform.position, 28f);
                GameEvents.TriggerCameraShakeRequested(0.28f, 0.18f);
            }

            if (player != null)
            {
                StartCoroutine(VaultMotionRoutine(player));
            }
        }

        private IEnumerator VaultMotionRoutine(GameObject player)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Vector3 startPos = player.transform.position;
            Vector3 targetPos = vaultLandingPoint != null ? vaultLandingPoint.position : (transform.position + transform.forward * 1.8f);
            float elapsed = 0f;
            float dur = 0.45f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                // Parabolic vault arc
                float heightOffset = Mathf.Sin(t * Mathf.PI) * 0.65f;
                Vector3 current = Vector3.Lerp(startPos, targetPos, t);
                current.y += heightOffset;
                player.transform.position = current;
                yield return null;
            }

            player.transform.position = targetPos;
            if (cc != null) cc.enabled = true;
            GameEvents.TriggerObjectiveToastRequested("PENCEREDEN ATLANDI!", 2.0f);
        }
    }
}
