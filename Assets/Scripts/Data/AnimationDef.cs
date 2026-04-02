using UnityEngine;

[CreateAssetMenu(menuName = "Game/AnimationDef")]
public class AnimationDef : ScriptableObject
{
    [System.Serializable]
    public class AnimEntry
    {
        public string stateName;
        public AnimationClip clip;
        public float expectedDuration;
        public bool isLooping;
    }

    public enum EntityType { Player, Monster, NPC, Skill }

    public EntityType entityType;
    public AnimEntry[] entries;

    public AnimEntry GetEntry(string stateName)
    {
        if (entries == null) return null;
        foreach (var e in entries)
        {
            if (e.stateName == stateName) return e;
        }
        return null;
    }

    public bool HasClip(string stateName)
    {
        var entry = GetEntry(stateName);
        return entry != null && entry.clip != null;
    }

    public void LogMissingClips()
    {
        if (entries == null) return;
        foreach (var e in entries)
        {
            if (e.clip == null)
                Debug.LogWarning($"[AnimationDef] Missing clip for state '{e.stateName}' in {name}");
        }
    }
}
