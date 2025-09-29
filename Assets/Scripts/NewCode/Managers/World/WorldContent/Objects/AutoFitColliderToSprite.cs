using UnityEngine;

namespace Game.World.Objects
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AutoFitColliderToSprite : MonoBehaviour
    {
        [SerializeField] private bool preferCapsuleForTallSprites = true;
        [SerializeField] private float tallAspectThreshold = 1.4f;

        void Reset() { Fit(); }
        void OnEnable() { Fit(); }

        public void Fit()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (!sr || !sr.sprite) return;

            var b = sr.sprite.bounds;
            var scale = transform.lossyScale;
            var aspect = b.size.y / Mathf.Max(0.0001f, b.size.x);

            if (preferCapsuleForTallSprites && aspect >= tallAspectThreshold)
            {
                if (TryGetComponent(out CapsuleCollider2D cap))
                {
                    cap.direction = CapsuleDirection2D.Vertical;
                    cap.size = new Vector2(b.size.x * scale.x, b.size.y * scale.y);
                    cap.offset = new Vector2(b.center.x * scale.x, b.center.y * scale.y);
                }
                var bc = GetComponent<BoxCollider2D>(); if (bc) bc.enabled = false;
            }
            else
            {
                var bc = GetComponent<BoxCollider2D>() ?? gameObject.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(b.size.x * scale.x, b.size.y * scale.y);
                bc.offset = new Vector2(b.center.x * scale.x, b.center.y * scale.y);
                var cc = GetComponent<CapsuleCollider2D>(); if (cc) cc.enabled = false;
            }
        }
    }
}