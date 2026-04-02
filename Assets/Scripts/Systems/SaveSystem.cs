using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class SaveSystem
{
    const int CurrentVersion = 1;
    static string SavePath => Path.Combine(Application.persistentDataPath, "rpg_save.json");
    static string BackupPath => SavePath + ".bak";

    public static void Save(SaveData data)
    {
        data.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var envelope = new JObject
        {
            ["version"] = CurrentVersion,
            ["data"] = JObject.FromObject(data)
        };
        File.WriteAllText(SavePath, envelope.ToString(Formatting.Indented));
        Debug.Log($"[SaveSystem] Saved v{CurrentVersion} to {SavePath}");
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath)) return null;
        try
        {
            string json = File.ReadAllText(SavePath);
            var root = JObject.Parse(json);

            int version = root["version"]?.Value<int>() ?? 0;
            JObject data;

            if (version == 0)
            {
                // Legacy save: entire JSON is the SaveData (no envelope)
                data = root;
            }
            else
            {
                data = (JObject)root["data"];
                if (data == null)
                {
                    Debug.LogError("[SaveSystem] Save envelope has no data field");
                    return null;
                }
            }

            if (version < CurrentVersion)
            {
                Debug.Log($"[SaveSystem] Migrating save from v{version} to v{CurrentVersion}");
                CreateBackup(json);
                data = SaveMigrations.Apply(version, CurrentVersion, data);
            }

            var result = data.ToObject<SaveData>();
            if (result == null)
            {
                Debug.LogError("[SaveSystem] Save data deserialized to null");
                return null;
            }
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to load save: {e.Message}");
            CreateBackup(null);
            return null;
        }
    }

    static void CreateBackup(string json)
    {
        try
        {
            if (json != null)
                File.WriteAllText(BackupPath, json);
            else if (File.Exists(SavePath))
                File.Copy(SavePath, BackupPath, true);
            Debug.Log($"[SaveSystem] Backup created at {BackupPath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Failed to create backup: {e.Message}");
        }
    }

    public static bool HasSave() => File.Exists(SavePath);

    public static void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }
}

public static class SaveMigrations
{
    static readonly Dictionary<int, Func<JObject, JObject>> _migrations = new()
    {
        { 0, MigrateV0ToV1 },
    };

    public static JObject Apply(int fromVersion, int toVersion, JObject data)
    {
        for (int v = fromVersion; v < toVersion; v++)
        {
            if (_migrations.TryGetValue(v, out var migrate))
            {
                data = migrate(data);
                Debug.Log($"[SaveMigrations] Applied v{v} → v{v + 1}");
            }
            else
            {
                Debug.LogWarning($"[SaveMigrations] No migration for v{v} → v{v + 1}");
            }
        }
        return data;
    }

    static JObject MigrateV0ToV1(JObject data)
    {
        // V0 → V1: version system introduction, no schema changes needed
        return data;
    }
}
