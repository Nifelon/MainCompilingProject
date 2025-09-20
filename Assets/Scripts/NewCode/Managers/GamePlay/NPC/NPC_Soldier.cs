using UnityEngine;

/// Солдат: выдаёт «слухи». Реализует IInteractable (Hint + Interact).
public class NPC_Soldier : MonoBehaviour, IInteractable
{
    [TextArea]
    public string[] lines =
    {
        "В роще к северу слышны вои — там волки.",
        "Командир ищет смельчаков. Загляни в лагерь."
    };

    [SerializeField] private string hint = "E — Поговорить (Солдат)";
    public string Hint => hint;                  // Требуемое свойство интерфейса

    public void Interact(GameObject actor)
    {
        var panel = Object.FindFirstObjectByType<DialogPanel>();
        DialogUtil.ShowLines(panel, "Солдат", lines);
    }
}