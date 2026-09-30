using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Three slots next to a bar. Empty slots stay black. Filled slots show filledSprite.
/// Put one of these by the battery bar (Battery) and one by the stamina bar (EnergyDrink).
/// </summary>
public class ReserveSlotsUI : MonoBehaviour
{
    public enum ReserveKind
    {
        Battery,
        EnergyDrink
    }
    public ReserveKind kind = ReserveKind.Battery;
    public PlayerStats playerStats;
    [Header("Look")]
    [Tooltip("Icon shown in a slot after the player picks that item up.")]
    public Sprite filledSprite;
    public Color emptyColor = Color.black;
    public Color filledColor = Color.white;
    public Vector2 slotSize = new Vector2(36f, 36f);
    public float spacing = 6f;
    [Header("Optional")]
    [Tooltip("Leave empty and three black slots are created for you.")]
    public Image[] slots;
    PlayerReserves reserves;
    void Start()
    {
        if (playerStats == null)
            playerStats = Object.FindAnyObjectByType<PlayerStats>();
        if (playerStats != null)
            reserves = PlayerReserves.Get(playerStats);
        if (slots == null || slots.Length == 0)
            BuildSlots();
        Refresh();
    }
    void Update()
    {
        if (reserves == null && playerStats != null)
            reserves = PlayerReserves.Get(playerStats);
        Refresh();
    }
    void BuildSlots()
    {
        slots = new Image[PlayerReserves.MaxSlots];
        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotObject = new GameObject("Slot " + (i + 1), typeof(RectTransform), typeof(Image));
            slotObject.transform.SetParent(transform, false);
            RectTransform rect = slotObject.GetComponent<RectTransform>();
            rect.sizeDelta = slotSize;
            Image image = slotObject.GetComponent<Image>();
            image.color = emptyColor;
            slots[i] = image;
        }
    }
    void Refresh()
    {
        if (slots == null)
            return;
        int filled = 0;
        if (reserves != null)
            filled = kind == ReserveKind.Battery ? reserves.batteryCount : reserves.drinkCount;
        for (int i = 0; i < slots.Length; i++)
        {
            Image slot = slots[i];
            if (slot == null)
                continue;
            bool hasItem = i < filled;
            slot.sprite = hasItem ? filledSprite : null;
            slot.color = hasItem ? filledColor : emptyColor;
        }
    }
}