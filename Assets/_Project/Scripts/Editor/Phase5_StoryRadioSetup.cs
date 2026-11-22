using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using ProjectSurvival.Core;
using ProjectSurvival.Survival;
using ProjectSurvival.Narrative;
using ProjectSurvival.Interaction;
using ProjectSurvival.UI;
using ProjectSurvival.Managers;

namespace ProjectSurvival.EditorTools
{
    /// <summary>
    /// Active Phase 5: Scene Setup and World Injection for Tactical Radio Scanner,
    /// 4-Chapter Branching Story Campaign, Scientist Audio Logs, and Secret Supply Caches.
    /// </summary>
    public static class Phase5_StoryRadioSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main_Prototype.unity";

#if UNITY_EDITOR
        [MenuItem("ProjectSurvival/Setup Phase 5 Story, Radio & Audio Logs", false, 37)]
        public static void SetupPhase5()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[Phase5_StoryRadioSetup] Starting Phase 5 World & UI Integration...");

            // 1. Create or Find Manager Root
            GameObject managerRoot = GameObject.Find("--- STORY & RADIO MANAGERS ---");
            if (managerRoot == null)
            {
                managerRoot = new GameObject("--- STORY & RADIO MANAGERS ---");
            }

            if (managerRoot.GetComponent<TacticalRadioScanner>() == null)
            {
                managerRoot.AddComponent<TacticalRadioScanner>();
            }

            if (managerRoot.GetComponent<MainStoryDirector>() == null)
            {
                managerRoot.AddComponent<MainStoryDirector>();
            }

            if (managerRoot.GetComponent<CommunityMoraleManager>() == null)
            {
                managerRoot.AddComponent<CommunityMoraleManager>();
            }

            // 2. Setup URP Materials for Recorders and Terminals
            Material recorderMat = GetOrCreateLitMaterial("Mat_AudioLog_Recorder", new Color(0.12f, 0.14f, 0.18f), new Color(0f, 0.94f, 1f));
            Material terminalMat = GetOrCreateLitMaterial("Mat_Story_Terminal", new Color(0.15f, 0.18f, 0.22f), new Color(0.2f, 0.85f, 0.45f));
            Material cacheMat = GetOrCreateLitMaterial("Mat_Secret_Cache", new Color(0.18f, 0.24f, 0.20f), new Color(0.2f, 0.98f, 0.5f));

            // 3. Spawn the 6 Physical Audio Log Recorders
            GameObject audioLogsRoot = GameObject.Find("--- SCIENTIST AUDIO LOGS ---");
            if (audioLogsRoot == null)
            {
                audioLogsRoot = new GameObject("--- SCIENTIST AUDIO LOGS ---");
            }

            SpawnAudioLog(audioLogsRoot.transform, "AudioLog_SafeHouse_Origin",
                new Vector3(4.2f, 0.85f, 2.0f), recorderMat,
                "log_safehouse_origin", "Kayıt #01 — Karantina Başlangıcı", "Dr. Selin Kaya",
                "Karantina Protokolü 9. gün. Şehir dış bağlantıları kesildi. Vakalar beklediğimizden hızlı mutasyona uğruyor. Virüs sıradan bir kuduz suşu değil, yapay bir retrovirüs taşıyıcısı. Sığınaktaki laboratuvarımı korumam gerek.");

            SpawnAudioLog(audioLogsRoot.transform, "AudioLog_Clinic_PatientZero",
                new Vector3(26.5f, 0.85f, 17.2f), recorderMat,
                "log_clinic_patient_zero", "Kayıt #02 — Sıfır Numaralı Hasta", "Dr. Selin Kaya",
                "Kliniğe ilk gelen denek bilincini tamamen yitirmişti. Isırık yaraları hücresel nekroza yol açıyor ve merkezi sinir sistemini ele geçiriyor. Antiviral sentezi için RNA anahtarını izole ettim, ancak klinikten çıkış yolları kapandı.");

            SpawnAudioLog(audioLogsRoot.transform, "AudioLog_Police_Containment",
                new Vector3(-18.5f, 0.85f, 42.0f), recorderMat,
                "log_police_containment", "Kayıt #03 — Emniyet Tahliye Emri", "Başkomiser Demir",
                "Tüm devriye ekiplerine: Emniyet binası doğu kanadı düştü. Karantina hattını tutamıyoruz. Kalan tüm personelin Doğu Tren Garı'ndaki askeri tahliye konvoyuna çekilmesi emredildi. Tanrı yardımcımız olsun.");

            SpawnAudioLog(audioLogsRoot.transform, "AudioLog_Substation_Sabotage",
                new Vector3(210.0f, 0.95f, -215.0f), recorderMat,
                "log_substation_sabotage", "Kayıt #04 — Trafo Sabotajı", "Mühendis Kemal",
                "Şehir şebekesi aşırı yüklendi. Birileri karantina bariyerlerinin elektriğini kasten kesti. Trafo merkezindeki ana jeneratörleri devre dışı bıraktılar. Eğer bölgesel şebeke panellerini birleştiremezsek tüm şehir karanlığa gömülecek.");

            SpawnAudioLog(audioLogsRoot.transform, "AudioLog_Subway_Quarantine",
                new Vector3(0.0f, -3.8f, -192.0f), recorderMat,
                "log_metro_quarantine", "Kayıt #05 — Metro Hattı İzolasyonu", "Dr. Aras",
                "Metro tünellerine sığınan yüzlerce sivil vardı... ama havalandırma şaftlarından sızdılar. Çığlık atan mutantlar tünelleri doldurduğunda demir servis kapılarını arkamızdan mühürlemek zorunda kaldık. O kapıların asla açılmaması gerek.");

            SpawnAudioLog(audioLogsRoot.transform, "AudioLog_Ranger_ApexMutation",
                new Vector3(-220.0f, 7.2f, 259.0f), recorderMat,
                "log_ranger_apex_mutation", "Kayıt #06 — Ormandaki Mutasyonlar", "Korucu Berk",
                "Kuzeybatı ormanındaki göl kenarında devasa bir kütle gördüm. Normal bir enfekte değil, biyokütleleri birleştiren bir tepe avcı. Çığlığı kilometrelerce öteden duyuluyor. Sığınağın kapılarını güçlendirin, o şey şehre doğru iniyor.");

            // 4. Spawn the 4 Story Milestone Terminals
            GameObject storyTerminalsRoot = GameObject.Find("--- STORY TERMINALS ---");
            if (storyTerminalsRoot == null)
            {
                storyTerminalsRoot = new GameObject("--- STORY TERMINALS ---");
            }

            SpawnStoryTerminal(storyTerminalsRoot.transform, "Terminal_Clinic_DrKaya",
                new Vector3(24.0f, 0.5f, 15.0f), terminalMat,
                "terminal_clinic", "Dr. Selin Kaya Araştırma Terminali", "Zone Klinik Laboratuvarı", "ARAŞTIRMA VERİLERİNİ İNDİR", 1);

            SpawnStoryTerminal(storyTerminalsRoot.transform, "Terminal_EastRailYard_BlackBox",
                new Vector3(280.0f, 0.5f, 30.0f), terminalMat,
                "terminal_railcar", "Askeri Konvoy Zırhlı Kargo Terminali", "8. Mekanize Birlik Vericisi", "ŞİFRE DİSKİNİ ELE GEÇİR", 2);

            SpawnStoryTerminal(storyTerminalsRoot.transform, "Terminal_NW_Bunker",
                new Vector3(-240.0f, 0.5f, 270.0f), terminalMat,
                "terminal_bunker", "Delta Araştırma Sığınağı Konsolu", "Yeraltı Biyoloji Laboratuvarı", "ANTİVİRAL SENTEZ ANAHTARINI AL", 3);

            SpawnStoryTerminal(storyTerminalsRoot.transform, "Terminal_North_RelayTower",
                new Vector3(0.0f, 7.5f, 220.0f), terminalMat,
                "terminal_relay_tower", "Kuzey Otoyolu Yayın Kulesi Konsolu", "Merkezi İletişim Rölesi", "NİHAİ YAYIN MENÜSÜNÜ AÇ", 4);

            // 5. Spawn the 2 Hidden Secret Supply Caches
            GameObject secretCachesRoot = GameObject.Find("--- SECRET RADIO CACHES ---");
            if (secretCachesRoot == null)
            {
                secretCachesRoot = new GameObject("--- SECRET RADIO CACHES ---");
            }

            SpawnSecretCache(secretCachesRoot.transform, "SecretCache_WestForest",
                new Vector3(-260.0f, 0.45f, 75.0f), cacheMat,
                "cache_west_forest", "Korucu Acil İkmal Zulası",
                new List<string> { "item_scrap", "item_antiviral", "item_bandage" },
                new List<int> { 15, 2, 3 });

            SpawnSecretCache(secretCachesRoot.transform, "SecretCache_SWPharmacy",
                new Vector3(-210.0f, 0.3f, -220.0f), cacheMat,
                "cache_sw_pharmacy", "Batık Eczane Kaçakçı Deposu",
                new List<string> { "item_medkit", "item_bandage", "item_purified_water" },
                new List<int> { 2, 4, 3 });

            // 6. Setup UI Elements under HUD_Canvas
            SetupHUDCanvasElements();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase5_StoryRadioSetup] Phase 5 World & UI successfully saved to Main_Prototype.unity.");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static Material GetOrCreateLitMaterial(string matName, Color baseCol, Color emitCol)
        {
            string path = $"Assets/_Project/Materials/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(s);
                mat.SetColor("_BaseColor", baseCol);
                mat.SetColor("_Color", baseCol);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emitCol);
                mat.enableInstancing = true;
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void SpawnAudioLog(Transform parent, string goName, Vector3 pos, Material mat,
            string id, string title, string speaker, string transcript)
        {
            Transform existing = parent.Find(goName);
            GameObject go = existing != null ? existing.gameObject : null;
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = goName;
                go.transform.SetParent(parent, false);
            }

            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.35f, 0.12f, 0.25f);

            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            var log = go.GetComponent<AudioLogInteractable>();
            if (log == null) log = go.AddComponent<AudioLogInteractable>();

            log.ConfigureLog(id, title, speaker, transcript);
        }

        private static void SpawnStoryTerminal(Transform parent, string goName, Vector3 pos, Material mat,
            string id, string title, string subtitle, string verbLabel, int chapter)
        {
            Transform existing = parent.Find(goName);
            GameObject go = existing != null ? existing.gameObject : null;
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = goName;
                go.transform.SetParent(parent, false);
            }

            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.9f, 1.2f, 0.5f);

            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            var term = go.GetComponent<StoryTerminalInteractable>();
            if (term == null) term = go.AddComponent<StoryTerminalInteractable>();

            term.ConfigureTerminal(id, title, subtitle, verbLabel, chapter);
        }

        private static void SpawnSecretCache(Transform parent, string goName, Vector3 pos, Material mat,
            string cacheId, string title, List<string> items, List<int> counts)
        {
            Transform existing = parent.Find(goName);
            GameObject go = existing != null ? existing.gameObject : null;
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = goName;
                go.transform.SetParent(parent, false);
            }

            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.1f, 0.7f, 0.9f);

            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            var cache = go.GetComponent<SecretCacheInteractable>();
            if (cache == null) cache = go.AddComponent<SecretCacheInteractable>();

            cache.ConfigureCache(cacheId, title, items, counts);
        }

        private static void SetupHUDCanvasElements()
        {
            GameObject hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null)
            {
                Debug.LogWarning("[Phase5_StoryRadioSetup] HUD_Canvas not found in scene!");
                return;
            }

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // A. Upgrade QuestTracker_Panel with QuestTrackerHUD
            Transform questPanelTr = hudCanvas.transform.Find("QuestTracker_Panel");
            if (questPanelTr != null)
            {
                QuestTrackerHUD qhud = questPanelTr.GetComponent<QuestTrackerHUD>();
                if (qhud == null) qhud = questPanelTr.gameObject.AddComponent<QuestTrackerHUD>();

                Text headerText = questPanelTr.Find("QuestHeader")?.GetComponent<Text>();
                Text titleText = questPanelTr.Find("QuestTitle")?.GetComponent<Text>();
                Text descText = questPanelTr.Find("QuestDesc")?.GetComponent<Text>();
                Text distText = questPanelTr.Find("QuestDistance")?.GetComponent<Text>();
                Image dotImg = questPanelTr.Find("StatusDot")?.GetComponent<Image>();

                qhud.BindUIReferences(questPanelTr.gameObject, headerText, titleText, descText, distText, dotImg);
            }

            // B. Build RadioTranscriptBannerUI (Bottom-Center above QuickSlots)
            Transform existingBanner = hudCanvas.transform.Find("Panel_RadioTranscriptBanner");
            if (existingBanner == null)
            {
                GameObject bannerObj = new GameObject("Panel_RadioTranscriptBanner");
                bannerObj.transform.SetParent(hudCanvas.transform, false);

                RectTransform rt = bannerObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 130f); // Cleanly above QuickSlots
                rt.sizeDelta = new Vector2(620f, 75f);

                CanvasGroup cg = bannerObj.AddComponent<CanvasGroup>();
                Image bg = bannerObj.AddComponent<Image>();
                bg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
                bg.raycastTarget = false;

                GameObject spkObj = new GameObject("SpeakerLabel");
                spkObj.transform.SetParent(bannerObj.transform, false);
                Text spkText = spkObj.AddComponent<Text>();
                spkText.font = defaultFont;
                spkText.fontSize = 12;
                spkText.fontStyle = FontStyle.Bold;
                spkText.color = new Color(0f, 0.92f, 1f); // Neon Cyan
                spkText.alignment = TextAnchor.MiddleLeft;
                RectTransform spkRt = spkObj.GetComponent<RectTransform>();
                spkRt.anchorMin = new Vector2(0f, 1f);
                spkRt.anchorMax = new Vector2(1f, 1f);
                spkRt.pivot = new Vector2(0f, 1f);
                spkRt.anchoredPosition = new Vector2(16f, -8f);
                spkRt.sizeDelta = new Vector2(-32f, 18f);

                GameObject bodyObj = new GameObject("TranscriptBody");
                bodyObj.transform.SetParent(bannerObj.transform, false);
                Text bodyText = bodyObj.AddComponent<Text>();
                bodyText.font = defaultFont;
                bodyText.fontSize = 12;
                bodyText.color = Color.white;
                bodyText.alignment = TextAnchor.UpperLeft;
                RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
                bodyRt.anchorMin = Vector2.zero;
                bodyRt.anchorMax = Vector2.one;
                bodyRt.offsetMin = new Vector2(16f, 8f);
                bodyRt.offsetMax = new Vector2(-16f, -28f);

                RadioTranscriptBannerUI bannerUI = bannerObj.AddComponent<RadioTranscriptBannerUI>();
                bannerUI.BindUIReferences(bannerObj, cg, spkText, bodyText);
            }

            // C. Build RadioScannerUI Modal & HUD Quick Pill
            Transform existingRadio = hudCanvas.transform.Find("Panel_RadioScanner");
            if (existingRadio == null)
            {
                // 1. HUD Quick Pill Button 'TELSİZ'
                GameObject pillObj = new GameObject("Btn_RadioQuickPill");
                pillObj.transform.SetParent(hudCanvas.transform, false);
                RectTransform rtPill = pillObj.AddComponent<RectTransform>();
                rtPill.anchorMin = new Vector2(0.5f, 0f);
                rtPill.anchorMax = new Vector2(0.5f, 0f);
                rtPill.pivot = new Vector2(0.5f, 0f);
                rtPill.anchoredPosition = new Vector2(-170f, 20f);
                rtPill.sizeDelta = new Vector2(85f, 34f);

                Image pillImg = pillObj.AddComponent<Image>();
                pillImg.color = new Color(0.12f, 0.16f, 0.22f, 0.92f);
                Button pillBtn = pillObj.AddComponent<Button>();
                pillBtn.targetGraphic = pillImg;

                GameObject pillTextObj = new GameObject("Text");
                pillTextObj.transform.SetParent(pillObj.transform, false);
                Text pillTxt = pillTextObj.AddComponent<Text>();
                pillTxt.font = defaultFont;
                pillTxt.fontSize = 11;
                pillTxt.fontStyle = FontStyle.Bold;
                pillTxt.color = new Color(0f, 0.94f, 1f);
                pillTxt.alignment = TextAnchor.MiddleCenter;
                pillTxt.text = "TELSİZ (T)";
                RectTransform ptxtRt = pillTextObj.GetComponent<RectTransform>();
                ptxtRt.anchorMin = Vector2.zero;
                ptxtRt.anchorMax = Vector2.one;
                ptxtRt.sizeDelta = Vector2.zero;

                // 2. Radio Scanner Full Modal
                GameObject radioPanel = new GameObject("Panel_RadioScanner");
                radioPanel.transform.SetParent(hudCanvas.transform, false);
                RectTransform rtRadio = radioPanel.AddComponent<RectTransform>();
                rtRadio.anchorMin = new Vector2(0.5f, 0.5f);
                rtRadio.anchorMax = new Vector2(0.5f, 0.5f);
                rtRadio.pivot = new Vector2(0.5f, 0.5f);
                rtRadio.anchoredPosition = Vector2.zero;
                rtRadio.sizeDelta = new Vector2(560f, 420f);

                Image bgRadio = radioPanel.AddComponent<Image>();
                bgRadio.color = new Color(0.08f, 0.10f, 0.14f, 0.96f);

                // Close Button
                GameObject closeBtnObj = new GameObject("Btn_CloseRadio");
                closeBtnObj.transform.SetParent(radioPanel.transform, false);
                RectTransform rtClose = closeBtnObj.AddComponent<RectTransform>();
                rtClose.anchorMin = new Vector2(1f, 1f);
                rtClose.anchorMax = new Vector2(1f, 1f);
                rtClose.pivot = new Vector2(1f, 1f);
                rtClose.anchoredPosition = new Vector2(-12f, -12f);
                rtClose.sizeDelta = new Vector2(32f, 32f);
                Image imgClose = closeBtnObj.AddComponent<Image>();
                imgClose.color = new Color(0.85f, 0.2f, 0.25f, 0.9f);
                Button btnClose = closeBtnObj.AddComponent<Button>();
                btnClose.targetGraphic = imgClose;

                // Title
                GameObject titleObj = new GameObject("RadioTitle");
                titleObj.transform.SetParent(radioPanel.transform, false);
                Text radioTitle = titleObj.AddComponent<Text>();
                radioTitle.font = defaultFont;
                radioTitle.fontSize = 15;
                radioTitle.fontStyle = FontStyle.Bold;
                radioTitle.color = new Color(0.95f, 0.95f, 0.95f);
                radioTitle.text = "TAKTİK TELSİZ TARAYICI / FREKANS ALICISI";
                RectTransform rtT = titleObj.GetComponent<RectTransform>();
                rtT.anchorMin = new Vector2(0f, 1f);
                rtT.anchorMax = new Vector2(1f, 1f);
                rtT.anchoredPosition = new Vector2(24f, -18f);
                rtT.sizeDelta = new Vector2(-60f, 26f);

                // Frequency Display Text
                GameObject freqObj = new GameObject("Text_Frequency");
                freqObj.transform.SetParent(radioPanel.transform, false);
                Text freqText = freqObj.AddComponent<Text>();
                freqText.font = defaultFont;
                freqText.fontSize = 34;
                freqText.fontStyle = FontStyle.Bold;
                freqText.color = new Color(0.2f, 0.98f, 0.5f); // Green digital readout
                freqText.alignment = TextAnchor.MiddleCenter;
                freqText.text = "88.0 MHz";
                RectTransform rtFreq = freqObj.GetComponent<RectTransform>();
                rtFreq.anchorMin = new Vector2(0.5f, 1f);
                rtFreq.anchorMax = new Vector2(0.5f, 1f);
                rtFreq.pivot = new Vector2(0.5f, 1f);
                rtFreq.anchoredPosition = new Vector2(0f, -55f);
                rtFreq.sizeDelta = new Vector2(260f, 44f);

                // Status Badge
                GameObject statusObj = new GameObject("Text_Status");
                statusObj.transform.SetParent(radioPanel.transform, false);
                Text statusText = statusObj.AddComponent<Text>();
                statusText.font = defaultFont;
                statusText.fontSize = 12;
                statusText.color = new Color(0.7f, 0.75f, 0.85f);
                statusText.alignment = TextAnchor.MiddleCenter;
                statusText.text = "STATİK PARAZİT";
                RectTransform rtStat = statusObj.GetComponent<RectTransform>();
                rtStat.anchorMin = new Vector2(0.5f, 1f);
                rtStat.anchorMax = new Vector2(0.5f, 1f);
                rtStat.pivot = new Vector2(0.5f, 1f);
                rtStat.anchoredPosition = new Vector2(0f, -100f);
                rtStat.sizeDelta = new Vector2(260f, 20f);

                // Tuning Slider & Step Buttons
                GameObject sliderObj = new GameObject("Slider_Frequency");
                sliderObj.transform.SetParent(radioPanel.transform, false);
                RectTransform rtSl = sliderObj.AddComponent<RectTransform>();
                rtSl.anchorMin = new Vector2(0.5f, 1f);
                rtSl.anchorMax = new Vector2(0.5f, 1f);
                rtSl.pivot = new Vector2(0.5f, 1f);
                rtSl.anchoredPosition = new Vector2(0f, -135f);
                rtSl.sizeDelta = new Vector2(360f, 28f);

                Slider freqSlider = sliderObj.AddComponent<Slider>();
                Image slBg = sliderObj.AddComponent<Image>();
                slBg.color = new Color(0.15f, 0.18f, 0.24f, 0.9f);

                // Fine Tune Down (-0.2)
                GameObject btnDownObj = new GameObject("Btn_TuneDown");
                btnDownObj.transform.SetParent(radioPanel.transform, false);
                RectTransform rtDown = btnDownObj.AddComponent<RectTransform>();
                rtDown.anchorMin = new Vector2(0.5f, 1f);
                rtDown.anchorMax = new Vector2(0.5f, 1f);
                rtDown.pivot = new Vector2(1f, 1f);
                rtDown.anchoredPosition = new Vector2(-190f, -132f);
                rtDown.sizeDelta = new Vector2(65f, 32f);
                Image imgDown = btnDownObj.AddComponent<Image>();
                imgDown.color = new Color(0.2f, 0.25f, 0.35f);
                Button btnDown = btnDownObj.AddComponent<Button>();
                btnDown.targetGraphic = imgDown;

                GameObject downTxtObj = new GameObject("Text");
                downTxtObj.transform.SetParent(btnDownObj.transform, false);
                Text downTxt = downTxtObj.AddComponent<Text>();
                downTxt.font = defaultFont;
                downTxt.fontSize = 11;
                downTxt.alignment = TextAnchor.MiddleCenter;
                downTxt.color = Color.white;
                downTxt.text = "-0.2 MHz";
                RectTransform rtdt = downTxtObj.GetComponent<RectTransform>();
                rtdt.anchorMin = Vector2.zero;
                rtdt.anchorMax = Vector2.one;
                rtdt.sizeDelta = Vector2.zero;

                // Fine Tune Up (+0.2)
                GameObject btnUpObj = new GameObject("Btn_TuneUp");
                btnUpObj.transform.SetParent(radioPanel.transform, false);
                RectTransform rtUp = btnUpObj.AddComponent<RectTransform>();
                rtUp.anchorMin = new Vector2(0.5f, 1f);
                rtUp.anchorMax = new Vector2(0.5f, 1f);
                rtUp.pivot = new Vector2(0f, 1f);
                rtUp.anchoredPosition = new Vector2(190f, -132f);
                rtUp.sizeDelta = new Vector2(65f, 32f);
                Image imgUp = btnUpObj.AddComponent<Image>();
                imgUp.color = new Color(0.2f, 0.25f, 0.35f);
                Button btnUp = btnUpObj.AddComponent<Button>();
                btnUp.targetGraphic = imgUp;

                GameObject upTxtObj = new GameObject("Text");
                upTxtObj.transform.SetParent(btnUpObj.transform, false);
                Text upTxt = upTxtObj.AddComponent<Text>();
                upTxt.font = defaultFont;
                upTxt.fontSize = 11;
                upTxt.alignment = TextAnchor.MiddleCenter;
                upTxt.color = Color.white;
                upTxt.text = "+0.2 MHz";
                RectTransform rtut = upTxtObj.GetComponent<RectTransform>();
                rtut.anchorMin = Vector2.zero;
                rtut.anchorMax = Vector2.one;
                rtut.sizeDelta = Vector2.zero;

                // Waveform Bars Container (12 bars)
                GameObject waveCont = new GameObject("Waveform_Container");
                waveCont.transform.SetParent(radioPanel.transform, false);
                RectTransform rtWave = waveCont.AddComponent<RectTransform>();
                rtWave.anchorMin = new Vector2(0.5f, 1f);
                rtWave.anchorMax = new Vector2(0.5f, 1f);
                rtWave.pivot = new Vector2(0.5f, 1f);
                rtWave.anchoredPosition = new Vector2(0f, -180f);
                rtWave.sizeDelta = new Vector2(320f, 38f);

                List<Image> waveBars = new List<Image>();
                for (int i = 0; i < 12; i++)
                {
                    GameObject barObj = new GameObject($"Bar_{i}");
                    barObj.transform.SetParent(waveCont.transform, false);
                    RectTransform rtB = barObj.AddComponent<RectTransform>();
                    rtB.anchorMin = new Vector2((float)i / 12f, 0.5f);
                    rtB.anchorMax = new Vector2((float)(i + 1) / 12f, 0.5f);
                    rtB.pivot = new Vector2(0.5f, 0.5f);
                    rtB.sizeDelta = new Vector2(-4f, 34f);
                    Image barImg = barObj.AddComponent<Image>();
                    barImg.color = new Color(0.3f, 0.8f, 0.5f, 0.8f);
                    waveBars.Add(barImg);
                }

                // Decode Button
                GameObject btnDecodeObj = new GameObject("Btn_DecodeSignal");
                btnDecodeObj.transform.SetParent(radioPanel.transform, false);
                RectTransform rtDec = btnDecodeObj.AddComponent<RectTransform>();
                rtDec.anchorMin = new Vector2(0.5f, 1f);
                rtDec.anchorMax = new Vector2(0.5f, 1f);
                rtDec.pivot = new Vector2(0.5f, 1f);
                rtDec.anchoredPosition = new Vector2(0f, -230f);
                rtDec.sizeDelta = new Vector2(260f, 40f);
                Image imgDec = btnDecodeObj.AddComponent<Image>();
                imgDec.color = new Color(0.15f, 0.65f, 0.35f, 0.95f);
                Button btnDec = btnDecodeObj.AddComponent<Button>();
                btnDec.targetGraphic = imgDec;

                GameObject decTxtObj = new GameObject("Text");
                decTxtObj.transform.SetParent(btnDecodeObj.transform, false);
                Text decTxt = decTxtObj.AddComponent<Text>();
                decTxt.font = defaultFont;
                decTxt.fontSize = 13;
                decTxt.fontStyle = FontStyle.Bold;
                decTxt.color = Color.white;
                decTxt.alignment = TextAnchor.MiddleCenter;
                decTxt.text = "SİNYALİ ÇÖZ";
                RectTransform rtdc = decTxtObj.GetComponent<RectTransform>();
                rtdc.anchorMin = Vector2.zero;
                rtdc.anchorMax = Vector2.one;
                rtdc.sizeDelta = Vector2.zero;

                // Transcript Box
                GameObject transBox = new GameObject("Box_Transcript");
                transBox.transform.SetParent(radioPanel.transform, false);
                RectTransform rtTb = transBox.AddComponent<RectTransform>();
                rtTb.anchorMin = new Vector2(0.5f, 0f);
                rtTb.anchorMax = new Vector2(0.5f, 0f);
                rtTb.pivot = new Vector2(0.5f, 0f);
                rtTb.anchoredPosition = new Vector2(0f, 20f);
                rtTb.sizeDelta = new Vector2(510f, 120f);
                Image tbImg = transBox.AddComponent<Image>();
                tbImg.color = new Color(0.04f, 0.06f, 0.08f, 0.95f);

                GameObject senderObj = new GameObject("Text_Sender");
                senderObj.transform.SetParent(transBox.transform, false);
                Text sndTxt = senderObj.AddComponent<Text>();
                sndTxt.font = defaultFont;
                sndTxt.fontSize = 11;
                sndTxt.fontStyle = FontStyle.Bold;
                sndTxt.color = new Color(0f, 0.92f, 1f);
                RectTransform rtSnd = senderObj.GetComponent<RectTransform>();
                rtSnd.anchorMin = new Vector2(0f, 1f);
                rtSnd.anchorMax = new Vector2(1f, 1f);
                rtSnd.anchoredPosition = new Vector2(12f, -8f);
                rtSnd.sizeDelta = new Vector2(-24f, 16f);

                GameObject bodyObj = new GameObject("Text_Body");
                bodyObj.transform.SetParent(transBox.transform, false);
                Text bdyTxt = bodyObj.AddComponent<Text>();
                bdyTxt.font = defaultFont;
                bdyTxt.fontSize = 11;
                bdyTxt.color = new Color(0.9f, 0.92f, 0.95f);
                RectTransform rtBdy = bodyObj.GetComponent<RectTransform>();
                rtBdy.anchorMin = Vector2.zero;
                rtBdy.anchorMax = Vector2.one;
                rtBdy.offsetMin = new Vector2(12f, 26f);
                rtBdy.offsetMax = new Vector2(-12f, -26f);

                GameObject locObj = new GameObject("Text_Location");
                locObj.transform.SetParent(transBox.transform, false);
                Text locTxt = locObj.AddComponent<Text>();
                locTxt.font = defaultFont;
                locTxt.fontSize = 10;
                locTxt.color = new Color(0.3f, 0.95f, 0.5f);
                RectTransform rtLoc = locObj.GetComponent<RectTransform>();
                rtLoc.anchorMin = new Vector2(0f, 0f);
                rtLoc.anchorMax = new Vector2(1f, 0f);
                rtLoc.anchoredPosition = new Vector2(12f, 6f);
                rtLoc.sizeDelta = new Vector2(-24f, 16f);

                RadioScannerUI scannerUI = radioPanel.AddComponent<RadioScannerUI>();
                scannerUI.BindUIReferences(radioPanel, btnClose, freqSlider, btnDown, btnUp,
                    freqText, statusText, imgDec, btnDec, decTxt, transBox, sndTxt, bdyTxt, locTxt, pillBtn);
                scannerUI.SetWaveformBars(waveBars);
            }

            // D. Build StoryChoiceUI Modal
            Transform existingChoice = hudCanvas.transform.Find("Panel_StoryChoice");
            if (existingChoice == null)
            {
                GameObject choicePanel = new GameObject("Panel_StoryChoice");
                choicePanel.transform.SetParent(hudCanvas.transform, false);
                RectTransform rtCp = choicePanel.AddComponent<RectTransform>();
                rtCp.anchorMin = new Vector2(0.5f, 0.5f);
                rtCp.anchorMax = new Vector2(0.5f, 0.5f);
                rtCp.pivot = new Vector2(0.5f, 0.5f);
                rtCp.anchoredPosition = Vector2.zero;
                rtCp.sizeDelta = new Vector2(640f, 440f);

                Image cpBg = choicePanel.AddComponent<Image>();
                cpBg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

                // Title
                GameObject cpTitleObj = new GameObject("Title");
                cpTitleObj.transform.SetParent(choicePanel.transform, false);
                Text cpTitle = cpTitleObj.AddComponent<Text>();
                cpTitle.font = defaultFont;
                cpTitle.fontSize = 16;
                cpTitle.fontStyle = FontStyle.Bold;
                cpTitle.color = new Color(1f, 0.82f, 0.28f);
                cpTitle.alignment = TextAnchor.MiddleCenter;
                cpTitle.text = "KUZEY OTOYOLU YAYIN KULESİ — NİHAİ KARAR";
                RectTransform rtCpt = cpTitleObj.GetComponent<RectTransform>();
                rtCpt.anchorMin = new Vector2(0f, 1f);
                rtCpt.anchorMax = new Vector2(1f, 1f);
                rtCpt.anchoredPosition = new Vector2(0f, -20f);
                rtCpt.sizeDelta = new Vector2(0f, 30f);

                // Close Button
                GameObject cpCloseObj = new GameObject("Btn_CloseChoice");
                cpCloseObj.transform.SetParent(choicePanel.transform, false);
                RectTransform rtCpc = cpCloseObj.AddComponent<RectTransform>();
                rtCpc.anchorMin = new Vector2(1f, 1f);
                rtCpc.anchorMax = new Vector2(1f, 1f);
                rtCpc.pivot = new Vector2(1f, 1f);
                rtCpc.anchoredPosition = new Vector2(-12f, -12f);
                rtCpc.sizeDelta = new Vector2(30f, 30f);
                Image imgCpc = cpCloseObj.AddComponent<Image>();
                imgCpc.color = new Color(0.85f, 0.2f, 0.25f);
                Button btnCpc = cpCloseObj.AddComponent<Button>();
                btnCpc.targetGraphic = imgCpc;

                // Choice Card A: Tahliye Koordinatları
                GameObject cardA = new GameObject("Card_ChoiceA");
                cardA.transform.SetParent(choicePanel.transform, false);
                RectTransform rtCa = cardA.AddComponent<RectTransform>();
                rtCa.anchorMin = new Vector2(0f, 0.5f);
                rtCa.anchorMax = new Vector2(0.5f, 0.5f);
                rtCa.anchoredPosition = new Vector2(150f, -20f);
                rtCa.sizeDelta = new Vector2(280f, 320f);
                Image imgCa = cardA.AddComponent<Image>();
                imgCa.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

                GameObject titleAObj = new GameObject("Title");
                titleAObj.transform.SetParent(cardA.transform, false);
                Text titleATxt = titleAObj.AddComponent<Text>();
                titleATxt.font = defaultFont;
                titleATxt.fontSize = 13;
                titleATxt.fontStyle = FontStyle.Bold;
                titleATxt.color = new Color(0.2f, 0.95f, 0.45f);
                titleATxt.text = "SEÇENEK A:\nTAHLİYE KOORDİNATLARI";
                titleATxt.alignment = TextAnchor.MiddleCenter;
                RectTransform rtTa = titleAObj.GetComponent<RectTransform>();
                rtTa.anchorMin = new Vector2(0f, 1f);
                rtTa.anchorMax = new Vector2(1f, 1f);
                rtTa.anchoredPosition = new Vector2(0f, -25f);
                rtTa.sizeDelta = new Vector2(0f, 40f);

                GameObject descAObj = new GameObject("Desc");
                descAObj.transform.SetParent(cardA.transform, false);
                Text descATxt = descAObj.AddComponent<Text>();
                descATxt.font = defaultFont;
                descATxt.fontSize = 11;
                descATxt.color = Color.white;
                descATxt.text = "Bölgedeki tüm insanlara güvenli koridor koordinatlarını ilet.\n\n• +40 Topluluk Morali\n• Dr. Aras, Kerem, Elif ve Zeynep Telsiz Kutlamaları\n• +500 XP\n• Umut Dolu Şafak Işığı";
                RectTransform rtDa = descAObj.GetComponent<RectTransform>();
                rtDa.anchorMin = new Vector2(0f, 0f);
                rtDa.anchorMax = new Vector2(1f, 1f);
                rtDa.offsetMin = new Vector2(14f, 65f);
                rtDa.offsetMax = new Vector2(-14f, -70f);

                GameObject btnAObj = new GameObject("Btn_SelectA");
                btnAObj.transform.SetParent(cardA.transform, false);
                RectTransform rtBa = btnAObj.AddComponent<RectTransform>();
                rtBa.anchorMin = new Vector2(0.5f, 0f);
                rtBa.anchorMax = new Vector2(0.5f, 0f);
                rtBa.pivot = new Vector2(0.5f, 0f);
                rtBa.anchoredPosition = new Vector2(0f, 16f);
                rtBa.sizeDelta = new Vector2(240f, 40f);
                Image imgBa = btnAObj.AddComponent<Image>();
                imgBa.color = new Color(0.15f, 0.65f, 0.35f);
                Button btnA = btnAObj.AddComponent<Button>();
                btnA.targetGraphic = imgBa;

                GameObject txtBaObj = new GameObject("Text");
                txtBaObj.transform.SetParent(btnAObj.transform, false);
                Text txtBa = txtBaObj.AddComponent<Text>();
                txtBa.font = defaultFont;
                txtBa.fontSize = 12;
                txtBa.fontStyle = FontStyle.Bold;
                txtBa.color = Color.white;
                txtBa.alignment = TextAnchor.MiddleCenter;
                txtBa.text = "SEÇ VE YAYINLA";
                RectTransform rtTba = txtBaObj.GetComponent<RectTransform>();
                rtTba.anchorMin = Vector2.zero;
                rtTba.anchorMax = Vector2.one;
                rtTba.sizeDelta = Vector2.zero;

                // Choice Card B: Küresel Antiviral Formülü
                GameObject cardB = new GameObject("Card_ChoiceB");
                cardB.transform.SetParent(choicePanel.transform, false);
                RectTransform rtCb = cardB.AddComponent<RectTransform>();
                rtCb.anchorMin = new Vector2(0.5f, 0.5f);
                rtCb.anchorMax = new Vector2(1f, 0.5f);
                rtCb.anchoredPosition = new Vector2(170f, -20f);
                rtCb.sizeDelta = new Vector2(280f, 320f);
                Image imgCb = cardB.AddComponent<Image>();
                imgCb.color = new Color(0.18f, 0.14f, 0.24f, 0.95f);

                GameObject titleBObj = new GameObject("Title");
                titleBObj.transform.SetParent(cardB.transform, false);
                Text titleBTxt = titleBObj.AddComponent<Text>();
                titleBTxt.font = defaultFont;
                titleBTxt.fontSize = 13;
                titleBTxt.fontStyle = FontStyle.Bold;
                titleBTxt.color = new Color(0f, 0.92f, 1f);
                titleBTxt.text = "SEÇENEK B:\nKÜRESEL ANTİVİRAL";
                titleBTxt.alignment = TextAnchor.MiddleCenter;
                RectTransform rtTb = titleBObj.GetComponent<RectTransform>();
                rtTb.anchorMin = new Vector2(0f, 1f);
                rtTb.anchorMax = new Vector2(1f, 1f);
                rtTb.anchoredPosition = new Vector2(0f, -25f);
                rtTb.sizeDelta = new Vector2(0f, 40f);

                GameObject descBObj = new GameObject("Desc");
                descBObj.transform.SetParent(cardB.transform, false);
                Text descBTxt = descBObj.AddComponent<Text>();
                descBTxt.font = defaultFont;
                descBTxt.fontSize = 11;
                descBTxt.color = Color.white;
                descBTxt.text = "Dr. Kaya'nın antiviral patojen sentezini küresel ağa aktar.\n\n• +35 Topluluk Morali\n• Kalıcı -%50 Enfeksiyon Direnci\n• +500 XP\n• Küresel Tedavi Protokolü";
                RectTransform rtDb = descBObj.GetComponent<RectTransform>();
                rtDb.anchorMin = new Vector2(0f, 0f);
                rtDb.anchorMax = new Vector2(1f, 1f);
                rtDb.offsetMin = new Vector2(14f, 65f);
                rtDb.offsetMax = new Vector2(-14f, -70f);

                GameObject btnBObj = new GameObject("Btn_SelectB");
                btnBObj.transform.SetParent(cardB.transform, false);
                RectTransform rtBb = btnBObj.AddComponent<RectTransform>();
                rtBb.anchorMin = new Vector2(0.5f, 0f);
                rtBb.anchorMax = new Vector2(0.5f, 0f);
                rtBb.pivot = new Vector2(0.5f, 0f);
                rtBb.anchoredPosition = new Vector2(0f, 16f);
                rtBb.sizeDelta = new Vector2(240f, 40f);
                Image imgBb = btnBObj.AddComponent<Image>();
                imgBb.color = new Color(0.2f, 0.5f, 0.85f);
                Button btnB = btnBObj.AddComponent<Button>();
                btnB.targetGraphic = imgBb;

                GameObject txtBbObj = new GameObject("Text");
                txtBbObj.transform.SetParent(btnBObj.transform, false);
                Text txtBb = txtBbObj.AddComponent<Text>();
                txtBb.font = defaultFont;
                txtBb.fontSize = 12;
                txtBb.fontStyle = FontStyle.Bold;
                txtBb.color = Color.white;
                txtBb.alignment = TextAnchor.MiddleCenter;
                txtBb.text = "SEÇ VE AKTAR";
                RectTransform rtTbb = txtBbObj.GetComponent<RectTransform>();
                rtTbb.anchorMin = Vector2.zero;
                rtTbb.anchorMax = Vector2.one;
                rtTbb.sizeDelta = Vector2.zero;

                StoryChoiceUI choiceUI = choicePanel.AddComponent<StoryChoiceUI>();
                choiceUI.BindUIReferences(choicePanel, btnCpc, btnA, titleATxt, descATxt, btnB, titleBTxt, descBTxt);
            }
        }
#endif
    }
}
