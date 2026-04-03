using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeybindingsUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI keybindingsText;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (panel != null)
            {
                bool show = !panel.activeSelf;
                panel.SetActive(show);
                if (show) RefreshText();
            }
        }
    }

    void RefreshText()
    {
        if (keybindingsText == null) return;
        keybindingsText.color = Color.white;
        const string H = "<color=#ffd900><b>";
        const string HE = "</b></color>";
        const string K = "<color=#aaddff>";
        const string KE = "</color>";
        keybindingsText.text =
            $"<b><color=#ffffff>Keybindings</color></b>  <color=#555555>[F1 to close]</color>\n\n" +
            $"{H}Movement{HE}\n" +
            $"  {K}WASD / Arrows{KE} — Move\n" +
            $"  {K}Mouse{KE} — Aim direction\n\n" +
            $"{H}Combat{HE}\n" +
            $"  {K}Left Click{KE} — Auto attack\n" +
            $"  {K}1-6{KE} — Skills\n" +
            $"  {K}Ctrl/Space{KE} — Dodge\n\n" +
            $"{H}UI{HE}\n" +
            $"  {K}I / Tab{KE} — Inventory\n" +
            $"  {K}J{KE} — Achievements\n" +
            $"  {K}V{KE} — Quest tracker toggle\n" +
            $"  {K}M{KE} — Minimap zoom\n" +
            $"  {K}Esc{KE} — Pause menu\n" +
            $"  {K}F1{KE} — Keybindings (this)\n\n" +
            $"{H}Other{HE}\n" +
            $"  {K}E{KE} — NPC interact";
    }
}
