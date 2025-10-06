using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/ItemSO")]
public class ItemSO : ScriptableObject
{
    [SerializeField, HideInInspector] private string guid;
    public string Guid => guid;

    [Header("Designer Name (UI)")]
    public string displayName;
    public Sprite icon;

    [Header("Optional code name (ASCII for enum). If empty, generator uses asset name.")]
    public string codeName; // <-- новое поле

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(guid))
            guid = System.Guid.NewGuid().ToString("N");
        // можно автозаполнить: если пусто — подставить name, чтобы видно было в инспекторе
        if (string.IsNullOrEmpty(codeName)) codeName = name;
    }
}