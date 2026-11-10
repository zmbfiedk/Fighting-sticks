using UnityEngine;

[RequireComponent(typeof(PlayerIsGrounded))]
public class CustomGravity : MonoBehaviour
{
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float fallMultiplier = 1.5f;
    [SerializeField] private float maxFallSpeed = 25f;

    private PlayerIsGrounded _grounded;

    public float VerticalVelocity { get; set; } 

    void Awake() => _grounded = GetComponent<PlayerIsGrounded>();

    void Update()
    {
        float dt = Time.deltaTime;

        if (!(_grounded.IsGrounded && VerticalVelocity <= 0f))
        {
            float g = VerticalVelocity < 0f ? gravity * fallMultiplier : gravity;
            VerticalVelocity = Mathf.Max(VerticalVelocity - g * dt, -maxFallSpeed);
        }

        float dy = VerticalVelocity * dt;
        Vector3 pos = transform.position;

        if (VerticalVelocity <= 0f)
        {
            if (_grounded.Check(-dy, out float groundY))
            {
                pos.y = groundY;
                VerticalVelocity = 0f;
            }
            else
            {
                pos.y += dy;
            }
        }
        else
        {
            _grounded.SetAirborne();
            pos.y += dy;
        }

        transform.position = pos;
    }
}