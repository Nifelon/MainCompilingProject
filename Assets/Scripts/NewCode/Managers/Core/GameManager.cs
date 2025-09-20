using UnityEngine;
using System;

[DefaultExecutionOrder(-950)]
public class GameManager : MonoBehaviour
{
    // === Доступ к состоянию UI-блокировки для любых систем (инвентарь, карта, диалог, лут) ===
    public bool IsUiBlocked => _uiBlocked;
    public event Action<bool> OnUiBlockChanged;

    WorldManager _wm;           // кэш WM, чтобы не дергать GlobalCore каждый раз
    bool _initialized;
    bool _uiBlocked;

    private void Awake()
    {
        // Регистрируемся в GlobalCore, чтобы к нам могли обратиться: GlobalCore.Instance.GameManager
        GlobalCore.Instance?.Register(this);
    }

    private void Start()
    {
        // Забираем WorldManager из GlobalCore, а если не задан — находим в сцене (Unity 6)
        _wm = GlobalCore.Instance?.WorldManager;
#if UNITY_2023_1_OR_NEWER
        if (_wm == null) _wm = FindFirstObjectByType<WorldManager>(FindObjectsInactive.Exclude);
#else
        if (_wm == null) _wm = FindObjectOfType<WorldManager>();
#endif
        // Инициализация мира (однократно, с защитой от двойного запуска)
        TryInitWorld();
    }

    // ========== ПУБЛИЧНЫЕ API ==========

    /// Блокировать/разблокировать геймплейный ввод (зови при открытии/закрытии любых окон UI)
    public void SetUiBlock(bool value)
    {
        if (_uiBlocked == value) return;
        _uiBlocked = value;
        OnUiBlockChanged?.Invoke(value);

        // Если есть единый вход управления (InputRouter/PlayerController) — прокиньте флаг туда:
        // var input = FindFirstObjectByType<PlayerInputRouter>();
        // if (input) input.SetBlocked(value);
    }

    /// (Опционально) Принудительно сохранить состояние мира/настроек
    //public void Save()
    //{
    //    try { _wm?.SaveToPlayerPrefs(); } catch { /* no-op */ }
    //}

    // ========== ВНУТРЕННЕЕ ==========

    void TryInitWorld()
    {
        if (_initialized) return;
        _initialized = true;

        // Безопасные вызовы (если методов нет — просто пропустятся)
        try { _wm?.LoadFromPlayerPrefs(); } catch { /* no-op */ }
        try { _wm?.InitializeWorld(); } catch { /* no-op */ }
    }

    private void OnApplicationQuit()
    {
        // На выходе игры — бережно сохраняем (если реализовано в WM)
       // Save();
    }
}