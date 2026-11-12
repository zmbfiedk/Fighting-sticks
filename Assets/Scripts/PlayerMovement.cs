using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float blockSpeed = 2.5f;
    [SerializeField] private float blockDamageMultiplier = 0.5f; 
    [SerializeField] private Transform opponent;
    [SerializeField] private Animator animator;
    [SerializeField] private Health health; 

    private bool _hasBlockParam;

    private Rigidbody2D _rb;
    private InputAction _moveAction;
    private float _moveInput;

    public bool IsBlocking { get; private set; }
    public int FacingDirection { get; private set; } = 1;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (health == null) health = GetComponentInParent<Health>();

        _moveAction = new InputAction("Move", InputActionType.Value);

        _moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
    }

    void Start()
    {
        if (animator == null) return;

        foreach (var p in animator.parameters)
        {
            if (p.name == "IsBlocking" && p.type == AnimatorControllerParameterType.Bool)
            {
                _hasBlockParam = true;
                break;
            }
        }

        if (!_hasBlockParam)
            Debug.LogWarning($"Animator on {animator.name} has no Bool parameter named 'IsBlocking'.", this);
    }

    void OnEnable() => _moveAction.Enable();

    void OnDisable()
    {
        _moveAction.Disable();
        if (health != null) health.DamageMultiplier = 1f; 
    }

    void OnDestroy() => _moveAction.Dispose();

    void Update()
    {
        _moveInput = _moveAction.ReadValue<float>();

        if (opponent != null)
            FacingDirection = opponent.position.x >= transform.position.x ? 1 : -1;

        IsBlocking = Mathf.Abs(_moveInput) > 0.01f && Mathf.Sign(_moveInput) != FacingDirection;

        if (health != null)
            health.DamageMultiplier = IsBlocking ? blockDamageMultiplier : 1f;

        if (_hasBlockParam)
            animator.SetBool("IsBlocking", IsBlocking);
    }

    void FixedUpdate()
    {
        float speed = IsBlocking ? blockSpeed : moveSpeed;
        _rb.linearVelocity = new Vector2(_moveInput * speed, _rb.linearVelocity.y);
    }

    public void SetFacing(int direction) => FacingDirection = direction >= 0 ? 1 : -1;
}