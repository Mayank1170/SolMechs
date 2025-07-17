using UnityEngine;

public class ImpactEffect : MonoBehaviour
{
    public Renderer targetRenderer;
    public Color flashColor = Color.red;
    public float flashDuration = 0.2f;

    private Color originalColor;
    private float timer;
    private bool flashing = false;

    void Start()
    {
        if (targetRenderer != null)
            originalColor = targetRenderer.material.color;
    }

    public void TriggerImpact()
    {
        if (targetRenderer == null) return;

        originalColor = targetRenderer.material.color;
        targetRenderer.material.color = flashColor;
        timer = flashDuration;
        flashing = true;
    }

    void Update()
    {
        if (flashing)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                targetRenderer.material.color = originalColor;
                flashing = false;
            }
        }
    }
}