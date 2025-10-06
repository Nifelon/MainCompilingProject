// Assets/Scripts/NPC/SoldierDialogController.cs
using System.Collections.Generic;
using UnityEngine;

public class SoldierDialogController : MonoBehaviour, IInteractable
{
    DialogPanelTMP panel;
    string npcName;
    Sprite portrait;
    string[] fallbackRumors = {
        "Говорят, волки ожесточились к северу…",
        "Караваны видели следы гиен на юге.",
        "Командир ищет тех, кто справится."
    };

    public string Hint => "E — Поговорить";

    // dialogueRef можно сделать TextAsset/ScriptableObject – тут просто передаём ссылку и читаем строки, если нужно
    public void Init(DialogPanelTMP p, string name, Sprite face, Object dialogueRef)
    { panel = p; npcName = string.IsNullOrEmpty(name) ? "Солдат" : name; portrait = face; /* при желании распарсить dialogueRef */ }

    public void Interact(GameObject actor)
    {
        if (!panel) return;
        var text = fallbackRumors[Random.Range(0, fallbackRumors.Length)];
        panel.OpenNPC(portrait, npcName, text, new List<DialogOption> { new("Закрыть", () => { }) });
    }
}
