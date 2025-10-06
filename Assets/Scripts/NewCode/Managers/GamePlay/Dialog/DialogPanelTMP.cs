// Assets/Scripts/UI/Dialog/DialogPanelTMP.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogPanelTMP : MonoBehaviour
{
    [Header("Root & Close")]
    [SerializeField] GameObject root;           // корневой GO окна (SetActive)
    [SerializeField] Button closeBtn;           // X в хедере (опционально)

    [Header("Header")]
    [SerializeField] Image portrait;
    [SerializeField] TMP_Text title;            // им€ NPC

    [Header("Body")]
    [SerializeField] ScrollRect scroll;
    [SerializeField] TMP_Text message;          // Text (TMP) внутри ScrollView/Viewport/Content

    [Header("Options")]
    [SerializeField] Transform optionsParent;   // контейнер дл€ кнопок-опций (Footer/Options)
    [SerializeField] GameObject optionPrefab;   // ваш префаб Button (+ TMP_Text внутри)
    [SerializeField] bool autoSelectFirst = true;

    public bool IsOpen => root && root.activeSelf;

    readonly List<Button> _btns = new();
    readonly List<GameObject> _pool = new();

    void Awake()
    {
        if (!root) root = gameObject;
        if (closeBtn) closeBtn.onClick.AddListener(Close);
        root.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Close();

        // шорткаты: цифры 1..9, E Ч перва€ кнопка
        if (Input.anyKeyDown)
        {
            for (int i = 0; i < _btns.Count && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                { _btns[i].onClick?.Invoke(); return; }
            }
            if (autoSelectFirst && _btns.Count > 0 && Input.GetKeyDown(KeyCode.E))
            { _btns[0].onClick?.Invoke(); return; }
        }
    }

    public void OpenNPC(Sprite p, string npcName, string body, IReadOnlyList<DialogOption> options)
    {
        if (!root) return;

        root.SetActive(true);
        InputBlocker.SetBlocked(true);

        if (portrait) { portrait.enabled = p != null; portrait.sprite = p; }
        if (title) title.text = npcName ?? "";

        if (message) message.text = body ?? "";

        // сброс скролла наверх
        if (scroll) { Canvas.ForceUpdateCanvases(); scroll.normalizedPosition = Vector2.one; }

        BuildOptions(options);
    }

    public void Close()
    {
        root.SetActive(false);
        InputBlocker.SetBlocked(false);
        ClearOptions();
    }

    // ------- options -------

    void BuildOptions(IReadOnlyList<DialogOption> options)
    {
        ClearOptions();
        if (options == null || options.Count == 0) return;

        EnsurePool(options.Count);
        _btns.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            var go = _pool[i];
            go.SetActive(true);

            var btn = go.GetComponent<Button>();
            var txt = go.GetComponentInChildren<TMP_Text>(true);
            if (txt) txt.text = opt.label ?? $"Option {i + 1}";

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                try { opt.onClick?.Invoke(); }
                finally { if (opt.closeOnClick) Close(); }
            });

            _btns.Add(btn);
        }

        // автофокус на первой
        if (autoSelectFirst && _btns.Count > 0)
        {
            _btns[0].Select();
        }
    }

    void EnsurePool(int need)
    {
        while (_pool.Count < need)
        {
            var go = Instantiate(optionPrefab, optionsParent);
            go.SetActive(false);
            _pool.Add(go);
        }
    }

    void ClearOptions()
    {
        for (int i = 0; i < _pool.Count; i++) _pool[i].SetActive(false);
        _btns.Clear();
    }
}

[Serializable]
public struct DialogOption
{
    public string label;
    public bool closeOnClick;
    public Action onClick;

    public DialogOption(string label, Action onClick, bool closeOnClick = true)
    { this.label = label; this.onClick = onClick; this.closeOnClick = closeOnClick; }
}

// простой блокировщик ввода Ч уже используем по всему UI
public static class InputBlocker
{
    static int _depth;
    public static bool IsBlocked => _depth > 0;
    public static void SetBlocked(bool v) => _depth = Mathf.Clamp(_depth + (v ? 1 : -1), 0, 999);
}