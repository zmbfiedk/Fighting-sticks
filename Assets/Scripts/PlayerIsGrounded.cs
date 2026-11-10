using UnityEngine;

public class PlayerIsGrounded : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float feetOffset = 0.5f;    
    [SerializeField] private float skin = 0.1f;          
    [SerializeField] private float snapDistance = 0.05f; 

    public bool IsGrounded { get; private set; }

    public bool Check(float fallDistance, out float pivotY)
    {
        Vector2 feet = (Vector2)transform.position + Vector2.down * feetOffset;
        Vector2 origin = feet + Vector2.up * skin;
        float distance = skin + snapDistance + fallDistance;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, distance, groundLayer);
        IsGrounded = hit.collider != null;

        pivotY = IsGrounded ? hit.point.y + feetOffset : 0f;

        Debug.DrawRay(origin, Vector2.down * distance, IsGrounded ? Color.green : Color.red);
        return IsGrounded;
    }

    public void SetAirborne() => IsGrounded = false;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * feetOffset, 0.05f);
    }
}