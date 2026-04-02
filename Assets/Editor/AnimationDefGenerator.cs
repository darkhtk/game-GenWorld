using UnityEditor;
using UnityEngine;
using System.IO;

public static class AnimationDefGenerator
{
    const string OutputDir = "Assets/ScriptableObjects/AnimationDefs";

    [MenuItem("Game/Generate Default AnimationDefs")]
    public static void GenerateAll()
    {
        if (!Directory.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);

        CreateDef("PlayerAnimDef", AnimationDef.EntityType.Player, new[]
        {
            ("idle", 0.8f, true),
            ("run", 0.6f, true),
            ("attack", 0.4f, false),
            ("dodge", 0.2f, false),
            ("hit", 0.3f, false),
            ("die", 1.0f, false),
        });

        CreateDef("MonsterAnimDef", AnimationDef.EntityType.Monster, new[]
        {
            ("idle", 0.8f, true),
            ("walk", 0.6f, true),
            ("attack", 0.5f, false),
            ("hit", 0.3f, false),
            ("die", 1.2f, false),
        });

        CreateDef("NPCAnimDef", AnimationDef.EntityType.NPC, new[]
        {
            ("idle", 1.0f, true),
            ("talk", 0.8f, true),
            ("react", 0.5f, false),
        });

        CreateDef("SkillAnimDef", AnimationDef.EntityType.Skill, new[]
        {
            ("cast", 0.4f, false),
            ("projectile", 0.6f, false),
            ("impact", 0.3f, false),
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AnimationDefGenerator] Created 4 default AnimationDef assets in {OutputDir}");
    }

    static void CreateDef(string name, AnimationDef.EntityType type,
        (string state, float duration, bool loop)[] entries)
    {
        string path = $"{OutputDir}/{name}.asset";
        if (File.Exists(path)) return;

        var def = ScriptableObject.CreateInstance<AnimationDef>();
        def.entityType = type;
        def.entries = new AnimationDef.AnimEntry[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            def.entries[i] = new AnimationDef.AnimEntry
            {
                stateName = entries[i].state,
                expectedDuration = entries[i].duration,
                isLooping = entries[i].loop,
                clip = null
            };
        }
        AssetDatabase.CreateAsset(def, path);
    }
}
