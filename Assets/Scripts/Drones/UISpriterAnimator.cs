using UnityEngine;
using UnityEngine.UI;

public class UISpriteAnimator : MonoBehaviour
{
    private Image image;
    private Sprite[] frames;
    private float fps;
    private float timer;
    private int currentFrame;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    public void SetAnimation(Sprite[] animFrames, float animFPS)
    {
        frames = animFrames;
        fps = animFPS;
        currentFrame = 0;
        timer = 0f;

        if (frames != null && frames.Length > 0 && image != null)
        {
            image.sprite = frames[0];
        }
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || image == null) return;

        timer += Time.deltaTime;

        if (timer >= 1f / fps)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            image.sprite = frames[currentFrame];
        }
    }
}