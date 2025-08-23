using UnityEngine;

// PSG1 Detection Script - Tests if Play Solana SDK is working
public class PSG1_Detection_Test : MonoBehaviour
{
    [Header("MILESTONE 1 - PSG1 SDK Detection")]
    public KeyCode testKey = KeyCode.P; // Press P to test PSG1
    
    void Start()
    {
        Debug.Log("🚀 MILESTONE 1: PSG1 Detection Script Started");
        TestPSG1Installation();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            TestPSG1Installation();
        }
    }
    
    void TestPSG1Installation()
    {
        Debug.Log("🔍 MILESTONE 1: Testing PSG1 SDK installation...");
        
        // Test 1: Check if PSG1 namespace exists
        System.Type psg1Type = System.Type.GetType("PlaySolana.Unity.SDK.PSG1Manager");
        if (psg1Type != null)
        {
            Debug.Log("✅ MILESTONE 1: PSG1Manager class found!");
        }
        else
        {
            Debug.Log("❌ MILESTONE 1: PSG1Manager class not found - trying alternative...");
            
            // Try alternative class names
            var alternativeTypes = new string[]
            {
                "PSG1.PSG1Manager",
                "PlaySolana.PSG1Manager",
                "PSG1Manager",
                "PlaySolana.Unity.PSG1"
            };
            
            bool found = false;
            foreach (string typeName in alternativeTypes)
            {
                var altType = System.Type.GetType(typeName);
                if (altType != null)
                {
                    Debug.Log($"✅ MILESTONE 1: Found PSG1 class: {typeName}");
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                Debug.LogWarning("⚠️ MILESTONE 1: PSG1 classes not detected. Check installation.");
            }
        }
        
        // Test 2: Check if PSG1 Input System is working
        TestPSG1InputSystem();
        
        // Test 3: List all available assemblies (for debugging)
        Debug.Log("📦 MILESTONE 1: Checking loaded assemblies for PSG1...");
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        bool psg1AssemblyFound = false;
        
        foreach (var assembly in assemblies)
        {
            if (assembly.FullName.Contains("PSG1") || 
                assembly.FullName.Contains("PlaySolana") || 
                assembly.FullName.Contains("Solana"))
            {
                Debug.Log($"📦 Found PSG1-related assembly: {assembly.FullName}");
                psg1AssemblyFound = true;
            }
        }
        
        if (!psg1AssemblyFound)
        {
            Debug.LogWarning("⚠️ MILESTONE 1: No PSG1 assemblies found in loaded assemblies");
        }
        
        Debug.Log("🧪 MILESTONE 1: PSG1 detection test complete!");
    }
    
    void TestPSG1InputSystem()
    {
        Debug.Log("🎮 MILESTONE 1: Testing PSG1 Input System...");
        
        // Test if PSG1 input bindings are working
        try
        {
            // Check if Unity's new Input System is available (PSG1 likely uses it)
            var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.InputSystem");
            if (inputSystemType != null)
            {
                Debug.Log("✅ MILESTONE 1: Unity Input System detected (required for PSG1)");
            }
            else
            {
                Debug.LogWarning("⚠️ MILESTONE 1: Unity Input System not found - PSG1 may not work");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ MILESTONE 1: Input System test failed: {e.Message}");
        }
    }
}