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
        [Tooltip("≈сли задан, перекроет portrait из ActorProfile дл€ UI.")]
        public Sprite portraitOverride;

        [Header("Animator")]
        public AnimatorOverrideController animatorOverride; // опционально, замен€ет клипы базового аниматора

        [Header("Rendering")]
        public Material materialOverride;                   // опционально
        public Color colorTint = Color.white;

#if UNITY_2D_ANIMATION
        [Header("2D Animation (Sprite Library)")]
        public SpriteLibraryAsset spriteLibrary;            // если используешь SpriteLibrary/SpriteResolver
#endif

        [Header("Fallback (без 2D Animation)")]
        public Sprite baseSprite;                           // дл€ простого SpriteRenderer
    }
}