using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public struct GroundPaint
{
    public bool overrideSprite;    // заменить спрайт?
    public Sprite sprite;          // спрайт «ковра» на тайлах
    public bool useTint;           // применить оттенок?
    public Color tint;             // множитель цвета (умножение)

    public static GroundPaint FromSprite(Sprite s) => new GroundPaint { overrideSprite = s != null, sprite = s };
    public static GroundPaint FromTint(Color c) => new GroundPaint { useTint = true, tint = c };
}