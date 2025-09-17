using UnityEngine;
public class InteractOnKey : MonoBehaviour
{
    Collider npc; void OnTriggerEnter(Collider c) { npc = c; }
    void OnTriggerExit(Collider c) { if (c == npc) npc = null; }
    void Update() { if (npc && Input.GetKeyDown(KeyCode.E)) npc.SendMessage("Interact", SendMessageOptions.DontRequireReceiver); }
}