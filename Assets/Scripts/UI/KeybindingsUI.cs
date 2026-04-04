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
        const string K = "<color=#88ddff>";
        const string KE = "</color>";
        keybindingsText.text =
            $"<b><color=#ffffff>Keybindings</color></b>  <color=#444444>[F1 to close]</color>\n\n" +
            $"{H}Movement{HE}\n" +
            $"  {K}WASD / Arrows{KE} — Move\n" +
            $"  {K}Mouse{KE} — Aim direction\n\n" +
            $"{H}Combat{HE}\n" +
            $"  {K}Left Click{KE} — Attack\n" +
            $"  {K}1-6{KE} — Skills\n" +
            $"  {K}Ctrl / Space{KE} — Dodge\n\n" +
            $"{H}UI Panels{HE}\n" +
            $"  {K}I{KE} — Inventory\n" +
            $"  {K}K{KE} — Skill tree\n" +
            $"  {K}J{KE} — Quests / Achievements\n" +
            $"  {K}Tab{KE} — Minimap toggle\n" +
            $"  {K}V{KE} — Quest tracker\n" +
            $"  {K}H{KE} — History log\n" +
            $"  {K}Esc{KE} — Pause / Close panel\n" +
            $"  {K}F1{KE} — Keybindings (this)\n" +
            $"  {K}Shift+P{KE} — Game stats\n\n" +
            $"{H}Other{HE}\n" +
            $"  {K}E{KE} — Interact with NPC\n" +
            $"  {K}R{KE} — Use HP potion\n" +
            $"  {K}T{KE} — Use MP potion";
    }
}
