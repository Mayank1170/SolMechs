using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BattleResultPanel : MonoBehaviour
{
    [Header("Wiring")]
    public CanvasGroup group;        // CanvasGroup on the overlay root
    public Text titleText;           // Use TMP_Text if preferred
    public Text pointsText;          // Optional
    public Image iconImage;          // Icon that swaps on win/lose
    public Button btnAgain;          // Play again button
    public Button btnMenu;           // Back to menu button

    [Header("Visuals")]
    public string victoryTitle = "VICTORY!";
    public string defeatTitle = "DEFEAT!";
    public Sprite victoryIcon;
    public Sprite defeatIcon;
    public float fadeTime = 0.15f;

    [Header("Navigation")]
    [Tooltip("Scene to load when the Menu button is clicked.")]
    public string mainMenuSceneName = "1_Main_Menu";
    [Tooltip("If true, automatically wires default listeners to the buttons.")]
    public bool autoWireButtons = true;

    void Awake()
    {
        // Ensure CanvasGroup
        if (!group) group = GetComponent<CanvasGroup>();

        // Start hidden and non-interactable
        if (group)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        // Auto-wire default button listeners (you can disable and use Inspector OnClick instead)
        if (autoWireButtons)
        {
            if (btnAgain)
            {
                btnAgain.onClick.RemoveAllListeners();
                btnAgain.onClick.AddListener(PlayAgain);
            }
            if (btnMenu)
            {
                btnMenu.onClick.RemoveAllListeners();
                btnMenu.onClick.AddListener(GoToMenu);
            }
        }
    }

    // ---------- API called by UIManager ----------
    public void Show(bool playerWon, int points = 0)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        // Title and points
        if (titleText) titleText.text = playerWon ? victoryTitle : defeatTitle;
        if (pointsText) pointsText.text = points > 0 ? $"+{points} pts" : string.Empty;

        // Icon swap (keep the behavior that worked before)
        if (iconImage)
        {
            var target = playerWon ? victoryIcon : defeatIcon;
            iconImage.enabled = target != null;
            iconImage.sprite = target;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            // Optional: iconImage.SetNativeSize();
        }

        StopAllCoroutines();
        StartCoroutine(Fade(1f, true));
    }

    public void Hide()
    {
        // If already inactive in hierarchy, just ensure a clean state
        if (!gameObject.activeInHierarchy)
        {
            if (group)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            return;
        }

        StopAllCoroutines();
        StartCoroutine(Fade(0f, false));
    }

    public void HideImmediate()
    {
        if (!group) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    // ---------- Default button actions (replace in Inspector if desired) ----------
    public void PlayAgain()
    {
        // Reload current scene
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    public void GoToMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogWarning("[BattleResultPanel] mainMenuSceneName is empty.");
    }

    // ---------- Fade helper ----------
    private IEnumerator Fade(float targetAlpha, bool enableInputAtEnd)
    {
        if (!group) yield break;

        // Accept clicks during fade-in
        if (targetAlpha > 0f)
        {
            group.blocksRaycasts = true;
            group.interactable = true;
        }

        float start = group.alpha;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / fadeTime));
            yield return null;
        }
        group.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            // At hide: cut interaction and deactivate GameObject
            group.interactable = false;
            group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
        else
        {
            // At show: ensure interaction
            group.interactable = enableInputAtEnd;
            group.blocksRaycasts = enableInputAtEnd;
        }
    }
}
