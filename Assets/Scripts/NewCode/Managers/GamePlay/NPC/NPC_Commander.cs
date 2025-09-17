using UnityEngine;

public class NPC_Commander : MonoBehaviour
{
    public DialogPanel dialog;
    public QuestManager questManager;

    public void Interact()
    {
        if (!dialog || !questManager) return;

        dialog.SetBody(
            title: "Командир",
            body: "Нужна помощь: выбери задание.",
            primaryLabel: "Принять квест",
            onPrimary: () => {
                questManager.ActivateAnyAvailable();
                dialog.Show(false);
            },
            secondaryLabel: "Закрыть",
            onSecondary: () => dialog.Show(false),
            subtitle: "Гарнизонный офицер"   // ← опционально: профессия
        );
        dialog.Show(true);
    }
}