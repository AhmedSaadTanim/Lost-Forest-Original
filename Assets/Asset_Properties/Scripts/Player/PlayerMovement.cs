using System.Collections;
using System.Collections.Generic;
using LostForest.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LostForest.Gameplay
{
    public class PlayerMovement : MonoBehaviour
    {
        #region Dust Particles Variables

        [Header("Dust Particles")] [SerializeField]
        ParticleSystem dust;

        #endregion

        #region References

        private Player player;

        #endregion

        #region Player Movement Variables

        private PlayerInput input;
        private Vector2 movementInput;
        public Vector2 GetMovementInput => movementInput;
        public bool IsStandingOnGround => isGrounded && Mathf.Abs(player.Body.linearVelocity.y) < 0.1f;
        private readonly List<ContactPoint2D> groundContacts = new(8);
        private bool canJump = true;
        private bool doubleJump;
        private bool canDash = true;
        private bool isDashing;
        private bool hasAirDashed;

        [Header("Player Movement Variables")] [SerializeField]
        float moveSpeed;

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
            player = GetComponent<Player>();
        }

        private void Start()
        {
            InputSubscriptions();
            
            if (player.IsPlayerControlEnabled)
            {
                input.Player.Enable();
            }
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
            UpdateGroundedState();
            
            if (player.IsPlayerMovementEnabled && player.IsPlayerControlEnabled)
            {
                PerformMovement();
            }
        }

        private void UpdateGroundedState()
        {
            bool wasGrounded = isGrounded;
            isGrounded = HasGroundSupport();

            if (isGrounded && !wasGrounded)
            {
                canJump = true;
                doubleJump = true;
                canDash = true;
                hasAirDashed = true;
            }
            else if (!isGrounded && wasGrounded)
            {
                canDash = false;
                hasAirDashed = false;
            }
        }
        
        private bool HasGroundSupport()
        {
            // A jump can start before the previous physics step's contacts clear.
            if (player.Body.linearVelocity.y > 0.1f)
            {
                return false;
            }

            player.Collider.GetContacts(groundContacts);

            foreach (ContactPoint2D contact in groundContacts)
            {
                bool playerIsOther = contact.otherCollider == player.Collider;
                Collider2D surface = playerIsOther ? contact.collider : contact.otherCollider;
                Vector2 normal = playerIsOther ? contact.normal : -contact.normal;

                if (surface != null &&
                    (groundLayer.value & (1 << surface.gameObject.layer)) != 0 &&
                    normal.y > 0.5f)
                {
                    return true;
                }
            }

            return false;
        }
        
        #region Player Movement Functions

        private void PerformMovement()
        {
            // Move the player
            player.Body.linearVelocity = new Vector2(movementInput.x * moveSpeed, player.Body.linearVelocity.y);
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

        public void LockForSequence()
        {
            player.IsPlayerControlEnabled = false;
            player.IsPlayerMovementEnabled = false;
            input.Player.Disable();
            StopAllCoroutines();

            movementInput = Vector2.zero;
            isDashing = false;
            canDash = true;

            player.Body.linearVelocity = Vector2.zero;
            player.Body.angularVelocity = 0f;

            if (dust != null)
            {
                dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public void UnlockAfterSequence()
        {
            ResetAfterRespawn();
            player.IsPlayerControlEnabled = true;
            player.IsPlayerMovementEnabled = true;
            input.Player.Enable();

            // Pick up movement held while the action map was disabled.
            movementInput = input.Player.Movement.ReadValue<Vector2>();

            if (Mathf.Abs(movementInput.x) > 0.01f)
            {
                Flip(movementInput);
            }
        }
        
        #endregion

        #region Player Jump Functions

        private void Jump(InputAction.CallbackContext obj)
        {
            if (!player.IsPlayerControlEnabled)
            {
                return;
            }

            Vector2 velocity = player.Body.linearVelocity;

            //jump performed when jump button is pressed, if cancelled early jump 
            //height is reduced by half and can only double jump if player is in the air
            if (obj.performed && (canJump || doubleJump))
            {
                player.Body.linearVelocity = new Vector2(velocity.x, jumpForce);
                doubleJump = canJump && doubleJump;
                canJump = false;
            }
            else if (obj.canceled && player.Body.linearVelocity.y > 0)
            {
                player.Body.linearVelocity = new Vector2(velocity.x, velocity.y * 0.5f);
            }
        }
        
        #endregion

        #region Respawn

        public void ResetAfterRespawn()
        {
            StopAllCoroutines();
            isDashing = false;
            canDash = true;
            hasAirDashed = false;
            isGrounded = false;
            canJump = true;
            doubleJump = true;
            movementInput = input.Player.Movement.ReadValue<Vector2>();

            if (Mathf.Abs(movementInput.x) > 0.01f)
            {
                Flip(movementInput);
            }

            if (dust != null)
            {
                dust.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
        }

        #endregion

        #region Player Dash Functions

        private void Dash(InputAction.CallbackContext obj)
        {
            if (!player.IsPlayerControlEnabled || isDashing)
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
            canDash = false; // Prevent multiple dashes
            isDashing = true;
            player.IsPlayerMovementEnabled = false; // Disable normal movement during dash

            Vector2 dashDirection = new Vector2(transform.right.x, 0f).normalized; // Direction the player is facing

            // Apply the burst force to move player in the X direction
            player.Body.linearVelocity = new Vector2(dashDirection.x * dashSpeed, player.Body.linearVelocity.y);

            // Wait for the dash duration (burst time)
            yield return new WaitForSeconds(dashDuration);

            isDashing = false;
            player.IsPlayerMovementEnabled = true; // Re-enable normal movement

            yield return new WaitForSeconds(dashCooldown); // Wait for cooldown
            canDash = true; // Allow dashing again
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
            if (player.Animation.animationStateComplete.ContainsKey(States.PLAYER_ATTACK_1) &&
                !player.Animation.animationStateComplete[States.PLAYER_ATTACK_1])
            {
                return;
            }

            player.Animation.ChangeAnimationState(States.PLAYER_ATTACK_1);
        }

        #endregion

        void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("Wall") && !isGrounded)
            {
                canJump = true;
                doubleJump = false;
            }
        }
        
    }
}
