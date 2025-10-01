// Assets/Scripts/NewCode/GamePlay/Item/ItemSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/ItemNew", fileName = "Item_")]
public class ItemSO : ScriptableObject
{
    public ItemId id;
    public string displayName;
    public Sprite icon;
    [Min(1)] public int maxStack = 99;
}