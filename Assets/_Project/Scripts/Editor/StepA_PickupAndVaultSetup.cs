using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.Interaction;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Master Step A: Scene Setup and World Injection for Guaranteed Weapon Pickup Interaction
    /// and Jump / Obstacle Vaulting Parkour.
    /// </summary>
    public static class StepA_PickupAndVaultSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player_Root.prefab";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Setup Step A Pickup & Vault Parkour", false, 40)]
        public static void SetupStepA()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[StepA_Setup] Starting Step A Pickup & Vault Setup...");

            // 1. Update Player_Root Prefab & Scene Instances
            SetupPlayerComponents();

            // 2. Ensure Starter Weapons Placed & SphereColliders Configured
            SetupStarterWeapons();

            // 3. Configure HUD Buttons (Btn_PrimaryInteract and Btn_JumpVault)
            SetupHUDButtons();

            // 4. Fix Overturned_CargoVan offset if overlapping player spawn
            GameObject van = GameObject.Find("Overturned_CargoVan");
            if (van != null && van.transform.parent != null && van.transform.parent.name == "East_Barricade_Tactical")
            {
                van.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                Debug.Log("[StepA_Setup] Corrected Overturned_CargoVan to East_Barricade_Tactical local coordinates.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[StepA_Setup] Step A Pickup & Vault Setup completed successfully!");
        }

        private static void SetupPlayerComponents()
        {
            // 1. Prefab
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (prefabContents != null)
            {
                if (prefabContents.GetComponent<ParkourVaultController>() == null)
                {
                    prefabContents.AddComponent<ParkourVaultController>();
                }
                if (prefabContents.GetComponent<TPSPlayerController>() == null)
                {
                    prefabContents.AddComponent<TPSPlayerController>();
                }
                if (prefabContents.GetComponent<PlayerInteractionScanner>() == null)
                {
                    prefabContents.AddComponent<PlayerInteractionScanner>();
                }

                PrefabUtility.SaveAsPrefabAsset(prefabContents, PlayerPrefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);
                Debug.Log("[StepA_Setup] Updated Player_Root.prefab with ParkourVaultController and TPSPlayerController.");
            }

            // 2. Scene Instance
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) player = pc.gameObject;
            }

            if (player != null)
            {
                if (player.GetComponent<ParkourVaultController>() == null)
                {
                    player.AddComponent<ParkourVaultController>();
                }
                if (player.GetComponent<TPSPlayerController>() == null)
                {
                    player.AddComponent<TPSPlayerController>();
                }
                if (player.GetComponent<PlayerInteractionScanner>() == null)
                {
                    player.AddComponent<PlayerInteractionScanner>();
                }
                Debug.Log($"[StepA_Setup] Verified components on scene player: {player.name}");
            }
        }

        private static void SetupStarterWeapons()
        {
            StarterArsenalPlacer.PlaceAllStarterWeapons();

            // Ensure all WeaponPickupInteractable instances have active SphereCollider >= 1.65m
            WeaponPickupInteractable[] pickups = UnityEngine.Object.FindObjectsByType<WeaponPickupInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in pickups)
            {
                if (p == null) continue;
                SphereCollider sc = p.GetComponent<SphereCollider>();
                if (sc == null) sc = p.gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                if (sc.radius < 1.65f) sc.radius = 1.65f;
            }
            Debug.Log($"[StepA_Setup] Verified {pickups.Length} WeaponPickupInteractable triggers in scene.");
        }

        private static void SetupHUDButtons()
        {
            // Find HUD Canvas
            Canvas hudCanvas = null;
            var hudCtrl = UnityEngine.Object.FindAnyObjectByType<HUDController>();
            if (hudCtrl != null) hudCanvas = hudCtrl.GetComponentInParent<Canvas>();
            if (hudCanvas == null) hudCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();

            if (hudCanvas == null)
            {
                Debug.LogWarning("[StepA_Setup] Could not find HUD Canvas!");
                return;
            }

            Transform safeArea = hudCanvas.transform.Find("SafeArea") ?? hudCanvas.transform.Find("Safe_Area");
            Transform parentTransform = safeArea != null ? safeArea : hudCanvas.transform;

            // 1. Rebuild / Ensure Btn_PrimaryInteract (AL / ETKİLEŞ)
            SetupPrimaryInteractButton(parentTransform);

            // 2. Build / Ensure Btn_JumpVault (ZIPLA / ATLA)
            SetupJumpVaultButton(parentTransform);

            // 3. Re-align other buttons to ensure zero overlap
            AlignCombatClusterButtons(parentTransform);
        }

        private static void SetupPrimaryInteractButton(Transform parent)
        {
            Transform existing = parent.Find("Btn_PrimaryInteract");
            GameObject btnObj;

            if (existing != null)
            {
                btnObj = existing.gameObject;
            }
            else
            {
                btnObj = new GameObject("Btn_PrimaryInteract", typeof(RectTransform));
                btnObj.transform.SetParent(parent, false);
            }

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-56f, 265f);
            rt.sizeDelta = new Vector2(184f, 60f);

            Image bg = btnObj.GetComponent<Image>() ?? btnObj.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.13f, 0.18f, 0.95f);
            bg.raycastTarget = true; // Crucial: ONLY root Image has raycastTarget = true

            Outline outline = btnObj.GetComponent<Outline>() ?? btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(1.0f, 0.72f, 0.01f, 0.85f); // Amber border
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnObj.GetComponent<Button>() ?? btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = cb;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                PlayerInteractionScanner.Instance?.TriggerPrimaryInteraction();
            });

            // EventTrigger for pointer hold actions
            EventTrigger trigger = btnObj.GetComponent<EventTrigger>() ?? btnObj.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var pDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pDown.callback.AddListener((d) => ContextualInteractionHUD.Instance?.OnPrimaryPointerDown());
            trigger.triggers.Add(pDown);

            var pUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pUp.callback.AddListener((d) => ContextualInteractionHUD.Instance?.OnPrimaryPointerUp());
            trigger.triggers.Add(pUp);

            // Child 1: ProgressFill
            Transform fillTrans = btnObj.transform.Find("ProgressFill");
            GameObject fillObj = fillTrans != null ? fillTrans.gameObject : new GameObject("ProgressFill", typeof(RectTransform));
            fillObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtFill = fillObj.GetComponent<RectTransform>();
            rtFill.anchorMin = new Vector2(0f, 0f);
            rtFill.anchorMax = new Vector2(0f, 1f);
            rtFill.pivot = new Vector2(0f, 0.5f);
            rtFill.sizeDelta = new Vector2(0f, 0f);
            Image fillImg = fillObj.GetComponent<Image>() ?? fillObj.AddComponent<Image>();
            fillImg.color = new Color(1.0f, 0.75f, 0.1f, 0.45f);
            fillImg.raycastTarget = false;

            // Child 2: Keybind Badge
            Transform badgeTrans = btnObj.transform.Find("Keybind_Badge");
            GameObject badgeObj = badgeTrans != null ? badgeTrans.gameObject : new GameObject("Keybind_Badge", typeof(RectTransform));
            badgeObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtBadge = badgeObj.GetComponent<RectTransform>();
            rtBadge.anchorMin = new Vector2(0f, 0f);
            rtBadge.anchorMax = new Vector2(0f, 1f);
            rtBadge.pivot = new Vector2(0f, 0.5f);
            rtBadge.anchoredPosition = new Vector2(8f, 0f);
            rtBadge.sizeDelta = new Vector2(28f, 0f);
            Text badgeText = badgeObj.GetComponent<Text>() ?? badgeObj.AddComponent<Text>();
            badgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            badgeText.fontSize = 12;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.alignment = TextAnchor.MiddleCenter;
            badgeText.color = new Color(1.0f, 0.82f, 0.35f, 0.95f);
            badgeText.raycastTarget = false;
            badgeText.text = "[E]";

            // Child 3: Label Text
            Transform labelTrans = btnObj.transform.Find("Label_Text");
            GameObject labelObj = labelTrans != null ? labelTrans.gameObject : new GameObject("Label_Text", typeof(RectTransform));
            labelObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtLabel = labelObj.GetComponent<RectTransform>();
            rtLabel.anchorMin = new Vector2(0f, 0f);
            rtLabel.anchorMax = new Vector2(1f, 1f);
            rtLabel.pivot = new Vector2(0.5f, 0.5f);
            rtLabel.anchoredPosition = new Vector2(14f, 0f);
            rtLabel.sizeDelta = new Vector2(-40f, 0f);
            Text labelText = labelObj.GetComponent<Text>() ?? labelObj.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 13;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
            labelText.text = "AL / ETKİLEŞ";

            btnObj.SetActive(false); // Controlled dynamically by ContextualInteractionHUD
            Debug.Log("[StepA_Setup] Configured Btn_PrimaryInteract (AL / ETKİLEŞ) cleanly at (-56, 265, 184x60).");
        }

        private static void SetupJumpVaultButton(Transform parent)
        {
            Transform existing = parent.Find("Btn_JumpVault");
            GameObject btnObj;

            if (existing != null)
            {
                btnObj = existing.gameObject;
            }
            else
            {
                btnObj = new GameObject("Btn_JumpVault", typeof(RectTransform));
                btnObj.transform.SetParent(parent, false);
            }

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-130f, 175f);
            rt.sizeDelta = new Vector2(68f, 68f);

            Image bg = btnObj.GetComponent<Image>() ?? btnObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
            bg.raycastTarget = true;
            bg.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);

            Outline outline = btnObj.GetComponent<Outline>() ?? btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.75f, 0.85f, 0.85f); // Cyan accent
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnObj.GetComponent<Button>() ?? btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = cb;

            // Child Label
            Transform labelTrans = btnObj.transform.Find("Label_Text");
            GameObject labelObj = labelTrans != null ? labelTrans.gameObject : new GameObject("Label_Text", typeof(RectTransform));
            labelObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtLabel = labelObj.GetComponent<RectTransform>();
            rtLabel.anchorMin = Vector2.zero;
            rtLabel.anchorMax = Vector2.one;
            rtLabel.sizeDelta = Vector2.zero;
            Text labelText = labelObj.GetComponent<Text>() ?? labelObj.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
            labelText.text = "ZIPLA";

            // Attach JumpVaultHUDButton
            if (btnObj.GetComponent<JumpVaultHUDButton>() == null)
            {
                btnObj.AddComponent<JumpVaultHUDButton>();
            }

            btnObj.SetActive(true);
            Debug.Log("[StepA_Setup] Configured Btn_JumpVault (ZIPLA / ATLA) cleanly at (-46, 175, 82x82).");
        }

        private static void AlignCombatClusterButtons(Transform parent)
        {
            Transform actionPanel = parent.Find("Action_Buttons_Panel");
            Transform root = actionPanel != null ? actionPanel : parent;

            // Position fire at (-75, 75, 104x104)
            Transform fire = root.Find("Btn_Fire") ?? root.Find("Button_Fire");
            if (fire != null)
            {
                RectTransform rt = fire.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-75f, 75f);
                    rt.sizeDelta = new Vector2(104f, 104f);
                }
            }

            // Position aim at (-195, 75, 90x90)
            Transform aim = root.Find("Btn_Aim") ?? root.Find("Button_Aim");
            if (aim != null)
            {
                RectTransform rt = aim.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-195f, 75f);
                    rt.sizeDelta = new Vector2(90f, 90f);
                }
            }

            // Position sprint at (-55, 175, 68x68)
            Transform sprint = root.Find("Btn_Sprint") ?? root.Find("Button_Sprint");
            if (sprint != null)
            {
                RectTransform rt = sprint.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-55f, 175f);
                    rt.sizeDelta = new Vector2(68f, 68f);
                }
            }

            // Position crouch at (-205, 175, 68x68)
            Transform crouch = root.Find("Btn_Crouch") ?? root.Find("Button_Crouch");
            if (crouch != null)
            {
                RectTransform rt = crouch.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-205f, 175f);
                    rt.sizeDelta = new Vector2(68f, 68f);
                }
            }
        }
#endif
    }
}
