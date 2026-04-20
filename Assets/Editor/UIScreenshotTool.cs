using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only automation that drives UIManager through UI states and
// captures screenshots for the UI evaluation pass (T-009).
// Batch entry point: -executeMethod UIScreenshotTool.RunBatch
[InitializeOnLoad]
public static class UIScreenshotTool
{
    const string ActiveKey = "UICap_Active";
    const string OutDir = "_discord_sessions/genworld__efa6b0ac/screenshots";
    const float BootWait = 6.0f;

    static UIScreenshotTool()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/Capture All UI Screenshots")]
    public static void Run()
    {
        EditorPrefs.SetBool(ActiveKey, true);
        if (!Application.isPlaying) EditorApplication.EnterPlaymode();
        else AttachDriver();
    }

    public static void RunBatch()
    {
        Debug.Log("[UIScreenshotTool] RunBatch invoked");
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        EditorPrefs.SetBool(ActiveKey, true);
        EditorApplication.EnterPlaymode();
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        if (!EditorPrefs.GetBool(ActiveKey, false)) return;
        EditorPrefs.SetBool(ActiveKey, false);
        AttachDriver();
    }

    static void AttachDriver()
    {
        var go = new GameObject("[UIScreenshotDriver]");
        go.AddComponent<UIScreenshotDriver>();
    }

    public static string OutputDirectory()
    {
        string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
        return Path.Combine(root, OutDir);
    }

    public static float BootWaitSeconds() => BootWait;
}

public class UIScreenshotDriver : MonoBehaviour
{
    IEnumerator Start()
    {
        Debug.Log("[UIScreenshotDriver] Start, waiting for game init...");
        yield return new WaitForSecondsRealtime(UIScreenshotTool.BootWaitSeconds());

        var ui = Object.FindFirstObjectByType<UIManager>();
        if (ui == null)
        {
            Debug.LogError("[UIScreenshotDriver] UIManager not found");
            Finish();
            yield break;
        }

        string dir = UIScreenshotTool.OutputDirectory();
        Directory.CreateDirectory(dir);
        Debug.Log($"[UIScreenshotDriver] writing to {dir}");

        var steps = BuildSteps(ui);
        int i = 0;
        foreach (var step in steps)
        {
            try { step.setup(); }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UIScreenshotDriver] setup '{step.name}' failed: {ex.Message}");
            }

            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.15f);

            string path = Path.Combine(dir, $"{i:D2}_{step.name}.png");
            bool ok = false;
            try
            {
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex != null)
                {
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    Debug.Log($"[UIScreenshotDriver] wrote {path} ({tex.width}x{tex.height})");
                    Object.DestroyImmediate(tex);
                    ok = true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UIScreenshotDriver] CaptureAsTexture failed '{step.name}': {ex.Message}");
            }

            if (!ok)
            {
                try { CaptureViaRenderTexture(path); }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[UIScreenshotDriver] fallback '{step.name}' failed: {ex.Message}");
                }
            }

            i++;
            yield return new WaitForSecondsRealtime(0.2f);
        }

        Debug.Log($"[UIScreenshotDriver] Done. {i} shots written to {dir}");
        Finish();
    }

    static void CaptureViaRenderTexture(string file)
    {
        int w = Screen.width > 0 ? Screen.width : 1920;
        int h = Screen.height > 0 ? Screen.height : 1080;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        var cams = Camera.allCameras;
        System.Array.Sort(cams, (a, b) => a.depth.CompareTo(b.depth));

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.black);

        foreach (var cam in cams)
        {
            if (cam == null || !cam.enabled || !cam.gameObject.activeInHierarchy) continue;
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevTarget;
        }

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c == null || !c.isActiveAndEnabled) continue;
            if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var a in cams) { if (a != null && a.enabled) { cam = a; break; } }
            }
            if (cam == null) break;
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = 1f;
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevTarget;
            c.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        File.WriteAllBytes(file, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        RenderTexture.active = prev;
        rt.Release();
        Object.DestroyImmediate(rt);
        Debug.Log($"[UIScreenshotDriver] wrote {file} ({w}x{h})");
    }

    void Finish()
    {
        EditorApplication.delayCall += () =>
        {
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        };
    }

    struct Step
    {
        public string name;
        public System.Action setup;
        public Step(string n, System.Action s) { name = n; setup = s; }
    }

    static List<Step> BuildSteps(UIManager ui)
    {
        return new List<Step>
        {
            new Step("hud_default",       () => ui.HideAll()),
            new Step("hud_history",       () => { ui.HideAll(); if (ui.Hud != null) ui.Hud.ToggleHistory(); }),
            new Step("hud_quest_tracker", () => { ui.HideAll(); if (ui.Hud != null) ui.Hud.ToggleQuestTracker(); }),
            new Step("minimap_zoom",      () => { ui.HideAll(); if (ui.Minimap != null) ui.Minimap.ToggleViewRadius(); }),
            new Step("inventory",         () => { ui.HideAll(); if (ui.Inventory != null) ui.Inventory.Toggle(); }),
            new Step("skilltree",         () => { ui.HideAll(); if (ui.SkillTree != null) ui.SkillTree.Toggle(); }),
            new Step("quest",             () => { ui.HideAll(); if (ui.Quest != null) ui.Quest.Toggle(); }),
            new Step("achievement",       () => { ui.HideAll(); if (ui.Achievement != null) ui.Achievement.Toggle(); }),
            new Step("keybindings",       () => { ui.HideAll(); if (ui.Keybindings != null) ui.Keybindings.Toggle(); }),
            new Step("gamestats",         () => { ui.HideAll(); if (ui.GameStats != null) ui.GameStats.Toggle(); }),
            new Step("pause_menu",        () => { ui.HideAll(); if (ui.PauseMenu != null) ui.PauseMenu.Toggle(); }),
            new Step("dialogue",          () => {
                ui.HideAll();
                var gm = GameManager.Instance;
                if (gm != null && gm.Data != null && gm.Data.Npcs != null && ui.Dialogue != null)
                {
                    foreach (var kv in gm.Data.Npcs) { ui.Dialogue.Show(kv.Value); break; }
                }
            }),
        };
    }
}
