using UnityEngine;

[DefaultExecutionOrder(-800)]
public class WorldGenLabStarter : MonoBehaviour
{
    public WorldManager world;
    public Game.UI.AlphaHUD hud;

    void Start()
    {
        if (!world) world = FindAnyObjectByType<WorldManager>(FindObjectsInactive.Exclude);
        if (!hud) hud = FindAnyObjectByType<Game.UI.AlphaHUD>(FindObjectsInactive.Exclude);

        if (hud && world) hud.Bind(world);  // ← прокинем WM в HUD
        if (world) world.InitializeWorld(force: true);
        hud?.SendMessage("RefreshLabels", true, SendMessageOptions.DontRequireReceiver);
    }
}