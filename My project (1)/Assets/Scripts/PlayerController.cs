using System.Collections;
using UnityEngine;


public enum MaskType
{
    None,
    LowGravityMask,
    LongJumpMask,
    SpeedMask
}
public class PlayerController : MonoBehaviour
{
    private float horizontal;
    [SerializeField] private float walkSpeed = 8f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float jumpingPower = 16f;
    private bool isFacingRight = true;

    private float coyoteTime = 0.2f;
    private float coyoteTimeCounter;

    private float jumpBufferTime = 0.2f;
    private float jumpBufferCounter;
    private bool isWallSliding;
    [SerializeField] private float wallSlidingSpeed = 6f;

    private bool canDash = true;
    private bool isDashing;
    private float dashingPower = 24f;
    private float dashingTime = 0.2f;
    private float dashingCooldown = 1f;

    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.4f;
    private Vector2 wallJumpingPower = new Vector2(8f, 16f);

    [Header("Mask System")]
    public MaskData currentMask;

    float baseMoveSpeed;
    float baseGravity;

    float longJumpTimer;


    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;



    void Start()
    {
        baseMoveSpeed = walkSpeed;
        baseGravity = rb.gravityScale;

        ApplyMask(currentMask);
    }

    private void Update()
    {
        if (isDashing) return;

        horizontal = Input.GetAxisRaw("Horizontal");

        // Grounded + coyote time
        if (IsGrounded()) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        // ----- JUMP BUFFER (only if mask allows jumping) -----
        if (currentMask == null || currentMask.allowJump)
        {
            if (Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBufferTime;
            else jumpBufferCounter -= Time.deltaTime;

            // Do a ground jump if both buffers are valid
            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);

                // Start long-jump window (if enabled)
                if (currentMask != null && currentMask.enableLongJump)
                    longJumpTimer = currentMask.longJumpHoldTime;

                jumpBufferCounter = 0f;
            }

            // Short hop (release jump cuts upward velocity)
            if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
                coyoteTimeCounter = 0f;
                longJumpTimer = 0f;
            }
        }
        else
        {
            // Mask forbids jumping: clear jump buffering so it doesn't "store" a jump.
            jumpBufferCounter = 0f;
            longJumpTimer = 0f;
        }

        // Wall slide can still exist even if wall jumping is disabled
        WallSlide();

        // Wall jump ONLY if mask allows wall jump AND jumping is allowed
        WallJump();

        // ----- LONG JUMP HOLD BOOST -----
        if (currentMask != null && currentMask.enableLongJump)
        {
            if (Input.GetButton("Jump") && longJumpTimer > 0f && rb.linearVelocity.y > 0f)
            {
                // Add extra upward movement while rising (controlled, not infinite)
                float extra = jumpingPower * (currentMask.longJumpMultiplier - 1f);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + (extra * Time.deltaTime));
                longJumpTimer -= Time.deltaTime;
            }
            else
            {
                // If they aren't holding jump, burn timer fast (prevents late boosting)
                longJumpTimer = 0f;
            }
        }

        if (!isWallJumping)
            Flip();
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

        // Sprint only if mask allows it
        bool wantsSprint = Input.GetKey(KeyCode.LeftShift);
        bool canSprintNow = (currentMask == null) || currentMask.allowSprint;

        float speed = walkSpeed;
        if (wantsSprint && canSprintNow)
            speed *= sprintMultiplier;

        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
    }
    public void ApplyMask(MaskData mask)
    {
        if (mask == null) return;

        currentMask = mask;

        walkSpeed = baseMoveSpeed * mask.moveSpeedMultiplier;
        rb.gravityScale = baseGravity * mask.gravityMultiplier;

        // Clear any in-progress jump tech that might conflict
        jumpBufferCounter = 0f;
        longJumpTimer = 0f;
        wallJumpingCounter = 0f;
    }

    public void EquipMask(MaskData newMask)
    {
        ApplyMask(newMask);
    }



    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private bool IsWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    }

    private void WallSlide()
    {
        if (IsWalled() && !IsGrounded() && horizontal != 0f)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void WallJump()
    {

        // If jump or wall jump is forbidden by mask, don't allow wall jumping at all.
        if (currentMask != null)
        {
            if (!currentMask.allowJump || !currentMask.allowWallJump)
            {
                // still allow wall slide, but kill wall jump windows
                wallJumpingCounter = 0f;
                return;
            }
        }

        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.linearVelocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            // Start long-jump window from a wall jump too (if enabled)
            if (currentMask != null && currentMask.enableLongJump)
                longJumpTimer = currentMask.longJumpHoldTime;

            if (transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private void Flip()
    {
        if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f)
        {
            Vector3 localScale = transform.localScale;
            isFacingRight = !isFacingRight;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    // private IEnumerator Dash()
    // {
    //     canDash = false;
    //     isDashing = true;
    //     float originalGravity = rb.gravityScale;
    //     rb.gravityScale = 0f;
    //     rb.linearVelocity = new Vector2(transform.localScale.x * dashingPower, 0f);
    //     yield return new WaitForSeconds(dashingTime);
    //     rb.gravityScale = originalGravity;
    //     isDashing = false;
    //     yield return new WaitForSeconds(dashingCooldown);
    //     canDash = true;
    // }
}