using UnityEngine;

public class RoomBounds : MonoBehaviour
{
    [SerializeField] private Vector2 center;
    [SerializeField] private Vector2 size = new Vector2(20f, 12f);

    public Vector2 Center => center;
    public Vector2 Size => size;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            new Vector3(center.x, center.y, 0f),
            new Vector3(size.x, size.y, 0.1f)
        );
    }

    private void OnValidate()
    {
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(0.1f, size.y);
    }
}