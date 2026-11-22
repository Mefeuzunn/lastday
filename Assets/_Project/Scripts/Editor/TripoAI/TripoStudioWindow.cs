using System;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEditor;

namespace ProjectSurvival.EditorTools.TripoAI
{
    public class TripoStudioWindow : EditorWindow
    {
        private string apiKey = string.Empty;
        private string promptText = "post-apocalyptic survivor wearing backpack and jacket, stylized low poly mobile game character, T-pose, clean topology";
        private int faceLimit = 10000;
        private bool autoRig = true;
        private float targetHeight = 1.8f;
        private GameObject targetPrefab = null;
        private bool autoDressPrefab = true;
        private bool spawnInScene = false;

        private bool isProcessing = false;
        private float currentProgress = 0f;
        private string statusMessage = "Hazır.";
        private CancellationTokenSource cts;

        private Vector2 scrollPos;
        private bool showAdvancedSettings = false;
        private bool showLocalModels = true;
        private string lastDownloadedPath = string.Empty;

        // Presets definitions
        private readonly TripoPreset[] presets = new TripoPreset[]
        {
            new TripoPreset("Survivor (Oyuncu)",
                "post-apocalyptic survivor wearing backpack and jacket, stylized low poly mobile game character, T-pose, clean topology",
                "Assets/_Project/Prefabs/Player/Player_Root.prefab", true, 10000, 1.8f),

            new TripoPreset("Standart Zombi",
                "stylized zombie in torn clothes, pale green skin, low poly mobile game character, T-pose",
                "Assets/_Project/Prefabs/Zombies/Zombie_Standard_Root.prefab", true, 8000, 1.8f),

            new TripoPreset("Koşucu Zombi",
                "thin agile fast zombie runner, reddish skin, tattered shorts, low poly mobile game character, T-pose",
                "Assets/_Project/Prefabs/Zombies/Zombie_Runner_Root.prefab", true, 8000, 1.75f),

            new TripoPreset("Çığırtkan Zombi",
                "mutated screamer zombie with glowing purple veins, open mouth, low poly mobile game character, T-pose",
                "Assets/_Project/Prefabs/Zombies/Zombie_Screamer_Root.prefab", true, 8000, 1.8f),

            new TripoPreset("Tank Zombi",
                "massive muscular heavy brute zombie, dark armored skin, low poly mobile game character, T-pose",
                "Assets/_Project/Prefabs/Zombies/Zombie_Tank_Root.prefab", true, 12000, 2.2f),

            new TripoPreset("Ganimet Sandığı",
                "post-apocalyptic wooden and metal supply loot crate, stylized low poly game prop",
                "Assets/_Project/Prefabs/Environment/LootContainer_Root.prefab", false, 4000, 1.0f)
        };

        [MenuItem("Tools/Tripo AI/3D Model Stüdyosu", false, 10)]
        [MenuItem("ProjectSurvival/Tripo AI 3D Studio", false, 20)]
        public static void OpenWindow()
        {
            var win = GetWindow<TripoStudioWindow>("Tripo 3D Studio");
            win.minSize = new Vector2(460, 680);
            win.Show();
        }

        private void OnEnable()
        {
            apiKey = TripoApiClient.GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = TripoApiClient.DefaultApiKey;
                TripoApiClient.SetApiKey(apiKey);
            }

            // Auto-load Survivor prefab as default target
            if (targetPrefab == null)
            {
                targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player_Root.prefab");
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawHeader();
            EditorGUILayout.Space(8);

            DrawApiSection();
            EditorGUILayout.Space(10);

            DrawPresetButtons();
            EditorGUILayout.Space(10);

            DrawPromptAndConfigSection();
            EditorGUILayout.Space(10);

            DrawTargetSection();
            EditorGUILayout.Space(12);

            DrawActionSection();
            EditorGUILayout.Space(12);

            DrawLocalModelsSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(4);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("TRIPO AI 3D MODEL & PREFAB GİYDİRME STÜDYOSU", titleStyle);

            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            GUILayout.Label("Unity 6 (URP) - Mobil Uyumlu Model Üretimi ve Otomatik Visual_Model Değişimi", subtitleStyle);
            GUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        private void DrawApiSection()
        {
            EditorGUILayout.LabelField("1. Tripo AI Kimlik Doğrulama", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("API Anahtarı");
            apiKey = EditorGUILayout.PasswordField(apiKey);
            if (GUILayout.Button("Kaydet", GUILayout.Width(70)))
            {
                TripoApiClient.SetApiKey(apiKey);
                ShowNotification(new GUIContent("API Anahtarı Kaydedildi!"));
            }
            EditorGUILayout.EndHorizontal();

            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Gelişmiş API Ayarları");
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                string baseUrl = EditorGUILayout.TextField("API Base URL", TripoApiClient.GetBaseUrl());
                if (baseUrl != TripoApiClient.GetBaseUrl())
                {
                    TripoApiClient.SetBaseUrl(baseUrl);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPresetButtons()
        {
            EditorGUILayout.LabelField("2. Hazır Oyun Varlığı Şablonları (Tek Tıkla Yükle)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            int columns = 2;
            for (int i = 0; i < presets.Length; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns && (i + c) < presets.Length; c++)
                {
                    var p = presets[i + c];
                    if (GUILayout.Button(p.Title, GUILayout.Height(28)))
                    {
                        ApplyPreset(p);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplyPreset(TripoPreset p)
        {
            promptText = p.Prompt;
            autoRig = p.AutoRig;
            faceLimit = p.FaceLimit;
            targetHeight = p.TargetHeight;

            if (!string.IsNullOrEmpty(p.DefaultPrefabPath))
            {
                targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(p.DefaultPrefabPath);
            }

            autoDressPrefab = targetPrefab != null;
            spawnInScene = false;
            GUI.FocusControl(null);
            ShowNotification(new GUIContent($"'{p.Title}' şablonu yüklendi!"));
        }

        private void DrawPromptAndConfigSection()
        {
            EditorGUILayout.LabelField("3. Üretim Parametreleri ve Prompt", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("İngilizce Açıklama (Prompt):");
            promptText = EditorGUILayout.TextArea(promptText, GUILayout.Height(55));

            EditorGUILayout.Space(4);
            faceLimit = EditorGUILayout.IntSlider("Poligon Limiti (Face Limit)", faceLimit, 2000, 30000);

            autoRig = EditorGUILayout.Toggle(new GUIContent("Karakter İskeleti Ekle (Auto-Rig)",
                "İşaretliyse model üretildikten sonra otomatik iskeletlendirilir."), autoRig);

            targetHeight = EditorGUILayout.FloatField("Hedef Model Yüksekliği (m)", targetHeight);

            EditorGUILayout.EndVertical();
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("4. Hedef Prefab ve Yerleşim", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            targetPrefab = (GameObject)EditorGUILayout.ObjectField("Hedef Prefab", targetPrefab, typeof(GameObject), false);

            autoDressPrefab = EditorGUILayout.Toggle(new GUIContent("Visual_Model Otomatik Değiştir",
                "İndirilen 3D model hedef prefabın Visual_Model alt objesine yerleştirilir ve eski geçici mesh temizlenir."), autoDressPrefab);

            spawnInScene = EditorGUILayout.Toggle(new GUIContent("Doğrudan Sahneye Yerleştir",
                "Model üretildikten sonra Scene görünümündeki kameranın önüne fiziksel obje olarak yerleştirilir."), spawnInScene);

            EditorGUILayout.EndVertical();
        }

        private void DrawActionSection()
        {
            EditorGUILayout.BeginVertical("box");

            if (isProcessing)
            {
                EditorGUILayout.LabelField("İşlem Durumu:", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), currentProgress, $"%{Mathf.RoundToInt(currentProgress * 100)}");

                EditorGUILayout.Space(6);
                if (GUILayout.Button("İşlemi İptal Et", GUILayout.Height(30)))
                {
                    cts?.Cancel();
                    isProcessing = false;
                    statusMessage = "İşlem kullanıcı tarafından iptal edildi.";
                }
            }
            else
            {
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.2f, 0.75f, 0.4f);
                if (GUILayout.Button("🚀 3D MODELİ ÜRET VE ENTEGRE ET", GUILayout.Height(40)))
                {
                    StartGeneration();
                }
                GUI.backgroundColor = prevBg;

                if (!string.IsNullOrEmpty(statusMessage))
                {
                    EditorGUILayout.HelpBox(statusMessage, MessageType.None);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLocalModelsSection()
        {
            showLocalModels = EditorGUILayout.Foldout(showLocalModels, "Daha Önce İndirilen Modeller (Assets/_Project/Models/TripoGenerated/)");
            if (!showLocalModels) return;

            EditorGUILayout.BeginVertical("box");
            string modelsFolder = "Assets/_Project/Models/TripoGenerated";

            if (Directory.Exists(modelsFolder))
            {
                string[] glbFiles = Directory.GetFiles(modelsFolder, "*.glb");
                if (glbFiles.Length == 0)
                {
                    EditorGUILayout.LabelField("Henüz indirilmiş .glb modeli bulunmuyor.", EditorStyles.miniLabel);
                }
                else
                {
                    foreach (string filePath in glbFiles)
                    {
                        string fileName = Path.GetFileName(filePath);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(fileName, EditorStyles.wordWrappedMiniLabel);

                        if (GUILayout.Button("Prefaba Giydir", GUILayout.Width(100)))
                        {
                            ApplyLocalModelToTarget(filePath);
                        }

                        if (GUILayout.Button("Sahneye Ekle", GUILayout.Width(85)))
                        {
                            var glbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(filePath);
                            if (glbAsset != null)
                            {
                                TripoModelDresser.SpawnInSceneView(glbAsset, targetHeight);
                            }
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("Klasör henüz mevcut değil.", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private async void StartGeneration()
        {
            if (string.IsNullOrEmpty(promptText))
            {
                EditorUtility.DisplayDialog("Hata", "Lütfen bir prompt (açıklama) girin.", "Tamam");
                return;
            }

            TripoApiClient.SetApiKey(apiKey);
            isProcessing = true;
            currentProgress = 0f;
            statusMessage = "İşlem başlatılıyor...";
            cts = new CancellationTokenSource();

            string outputName = targetPrefab != null ? targetPrefab.name : "Tripo_Model";

            try
            {
                string downloadedGlbPath = await TripoApiClient.GenerateAndDownloadModelAsync(
                    promptText,
                    faceLimit,
                    autoRig,
                    outputName,
                    (msg, p) =>
                    {
                        statusMessage = msg;
                        currentProgress = p;
                        Repaint();
                    },
                    cts.Token
                );

                lastDownloadedPath = downloadedGlbPath;
                statusMessage = $"Model indirildi: {downloadedGlbPath}";

                var glbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(downloadedGlbPath);
                if (glbAsset != null)
                {
                    if (autoDressPrefab && targetPrefab != null)
                    {
                        string prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
                        bool dressed = TripoModelDresser.DressPrefab(prefabPath, glbAsset, targetHeight);
                        if (dressed)
                        {
                            statusMessage += "\n✓ Hedef Prefab (Visual_Model) başarıyla güncellendi!";
                        }
                    }

                    if (spawnInScene)
                    {
                        GameObject spawned = TripoModelDresser.SpawnInSceneView(glbAsset, targetHeight);
                        if (spawned != null)
                        {
                            statusMessage += "\n✓ Sahneye fiziksel obje eklendi!";
                        }
                    }
                }
                else
                {
                    statusMessage += "\nUyarı: .glb modeli yüklendi ancak GameObject olarak açılamadı. glTFast paketinin kurulu olduğunu doğrulayın.";
                }

                EditorUtility.DisplayDialog("Başarılı", "Tripo 3D model üretimi ve entegrasyonu tamamlandı!\n\n" + statusMessage, "Tamam");
            }
            catch (OperationCanceledException)
            {
                statusMessage = "İşlem iptal edildi.";
            }
            catch (Exception ex)
            {
                statusMessage = $"HATA: {ex.Message}";
                Debug.LogError($"[TripoStudioWindow] Hata: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Tripo AI Hatası", ex.Message, "Tamam");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }

        private void ApplyLocalModelToTarget(string glbPath)
        {
            if (targetPrefab == null)
            {
                EditorUtility.DisplayDialog("Uyarı", "Lütfen önce yukarıdaki 'Hedef Prefab' alanından giydirilecek prefabı seçin.", "Tamam");
                return;
            }

            var glbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
            if (glbAsset == null)
            {
                EditorUtility.DisplayDialog("Hata", "Model dosyası yüklenemedi: " + glbPath, "Tamam");
                return;
            }

            string prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
            bool success = TripoModelDresser.DressPrefab(prefabPath, glbAsset, targetHeight);
            if (success)
            {
                EditorUtility.DisplayDialog("Başarılı", $"'{targetPrefab.name}' prefabı '{Path.GetFileName(glbPath)}' modeliyle güncellendi!", "Tamam");
            }
        }
    }
}
