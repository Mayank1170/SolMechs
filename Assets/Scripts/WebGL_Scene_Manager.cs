using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// WebGL-compatible Scene Manager
public class WebGL_Scene_Manager : MonoBehaviour
{
    [Header("Scene Management for WebGL")]
    public bool useWebGLCompatibleScenes = true;
    
    [Header("Scene Names")]
    public string titleScene = "0_TitleConnect";
    public string mainMenuScene = "1_Main_Menu"; 
    public string mechEditorScene = "2_MechEditorScene";
    public string battleScene = "3_PilotMechBattleV1";
    public string pvpScene = "4_PlayPvP";
    public string postBattleScene = "5_PostBattle";
    public string localPvPScene = "6_LocalPvP";
    
    private PSG1_WebGL_Controller psg1Controller;
    
    void Start()
    {
        // Find PSG1 controller
        psg1Controller = FindObjectOfType<PSG1_WebGL_Controller>();
        
        Debug.Log("🌐 WebGL Scene Manager ready");
    }
    
    // WebGL-compatible scene loading
    public void LoadSceneWebGL(string sceneName)
    {
        Debug.Log($"🌐 Loading scene for WebGL: {sceneName}");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            if (useWebGLCompatibleScenes)
            {
                StartCoroutine(LoadSceneAsyncWebGL(sceneName));
            }
            else
            {
                // Fallback to direct scene load
                SceneManager.LoadScene(sceneName);
            }
        #else
            // In Editor, use normal scene loading
            SceneManager.LoadScene(sceneName);
        #endif
    }
    
    public void LoadSceneWebGL(int sceneIndex)
    {
        string sceneName = GetSceneNameFromIndex(sceneIndex);
        LoadSceneWebGL(sceneName);
    }
    
    // Async scene loading for WebGL
    IEnumerator LoadSceneAsyncWebGL(string sceneName)
    {
        Debug.Log($"🔄 Async loading scene: {sceneName}");
        
        // Show loading indicator if PSG1 is available
        if (psg1Controller != null)
        {
            psg1Controller.ShowPSG1Screen();
        }
        
        // Start loading the scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Wait until the scene loads
        while (!asyncLoad.isDone)
        {
            // You can add a loading bar here
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            Debug.Log($"⏳ Loading progress: {progress * 100}%");
            yield return null;
        }
        
        Debug.Log($"✅ Scene loaded successfully: {sceneName}");
        
        // After scene loads, decide whether to show PSG1 or game
        yield return new WaitForSeconds(0.5f); // Small delay for stability
        
        if (psg1Controller != null && IsGameScene(sceneName))
        {
            // If it's a game scene, show PSG1 first
            psg1Controller.ShowPSG1Screen();
        }
    }
    
    bool IsGameScene(string sceneName)
    {
        return sceneName == battleScene || 
               sceneName == pvpScene || 
               sceneName == localPvPScene;
    }
    
    string GetSceneNameFromIndex(int index)
    {
        switch (index)
        {
            case 0: return titleScene;
            case 1: return mainMenuScene;
            case 2: return mechEditorScene;
            case 3: return battleScene;
            case 4: return pvpScene;
            case 5: return postBattleScene;
            case 6: return localPvPScene;
            default: return mainMenuScene;
        }
    }
    
    // Public methods for UI buttons
    public void GoToTitleScreen()
    {
        LoadSceneWebGL(titleScene);
    }
    
    public void GoToMainMenu()
    {
        LoadSceneWebGL(mainMenuScene);
    }
    
    public void GoToMechEditor()
    {
        LoadSceneWebGL(mechEditorScene);
    }
    
    public void GoToBattle()
    {
        LoadSceneWebGL(battleScene);
    }
    
    public void GoToPvP()
    {
        LoadSceneWebGL(pvpScene);
    }
    
    public void GoToPostBattle()
    {
        LoadSceneWebGL(postBattleScene);
    }
    
    public void GoToLocalPvP()
    {
        LoadSceneWebGL(localPvPScene);
    }
    
    // Restart current scene
    public void RestartCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadSceneWebGL(currentScene);
    }
    
    // Quit application (WebGL compatible)
    public void QuitApplication()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("🌐 Cannot quit in WebGL - redirecting to main menu");
            GoToMainMenu();
        #else
            Application.Quit();
        #endif
    }
}