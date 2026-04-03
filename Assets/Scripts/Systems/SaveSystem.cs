using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class SaveSystem
{
    const int CurrentVersion = 1;
    const int MaxBackups = 3;
    static string SavePath => Path.Combine(Application.persistentDataPath, "rpg_save.json");
    static bool _isSaving;

    static string BackupPath(int index) =>
        Path.Combine(Application.persistentDataPath, $"rpg_save.bak{index}.json");

    public static void Save(SaveData data)
    {
        if (_isSaving)
        {
            Debug.LogWarning("[SaveSystem] Save already in progress, skipping duplicate request");
            return;
        }
        _isSaving = true;
        try
        {
            data.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var dataObj = JObject.FromObject(data);
            string dataJson = dataObj.ToString(Formatting.None);
            string checksum = ComputeChecksum(dataJson);

            var envelope = new JObject
            {
                ["version"] = CurrentVersion,
                ["checksum"] = checksum,
                ["data"] = dataObj
            };
            string json = envelope.ToString(Formatting.Indented);

            RotateBackups();

            string tempPath = SavePath + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(tempPath, SavePath);
            Debug.Log($"[SaveSystem] Saved v{CurrentVersion} (checksum={checksum[..8]}…) to {SavePath}");

            if (SteamCloudStorage.IsAvailable)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                SteamCloudStorage.SaveToCloud("rpg_save.json", bytes);
                Debug.Log("[SaveSystem] Cloud save synced");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
        }
        finally
        {
            _isSaving = false;
        }
    }

    public static SaveData Load()
    {
        var result = TryLoadFrom(SavePath);
        if (result != null) return result;

        Debug.LogWarning("[SaveSystem] Primary save failed, attempting backup recovery...");
        for (int i = 1; i <= MaxBackups; i++)
        {
            string path = BackupPath(i);
            result = TryLoadFrom(path);
            if (result != null)
            {
                Debug.LogWarning($"[SaveSystem] Recovered from backup {i}: {path}");
                return result;
            }
        }

        Debug.LogError("[SaveSystem] All save files corrupted or missing. No recovery possible.");
        return null;
    }

    static SaveData TryLoadFrom(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            string json = File.ReadAllText(path);
            var root = JObject.Parse(json);

            int version = root["version"]?.Value<int>() ?? 0;
            JObject data;

            if (version == 0)
            {
                data = root;
            }
            else
            {
                data = (JObject)root["data"];
                if (data == null)
                {
                    Debug.LogError($"[SaveSystem] {path}: envelope has no data field");
                    return null;
                }

                string storedChecksum = root["checksum"]?.Value<string>();
                if (storedChecksum != null)
                {
                    string dataJson = data.ToString(Formatting.None);
                    string computed = ComputeChecksum(dataJson);
                    if (storedChecksum != computed)
                    {
                        Debug.LogError($"[SaveSystem] {path}: checksum mismatch (stored={storedChecksum[..8]}… computed={computed[..8]}…)");
                        return null;
                    }
                }
            }

            if (version < CurrentVersion)
            {
                Debug.Log($"[SaveSystem] Migrating save from v{version} to v{CurrentVersion}");
                data = SaveMigrations.Apply(version, CurrentVersion, data);
            }

            var result = data.ToObject<SaveData>();
            if (result == null)
            {
                Debug.LogError($"[SaveSystem] {path}: deserialized to null");
                return null;
            }

            var fixes = result.Validate();
            if (fixes.Count > 0)
            {
                Debug.LogWarning($"[SaveSystem] {path}: applied {fixes.Count} validation fix(es):\n  " +
                                 string.Join("\n  ", fixes));
            }

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] {path}: load failed — {e.Message}");
            return null;
        }
    }

    static void RotateBackups()
    {
        try
        {
            string oldest = BackupPath(MaxBackups);
            if (File.Exists(oldest)) File.Delete(oldest);

            for (int i = MaxBackups - 1; i >= 1; i--)
            {
                string src = BackupPath(i);
                string dst = BackupPath(i + 1);
                if (File.Exists(src)) File.Move(src, dst);
            }

            if (File.Exists(SavePath))
            {
                File.Copy(SavePath, BackupPath(1), true);
                Debug.Log($"[SaveSystem] Backup rotated (max {MaxBackups})");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Backup rotation failed: {e.Message}");
        }
    }

    static string ComputeChecksum(string content)
    {
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public static bool HasSave() => File.Exists(SavePath);

    public static void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        for (int i = 1; i <= MaxBackups; i++)
        {
            string path = BackupPath(i);
            if (File.Exists(path)) File.Delete(path);
        }
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
