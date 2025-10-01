using UnityEngine;

public class CraftBench : MonoBehaviour, IInteractable
{
    public string Hint => "E — Сделать заплатки (1 кожа → 3)";

    public void Interact(GameObject actor)
    {
        //        const string Skin = "Skin";
        //        const string Patch = "LeatherPatch";
        //        if (InventoryService.Count(Skin) >= 1)
        //            {
        //    InventoryService.TryRemove(Skin, 1);
        //    InventoryService.TryAdd(Patch, 3);
        //    QuestEventBus.RaiseCraft(Patch, 3);
        //    FloatingText.Show(actor.transform.position, "+3 Leather Patch"); // если есть такой хелпер
        //            }
        //        else
        //            {
        //    FloatingText.Show(actor.transform.position, "Нужно: 1 кожа"); // или любой твой UI-хинт
        //            }
        //InteractPrompt.Instance?.Hide();
    }
}