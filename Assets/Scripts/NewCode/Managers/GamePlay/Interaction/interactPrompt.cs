using UnityEngine;
using UnityEngine.UI;

public class InteractPrompt : MonoBehaviour
{
    public static InteractPrompt Instance;
    public GameObject root; public Text label;

    void Awake()
    {
        Instance = this;
        if (!root) root = gameObject;
        if (!label) label = GetComponentInChildren<UnityEngine.UI.Text>(true);
    }
    void Start() { Hide(); } // ← перенос
    public void Show(string text = "E — Взаимодействие") { if (label) label.text = text; root.SetActive(true); }
    public void Hide() { if (root) root.SetActive(false); }
}