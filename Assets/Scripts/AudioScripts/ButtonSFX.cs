using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SolMechs.Audio;

[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private SFXType clickSfx = SFXType.ButtonClick;
    [SerializeField] private SFXType hoverSfx = SFXType.ButtonHover;
    [SerializeField] private bool enableHoverSound = true;
    [SerializeField] private bool enableClickSound = true;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button && enableClickSound)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnButtonClick()
    {
        if (AudioManager.Instance != null && enableClickSound)
        {
            AudioManager.Instance.PlaySFX(clickSfx);
        }
        else if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[ButtonSFX] AudioManager not available for click sound on {gameObject.name}");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only play hover sound if button is interactable
        if (button != null && !button.interactable) return;

        if (AudioManager.Instance != null && enableHoverSound)
        {
            AudioManager.Instance.PlaySFX(hoverSfx);
        }
        else if (AudioManager.Instance == null && enableHoverSound)
        {
            Debug.LogWarning($"[ButtonSFX] AudioManager not available for hover sound on {gameObject.name}");
        }
    }

    private void OnDestroy()
    {
        if (button)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    // Optional: Method to change SFX types at runtime
    public void SetSFX(SFXType newClickSfx, SFXType newHoverSfx)
    {
        clickSfx = newClickSfx;
        hoverSfx = newHoverSfx;
    }
}