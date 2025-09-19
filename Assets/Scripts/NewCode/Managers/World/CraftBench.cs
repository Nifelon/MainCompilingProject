using UnityEngine;

public class CraftBench : MonoBehaviour, IInteractable
{
    public string Hint => "E — Сделать заплатки (1 кожа → 3)";

    public void Interact(GameObject actor)
    {
        // TODO: проверить и списать Skin, выдать 3 Patch
        QuestEventBus.RaiseCraft("Patch", 1);
        InteractPrompt.Instance?.Hide();
    }
}