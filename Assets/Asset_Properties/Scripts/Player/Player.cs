using UnityEngine;

public class Player : MonoBehaviour
{
    public static Rigidbody2D playerRb;
    public static Collider2D playerCollider;
    public PlayerMovement movement;
    public PlayerAnimation animation;
    public bool IsPlayerControlEnabled { get; set; }
    public bool IsPlayerMovementEnabled { get; set; }

    private void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();

        movement = GetComponent<PlayerMovement>();
        animation = GetComponent<PlayerAnimation>();
    }

}
