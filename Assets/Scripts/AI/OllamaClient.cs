using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaClient
{
    readonly string _url, _fastModel, _largeModel;
    const float DialogueTimeout = 30f;
    const float AvailabilityTimeout = 3f;

    public OllamaClient(string url = "http://localhost:11434",
        string fastModel = "gemma3:4b", string largeModel = "gemma3:12b")
    {
        _url = url;
        _fastModel = fastModel;
        _largeModel = largeModel;
    }

    public async Task<bool> CheckAvailability()
    {
        try
        {
            string endpoint = $"{_url}/api/tags";
            using var req = UnityWebRequest.Get(endpoint);
            req.timeout = (int)AvailabilityTimeout;
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("[OllamaClient] Ollama not available");
                return false;
            }

            Debug.Log("[OllamaClient] Ollama available, warming model...");
            await WarmModel();
            return true;
        }
        catch (Exception e)
        {
            Debug.Log($"[OllamaClient] Availability check failed: {e.Message}");
            return false;
        }
    }

    async Task WarmModel()
    {
        try
        {
            var body = JsonBody(new { model = _fastModel, prompt = "hello", stream = false });
            string endpoint = $"{_url}/api/generate";
            using var req = new UnityWebRequest(endpoint, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 10;
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
        }
        catch { /* warm-up failure is non-critical */ }
    }

    public async Task<string> GenerateDialogue(string prompt)
    {
        try
        {
            var payload = new OllamaRequest
            {
                model = _fastModel,
                prompt = prompt,
                stream = false,
                format = "json",
                options = new OllamaOptions
                {
                    temperature = 0.8f,
                    top_p = 0.9f,
                    repeat_penalty = 1.3f
                }
            };

            string body = JsonUtility.ToJson(payload);
            string endpoint = $"{_url}/api/generate";

            using var req = new UnityWebRequest(endpoint, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = (int)DialogueTimeout;

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[OllamaClient] Request failed: {req.error}");
                return null;
            }

            string responseText = req.downloadHandler.text;
            var response = JsonUtility.FromJson<OllamaResponse>(responseText);
            return response?.response;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OllamaClient] GenerateDialogue error: {e.Message}");
            return null;
        }
    }

    static string JsonBody(object obj) => JsonUtility.ToJson(obj);

    [Serializable]
    class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
        public string format;
        public OllamaOptions options;
    }

    [Serializable]
    class OllamaOptions
    {
        public float temperature;
        public float top_p;
        public float repeat_penalty;
    }

    [Serializable]
    class OllamaResponse
    {
        public string response;
    }
}
