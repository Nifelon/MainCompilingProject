using UnityEngine;

public class BerryPickup : MonoBehaviour, IInteractable
{
    public string itemId = "Berry";
    public int count = 1;
    public string Hint => "E Ч —обрать €годы";

    public void Interact(GameObject actor)
    {
        QuestEventBus.RaiseCollect(itemId, count);
        // TODO: добавить в инвентарь
        gameObject.SetActive(false);
        InteractPrompt.Instance?.Hide();
    }
}