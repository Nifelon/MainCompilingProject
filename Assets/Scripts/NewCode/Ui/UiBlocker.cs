using UnityEngine;

/// ����� �� �������� GO ���� (��� ��, ��� CanvasGroup).
/// ���� ������ ������� � ����������� ���� �����������, � UI ����� �����.
[RequireComponent(typeof(CanvasGroup))]
public class UiBlocker : MonoBehaviour
{
    CanvasGroup _group;

    void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        if (_group) { _group.blocksRaycasts = true; /* ����� ����� ����� */ }
    }

    void OnEnable()
    {
        GlobalCore.Instance?.GameManager?.SetUiBlock(true);
        if (_group) { _group.interactable = true; _group.alpha = Mathf.Max(_group.alpha, 1f); }
    }

    void OnDisable()
    {
        GlobalCore.Instance?.GameManager?.SetUiBlock(false);
        if (_group) { _group.interactable = false; }
    }
}