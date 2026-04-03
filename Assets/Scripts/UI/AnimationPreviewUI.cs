#if DEBUG || DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnimationPreviewUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI entityNameLabel;
    [SerializeField] TextMeshProUGUI currentStateLabel;
    [SerializeField] Transform animListContent;
    [SerializeField] Button animEntryButtonPrefab;
    [SerializeField] Slider speedSlider;
    [SerializeField] TextMeshProUGUI speedText;
    [SerializeField] Button closeButton;

    Animator _targetAnimator;
    AnimationDef _targetDef;
    readonly List<Button> _entryButtons = new();

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (speedSlider != null)
        {
            speedSlider.minValue = 0.1f;
            speedSlider.maxValue = 3f;
            speedSlider.value = 1f;
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            if (panel != null && panel.activeSelf) Hide();
            else TrySelectUnderMouse();
        }

        if (panel != null && panel.activeSelf && _targetAnimator != null)
        {
            var info = _targetAnimator.GetCurrentAnimatorStateInfo(0);
            if (currentStateLabel != null)
            {
                currentStateLabel.color = Color.white;
                currentStateLabel.text = $"<color=#888888>State:</color> <color=#aaddff><b>{info.shortNameHash}</b></color>  <color=#888888>t:</color> <color=#ffdd44><b>{info.normalizedTime:F2}</b></color>";
            }
        }
    }

    void TrySelectUnderMouse()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.OverlapPoint(mouseWorld);
        if (hit != null)
        {
            var animator = hit.GetComponent<Animator>();
            if (animator == null) animator = hit.GetComponentInParent<Animator>();
            if (animator != null)
            {
                Show(animator.gameObject);
                return;
            }
        }

        // Fallback: find player
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) Show(player.gameObject);
    }

    public void Show(GameObject target)
    {
        if (panel == null || target == null) return;

        _targetAnimator = target.GetComponent<Animator>();
        _targetDef = null;

        var monster = target.GetComponent<MonsterController>();
        if (monster != null && monster.Def != null) _targetDef = monster.Def.animationDef;

        var npc = target.GetComponent<VillageNPC>();
        if (npc != null && npc.Def != null) _targetDef = npc.Def.animationDef;

        if (entityNameLabel != null) { entityNameLabel.text = $"<b><color=#ffcc88>{target.name}</color></b>"; entityNameLabel.color = Color.white; }
        panel.SetActive(true);
        BuildList();
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    void BuildList()
    {
        foreach (var btn in _entryButtons)
            if (btn != null) Destroy(btn.gameObject);
        _entryButtons.Clear();

        if (_targetDef == null || _targetDef.entries == null || animListContent == null || animEntryButtonPrefab == null)
            return;

        foreach (var entry in _targetDef.entries)
        {
            var btn = Instantiate(animEntryButtonPrefab, animListContent);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            bool missing = entry.clip == null;
            string loopTag = entry.isLooping ? "loop" : "once";
            string durStr = $"{entry.expectedDuration:F1}s";

            if (label != null)
            {
                label.color = Color.white;
                if (missing)
                {
                    label.text = $"<color=#ff9944>\u26a0 {entry.stateName}</color>  <color=#ff5555>clip missing!</color>";
                }
                else
                {
                    string loopColor = entry.isLooping ? "#88ccff" : "#888888";
                    label.text = $"\u25b6 <color=#ffffff>{entry.stateName}</color>  <color={loopColor}>{loopTag}</color>  <color=#ffdd44>{durStr}</color>";
                }
            }

            string state = entry.stateName;
            btn.onClick.AddListener(() => PlayState(state));
            btn.gameObject.SetActive(true);
            _entryButtons.Add(btn);
        }
    }

    void PlayState(string stateName)
    {
        if (_targetAnimator == null) return;
        _targetAnimator.Play(stateName, 0, 0f);
    }

    void OnSpeedChanged(float value)
    {
        if (_targetAnimator != null) _targetAnimator.speed = value;
        if (speedText != null) { speedText.color = Color.white; speedText.text = $"<color=#aaddff><b>{value:F1}x</b></color>"; }
    }
}
#endif
