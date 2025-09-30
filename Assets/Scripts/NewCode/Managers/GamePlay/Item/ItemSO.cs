using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Items/Item")]
public class ItemSO : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public int maxStack = 99;
    public List<ItemActionDef> actions; // на альфу можно пусто
}

[System.Serializable]
public class ItemActionDef
{
    public string actionId; // "consume", "equip", ...
    public int intParam;
    public string strParam;
}