using UnityEngine;
#if UNITY_2D_ANIMATION
using UnityEngine.U2D.Animation;
#endif

namespace Game.World.NPC
{
    [CreateAssetMenu(menuName = "World/NPC/NpcSkin", fileName = "skin_")]
    public class NpcSkin : ScriptableObject
    {
        [Header("Portrait/UI (override)")]
        [Tooltip("���� �����, ��������� portrait �� ActorProfile ��� UI.")]
        public Sprite portraitOverride;

        [Header("Animator")]
        public AnimatorOverrideController animatorOverride; // �����������, �������� ����� �������� ���������

        [Header("Rendering")]
        public Material materialOverride;                   // �����������
        public Color colorTint = Color.white;

#if UNITY_2D_ANIMATION
        [Header("2D Animation (Sprite Library)")]
        public SpriteLibraryAsset spriteLibrary;            // ���� ����������� SpriteLibrary/SpriteResolver
#endif

        [Header("Fallback (��� 2D Animation)")]
        public Sprite baseSprite;                           // ��� �������� SpriteRenderer
    }
}