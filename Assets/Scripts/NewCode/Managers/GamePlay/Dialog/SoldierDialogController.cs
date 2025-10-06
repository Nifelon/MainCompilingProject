// Assets/Scripts/NPC/SoldierDialogController.cs
using System.Collections.Generic;
using UnityEngine;

public class SoldierDialogController : MonoBehaviour, IInteractable
{
    DialogPanelTMP panel;
    string npcName;
    Sprite portrait;
    string[] fallbackRumors = {
        "�������, ����� ������������ � ������",
        "�������� ������ ����� ���� �� ���.",
        "�������� ���� ���, ��� ���������."
    };

    public string Hint => "E � ����������";

    // dialogueRef ����� ������� TextAsset/ScriptableObject � ��� ������ ������� ������ � ������ ������, ���� �����
    public void Init(DialogPanelTMP p, string name, Sprite face, Object dialogueRef)
    { panel = p; npcName = string.IsNullOrEmpty(name) ? "������" : name; portrait = face; /* ��� ������� ���������� dialogueRef */ }

    public void Interact(GameObject actor)
    {
        if (!panel) return;
        var text = fallbackRumors[Random.Range(0, fallbackRumors.Length)];
        panel.OpenNPC(portrait, npcName, text, new List<DialogOption> { new("�������", () => { }) });
    }
}
