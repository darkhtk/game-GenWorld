using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class BuildScript
{
    const string BuildPath = "SteamBuild/output";
    const string ExeName = "GenWorld.exe";

    [MenuItem("Build/Steam Windows")]
    public static void BuildWindows()
    {
        string outputPath = Path.Combine(BuildPath, ExeName);
        Directory.CreateDirectory(BuildPath);

        var options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            // Remove steam_appid.txt from build output if copied
            string steamIdFile = Path.Combine(BuildPath, "steam_appid.txt");
            if (File.Exists(steamIdFile))
                File.Delete(steamIdFile);

            // Write version file
            string version = PlayerSettings.bundleVersion;
            File.WriteAllText(Path.Combine(BuildPath, "version.txt"), version);

            Debug.Log($"[BuildScript] Build succeeded: {outputPath} (v{version})");
        }
        else
        {
            Debug.LogError($"[BuildScript] Build failed: {report.summary.result}");
        }
    }

    [MenuItem("Build/Steam Windows + Upload")]
    public static void BuildAndUpload()
    {
        BuildWindows();
        Debug.Log("[BuildScript] Build complete. Run SteamPipe/build_and_upload.sh to upload.");
    }

    static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        var result = new System.Collections.Generic.List<string>();
        foreach (var s in scenes)
            if (s.enabled) result.Add(s.path);
        return result.ToArray();
    }
}
