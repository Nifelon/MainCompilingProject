using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public struct GroundPaint
{
    public bool overrideSprite;    // �������� ������?
    public Sprite sprite;          // ������ ������ �� ������
    public bool useTint;           // ��������� �������?
    public Color tint;             // ��������� ����� (���������)

    public static GroundPaint FromSprite(Sprite s) => new GroundPaint { overrideSprite = s != null, sprite = s };
    public static GroundPaint FromTint(Color c) => new GroundPaint { useTint = true, tint = c };
}