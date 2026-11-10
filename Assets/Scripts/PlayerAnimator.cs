using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float dampTime = 0.001f; 

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        float speed = Mathf.Abs(_rb.linearVelocity.x);
        animator.SetFloat(SpeedHash, speed, dampTime, Time.deltaTime);

      
        if (_rb.linearVelocity.x > 0.01f)
            spriteRenderer.flipX = false;
        else if (_rb.linearVelocity.x < -0.01f)
            spriteRenderer.flipX = true;
    }
}