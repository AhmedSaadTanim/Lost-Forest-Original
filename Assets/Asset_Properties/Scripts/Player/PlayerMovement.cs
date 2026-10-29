using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    #region Dust Particles Variables

    [Header("Dust Particles")]
    [SerializeField] ParticleSystem dust;

    #endregion

    #region References

    Player player;

    #endregion

    #region Player Movement Variables

    private PlayerInput input;
    private Vector2 movementInput;
    public Vector2 GetMovementInput => movementInput;
    private bool canJump = true;
    private bool doubleJump;
    private bool canDash = true;
    private bool isDashing;
    private bool hasAirDashed;
    
    [Header("Player Movement Variables")]
    [SerializeField] float moveSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] float dashSpeed;
    [SerializeField] float dashDuration;
    [SerializeField] float dashCooldown;
    [SerializeField] LayerMask groundLayer;
    
    #endregion

    #region Properties

    private bool isGrounded;

    #endregion

    private void Awake()
    {
        input = new PlayerInput();
    }

    private void Start()
    {
        input.Player.Enable();
        player = GetComponent<Player>();
        player.IsPlayerControlEnabled = true;
        player.IsPlayerMovementEnabled = true;
        InputSubscriptions();
    }

    private void InputSubscriptions()
    {
        input.Player.Movement.performed += Move;
        input.Player.Movement.canceled += Move;
        
        input.Player.Jump.performed += Jump;
        input.Player.Jump.canceled += Jump;

        input.Player.Dash.performed += Dash;

        input.Player.Attack.performed += Attack;
    }

    private void FixedUpdate()
    {
        if (player.IsPlayerMovementEnabled)
        {
            PerformMovement();
        }
    }

    #region Player Movement Functions

    private void PerformMovement()
    {
        // Move the player
        Player.playerRb.linearVelocity = new Vector2(movementInput.x * moveSpeed, Player.playerRb.linearVelocity.y);
    }

    private void Move(InputAction.CallbackContext obj)
    {
        if (!player.IsPlayerControlEnabled)
        {
            return;
        }

        movementInput = obj.ReadValue<Vector2>();
        if (obj.performed)
        {
            Flip(movementInput);
            CreateDust(isGrounded);
        }
    }

    private void Flip(Vector2 moveInput)
    {
        float direction = moveInput.x > 0f ? 0f : 180f;
        transform.rotation = Quaternion.Euler(0f, direction, 0f);
    }

    #endregion

    #region Player Jump Functions

    private void Jump(InputAction.CallbackContext obj)
    {
        if (!player.IsPlayerControlEnabled)
        {
            return;
        }
        
        Vector2 velocity = Player.playerRb.linearVelocity;
        
        //jump performed when jump button is pressed, if cancelled early jump 
        //height is reduced by half and can only double jump if player is in the air
        if (obj.performed && (canJump || doubleJump))
        {
            Player.playerRb.linearVelocity = new Vector2(velocity.x, jumpForce);
            doubleJump = canJump && doubleJump;
            canJump = false;
        }
        else if (obj.canceled && Player.playerRb.linearVelocity.y > 0)
        {
            Player.playerRb.linearVelocity = new Vector2(velocity.x, velocity.y * 0.5f);
        }
    }

    bool IsGrounded() 
    {
        float extraHeight = 0.1f;
        Vector2 rayOrigin = new Vector2(Player.playerCollider.bounds.center.x, Player.playerCollider.bounds.min.y - 0.05f);
        float rayLength = Player.playerCollider.bounds.extents.y + extraHeight;

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, groundLayer);

        // Debug the Raycast in the Scene view
        Debug.DrawLine(rayOrigin, rayOrigin + Vector2.down * rayLength, hit.collider != null ? Color.green : Color.red, 0.1f);

        return hit.collider != null;
    }


    
    #endregion

    #region Player Dash Functions

    private void Dash(InputAction.CallbackContext obj)
    {
        if (!player.IsPlayerControlEnabled)
        {
            return;
        }
        
        //dash performed when dash button is pressed, if cancelled early dash 
        //distance is reduced by half and can only dash if player is on the ground
        if (obj.performed && (canDash || !hasAirDashed))
        {
            Debug.Log("Dash");
            StartCoroutine(DashCoroutine());
        }
    }
    
    private IEnumerator DashCoroutine()
    {
        if (!hasAirDashed) hasAirDashed = true;
        canDash = false;  // Prevent multiple dashes
        isDashing = true;
        player.IsPlayerMovementEnabled = false;  // Disable normal movement during dash
        
        Vector2 dashDirection = new Vector2(transform.right.x, 0f).normalized;  // Direction the player is facing

        // Apply the burst force to move player in the X direction
        Player.playerRb.linearVelocity = new Vector2(dashDirection.x * dashSpeed, Player.playerRb.linearVelocity.y);

        // Wait for the dash duration (burst time)
        yield return new WaitForSeconds(dashDuration);
        
        isDashing = false;
        player.IsPlayerMovementEnabled = true;  // Re-enable normal movement

        yield return new WaitForSeconds(dashCooldown);  // Wait for cooldown
        canDash = true;  // Allow dashing again
    }
    
    #endregion

    #region Player Attack Functions

    private void Attack(InputAction.CallbackContext obj)
    {
        if (!player.IsPlayerControlEnabled)
        {
            return;
        }

        if (obj.performed)
        {
            PerformAttackAnimation();
        }
    }

    #endregion

    #region Dust Particles Functions

    private void CreateDust(bool shouldPlay)
    {
        if (shouldPlay)
        {
            dust.Play();
        }
    }

    #endregion

    #region Animation

    private void PerformAttackAnimation()
    {
        if (player.animation.animationStateComplete.ContainsKey(States.PLAYER_ATTACK_1) &&
            !player.animation.animationStateComplete[States.PLAYER_ATTACK_1])
        {
            return;
        }

        player.animation.ChangeAnimationState(States.PLAYER_ATTACK_1);
    }

    #endregion

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground") && IsGrounded())
        {
            hasAirDashed = true; // Reset air dash on landing
            canDash = true;
            canJump = true;
            doubleJump = true;
            isGrounded = true;
        }
        else if (other.gameObject.CompareTag("Wall"))
        {
            canJump = true;
            doubleJump = false;
            isGrounded = false;
        }
    }

    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            canDash = false;
            hasAirDashed = false;
            isGrounded = false;
        }
    }
}
