using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimpleProfileHUD : MonoBehaviour
{
    [Header("Profile Display")]
    public Image profilePictureImage;
    public Text playerNameText;

    [Header("Profile Picture Settings")]
    public Sprite defaultPFPSprite;
    public Sprite[] availablePFPs; // Array of available profile pictures

    [Header("Placeholder Settings")]
    public Color placeholderTint = new Color(0.7f, 0.7f, 0.7f, 1f);
    public string placeholderName = "Pilot";

    [Header("Future: Multi-Mech Display")]
    public Text remainingMechsText; // For future use
    [SerializeField] private int totalMechs = 1; // Will be expanded later
    [SerializeField] private int currentMechIndex = 0; // Will be expanded later

    // Private variables
    private Sprite currentPFP;
    private bool hasPFP = false;
    private string currentPlayerName;

    void Start()
    {
        InitializeProfile();
        UpdateDisplay();
    }

    private void InitializeProfile()
    {
        currentPlayerName = placeholderName;

        // Set default PFP
        if (defaultPFPSprite != null)
        {
            currentPFP = defaultPFPSprite;
        }
        else
        {
            CreateDefaultPlaceholder();
        }
    }

    private void CreateDefaultPlaceholder()
    {
        // Create a simple colored texture as placeholder
        Texture2D defaultTexture = new Texture2D(128, 128);
        Color[] pixels = new Color[128 * 128];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = placeholderTint;
        }

        defaultTexture.SetPixels(pixels);
        defaultTexture.Apply();

        currentPFP = Sprite.Create(defaultTexture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
    }

    public void UpdateDisplay()
    {
        // Update profile picture
        if (profilePictureImage != null)
        {
            profilePictureImage.sprite = currentPFP;
            profilePictureImage.color = hasPFP ? Color.white : placeholderTint;
        }

        // Update player name
        if (playerNameText != null)
        {
            playerNameText.text = currentPlayerName;
            playerNameText.color = hasPFP ? Color.white : placeholderTint;
        }

        // Future: Update remaining mechs display
        if (remainingMechsText != null)
        {
            remainingMechsText.text = $"Mechs: {totalMechs}";
        }
    }

    // Public methods for updating profile data

    public void SetProfilePicture(Sprite newPFP)
    {
        if (newPFP != null)
        {
            currentPFP = newPFP;
            hasPFP = true;
        }
        else
        {
            currentPFP = defaultPFPSprite;
            hasPFP = false;
        }

        UpdateDisplay();
    }

    public void SetProfilePictureByIndex(int pfpIndex)
    {
        if (availablePFPs != null && pfpIndex >= 0 && pfpIndex < availablePFPs.Length)
        {
            SetProfilePicture(availablePFPs[pfpIndex]);
        }
    }

    public void SetPlayerName(string name)
    {
        currentPlayerName = string.IsNullOrEmpty(name) ? placeholderName : name;
        UpdateDisplay();
    }

    public void SetPlayerProfile(string name, Sprite pfp = null)
    {
        SetPlayerName(name);
        if (pfp != null)
        {
            SetProfilePicture(pfp);
        }
    }

    // Future methods for multi-mech battles
    public void SetMechCount(int mechCount)
    {
        totalMechs = Mathf.Max(1, mechCount);
        UpdateDisplay();
    }

    public void SetCurrentMech(int mechIndex)
    {
        currentMechIndex = Mathf.Clamp(mechIndex, 0, totalMechs - 1);
        // Future: Will update mech-specific display
        UpdateDisplay();
    }

    // Utility methods
    public bool HasCustomPFP()
    {
        return hasPFP;
    }

    public string GetCurrentPlayerName()
    {
        return currentPlayerName;
    }

    public int GetTotalMechs()
    {
        return totalMechs;
    }

    public int GetCurrentMechIndex()
    {
        return currentMechIndex;
    }

    // Test methods
    [ContextMenu("Test Random PFP")]
    public void TestRandomPFP()
    {
        if (availablePFPs != null && availablePFPs.Length > 0)
        {
            int randomIndex = Random.Range(0, availablePFPs.Length);
            SetProfilePictureByIndex(randomIndex);
        }
    }

    [ContextMenu("Test Set Name")]
    public void TestSetName()
    {
        string[] testNames = { "CyberPilot", "NeonRider", "MatrixHacker", "SolMech01" };
        string testName = testNames[Random.Range(0, testNames.Length)];
        SetPlayerName(testName);
    }

    [ContextMenu("Reset to Placeholder")]
    public void ResetToPlaceholder()
    {
        SetPlayerName(placeholderName);
        SetProfilePicture(null);
    }
}