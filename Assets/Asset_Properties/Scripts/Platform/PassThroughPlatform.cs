using System.Collections;
using UnityEngine;

public class PassThroughPlatform : MonoBehaviour
{
    Collider2D platformCollider;
    bool isPlayerOnPlatform;
    PlayerMovement playerMovementRef;

    private void Start()
    {
        playerMovementRef = GameObject.Find("Player").GetComponent<PlayerMovement>();
        platformCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (isPlayerOnPlatform && playerMovementRef.GetMovementInput.y < 0)
        {
            platformCollider.enabled = false;
            StartCoroutine(EnableCollider());
        }
    }

    private IEnumerator EnableCollider()
    {
        yield return new WaitForSeconds(0.5f);
        platformCollider.enabled = true;
    }

    private void SetPlayerOnPlatform(Collision2D other, bool value)
    {
        var player = other.gameObject.GetComponent<Player>();
        if (player != null)
        {
            isPlayerOnPlatform = value;
        }

    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        SetPlayerOnPlatform(other, true);
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        SetPlayerOnPlatform(other, false);
    }

}
