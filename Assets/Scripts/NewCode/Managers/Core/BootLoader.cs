using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BootLoader : MonoBehaviour
{
    [SerializeField] string firstScene = "01_Main_A1";

    IEnumerator Start()
    {
        // дать шанс GlobalCore проинициализироваться
        yield return null;

        if (string.IsNullOrEmpty(firstScene) || !Application.CanStreamedLevelBeLoaded(firstScene))
        {
            Debug.LogError($"[BootLoader] Сцена '{firstScene}' не найдена. Проверь имя и Build Settings.");
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(firstScene, LoadSceneMode.Single);
        if (op == null) Debug.LogError("[BootLoader] LoadSceneAsync вернул null");
    }
}