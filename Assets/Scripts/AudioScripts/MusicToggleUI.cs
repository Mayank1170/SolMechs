using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SolMechs.Audio;

[RequireComponent(typeof(Button))]
public class MusicToggleUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject iconOn;
    [SerializeField] private GameObject iconOff;

    [Header("Single Image (optional)")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite spriteOn;
    [SerializeField] private Sprite spriteOff;

    private void Awake()
    {
        if (!toggleButton) toggleButton = GetComponent<Button>();
        if (toggleButton) toggleButton.onClick.AddListener(OnToggleClicked);
        transform.SetAsLastSibling();
    }

    private void OnEnable()
    {
        StartCoroutine(EnsureAudioReadyThenRefresh());
    }

    private IEnumerator EnsureAudioReadyThenRefresh()
    {
        yield return new WaitUntil(() => AudioManager.Instance != null);
        yield return null; // wait one extra frame
        Refresh();
        AudioManager.Instance.ForceResumeIfPossible();
    }

    private void OnToggleClicked()
    {
        var am = AudioManager.Instance;
        if (am != null)
        {
            am.ToggleMusic();
            am.PlaySFX(SFXType.ButtonClick);
            am.PrimeOnUserInteraction(); // ensures music plays on browsers
        }
        else
        {
            Debug.LogWarning("[MusicToggleUI] AudioManager not ready yet.");
        }
        Refresh();
    }

    private void Refresh()
    {
        var am = AudioManager.Instance;
        bool musicOn = am != null && am.MusicEnabled;

        if (iconOn) iconOn.SetActive(musicOn);
        if (iconOff) iconOff.SetActive(!musicOn);

        if (targetImage)
        {
            if (musicOn && spriteOn) targetImage.sprite = spriteOn;
            else if (!musicOn && spriteOff) targetImage.sprite = spriteOff;
        }
    }

    private void OnDestroy()
    {
        if (toggleButton) toggleButton.onClick.RemoveListener(OnToggleClicked);
    }
}
