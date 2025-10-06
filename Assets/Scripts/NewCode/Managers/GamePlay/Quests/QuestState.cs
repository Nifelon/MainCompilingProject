using Game.Actors;
using System;
using UnityEngine;
using Game.Items;

public enum QuestKind { Kill, Collect }                // Craft �����
public enum QuestProgressState { NotStarted, Active, Completed }

[Serializable]
public struct KillTarget
{
    public ActorIdKind kind;        // Player / NPC / Creature
    public NPCEnums npcId;          // ���� NPC
    public CreatureEnums creatureId;// ���� Creature

    public bool Matches(ActorIdKind k, NPCEnums n, CreatureEnums c)
    {
        switch (kind)
        {
            case ActorIdKind.Player: return k == ActorIdKind.Player;
            case ActorIdKind.NPC: return k == ActorIdKind.NPC && n.Equals(npcId);
            case ActorIdKind.Creature: return k == ActorIdKind.Creature && c.Equals(creatureId);
            default: return false;
        }
    }

    public string Label =>
        kind == ActorIdKind.Player
            ? "Player"
            : (kind == ActorIdKind.NPC ? npcId.ToString() : creatureId.ToString());
}

[Serializable]
public class QuestConfig
{
    public string id;
    public string title;
    [TextArea] public string description;

    public QuestKind kind = QuestKind.Collect;

    // ����:
    public KillTarget killTarget;   // ��� Kill
    public ItemId itemTarget;   // ��� Collect

    public int targetCount = 1;

    [NonSerialized] public int progress = 0;
    [NonSerialized] public QuestProgressState state = QuestProgressState.NotStarted;

    public void Reset()
    {
        progress = 0;
        state = QuestProgressState.NotStarted;
    }

    public string GetTargetLabel()
        => kind == QuestKind.Kill ? killTarget.Label : itemTarget.ToString();
}
