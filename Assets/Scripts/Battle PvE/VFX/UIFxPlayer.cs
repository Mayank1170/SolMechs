using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI pixel-art FX player:
/// - Works under a Canvas (Screen Space Overlay or Camera)
/// - Expect an Image + Animator on the same GameObject
/// - Flips on X for right-to-left attacks
/// - Optional local offset
/// - Auto-destroys at the end (via Animation Event or fallback timer)
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Animator))]
public class UIFxPlayer : MonoBehaviour
{
    [Header("Placement")]
    public Vector2 localOffset = Vector2.zero;   // small tweak per prefab
    public bool matchAnchorPivot = true;         // keep parent pivot/anchors

    [Header("Lifetime")]
    public float fallbackLifetime = 1.0f;        // if no animation event fires

    private RectTransform _rt;
    private Image _img;
    private Animator _anim;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _img = GetComponent<Image>();
        _anim = GetComponent<Animator>();
        if (_img) _img.preserveAspect = true;
    }

    /// <summary>
    /// Spawns under 'parent', sets flip according to 'fromLeftToRight'.
    /// </summary>
    public void PlayUnder(Transform parent, bool fromLeftToRight)
    {
        transform.SetParent(parent, false);

        if (matchAnchorPivot)
        {
            var prt = parent as RectTransform;
            if (prt)
            {
                _rt.anchorMin = prt.anchorMin;
                _rt.anchorMax = prt.anchorMax;
                _rt.pivot = prt.pivot;
            }
        }

        _rt.anchoredPosition = localOffset;

        // flip horizontally when needed (for enemy → player, usually false)
        var s = _rt.localScale;
        s.x = Mathf.Abs(s.x) * (fromLeftToRight ? 1f : -1f);
        _rt.localScale = s;

        if (_anim) _anim.Play(0, 0, 0f);  // play default state
        if (fallbackLifetime > 0f) Destroy(gameObject, fallbackLifetime);
    }

    /// <summary>
    /// Optional Animation Event hook to destroy exactly on last frame.
    /// Put an Animation Event calling OnFxComplete() at the end of the clip.
    /// </summary>
    public void OnFxComplete()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Optional Animation Event hook to sync a "hit" moment with battle logic.
    /// (not used in this first integration; kept for later)
    /// </summary>
    public void OnFxHit()
    {
        // Intentionally blank for now.
        // In the future you can invoke a callback or audio here.
    }
}
