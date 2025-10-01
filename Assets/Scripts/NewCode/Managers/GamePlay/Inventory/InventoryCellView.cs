// Assets/Scripts/NewCode/UI/Inventory/InventoryCellView.cs
using UnityEngine;
using UnityEngine.UI;

public class InventoryCellView : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] Text countText;

    public void SetEmpty()
    {
        if (icon) { icon.enabled = false; icon.sprite = null; }
        if (countText) countText.text = "";
    }

    public void Set(Sprite sprite, int amount)
    {
        if (icon) { icon.sprite = sprite; icon.enabled = sprite != null; }
        if (countText) countText.text = amount > 1 ? amount.ToString() : "";
    }
}