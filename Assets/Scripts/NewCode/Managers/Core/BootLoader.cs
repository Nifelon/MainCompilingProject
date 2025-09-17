using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BootLoader : MonoBehaviour
{
    [SerializeField] string firstScene = "01_Main_A1";

    IEnumerator Start()
    {
        // ���� ���� GlobalCore ���������������������
        yield return null;

        if (string.IsNullOrEmpty(firstScene) || !Application.CanStreamedLevelBeLoaded(firstScene))
        {
            Debug.LogError($"[BootLoader] ����� '{firstScene}' �� �������. ������� ��� � Build Settings.");
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(firstScene, LoadSceneMode.Single);
        if (op == null) Debug.LogError("[BootLoader] LoadSceneAsync ������ null");
    }
}