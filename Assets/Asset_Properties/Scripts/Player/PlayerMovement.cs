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
    private bool canDash = true, isDashing;

    [Header("Player Movement Variables")]
    [SerializeField] float moveSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] float dashSpeed;

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
        PerformMovement();
    }

    #region Player Movement Functions

    private void PerformMovement()
    {
        // Move the player
        moveSpeed = isDashing ? dashSpeed : moveSpeed;
        Player.playerRb.velocity = new Vector2(movementInput.x * moveSpeed,
                                                Player.playerRb.velocity.y);
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
        
        Vector2 velocity = Player.playerRb.velocity;
        
        //jump performed when jump button is pressed, if cancelled early jump 
        //height is reduced by half and can only double jump if player is in the air
        if (obj.performed && (canJump || doubleJump))
        {
            Player.playerRb.velocity = new Vector2(velocity.x, jumpForce);
            doubleJump = canJump;
            canJump = false;
        }
        else if (obj.canceled && Player.playerRb.velocity.y > 0)
        {
            Player.playerRb.velocity = new Vector2(Player.playerRb.velocity.x, velocity.y * 0.5f);
        }
    }

    #endregion

    #region Player Dash Functions

    private void Dash(InputAction.CallbackContext obj)
    {
        if (!player.IsPlayerControlEnabled)
        {
            return;
        }

        Vector2 velocity = Player.playerRb.velocity;
        
        //dash performed when dash button is pressed, if cancelled early dash 
        //distance is reduced by half and can only dash if player is on the ground
        if (obj.performed && canDash)
        {
            Debug.Log("Dash");
            Player.playerRb.velocity = new Vector2(velocity.x * dashSpeed, Player.playerRb.velocity.y);
            canDash = false;
            isDashing = true;
        }
        else if (obj.canceled && velocity.x > 0)
        {
            Player.playerRb.velocity = new Vector2(velocity.x * 0.5f, velocity.y);
        }
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
        if (other.gameObject.CompareTag("Ground"))
        {
            canJump = true;
            doubleJump = true;
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            canDash = false;
            isGrounded = false;
        }
    }
}
