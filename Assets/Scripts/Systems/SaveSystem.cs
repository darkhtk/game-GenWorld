using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveSystem
{
    static string SavePath => Path.Combine(Application.persistentDataPath, "rpg_save.json");

    public static void Save(SaveData data)
    {
        data.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveSystem] Saved to {SavePath}");
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath)) return null;
        try
        {
            string json = File.ReadAllText(SavePath);
            var data = JsonConvert.DeserializeObject<SaveData>(json);
            if (data == null)
            {
                Debug.LogError("[SaveSystem] Save data deserialized to null");
                return null;
            }
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to load save: {e.Message}");
            return null;
        }
    }

    public static bool HasSave() => File.Exists(SavePath);

    public static void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }
}
