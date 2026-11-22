using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

namespace ProjectSurvival.EditorTools.TripoAI
{
    public static class TripoApiClient
    {
        public const string ApiKeyPrefKey = "TripoAI_ApiKey";
        public const string BaseUrlPrefKey = "TripoAI_BaseUrl";
        public const string DefaultBaseUrl = "https://openapi.tripo3d.ai/v2/openapi";
        public const string DefaultApiKey = "";

        public static string GetApiKey()
        {
            string key = EditorPrefs.GetString(ApiKeyPrefKey, string.Empty);
            if (string.IsNullOrEmpty(key))
            {
                key = DefaultApiKey;
                EditorPrefs.SetString(ApiKeyPrefKey, key);
            }
            return key;
        }

        public static void SetApiKey(string key)
        {
            EditorPrefs.SetString(ApiKeyPrefKey, key?.Trim() ?? string.Empty);
        }

        public static string GetBaseUrl()
        {
            return EditorPrefs.GetString(BaseUrlPrefKey, DefaultBaseUrl);
        }

        public static void SetBaseUrl(string url)
        {
            EditorPrefs.SetString(BaseUrlPrefKey, url?.Trim() ?? DefaultBaseUrl);
        }

        /// <summary>
        /// Orchestrates text-to-model creation, polling, optional auto-rigging, and GLB download.
        /// </summary>
        public static async Task<string> GenerateAndDownloadModelAsync(
            string prompt,
            int faceLimit,
            bool autoRig,
            string outputFileName,
            Action<string, float> onProgress,
            CancellationToken cancellationToken = default)
        {
            string apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Tripo AI API Anahtarı bulunamadı. Lütfen stüdyo penceresinden geçerli bir API anahtarı girin.");
            }

            if (apiKey.StartsWith("tcli_"))
            {
                throw new InvalidOperationException("Girdiğiniz anahtar bir Client ID'dir ('tcli_...'). Tripo API model üretimi için 'tsk_' ile başlayan Secret Key gerektirir. Lütfen platform.tripo3d.ai üzerinden 'tsk_' anahtarınızı kopyalayın.");
            }

            string baseUrl = GetBaseUrl().TrimEnd('/');

            // 1. Text to Model Task
            onProgress?.Invoke("Model üretim görevi başlatılıyor...", 0.05f);
            var textReq = new TripoTextToModelRequest
            {
                type = "text_to_model",
                prompt = prompt,
                face_limit = Mathf.Clamp(faceLimit, 1000, 50000),
                texture = true,
                pbr = true
            };

            string taskId = await CreateTaskAsync(baseUrl, apiKey, JsonUtility.ToJson(textReq), cancellationToken);
            if (string.IsNullOrEmpty(taskId))
            {
                throw new Exception("Tripo AI görev kimliği (Task ID) oluşturulamadı.");
            }

            // 2. Poll Model Generation
            onProgress?.Invoke($"Model üretiliyor (Task: {taskId.Substring(0, Mathf.Min(8, taskId.Length))}...)...", 0.15f);
            TripoTaskData modelTask = await PollTaskStatusAsync(baseUrl, apiKey, taskId, (status, p) =>
            {
                float normalizedProgress = 0.15f + (p / 100f) * 0.45f; // 15% -> 60%
                onProgress?.Invoke($"Model işleniyor (%{p}): {status}", normalizedProgress);
            }, cancellationToken);

            string finalModelUrl = modelTask.output?.GetBestModelUrl();

            // 3. Optional Auto-Rigging
            if (autoRig)
            {
                onProgress?.Invoke("Karakter otomatik iskeletlendirme (Auto-Rig) başlatılıyor...", 0.65f);
                var rigReq = new TripoRigRequest
                {
                    type = "animate_rig",
                    original_model_task_id = taskId
                };

                try
                {
                    string rigTaskId = await CreateTaskAsync(baseUrl, apiKey, JsonUtility.ToJson(rigReq), cancellationToken);
                    if (!string.IsNullOrEmpty(rigTaskId))
                    {
                        TripoTaskData rigTask = await PollTaskStatusAsync(baseUrl, apiKey, rigTaskId, (status, p) =>
                        {
                            float normalizedProgress = 0.65f + (p / 100f) * 0.20f; // 65% -> 85%
                            onProgress?.Invoke($"İskelet ekleniyor (%{p}): {status}", normalizedProgress);
                        }, cancellationToken);

                        string riggedUrl = rigTask.output?.GetBestModelUrl();
                        if (!string.IsNullOrEmpty(riggedUrl))
                        {
                            finalModelUrl = riggedUrl;
                        }
                    }
                }
                catch (Exception rigEx)
                {
                    Debug.LogWarning($"[TripoApiClient] Auto-rig işlemi tamamlanamadı, ham model kullanılacak: {rigEx.Message}");
                }
            }

            if (string.IsNullOrEmpty(finalModelUrl))
            {
                throw new Exception("Üretilen 3D modelin indirme bağlantısı (GLB URL) bulunamadı.");
            }

            // 4. Download Model File
            onProgress?.Invoke("3D Model (.glb) indiriliyor...", 0.90f);
            string destinationFolder = "Assets/_Project/Models/TripoGenerated";
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            string sanitizedName = SanitizeFileName(outputFileName);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{sanitizedName}_{timestamp}.glb";
            string localFilePath = Path.Combine(destinationFolder, fileName);

            await DownloadFileAsync(finalModelUrl, localFilePath, (p) =>
            {
                float normalizedProgress = 0.90f + (p / 100f) * 0.08f;
                onProgress?.Invoke($"İndiriliyor: %{p:0}", normalizedProgress);
            }, cancellationToken);

            // 5. Refresh Unity Database
            onProgress?.Invoke("Unity varlık veritabanı yenileniyor...", 0.99f);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(localFilePath, ImportAssetOptions.ForceUpdate);

            onProgress?.Invoke("Model başarıyla indirildi ve içe aktarıldı!", 1.0f);
            return localFilePath;
        }

        private static async Task<string> CreateTaskAsync(string baseUrl, string apiKey, string jsonPayload, CancellationToken ct)
        {
            string url = $"{baseUrl}/task";
            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                await SendWebRequestAsync(request, ct);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Görev oluşturulamadı ({request.responseCode}): {request.error}\nYanıt: {request.downloadHandler.text}");
                }

                string responseText = request.downloadHandler.text;
                var res = JsonUtility.FromJson<TripoTaskResponse>(responseText);
                if (res != null && res.data != null && !string.IsNullOrEmpty(res.data.task_id))
                {
                    return res.data.task_id;
                }

                throw new Exception($"Geçersiz Tripo API yanıtı: {responseText}");
            }
        }

        private static async Task<TripoTaskData> PollTaskStatusAsync(
            string baseUrl,
            string apiKey,
            string taskId,
            Action<string, int> onPollUpdate,
            CancellationToken ct)
        {
            string url = $"{baseUrl}/task/{taskId}";
            int pollIntervalMs = 1500;
            int timeoutSeconds = 600; // 10 minutes max
            DateTime startTime = DateTime.UtcNow;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                if ((DateTime.UtcNow - startTime).TotalSeconds > timeoutSeconds)
                {
                    throw new TimeoutException("Model üretim işlemi zaman aşımına uğradı (10 dakika).");
                }

                using (var request = UnityWebRequest.Get(url))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    await SendWebRequestAsync(request, ct);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string json = request.downloadHandler.text;
                        var res = JsonUtility.FromJson<TripoTaskResponse>(json);
                        if (res != null && res.data != null)
                        {
                            string status = res.data.status?.ToLowerInvariant();
                            int progress = res.data.progress;

                            onPollUpdate?.Invoke(status, progress);

                            if (status == "success")
                            {
                                return res.data;
                            }
                            if (status == "failed" || status == "cancelled")
                            {
                                throw new Exception($"Tripo AI görev başarısız oldu: durum = {status}, mesaj = {res.message}");
                            }
                        }
                    }
                }

                await Task.Delay(pollIntervalMs, ct);
            }
        }

        private static async Task DownloadFileAsync(string url, string localPath, Action<float> onProgress, CancellationToken ct)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    ct.ThrowIfCancellationRequested();
                    onProgress?.Invoke(operation.progress * 100f);
                    await Task.Delay(100, ct);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Model dosyası indirilemedi: {request.error}");
                }

                File.WriteAllBytes(localPath, request.downloadHandler.data);
            }
        }

        private static Task SendWebRequestAsync(UnityWebRequest request, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();
            var operation = request.SendWebRequest();

            operation.completed += _ =>
            {
                if (ct.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(ct);
                }
                else
                {
                    tcs.TrySetResult(true);
                }
            };

            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
                    request.Abort();
                    tcs.TrySetCanceled(ct);
                });
            }

            return tcs.Task;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "TripoModel";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Replace(' ', '_');
        }
    }
}
