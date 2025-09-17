using UnityEngine;

public class NPC_Soldier : MonoBehaviour
{
    [TextArea]
    public string[] rumors = {
        "В лесу видели волков.",
        "Командир собирает людей.",
        "Говорят, ягоды уродились."
    };

    public DialogPanel dialog;
    int idx;

    public void Interact() => ShowRumor();

    void ShowRumor()
    {
        if (!dialog) return;
        var text = rumors.Length > 0 ? rumors[idx % rumors.Length] : "Пока слухов нет.";
        dialog.SetBody(
    title: "Солдат",
    body: text,
    primaryLabel: "Ещё",
    onPrimary: NextRumor,
    secondaryLabel: "Закрыть",
    onSecondary: () => dialog.Show(false),
    subtitle: "Стражник" // ← опционально
);
        dialog.Show(true);
    }

    void NextRumor()
    {
        idx = (idx + 1) % Mathf.Max(1, rumors.Length);
        ShowRumor();
    }
}