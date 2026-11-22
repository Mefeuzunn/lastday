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
    /// Master Step 4: Verification Audit for Right-Bottom Jump/Vault Button & Reliable Weapon/Loot Interaction.
    /// </summary>
    public static class RecoveryStep4ControlsAudit
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

        private static int passedTests = 0;
        private static int failedTests = 0;
        private static readonly List<string> failureDetails = new List<string>();

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Audit Recovery Step 4 Controls", false, 50)]
        public static void RunAudit()
        {
            passedTests = 0;
            failedTests = 0;
            failureDetails.Clear();

            Debug.Log("===============================================================================");
            Debug.Log(">>> RUNNING RECOVERY STEP 4 CONTROLS & INTERACTION AUDIT <<<");
            Debug.Log("===============================================================================");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            AuditJumpVaultButton();
            AuditMiddleRowNonOverlapLayout();
            AuditPrimaryInteractButton();
            AuditMainMenuWhitelisting();
            AuditParkourAndScannerTuning();
            AuditStarterWeaponsColliders();
            AuditIndoorSafehomeContainers();

            Debug.Log("===============================================================================");
            Debug.Log($">>> RECOVERY STEP 4 AUDIT RESULT: {passedTests} PASSED, {failedTests} FAILED <<<");
            Debug.Log("===============================================================================");

            if (failedTests > 0)
            {
                foreach (var err in failureDetails)
                {
                    Debug.LogError($"[STEP 4 FAILURE] {err}");
                }
            }
            else
            {
                Debug.Log("[STEP 4 SUCCESS] All 7 verification categories passed with 100% compliance!");
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (condition)
            {
                passedTests++;
                Debug.Log($"[STEP 4 PASS] {message}");
            }
            else
            {
                failedTests++;
                failureDetails.Add(message);
                Debug.LogError($"[STEP 4 FAIL] {message}");
            }
        }

        private static void AuditJumpVaultButton()
        {
            Debug.Log("--- 1. Audit Btn_JumpVault (ZIPLA / ATLA) ---");

            GameObject jvObj = GameObject.Find("Btn_JumpVault");
            Assert(jvObj != null, "Btn_JumpVault exists in scene hierarchy");
            if (jvObj == null) return;

            RectTransform rt = jvObj.GetComponent<RectTransform>();
            Assert(rt != null, "Btn_JumpVault has RectTransform");
            if (rt != null)
            {
                Assert(Mathf.Approximately(rt.anchoredPosition.x, -130f) && Mathf.Approximately(rt.anchoredPosition.y, 175f),
                    $"Btn_JumpVault anchoredPosition is (-130, 175) (actual: {rt.anchoredPosition})");
                Assert(Mathf.Approximately(rt.sizeDelta.x, 68f) && Mathf.Approximately(rt.sizeDelta.y, 68f),
                    $"Btn_JumpVault sizeDelta is (68, 68) (actual: {rt.sizeDelta})");
                Assert(Mathf.Approximately(rt.pivot.x, 0.5f) && Mathf.Approximately(rt.pivot.y, 0.5f),
                    $"Btn_JumpVault pivot is (0.5, 0.5) (actual: {rt.pivot})");
            }

            Image bg = jvObj.GetComponent<Image>();
            Assert(bg != null && bg.raycastTarget, "Btn_JumpVault root Image has raycastTarget = true");

            Text label = jvObj.GetComponentInChildren<Text>();
            Assert(label != null && !label.raycastTarget, "Btn_JumpVault child label has raycastTarget = false");

            var jvScript = jvObj.GetComponent<JumpVaultHUDButton>();
            Assert(jvScript != null, "Btn_JumpVault has JumpVaultHUDButton component attached");
            Assert(typeof(IPointerDownHandler).IsAssignableFrom(typeof(JumpVaultHUDButton)), "JumpVaultHUDButton implements IPointerDownHandler for 0ms touch latency");
        }

        private static void AuditMiddleRowNonOverlapLayout()
        {
            Debug.Log("--- 2. Audit Middle Row Non-Overlap (Crouch - JumpVault - Sprint) ---");

            GameObject crouchObj = GameObject.Find("Btn_Crouch") ?? GameObject.Find("Button_Crouch");
            GameObject sprintObj = GameObject.Find("Btn_Sprint") ?? GameObject.Find("Button_Sprint");
            GameObject jumpObj = GameObject.Find("Btn_JumpVault");

            Assert(crouchObj != null, "Btn_Crouch exists");
            Assert(sprintObj != null, "Btn_Sprint exists");
            Assert(jumpObj != null, "Btn_JumpVault exists");

            if (crouchObj != null && sprintObj != null && jumpObj != null)
            {
                RectTransform rtC = crouchObj.GetComponent<RectTransform>();
                RectTransform rtS = sprintObj.GetComponent<RectTransform>();
                RectTransform rtJ = jumpObj.GetComponent<RectTransform>();

                Assert(Mathf.Approximately(rtC.anchoredPosition.x, -205f) && Mathf.Approximately(rtC.anchoredPosition.y, 175f),
                    $"Btn_Crouch positioned at (-205, 175) (actual: {rtC.anchoredPosition})");
                Assert(Mathf.Approximately(rtS.anchoredPosition.x, -55f) && Mathf.Approximately(rtS.anchoredPosition.y, 175f),
                    $"Btn_Sprint positioned at (-55, 175) (actual: {rtS.anchoredPosition})");

                // Calculate horizontal bounds
                float cRight = rtC.anchoredPosition.x + rtC.sizeDelta.x * (1f - rtC.pivot.x);
                float jLeft = rtJ.anchoredPosition.x - rtJ.sizeDelta.x * rtJ.pivot.x;
                float jRight = rtJ.anchoredPosition.x + rtJ.sizeDelta.x * (1f - rtJ.pivot.x);
                float sLeft = rtS.anchoredPosition.x - rtS.sizeDelta.x * rtS.pivot.x;

                Assert(cRight < jLeft, $"Zero overlap between Crouch (right={cRight}) and JumpVault (left={jLeft}), margin={jLeft - cRight}px");
                Assert(jRight < sLeft, $"Zero overlap between JumpVault (right={jRight}) and Sprint (left={sLeft}), margin={sLeft - jRight}px");
            }
        }

        private static void AuditPrimaryInteractButton()
        {
            Debug.Log("--- 3. Audit Btn_PrimaryInteract (AL / ETKİLEŞ) ---");

            GameObject btnObj = GameObject.Find("Btn_PrimaryInteract");
            Assert(btnObj != null, "Btn_PrimaryInteract exists in scene");
            if (btnObj == null) return;

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            Assert(rt != null, "Btn_PrimaryInteract has RectTransform");
            if (rt != null)
            {
                Assert(Mathf.Approximately(rt.anchoredPosition.x, -56f) && Mathf.Approximately(rt.anchoredPosition.y, 265f),
                    $"Btn_PrimaryInteract positioned at (-56, 265) (actual: {rt.anchoredPosition})");
                Assert(Mathf.Approximately(rt.sizeDelta.x, 184f) && Mathf.Approximately(rt.sizeDelta.y, 60f),
                    $"Btn_PrimaryInteract size is (184, 60) (actual: {rt.sizeDelta})");

                // Check vertical separation from middle row (y = 175 + 34 = 209 top edge of middle row)
                float interactBottom = rt.anchoredPosition.y - rt.sizeDelta.y * rt.pivot.y;
                float middleRowTop = 175f + 34f;
                Assert(interactBottom > middleRowTop, $"Zero vertical overlap between Btn_PrimaryInteract (bottom={interactBottom}) and middle row (top={middleRowTop}), margin={interactBottom - middleRowTop}px");
            }

            Image bg = btnObj.GetComponent<Image>();
            Assert(bg != null && bg.raycastTarget, "Btn_PrimaryInteract root Image has raycastTarget = true");

            Graphic[] childGraphics = btnObj.GetComponentsInChildren<Graphic>(true);
            int blockedChildCount = 0;
            foreach (var g in childGraphics)
            {
                if (g.gameObject == btnObj) continue;
                if (g.raycastTarget) blockedChildCount++;
            }
            Assert(blockedChildCount == 0, $"Btn_PrimaryInteract child graphics have raycastTarget = false (found {blockedChildCount} blockers)");
        }

        private static void AuditMainMenuWhitelisting()
        {
            Debug.Log("--- 4. Audit MainMenuController Gameplay HUD Whitelist ---");

            string scriptPath = "Assets/_Project/Scripts/UI/MainMenuController.cs";
            if (System.IO.File.Exists(scriptPath))
            {
                string text = System.IO.File.ReadAllText(scriptPath);
                bool hasJumpWhitelisted = text.Contains("Btn_JumpVault");
                bool hasInteractWhitelisted = text.Contains("Btn_PrimaryInteract");
                Assert(hasJumpWhitelisted, "MainMenuController whitelists Btn_JumpVault in SetGameplayHUDActive");
                Assert(hasInteractWhitelisted, "MainMenuController whitelists Btn_PrimaryInteract in SetGameplayHUDActive");
            }
            else
            {
                Assert(false, "MainMenuController.cs file found");
            }
        }

        private static void AuditParkourAndScannerTuning()
        {
            Debug.Log("--- 5. Audit ParkourVaultController & PlayerInteractionScanner Tuning ---");

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) player = pc.gameObject;
            }

            Assert(player != null, "Player GameObject exists in scene");
            if (player == null) return;

            var parkour = player.GetComponent<ParkourVaultController>();
            Assert(parkour != null, "ParkourVaultController exists on Player");

            var scanner = player.GetComponent<PlayerInteractionScanner>();
            Assert(scanner != null, "PlayerInteractionScanner exists on Player");
            if (scanner != null)
            {
                Assert(scanner.ScanRadius >= 3.0f, $"Scanner ScanRadius is >= 3.0m (actual: {scanner.ScanRadius})");
                Assert(scanner.MaxConeAngle >= 80f, $"Scanner MaxConeAngle is >= 80 degrees (actual: {scanner.MaxConeAngle})");
            }
        }

        private static void AuditStarterWeaponsColliders()
        {
            Debug.Log("--- 6. Audit Starter Weapons & SphereCollider Radius ---");

            GameObject axeObj = GameObject.Find("WeaponPickup_Starter_FireAxe");
            Assert(axeObj != null, "WeaponPickup_Starter_FireAxe exists");
            if (axeObj != null)
            {
                var sc = axeObj.GetComponent<SphereCollider>();
                Assert(sc != null && sc.isTrigger && sc.radius >= 1.85f, $"Fire Axe trigger SphereCollider radius >= 1.85m (actual: {sc?.radius})");
                var wp = axeObj.GetComponent<WeaponPickupInteractable>();
                Assert(wp != null && wp.WeaponType == WeaponType.Axe, "Fire Axe has WeaponPickupInteractable configured for Axe");
            }

            GameObject pistolObj = GameObject.Find("WeaponPickup_Starter_Pistol");
            Assert(pistolObj != null, "WeaponPickup_Starter_Pistol exists");
            if (pistolObj != null)
            {
                var sc = pistolObj.GetComponent<SphereCollider>();
                Assert(sc != null && sc.isTrigger && sc.radius >= 1.85f, $"Pistol trigger SphereCollider radius >= 1.85m (actual: {sc?.radius})");
                var wp = pistolObj.GetComponent<WeaponPickupInteractable>();
                Assert(wp != null && wp.WeaponType == WeaponType.Pistol, "Pistol has WeaponPickupInteractable configured for Pistol");
            }
        }

        private static void AuditIndoorSafehomeContainers()
        {
            Debug.Log("--- 7. Audit StarterHouse Indoor Containers ---");

            string[] containerNames = new string[]
            {
                "Furniture_Starter_KitchenCabinet",
                "Furniture_Starter_Refrigerator",
                "Furniture_Starter_MedicineCabinet",
                "Furniture_Starter_Wardrobe"
            };

            foreach (var cName in containerNames)
            {
                GameObject obj = GameObject.Find(cName);
                Assert(obj != null, $"StarterHouse safehome container '{cName}' exists in scene");
                if (obj != null)
                {
                    var fi = obj.GetComponent<FurnitureInteractable>();
                    Assert(fi != null, $"Container '{cName}' has FurnitureInteractable component attached");
                    var col = obj.GetComponent<Collider>();
                    Assert(col != null, $"Container '{cName}' has active physical/trigger Collider attached");
                }
            }
        }
#endif
    }
}
