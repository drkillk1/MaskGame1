using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    // Singleton pattern to ensure only one GameManager exists
    public static GameManager Instance { get; private set; }
    
    // Player reference
    private GameObject player;
    private Vector3 lastCheckpointPosition;
    
    // Canvas and TextMeshPro references (for any optional UI you might add later)
    private Canvas mainCanvas;
    private Dictionary<string, TextMeshProUGUI> sceneTextElements = new Dictionary<string, TextMeshProUGUI>();
    
    // Scene management
    private string currentSceneName;
    private List<string> levelOrder = new List<string>();
    private int currentLevelIndex = 0;
    
    // Current active text (optional for any UI elements)
    private string currentActiveTextName = "";
    
    // Level transition settings
    [SerializeField] private float levelTransitionDelay = 0.5f;
    
    void Awake()
    {
        // Singleton implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            
            // Initialize level order (customize this with your scene names)
            levelOrder = new List<string> 
            { 
                "Tutorial Scene",
                "Level 1", 
                "Level 2", 
                "Level 3", 
                // Add more levels as needed
            };
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        currentLevelIndex = levelOrder.IndexOf(currentSceneName);
        InitializeCanvasAndTexts(); // Optional - keep for any UI
        FindPlayer();
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        currentLevelIndex = levelOrder.IndexOf(currentSceneName);
        InitializeCanvasAndTexts(); // Optional - keep for any UI
        FindPlayer();
        
        // Teleport player to checkpoint if they have one
        if (player != null && lastCheckpointPosition != Vector3.zero)
        {
            player.transform.position = lastCheckpointPosition;
        }
    }
    
    void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
        // If no player found, log warning (player might be instantiated later)
        if (player == null)
        {
            Debug.LogWarning("Player not found in scene. Make sure your player has the 'Player' tag.");
        }
    }
    
    void InitializeCanvasAndTexts()
    {
        // Find the main Canvas in the scene (optional)
        mainCanvas = FindObjectOfType<Canvas>();
        
        if (mainCanvas != null)
        {
            // Clear previous scene's text references
            sceneTextElements.Clear();
            
            // Find all TextMeshProUGUI components in the Canvas
            TextMeshProUGUI[] allTexts = mainCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            
            foreach (TextMeshProUGUI text in allTexts)
            {
                // Add to dictionary with the GameObject name as key
                sceneTextElements[text.gameObject.name] = text;
                text.gameObject.SetActive(false); // Start with all text hidden
                Debug.Log($"Found TextMeshPro: '{text.gameObject.name}'"); // Add this line
            }
        }
        
        // Hide any previously active text from previous scene
        currentActiveTextName = "";
    }
    
    void Update()
    {
        // Optional: Add debug shortcuts for testing
        if (Input.GetKeyDown(KeyCode.N))
        {
            LoadNextLevel();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadCurrentLevel();
        }
    }
    
    // Called by trigger colliders when player enters
    public void OnPlayerEnterTrigger(TriggerType triggerType, string triggerName = "")
    {
        Debug.Log($"Trigger exited - Type: {triggerType}, Name: '{triggerName}'");

        switch (triggerType)
        {
            case TriggerType.LevelEnd:
                // You can either load immediately or with a delay
                if (levelTransitionDelay > 0)
                {
                    Invoke("LoadNextLevel", levelTransitionDelay);
                }
                else
                {
                    LoadNextLevel();
                }
                break;
                
            case TriggerType.MessageArea:
                // Optional: if you want to keep text display functionality
                if (!string.IsNullOrEmpty(triggerName))
                {
                    ShowTextForTrigger(triggerName);
                }
                break;
                
            case TriggerType.Checkpoint:
                if (player != null)
                {
                    lastCheckpointPosition = player.transform.position;
                    Debug.Log("Checkpoint saved at: " + lastCheckpointPosition);
                }
                break;
                
            case TriggerType.DeathZone:
                RespawnPlayer();
                break;
                
            case TriggerType.LoadSpecificLevel:
                // If triggerName contains a scene name, load that specific scene
                if (!string.IsNullOrEmpty(triggerName))
                {
                    LoadSpecificLevel(triggerName);
                }
                break;
        }
    }
    
    // Called by trigger colliders when player exits
    public void OnPlayerExitTrigger(TriggerType triggerType, string triggerName = "")
    {
        switch (triggerType)
        {
            case TriggerType.MessageArea:
                // Optional: if you want to keep text display functionality
                if (!string.IsNullOrEmpty(triggerName))
                {
                    HideTextForTrigger(triggerName);
                }
                break;
        }
    }
    
    // Optional text display methods (keep if you want UI functionality)
    // Replace the existing ShowTextForTrigger method with this:
void ShowTextForTrigger(string triggerName)
{
    if (sceneTextElements.Count == 0) 
    {
        Debug.LogWarning("No TextMeshPro elements found in Canvas");
        return;
    }
    
    Debug.Log($"Looking for text to show for trigger: {triggerName}");
    
    // Hide any currently active text first
    if (!string.IsNullOrEmpty(currentActiveTextName))
    {
        HideTextForTrigger(currentActiveTextName);
    }
    
    // Try multiple naming patterns to find the correct text
    string[] possibleTextNames = {
        triggerName,  // Direct match (if trigger is named exactly like text)
        triggerName.Replace("Trigger", "Text"),  // "ExplanationTrigger" → "ExplanationText"
        triggerName.Replace("Trigger", ""),      // "ExplanationTrigger" → "Explanation"
        triggerName + "Text",                    // "Explanation" → "ExplanationText"
        triggerName.Replace("Area", "Text"),     // "ExplanationArea" → "ExplanationText"
        triggerName.Replace("Zone", "Text"),     // "ExplanationZone" → "ExplanationText"
        "TutorialText",                          // Fallback for tutorial areas
        "HintText",                              // Fallback for hint areas
        "InfoText"                               // Fallback for info areas
    };
    
    foreach (string textName in possibleTextNames)
    {
        if (sceneTextElements.ContainsKey(textName))
        {
            sceneTextElements[textName].gameObject.SetActive(true);
            currentActiveTextName = triggerName;
            Debug.Log($"Successfully showing text: {textName} for trigger: {triggerName}");
            return;
        }
    }
    
    // Debug: List all available text elements
    Debug.LogWarning($"No TextMeshPro element found for trigger: {triggerName}");
    Debug.Log("Available text elements in scene:");
    foreach (var key in sceneTextElements.Keys)
    {
        Debug.Log($"- '{key}'");
    }
}

// Replace the existing HideTextForTrigger method with this:
void HideTextForTrigger(string triggerName)
{
    if (currentActiveTextName != triggerName)
    {
        // Not the active trigger, so don't hide anything
        return;
    }
    
    // Try the same naming patterns to find and hide the text
    string[] possibleTextNames = {
        triggerName,
        triggerName.Replace("Trigger", "Text"),
        triggerName.Replace("Trigger", ""),
        triggerName + "Text",
        triggerName.Replace("Area", "Text"),
        triggerName.Replace("Zone", "Text"),
        "TutorialText",
        "HintText",
        "InfoText"
    };
    
    foreach (string textName in possibleTextNames)
    {
        if (sceneTextElements.ContainsKey(textName))
        {
            sceneTextElements[textName].gameObject.SetActive(false);
            Debug.Log($"Hiding text: {textName} for trigger: {triggerName}");
            break;
        }
    }
    
    currentActiveTextName = "";
}
    
    public void LoadNextLevel()
    {
        // Cancel any pending invokes to prevent multiple loads
        CancelInvoke("LoadNextLevel");
        
        if (currentLevelIndex < levelOrder.Count - 1)
        {
            currentLevelIndex++;
            SceneManager.LoadScene(levelOrder[currentLevelIndex]);
        }
        else
        {
            // Game completed - load main menu or credits
            Debug.Log("Game Completed!");
            SceneManager.LoadScene("MainMenu");
        }
    }
    
    public void LoadSpecificLevel(string levelName)
    {
        // Load a specific level by name
        if (Application.CanStreamedLevelBeLoaded(levelName))
        {
            SceneManager.LoadScene(levelName);
            // Update current level index if it's in our level order
            int newIndex = levelOrder.IndexOf(levelName);
            if (newIndex >= 0)
            {
                currentLevelIndex = newIndex;
            }
        }
        else
        {
            Debug.LogError($"Cannot load level: {levelName}. Make sure it's added to Build Settings.");
        }
    }
    
    public void ReloadCurrentLevel()
    {
        SceneManager.LoadScene(currentSceneName);
    }
    
    void RespawnPlayer()
    {
        if (player != null)
        {
            if (lastCheckpointPosition != Vector3.zero)
            {
                player.transform.position = lastCheckpointPosition;
            }
            else
            {
                ReloadCurrentLevel();
            }
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    // Public getters for other scripts
    public GameObject GetPlayer() => player;
    public string GetCurrentSceneName() => currentSceneName;
    public int GetCurrentLevelIndex() => currentLevelIndex;
}

// Updated enum with additional trigger types
public enum TriggerType
{
    LevelEnd,           // Loads next level in sequence
    MessageArea,        // Optional: For UI text display
    Checkpoint,         // Saves player position
    DeathZone,          // Respawns player at checkpoint
    LoadSpecificLevel   // Loads a specific level by name
}