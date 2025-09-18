using UnityEngine;
using UnityEngine.UI;

public class HPBarUI : MonoBehaviour
{
    public Health target;
    public Slider slider;

    void Reset() { slider = GetComponent<Slider>(); }
    void LateUpdate()
    {
        if (!target || !slider) return;
        float v = (float)target.currentHP / Mathf.Max(1, target.maxHP);
        if (slider.value != v) slider.value = v;
    }
}