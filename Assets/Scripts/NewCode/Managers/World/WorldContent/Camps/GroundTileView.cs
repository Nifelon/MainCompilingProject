using UnityEngine;

public class GroundTileView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite defaultSprite; // дефолт (из биома/теста)

    private void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (defaultSprite == null) defaultSprite = sr.sprite; // запомним исходник
    }

    public void SetDefaultSprite(Sprite s)
    {
        defaultSprite = s;
        sr.sprite = s;
    }

    public void ApplySpriteOverride(Sprite overrideSprite, bool hasOverride)
    {
        sr.sprite = hasOverride && overrideSprite ? overrideSprite : defaultSprite;
    }
}