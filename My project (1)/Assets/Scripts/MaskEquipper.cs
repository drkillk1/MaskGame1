using UnityEngine;

public class MaskEquipper : MonoBehaviour
{
    [Header("References")]
    public PlayerController controller;            // drag your PlayerController here
    public SpriteRenderer maskSpriteRenderer;      // drag the MaskVisual SpriteRenderer here

    public SpriteRenderer playerRenderer;

    public GameObject player;

    [Header("Default state")]
    public Sprite defaultMaskSprite;               // optional (none equipped)

    private void Awake()
    {
        if (controller == null) controller = GetComponent<PlayerController>();
    }

    public void Equip(MaskData mask, Sprite spriteOverride = null)
    {
        if (controller == null)
        {
            Debug.LogWarning("MaskEquipper: Missing PlayerController reference.");
            return;
        }

        controller.EquipMask(mask);

        // Update visual
        if (maskSpriteRenderer != null)
        {
            var chosen = spriteOverride != null ? spriteOverride : null;
            maskSpriteRenderer.sprite = chosen;
            maskSpriteRenderer.enabled = (maskSpriteRenderer.sprite != null);
            playerRenderer.sprite = null;
            //player.transform.localScale = new Vector3(4.7f, 4.7f, 4.7f);
        }
    }

    public void ClearMask()
    {
        if (controller != null && controller.currentMask != null)
        {
            // If you want "None", you can add a none mask and equip it instead.
            controller.currentMask = null;
            //player.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }

        if (maskSpriteRenderer != null)
        {
            maskSpriteRenderer.sprite = defaultMaskSprite;
            maskSpriteRenderer.enabled = (maskSpriteRenderer.sprite != null);
        }
    }
}
