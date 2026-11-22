using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.Player;
using ProjectSurvival.UI;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Phase 2 (Step 2A): Editor Setup Script to Inject / Configure Btn_CameraView into Main_Prototype.unity
    /// </summary>
    public static class Step2A_CameraSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Setup Step 2A Camera Switcher", false, 45)]
        public static void SetupStep2A()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[Step2A_CameraSetup] Configuring Btn_CameraView HUD button...");

            Canvas hudCanvas = null;
            Canvas[] allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allCanvases.Length; i++)
            {
                if (allCanvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    hudCanvas = allCanvases[i];
                    break;
                }
            }

            if (hudCanvas == null)
            {
                Debug.LogError("[Step2A_CameraSetup] HUD Canvas not found!");
                return;
            }

            Transform safeArea = hudCanvas.transform.Find("SafeArea") ?? hudCanvas.transform.Find("Safe_Area");
            Transform parentTransform = safeArea != null ? safeArea : hudCanvas.transform;

            Transform existing = parentTransform.Find("Btn_CameraView");
            GameObject btnObj;
            if (existing != null)
            {
                btnObj = existing.gameObject;
            }
            else
            {
                btnObj = new GameObject("Btn_CameraView", typeof(RectTransform));
                btnObj.transform.SetParent(parentTransform, false);
            }

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-166f, -36f); // Top-right row beside Btn_Pause (-36) and Btn_Backpack (-104)
            rt.sizeDelta = new Vector2(48f, 48f);

            Image img = btnObj.GetComponent<Image>();
            if (img == null) img = btnObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.22f, 0.92f);
            img.raycastTarget = true;

            Outline outline = btnObj.GetComponent<Outline>();
            if (outline == null) outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.72f, 0.15f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.AddComponent<Button>();

            var switcher = btnObj.GetComponent<CameraPerspectiveSwitcher>();
            if (switcher == null) switcher = btnObj.AddComponent<CameraPerspectiveSwitcher>();

            Transform textTf = btnObj.transform.Find("Badge_Text");
            GameObject textObj;
            if (textTf != null) textObj = textTf.gameObject;
            else
            {
                textObj = new GameObject("Badge_Text", typeof(RectTransform));
                textObj.transform.SetParent(btnObj.transform, false);
            }

            RectTransform rtText = textObj.GetComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.sizeDelta = Vector2.zero;

            Text txt = textObj.GetComponent<Text>();
            if (txt == null) txt = textObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 11;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1.0f, 0.85f, 0.35f);
            txt.text = "3. ŞAHIS";
            txt.raycastTarget = false;

            btnObj.SetActive(true);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Step2A_CameraSetup] Successfully configured and saved Btn_CameraView in Main_Prototype.unity.");
        }
#endif
    }
}
