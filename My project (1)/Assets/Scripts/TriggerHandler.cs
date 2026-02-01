using UnityEngine;

public class TriggerHandler : MonoBehaviour
{
    [Header("Trigger Type")]
    [SerializeField] private TriggerType triggerType = TriggerType.MessageArea;
    
    [Header("Trigger Settings")]
    [Tooltip("Name that will be sent to GameManager to find matching TextMeshPro")]
    [SerializeField] private string triggerName = "";
    
    [Header("For LoadSpecificLevel Only")]
    [Tooltip("Scene name to load (must be in Build Settings)")]
    [SerializeField] private string targetSceneName = "";
    
    [Header("Options")]
    [SerializeField] private bool oneTimeTrigger = false;
    
    private bool hasBeenTriggered = false;
    
    void Start()
    {
        // Auto-fill triggerName if empty
        if (string.IsNullOrEmpty(triggerName))
        {
            triggerName = gameObject.name;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (oneTimeTrigger && hasBeenTriggered) return;
            
            // Prepare the parameter to send to GameManager
            string parameter = GetTriggerParameter();
            
            // Call GameManager
            GameManager.Instance.OnPlayerEnterTrigger(triggerType, parameter);
            hasBeenTriggered = true;
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Only handle exit for MessageArea triggers
            if (triggerType == TriggerType.MessageArea)
            {
                GameManager.Instance.OnPlayerExitTrigger(triggerType, triggerName);
            }
        }
    }
    
    string GetTriggerParameter()
    {
        switch (triggerType)
        {
            case TriggerType.LevelEnd:
                // No parameter needed for LevelEnd
                return "";
                
            case TriggerType.MessageArea:
                // Send the triggerName to find matching text
                return triggerName;
                
            case TriggerType.Checkpoint:
                // No parameter needed for Checkpoint
                return "";
                
            case TriggerType.DeathZone:
                // No parameter needed for DeathZone
                return "";
                
            case TriggerType.LoadSpecificLevel:
                // Send the scene name to load
                return targetSceneName;
                
            default:
                return "";
        }
    }
    
    // For 3D triggers (optional - use if your game is 3D)
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (oneTimeTrigger && hasBeenTriggered) return;
            
            string parameter = GetTriggerParameter();
            GameManager.Instance.OnPlayerEnterTrigger(triggerType, parameter);
            hasBeenTriggered = true;
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && triggerType == TriggerType.MessageArea)
        {
            GameManager.Instance.OnPlayerExitTrigger(triggerType, triggerName);
        }
    }
    
    void OnDrawGizmos()
    {
        // Visualize trigger in editor with different colors
        Collider2D collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            Gizmos.color = GetTriggerColor();
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (collider2D is BoxCollider2D boxCollider)
            {
                Gizmos.DrawWireCube(boxCollider.offset, boxCollider.size);
            }
            else if (collider2D is CircleCollider2D circleCollider)
            {
                Gizmos.DrawWireSphere(circleCollider.offset, circleCollider.radius);
            }
        }
    }
    
    Color GetTriggerColor()
    {
        switch (triggerType)
        {
            case TriggerType.LevelEnd: return Color.green;
            case TriggerType.MessageArea: return Color.cyan;
            case TriggerType.Checkpoint: return Color.yellow;
            case TriggerType.DeathZone: return Color.red;
            case TriggerType.LoadSpecificLevel: return Color.blue;
            default: return Color.white;
        }
    }
}