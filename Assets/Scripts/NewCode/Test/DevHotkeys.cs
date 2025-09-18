using UnityEngine;
using UnityEngine.SceneManagement;

public class DevHotkeys : MonoBehaviour
{
    [Header("IDs должны совпадать с Quest.targetId")]
    public string collectId = "Berry"; // дл€ квеста Collect
    public string craftId = "Patch"; // дл€ квеста Craft
    public int amount = 1;

    void Update()
    {
        // быстрый рестарт сцены (удобно)
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // эмул€ци€ сбора
        if (Input.GetKeyDown(KeyCode.F5))
        {
            QuestEventBus.RaiseCollect(collectId, amount);
            Debug.Log($"[DEV] Collect: {collectId} +{amount}");
        }

        // эмул€ци€ крафта
        if (Input.GetKeyDown(KeyCode.F6))
        {
            QuestEventBus.RaiseCraft(craftId, amount);
            Debug.Log($"[DEV] Craft: {craftId} +{amount}");
        }

        // на вс€кий случай Ч быстро активировать следующий доступный квест
        if (Input.GetKeyDown(KeyCode.F4))
        {
            var qm = Object.FindAnyObjectByType<QuestManager>(FindObjectsInactive.Exclude);
            qm?.ActivateAnyAvailable();
        }
    }
}