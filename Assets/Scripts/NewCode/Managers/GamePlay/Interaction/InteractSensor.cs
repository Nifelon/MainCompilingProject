using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class InteractSensor : MonoBehaviour
{
    IInteractable _target;

    void OnTriggerEnter2D(Collider2D c)
    {
        var it = c.GetComponent<IInteractable>();
        if (it != null) { _target = it; InteractPrompt.Instance?.Show(it.Hint); }
    }
    void OnTriggerExit2D(Collider2D c)
    {
        if (c.GetComponent<IInteractable>() == _target)
        {
            _target = null; InteractPrompt.Instance?.Hide();
        }
    }
    void Update()
    {
        if (_target != null && Input.GetKeyDown(KeyCode.E))
            _target.Interact(gameObject);
    }
}