using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;
    public Transform groundCheck;
    public Transform wallCheckRight;
    public Transform wallCheckLeft;

    public LayerMask groundLayer;

    [Header("Move")]
    public float moveSpeed = 9f;
    public float sprintSpeed = 13f;
    public float acceleration = 70f;
    public float deceleration = 90f;
    public float airControlMultiplier = 0.75f;

    [Header("Jump")]
    public float jumpForce = 14f;
    public float jumpCutMultiplier = 0.5f;     // when releasing jump early
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Checks")]
    public Vector2 groundCheckSize = new Vector2(0.55f, 0.12f);
    public Vector2 wallCheckSize = new Vector2(0.18f, 0.75f);

    [Header("Wall")]
    public bool enableWallSlide = true;
    public float wallSlideSpeed = 2.25f;
    public bool enableWallJump = true;
    public Vector2 wallJumpForce = new Vector2(10f, 14f);
    public float wallJumpLockTime = 0.12f; // brief lock to prevent instant re-steer

    [Header("Health")]
    public int maxHealth = 5;
    public int health = 5;

    // --- Mask modifiers (set by your mask system later) ---
    [Header("Mask Modifiers (runtime)")]
    public bool maskDisableJump = false;
    public bool maskDisableSprint = false;
    public bool maskDisableWallJump = false;
    public float maskGravityMultiplier = 1f;
    public float maskMoveSpeedMultiplier = 1f;
    public float maskJumpForceMultiplier = 1f;

    // Internal

    Collider2D _col;

    float _xInput;
    bool _jumpPressed;
    bool _jumpHeld;
    bool _sprintHeld;

    float _coyoteCounter;
    float _jumpBufferCounter;

    float _baseGravity;

    bool _isFacingRight = true;
    bool _isWallSliding;
    bool _wallJumping;
    float _wallJumpLockCounter;

    bool IsGrounded => _col != null && _col.IsTouchingLayers(groundLayer);
    bool IsWalledRight => Physics2D.OverlapBox(wallCheckRight.position, wallCheckSize, 0f, groundLayer);
    bool IsWalledLeft  => Physics2D.OverlapBox(wallCheckLeft.position,  wallCheckSize, 0f, groundLayer);

    bool IsWalled => IsWalledLeft || IsWalledRight;

    int _wallSide; // -1 = left, +1 = right, 0 = none



    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        health = maxHealth;
        _baseGravity = rb.gravityScale;
        _col = GetComponent<Collider2D>();

    }

    void Update()
    {
        ReadInput();
        UpdateTimers();
        HandleJumpRequests();
        HandleFacing();
    }

    void FixedUpdate()
    {
        ApplyMaskPhysics();
        HandleHorizontal();
        HandleWall();
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 200),
            $"Grounded: {IsGrounded}\n" +
            $"Walled: {IsWalled}\n" +
            $"JumpPressed: {_jumpPressed}\n" +
            $"Coyote: {_coyoteCounter:F2}\n" +
            $"JumpBuffer: {_jumpBufferCounter:F2}\n" +
            $"VelY: {rb.linearVelocity.y:F2}");
    }


    void ReadInput()
    {
        _xInput = Input.GetAxisRaw("Horizontal");
        _jumpPressed = Input.GetKey(KeyCode.Space);
        _jumpHeld = Input.GetKey(KeyCode.Space);
        _sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    void UpdateTimers()
    {
        // Coyote time
        if (IsGrounded) _coyoteCounter = coyoteTime;
        else _coyoteCounter -= Time.deltaTime;

        // Jump buffer
        if (_jumpPressed) _jumpBufferCounter = jumpBufferTime;
        else _jumpBufferCounter -= Time.deltaTime;

        // Wall jump lock
        if (_wallJumpLockCounter > 0f) _wallJumpLockCounter -= Time.deltaTime;
        else _wallJumping = false;
    }

    void HandleJumpRequests()
    {
        if (maskDisableJump) return;

        bool canNormalJump = _jumpBufferCounter > 0f && _coyoteCounter > 0f;
        if (canNormalJump)
        {
            DoJump(jumpForce * maskJumpForceMultiplier);
            _jumpBufferCounter = 0f;
            return;
        }

        // Wall jump
        if (enableWallJump && !maskDisableWallJump)
        {
            bool canWallJump = _jumpPressed && _isWallSliding && _wallSide != 0;
            if (canWallJump)
            {
                // If touching left wall (-1), push right (+1). If right wall (+1), push left (-1).
                float pushXDir = -_wallSide;

                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                rb.AddForce(new Vector2(pushXDir * wallJumpForce.x, wallJumpForce.y), ForceMode2D.Impulse);

                _wallJumpLockCounter = wallJumpLockTime;
                _wallJumping = true;
                _jumpBufferCounter = 0f; // optional: clear buffer
            }
        }


        // Variable jump height: if you release jump early while rising, cut velocity
        if (!_jumpHeld && rb.linearVelocity.y > 0.01f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    void DoJump(float force)
    {
        // Reset vertical speed for consistent jump
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        _coyoteCounter = 0f;
    }

    void HandleHorizontal()
    {
        if (_wallJumping) return; // optional: lock steering briefly after wall jump

        float baseSpeed = moveSpeed * maskMoveSpeedMultiplier;
        float targetSpeed = baseSpeed;

        if (_sprintHeld && !maskDisableSprint)
            targetSpeed = sprintSpeed * maskMoveSpeedMultiplier;

        float targetVelX = _xInput * targetSpeed;
        float speedDiff = targetVelX - rb.linearVelocity.x;

        float accelRate = (Mathf.Abs(targetVelX) > 0.01f) ? acceleration : deceleration;
        if (!IsGrounded) accelRate *= airControlMultiplier;

        float movement = speedDiff * accelRate * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    void HandleWall()
    {
        if (!enableWallSlide) { _isWallSliding = false; _wallSide = 0; return; }

        bool notGrounded = !IsGrounded;
        bool left = IsWalledLeft;
        bool right = IsWalledRight;

        _wallSide = left ? -1 : (right ? +1 : 0);

        _isWallSliding = (_wallSide != 0) && notGrounded && !_wallJumping;

        if (_isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
        }
    }

    void HandleFacing()
    {
        if (_xInput > 0.01f && !_isFacingRight) Flip();
        else if (_xInput < -0.01f && _isFacingRight) Flip();
    }

    void Flip()
    {
        _isFacingRight = !_isFacingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    void ApplyMaskPhysics()
    {
        rb.gravityScale = _baseGravity * maskGravityMultiplier;
    }


    // --- Health hooks ---
    public void TakeDamage(int amount)
    {
        health = Mathf.Max(0, health - amount);
        if (health <= 0) Die();
    }

    void Die()
    {
        // TODO: respawn / reload
        Debug.Log("Player died");
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
        if (wallCheckRight)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(wallCheckRight.position, wallCheckSize);
        }
        if (wallCheckLeft)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(wallCheckLeft.position, wallCheckSize);
        }
    }
}
