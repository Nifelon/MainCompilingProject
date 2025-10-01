using System;
using UnityEngine;

public enum QuestKind { Kill, Collect, Craft }
public enum QuestProgressState { NotStarted, Active, Completed }

[Serializable]
public class QuestConfig
{
    public string id;
    public string title;
    [TextArea] public string description;

    public QuestKind kind = QuestKind.Collect;
    public string targetId;      // "Wolf", "Berry", "LeatherPatch" ...
    public int targetCount = 1;

    [NonSerialized] public int progress = 0;
    [NonSerialized] public QuestProgressState state = QuestProgressState.NotStarted;

    public void Reset()
    {
        progress = 0;
        state = QuestProgressState.NotStarted;
    }
}
