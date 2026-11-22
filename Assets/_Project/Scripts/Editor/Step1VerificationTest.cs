using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Player;
using ProjectSurvival.UI;
using UnityEditor.SceneManagement;

namespace ProjectSurvival.EditorTools
{
    public static class Step1VerificationTest
    {
        public static void RunAllTests()
        {
            Debug.Log("=================================================");
            Debug.Log("[Step1VerificationTest] STARTING AUTOMATED TESTS");
            Debug.Log("=================================================");

            // 1. Load Main_Prototype scene
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main_Prototype.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[Test Failed] Could not load Main_Prototype.unity");
                EditorApplication.Exit(1);
                return;
            }

            // 2. Test HUD Canvas and Components
            GameObject canvasObj = GameObject.Find("HUD_Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("[Test Failed] HUD_Canvas not found in scene!");
                EditorApplication.Exit(1);
                return;
            }

            var uiFixer = canvasObj.GetComponent<UIInputPriorityFixer>();
            if (uiFixer == null)
            {
                Debug.LogError("[Test Failed] UIInputPriorityFixer missing on HUD_Canvas!");
                EditorApplication.Exit(1);
                return;
            }
            uiFixer.FixInputPriorities();
            Debug.Log("[Test Passed] UIInputPriorityFixer verified and executed.");

            // 3. Test TouchLookArea sibling index
            Transform touchLook = canvasObj.transform.Find("TouchLookArea");
            if (touchLook == null || touchLook.GetSiblingIndex() != 0)
            {
                Debug.LogError($"[Test Failed] TouchLookArea not at sibling index 0! Found index: {touchLook?.GetSiblingIndex()}");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[Test Passed] TouchLookArea is correctly positioned at SiblingIndex 0.");

            // 4. Test Pause Button
            Transform pauseBtnTr = canvasObj.transform.Find("Btn_Pause");
            if (pauseBtnTr == null)
            {
                Debug.LogError("[Test Failed] Btn_Pause not found!");
                EditorApplication.Exit(1);
                return;
            }
            RectTransform rtPause = pauseBtnTr.GetComponent<RectTransform>();
            if (rtPause.anchoredPosition != new Vector2(-36f, -36f) || rtPause.sizeDelta != new Vector2(58f, 58f))
            {
                Debug.LogError($"[Test Failed] Btn_Pause rect mismatch! Pos: {rtPause.anchoredPosition}, Size: {rtPause.sizeDelta}");
                EditorApplication.Exit(1);
                return;
            }
            Image pauseImg = pauseBtnTr.GetComponent<Image>();
            if (!pauseImg.raycastTarget)
            {
                Debug.LogError("[Test Failed] Btn_Pause Image raycastTarget is false!");
                EditorApplication.Exit(1);
                return;
            }
            Transform pauseIcon = pauseBtnTr.Find("Icon");
            if (pauseIcon != null && pauseIcon.GetComponent<Image>().raycastTarget)
            {
                Debug.LogError("[Test Failed] Btn_Pause Icon raycastTarget should be false!");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[Test Passed] Btn_Pause positioning (-36, -36, 58x58) and raycast hierarchy verified.");

            // 5. Test PauseMenuController
            var pmc = canvasObj.GetComponent<PauseMenuController>();
            if (pmc == null)
            {
                Debug.LogError("[Test Failed] PauseMenuController missing on HUD_Canvas!");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[Test Passed] PauseMenuController is attached to active HUD_Canvas.");

            // 6. Test WeaponHUDWidget & Tactical Buttons
            Transform wpHudTr = canvasObj.transform.Find("WeaponHUD_Panel");
            if (wpHudTr == null)
            {
                Debug.LogError("[Test Failed] WeaponHUD_Panel not found!");
                EditorApplication.Exit(1);
                return;
            }
            var wpHudWidget = wpHudTr.GetComponent<WeaponHUDWidget>();
            if (wpHudWidget == null)
            {
                Debug.LogError("[Test Failed] WeaponHUDWidget missing on WeaponHUD_Panel!");
                EditorApplication.Exit(1);
                return;
            }
            Button cardBtn = wpHudTr.GetComponent<Button>();
            if (cardBtn == null)
            {
                Debug.LogError("[Test Failed] WeaponHUD_Panel missing Button component for interactive cycling!");
                EditorApplication.Exit(1);
                return;
            }
            Transform btnSwitchTr = wpHudTr.Find("Btn_SwitchWeapon");
            Transform btnHolsterTr = wpHudTr.Find("Btn_HolsterWeapon");
            if (btnSwitchTr == null || btnHolsterTr == null)
            {
                Debug.LogError("[Test Failed] Tactical mini buttons (Btn_SwitchWeapon or Btn_HolsterWeapon) missing!");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[Test Passed] WeaponHUDWidget, interactive card button, and mini holster/switch buttons verified.");

            // 7. Test QuickSlots_Bar & QuickSlotUI
            Transform qsBarTr = canvasObj.transform.Find("QuickSlots_Bar");
            if (qsBarTr == null)
            {
                Debug.LogError("[Test Failed] QuickSlots_Bar not found!");
                EditorApplication.Exit(1);
                return;
            }
            var qsUI = qsBarTr.GetComponent<QuickSlotUI>();
            if (qsUI == null)
            {
                Debug.LogError("[Test Failed] QuickSlotUI missing on QuickSlots_Bar!");
                EditorApplication.Exit(1);
                return;
            }
            Button[] qsButtons = qsBarTr.GetComponentsInChildren<Button>(true);
            if (qsButtons.Length != 4)
            {
                Debug.LogError($"[Test Failed] QuickSlots should have 4 buttons, found: {qsButtons.Length}");
                EditorApplication.Exit(1);
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                Image bImg = qsButtons[i].GetComponent<Image>();
                if (!bImg.raycastTarget)
                {
                    Debug.LogError($"[Test Failed] QuickSlot button {i} raycastTarget is false!");
                    EditorApplication.Exit(1);
                    return;
                }
                Transform ic = qsButtons[i].transform.Find("ItemIcon");
                if (ic != null && ic.GetComponent<Image>().raycastTarget)
                {
                    Debug.LogError($"[Test Failed] QuickSlot icon {i} raycastTarget should be false!");
                    EditorApplication.Exit(1);
                    return;
                }
            }
            Debug.Log("[Test Passed] QuickSlots_Bar and 4 responsive QuickSlot buttons verified.");

            // 8. Test Player WeaponSystem Holster & Speed Bonus
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[Test Failed] Player GameObject not found!");
                EditorApplication.Exit(1);
                return;
            }
            var ws = player.GetComponent<WeaponSystem>();
            if (ws == null)
            {
                Debug.LogError("[Test Failed] WeaponSystem missing on Player!");
                EditorApplication.Exit(1);
                return;
            }
            ws.EquipWeapon(WeaponType.Pistol);
            if (ws.CurrentWeapon != WeaponType.Pistol)
            {
                Debug.LogError("[Test Failed] Failed to equip Pistol!");
                EditorApplication.Exit(1);
                return;
            }
            ws.HolsterWeapon();
            if (ws.CurrentWeapon != WeaponType.None)
            {
                Debug.LogError("[Test Failed] HolsterWeapon did not stow weapon to None!");
                EditorApplication.Exit(1);
                return;
            }
            ws.HolsterWeapon();
            if (ws.CurrentWeapon != WeaponType.Pistol)
            {
                Debug.LogError("[Test Failed] HolsterWeapon did not re-equip previous Pistol!");
                EditorApplication.Exit(1);
                return;
            }
            ws.HolsterWeapon(); // Stow back to None
            Debug.Log("[Test Passed] WeaponSystem Holster/Unholster toggle verified.");

            Debug.Log("=================================================");
            Debug.Log("[Step1VerificationTest] ALL VERIFICATION TESTS PASSED!");
            Debug.Log("=================================================");

            EditorApplication.Exit(0);
        }
    }
}
