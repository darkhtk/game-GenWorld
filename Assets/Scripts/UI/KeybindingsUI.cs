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
        keybindingsText.text =
            "<b>Movement</b>\n" +
            "  WASD / Arrows — Move\n" +
            "  Mouse — Aim direction\n\n" +
            "<b>Combat</b>\n" +
            "  Left Click — Auto attack\n" +
            "  1-6 — Skills\n" +
            "  Ctrl/Space — Dodge\n\n" +
            "<b>UI</b>\n" +
            "  I — Inventory\n" +
            "  J — Achievements\n" +
            "  V — Quest tracker toggle\n" +
            "  M — Minimap zoom\n" +
            "  Esc — Pause menu\n" +
            "  F1 — Keybindings (this)\n" +
            "  F9 — Animation preview (debug)\n\n" +
            "<b>Other</b>\n" +
            "  Tab — Inventory\n" +
            "  E — NPC interact";
    }
}
