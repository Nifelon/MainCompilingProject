using UnityEngine;
using UnityEngine.SceneManagement;

public class DevHotkeys : MonoBehaviour
{
    [Header("IDs ������ ��������� � Quest.targetId")]
    public string collectId = "Berry"; // ��� ������ Collect
    public string craftId = "Patch";   // ��� ������ Craft
    public int amount = 1;

    [Header("Player damage hotkeys")]
    public KeyCode damageKey = KeyCode.K;
    public int damageAmount = 15;
    [SerializeField] Health _playerHp; // ���

    void Update()
    {
        // ������� ������� �����
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // ����� ����
        if (Input.GetKeyDown(KeyCode.F5))
        {
            QuestEventBus.RaiseCollect(collectId, amount);
            Debug.Log($"[DEV] Collect: {collectId} +{amount}");
        }

        // ����� ������
        if (Input.GetKeyDown(KeyCode.F6))
        {
            QuestEventBus.RaiseCraft(craftId, amount);
            Debug.Log($"[DEV] Craft: {craftId} +{amount}");
        }

        // ������� ����� ������ ���������� ������
        if (Input.GetKeyDown(KeyCode.F4))
        {
            var qm = Object.FindFirstObjectByType<QuestManager>(FindObjectsInactive.Exclude);
            qm?.ActivateAnyAvailable();
        }

        // --- HOTKEYS: ����/������ ������ ---
        if (Input.GetKeyDown(damageKey))
        {
            var hp = GetPlayerHealth();
            if (hp != null) hp.ApplyDamage(new DamageInfo { amount = damageAmount });
        }

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(damageKey))
        {
            var hp = GetPlayerHealth();
            if (hp != null) hp.ApplyDamage(new DamageInfo { amount = hp.currentHP }); // �������� �� 0
        }
    }

    Health GetPlayerHealth()
    {
        if (_playerHp != null) return _playerHp;

        // �������� "�����": � Health ������ respawnPoint
        var all = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
        foreach (var h in all)
        {
            if (h != null && h.respawnPoint != null) { _playerHp = h; break; }
        }
        if (_playerHp == null)
        {
            Debug.LogWarning("[DEV] Player Health �� ������ (����� ������� respawnPoint � ���������� Health ������).");
        }
        return _playerHp;
    }
}