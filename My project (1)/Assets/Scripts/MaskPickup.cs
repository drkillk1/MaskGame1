using UnityEngine;

public class MaskPickup : MonoBehaviour
{
    [Header("What mask this pickup gives")]
    public MaskData maskData;

    [Header("Optional: override the visual on the player")]
    public Sprite maskSpriteOverride;

    [Header("Pickup settings")]
    public bool destroyOnPickup = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player should have tag "Player"
        if (!other.CompareTag("Player")) return;

        // Try to equip via a component on the player
        var equipper = other.GetComponent<MaskEquipper>();
        if (equipper == null)
        {
            Debug.LogWarning("MaskPickup: Player has no MaskEquipper component.");
            return;
        }

        equipper.Equip(maskData, maskSpriteOverride);

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
