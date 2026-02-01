using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Masks/Mask Data")]
public class MaskData : ScriptableObject
{
    public MaskType maskType;

    [Header("Ability Toggles")]
    public bool allowJump = true;
    public bool allowSprint = true;
    public bool allowWallJump = true;

    [Header("Movement Modifiers")]
    public float gravityMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;

    [Header("Jump Modifiers")]
    public bool enableLongJump;
    public float longJumpMultiplier = 1.5f;

    public float longJumpHoldTime = 0.2f;
}
