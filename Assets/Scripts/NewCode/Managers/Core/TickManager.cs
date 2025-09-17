using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-925)]
public class TickManager : MonoBehaviour
{
    [Range(1, 60)] public int ticksPerSecond = 10;
    public static event System.Action OnTick;
    Coroutine loop;
    void OnEnable() { loop = StartCoroutine(Loop()); }
    void OnDisable() { if (loop != null) StopCoroutine(loop); }
    IEnumerator Loop()
    {
        var wait = new WaitForSeconds(1f / Mathf.Max(1, ticksPerSecond));
        while (true) { OnTick?.Invoke(); yield return wait; }
    }
}