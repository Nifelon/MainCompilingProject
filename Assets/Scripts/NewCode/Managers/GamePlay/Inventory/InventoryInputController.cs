// Assets/Scripts/NewCode/UI/Inventory/InventoryInputController.cs
using Game.Items;
using UnityEngine;

public class InventoryInputController : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] GameObject inventoryRoot;   // твой Panel/InventoryPanel
    [SerializeField] InventoryPanelBinder binder; // чтобы можно было форсить Rebuild при открытии
    [Header("Debug")]
    public ItemId debugGiveItem = ItemId.Berry;
    public int debugAmount = 5;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!inventoryRoot) return;
            bool next = !inventoryRoot.activeSelf;
            inventoryRoot.SetActive(next);
            if (next) binder?.Rebuild();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            InventoryService.Add(debugGiveItem, debugAmount);
        }
    }
}