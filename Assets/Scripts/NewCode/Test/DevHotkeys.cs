using UnityEngine;
using UnityEngine.SceneManagement;

public class DevHotkeys : MonoBehaviour
{
    [Header("IDs ������ ��������� � Quest.targetId")]
    public string collectId = "Berry"; // ��� ������ Collect
    public string craftId = "Patch"; // ��� ������ Craft
    public int amount = 1;

    void Update()
    {
        // ������� ������� ����� (������)
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // �������� �����
        if (Input.GetKeyDown(KeyCode.F5))
        {
            QuestEventBus.RaiseCollect(collectId, amount);
            Debug.Log($"[DEV] Collect: {collectId} +{amount}");
        }

        // �������� ������
        if (Input.GetKeyDown(KeyCode.F6))
        {
            QuestEventBus.RaiseCraft(craftId, amount);
            Debug.Log($"[DEV] Craft: {craftId} +{amount}");
        }

        // �� ������ ������ � ������ ������������ ��������� ��������� �����
        if (Input.GetKeyDown(KeyCode.F4))
        {
            var qm = Object.FindAnyObjectByType<QuestManager>(FindObjectsInactive.Exclude);
            qm?.ActivateAnyAvailable();
        }
    }
}