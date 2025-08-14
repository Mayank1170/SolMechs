using UnityEngine;
using UnityEngine.UI;

public class ArcHPBar : MonoBehaviour
{
    [Header("Images")]
    public Image fill;   // Image with Type=Filled, Method=Radial360
    public Image bg;     // optional track (also Radial360 with same arcLength)

    [Header("Arc Setup")]
    [Range(0f, 1f)] public float arcLength = 1f; // 1 = full circle, 0.75 = 270°
    public float startAngleDeg = -90f;           // -90 = topo

    [Header("Value")]
    [Range(0f, 1f)] public float value = 1f;

    void Reset()
    {
        if (!fill) fill = GetComponentInChildren<Image>();
    }

    void OnValidate() { Apply(); }
    public void Set01(float v) { value = Mathf.Clamp01(v); Apply(); }
    public void Configure(float startDeg, float arc01) { startAngleDeg = startDeg; arcLength = Mathf.Clamp01(arc01); Apply(); }

    void Apply()
    {
        if (!fill) return;

        SetupImage(fill);
        fill.rectTransform.localEulerAngles = new Vector3(0, 0, startAngleDeg);
        fill.fillAmount = Mathf.Clamp01(value) * Mathf.Clamp01(arcLength);

        if (bg)
        {
            SetupImage(bg);
            bg.rectTransform.localEulerAngles = new Vector3(0, 0, startAngleDeg);
            bg.fillAmount = Mathf.Clamp01(arcLength); // track mostra todo o arco
        }
    }

    static void SetupImage(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = 2;      // Top
        img.fillClockwise = true;
        img.preserveAspect = true;
    }
}
