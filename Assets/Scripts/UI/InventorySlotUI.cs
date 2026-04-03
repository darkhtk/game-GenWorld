using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler,
    IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IDragHandler
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] TextMeshProUGUI enhanceText;
    [SerializeField] Image borderImage;
    [SerializeField] Image iconImage;
    [SerializeField] Image gradeFrameImage;

    static Sprite _slotBgSprite;
    static Sprite _hoverSprite;
    Image _bgImage;

    public int SlotIndex;
    public Action<int> OnClicked;
    public Action<int> OnBeginDragAction;
    public Action<int> OnEndDragAction;
    public Action<int> OnDropAction;
    public Action<int> OnHover;
    public Action<int> OnHoverExit;

    public void SetItem(string name, int count, int enhanceLevel, Color gradeColor, ItemType type, string icon = null, string grade = null)
    {
        if (nameText != null) { nameText.text = name; nameText.color = gradeColor; }
        if (countText != null)
            countText.text = count > 1 ? count.ToString() : "";
        if (enhanceText != null)
        {
            enhanceText.color = Color.white;
            if (enhanceLevel > 0)
            {
                string eColor = enhanceLevel >= 10 ? "#ff9900"
                    : enhanceLevel >= 7 ? "#66aaff"
                    : enhanceLevel >= 4 ? "#66ff66"
                    : "#aaaaaa";
                enhanceText.text = $"<color={eColor}>+{enhanceLevel}</color>";
            }
            else enhanceText.text = "";
        }
        if (borderImage != null) borderImage.color = gradeColor;
        if (iconImage != null)
        {
            Sprite sprite = null;
            if (!string.IsNullOrEmpty(icon))
                sprite = Resources.Load<Sprite>($"Sprites/Items/{icon}");
            if (sprite == null)
                sprite = Resources.Load<Sprite>("Sprites/Items/item_placeholder");
            if (sprite != null) { iconImage.sprite = sprite; iconImage.color = Color.white; iconImage.enabled = true; }
            else { iconImage.sprite = null; iconImage.enabled = false; }
        }
        if (gradeFrameImage != null && !string.IsNullOrEmpty(grade))
        {
            var frame = Resources.Load<Sprite>($"Sprites/UI/grade_frame_{grade}");
            if (frame != null) { gradeFrameImage.sprite = frame; gradeFrameImage.enabled = true; gradeFrameImage.color = Color.white; }
            else gradeFrameImage.enabled = false;
        }
        ApplySlotBg();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void Clear()
    {
        if (nameText != null) { nameText.text = ""; nameText.color = Color.white; }
        if (countText != null) countText.text = "";
        if (enhanceText != null) enhanceText.text = "";
        if (borderImage != null) borderImage.color = new Color(0.3f, 0.3f, 0.3f);
        if (iconImage != null) { iconImage.sprite = null; iconImage.enabled = false; }
    }

    void ApplySlotBg()
    {
        if (_bgImage == null) _bgImage = GetComponent<Image>();
        if (_bgImage == null) return;
        if (_slotBgSprite == null) _slotBgSprite = Resources.Load<Sprite>("Sprites/UI/slot_bg");
        if (_hoverSprite == null) _hoverSprite = Resources.Load<Sprite>("Sprites/UI/slot_hover");
        if (_slotBgSprite != null) { _bgImage.sprite = _slotBgSprite; _bgImage.type = Image.Type.Sliced; }
    }

    public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke(SlotIndex);
    public void OnBeginDrag(PointerEventData eventData) => OnBeginDragAction?.Invoke(SlotIndex);
    public void OnEndDrag(PointerEventData eventData) => OnEndDragAction?.Invoke(SlotIndex);
    public void OnDrop(PointerEventData eventData) => OnDropAction?.Invoke(SlotIndex);
    public void OnDrag(PointerEventData eventData) { }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_bgImage != null && _hoverSprite != null) _bgImage.sprite = _hoverSprite;
        OnHover?.Invoke(SlotIndex);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_bgImage != null && _slotBgSprite != null) _bgImage.sprite = _slotBgSprite;
        OnHoverExit?.Invoke(SlotIndex);
    }
}
