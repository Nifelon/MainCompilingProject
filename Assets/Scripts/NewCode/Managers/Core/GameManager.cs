using UnityEngine;

[DefaultExecutionOrder(-950)]
public class GameManager : MonoBehaviour
{
    private void Awake() => GlobalCore.Instance?.Register(this);
    private void Start()
    {
        var wm = GlobalCore.Instance?.WorldManager;
        wm?.LoadFromPlayerPrefs();   // ← добавить
        wm?.InitializeWorld();
    }
}