using UnityEngine;
public struct DamageInfo
{
    public Transform source, target;
    public int amount;
    public bool isCrit;
    public Vector3 worldHitPos;
}