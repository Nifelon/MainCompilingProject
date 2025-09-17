using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GlobalCore : MonoBehaviour
{
    public static GlobalCore Instance { get; private set; }

    [SerializeField] private GameManager _gameManager;
    [SerializeField] private WorldManager _worldManager;

    private bool _didInitialScan;
    private bool _hasLoggedMissingGM;
    private bool _hasLoggedMissingWM;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CacheManagersIfNeeded();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _didInitialScan = false;
        _hasLoggedMissingGM = _hasLoggedMissingWM = false;
        CacheManagersIfNeeded();
    }

    private void CacheManagersIfNeeded()
    {
        if (_didInitialScan) return;
        _didInitialScan = true;

        if (_gameManager == null)
            _gameManager = FindAnyObjectByType<GameManager>(FindObjectsInactive.Exclude);

        if (_worldManager == null)
            _worldManager = FindAnyObjectByType<WorldManager>(FindObjectsInactive.Exclude);
    }

    public GameManager GameManager
    {
        get
        {
            if (_gameManager == null)
            {
                _gameManager = FindAnyObjectByType<GameManager>(FindObjectsInactive.Exclude);
                if (_gameManager == null && !_hasLoggedMissingGM)
                {
                    Debug.LogError("❌ GameManager не найден в сцене!");
                    _hasLoggedMissingGM = true;
                }
            }
            return _gameManager;
        }
    }

    public WorldManager WorldManager
    {
        get
        {
            if (_worldManager == null)
            {
                _worldManager = FindAnyObjectByType<WorldManager>(FindObjectsInactive.Exclude);
                if (_worldManager == null && !_hasLoggedMissingWM)
                {
                    Debug.LogError("❌ WorldManager не найден в сцене!");
                    _hasLoggedMissingWM = true;
                }
            }
            return _worldManager;
        }
    }

    // Ручная регистрация из Awake менеджеров (избегает Find)
    public void Register(GameManager gm)
    {
        if (gm == null) return;
        if (_gameManager != null && _gameManager != gm)
        {
            Debug.LogWarning($"[GlobalCore] Второй GameManager: {gm.name}. Оставляю {_gameManager.name}.");
            return;
        }
        _gameManager = gm;
    }

    public void Register(WorldManager wm)
    {
        if (wm == null) return;
        if (_worldManager != null && _worldManager != wm)
        {
            Debug.LogWarning($"[GlobalCore] Второй WorldManager: {wm.name}. Оставляю {_worldManager.name}.");
            return;
        }
        _worldManager = wm;
    }
}