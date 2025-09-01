using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SolMechs.Audio;

[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private SFXType clickSfx = SFXType.ButtonClick;
    [SerializeField] private SFXType hoverSfx = SFXType.ButtonHover;

    private void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(() => AudioManager.Instance.PlaySFX(clickSfx));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySFX(hoverSfx);
    }
}
