// Assets/Scripts/NPC/CommanderDialogController.cs
using System.Collections.Generic;
using UnityEngine;

public class CommanderDialogController : MonoBehaviour, IInteractable
{
    DialogPanelTMP panel;
    QuestManager quests;
    string npcName;
    Sprite portrait;

    public string Hint => "E — Поговорить";

    public void Init(DialogPanelTMP p, QuestManager q, string name, Sprite face)
    { panel = p; quests = q; npcName = string.IsNullOrEmpty(name) ? "Командир" : name; portrait = face; }

    public void Interact(GameObject actor)
    {
        if (!panel || !quests) return;

        var active = quests.GetActive();
        if (active == null || active.state == QuestProgressState.NotStarted)
        {
            panel.OpenNPC(portrait, npcName, "Нужна помощь, солдат.",
                new List<DialogOption>{
                    new("Взять квест", () => quests.ActivateAnyAvailable()),
                    new("Крафт 1→3",   () => CraftingService.ExchangeSkinToPatches(), closeOnClick:false),
                    new("Закрыть",     () => {})
                });
            return;
        }

        if (quests.IsActiveCompleted())
        {
            panel.OpenNPC(portrait, npcName, "Отлично сработано.",
                new List<DialogOption>{
                    new("Сдать квест", () => quests.CompleteActiveAndAdvance()),
                    new("Закрыть",     () => {})
                });
        }
        else
        {
            panel.OpenNPC(portrait, npcName, "Задача в работе.",
                new List<DialogOption>{
                    new("Крафт 1→3", () => CraftingService.ExchangeSkinToPatches(), closeOnClick:false),
                    new("Закрыть",   () => {})
                });
        }
    }
}