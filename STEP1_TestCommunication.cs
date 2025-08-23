using UnityEngine;
using System.Runtime.InteropServices;

public class TestCommunication : MonoBehaviour
{
    [Header("Step 1 - Basic Communication Test")]
    public KeyCode testKey = KeyCode.T;
    
    // Simple JavaScript bridge function
    [DllImport("__Internal")]
    private static extern void LogToJS(string message);
    
    void Start()
    {
        Debug.Log("🚀 STEP 1: Communication Test Script Loaded");
    }
    
    void Update()
    {
        // Test communication when T key is pressed
        if (Input.GetKeyDown(testKey))
        {
            RunCommunicationTest();
        }
    }
    
    public void RunCommunicationTest()
    {
        Debug.Log("🧪 STEP 1: Testing Unity to JavaScript communication...");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            // Send message to JavaScript
            LogToJS("STEP1_TEST: Unity can communicate with JavaScript!");
        #else
            Debug.Log("📝 STEP 1: Communication test (Editor mode - will work in WebGL build)");
        #endif
        
        Debug.Log("✅ STEP 1: Communication test initiated - check browser console!");
    }
}