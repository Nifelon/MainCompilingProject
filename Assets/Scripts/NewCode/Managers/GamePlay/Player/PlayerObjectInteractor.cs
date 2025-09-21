using UnityEngine;
using UnityEngine.UI;

public class PlayerObjectInteractor : MonoBehaviour
{
    [SerializeField] private float interactRadius = 0.9f;
    [SerializeField] private LayerMask interactMask; // поставь слой "Interactable"
    [SerializeField] private ObjectManager objectManager;
    [SerializeField] private Text promptText; // опционально, UI-текст "E: собрать"

    void Reset()
    {
#if UNITY_2023_1_OR_NEWER
        objectManager = FindFirstObjectByType<ObjectManager>();
#else
        objectManager = FindObjectOfType<ObjectManager>();
#endif
    }

    void Update()
    {
        var pos = (Vector2)transform.position;
        var col = Physics2D.OverlapCircle(pos, interactRadius, interactMask);
        if (!col) { if (promptText) promptText.enabled = false; return; }

        var wref = col.GetComponent<WorldObjectRef>();
        if (!wref) { if (promptText) promptText.enabled = false; return; }

        // показать подсказку
        if (promptText) { promptText.enabled = true; promptText.text = "E: собрать"; }

        if (Input.GetKeyDown(KeyCode.E) && objectManager)
        {
            if (objectManager.TryHarvest(wref.id))
            {
                // можно добавить анимацию/звук/уведомление
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}