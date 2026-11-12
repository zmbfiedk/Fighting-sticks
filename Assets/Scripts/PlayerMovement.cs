using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float blockSpeed = 2.5f;
    [SerializeField] private float blockDamageMultiplier = 0.5f;

    [Header("Dash")]
    [SerializeField] private float doubleTapWindow = 0.25f; 
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.15f;    
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("References")]
    [SerializeField] private Transform opponent;
    [SerializeField] private Animator animator;
    [SerializeField] private Health health;
    [SerializeField] private float flipDeadZone = 0.05f;

    private bool _hasBlockParam;
    private bool _hasDashParam;

    private Rigidbody2D _rb;
    private InputAction _moveAction;
    private float _moveInput;

    // Dash state
    private int _prevInputDir;
    private int _lastTapDir;
    private float _lastTapTime = -10f;
    private float _dashEndTime;
    private float _nextDashTime;
    private int _dashDir;

    public bool IsBlocking { get; private set; }
    public bool IsDashing { get; private set; }
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
                _hasBlockParam = true;
            if (p.name == "Dash" && p.type == AnimatorControllerParameterType.Trigger)
                _hasDashParam = true;
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
        {
            float dx = opponent.position.x - transform.position.x;
            if (Mathf.Abs(dx) > flipDeadZone)
                FacingDirection = dx >= 0f ? 1 : -1;
        }
        transform.eulerAngles = new Vector3(0f, FacingDirection > 0 ? 0f : 180f, 0f);

        HandleDash();

        IsBlocking = !IsDashing
                     && Mathf.Abs(_moveInput) > 0.01f
                     && Mathf.Sign(_moveInput) != FacingDirection;

        if (health != null)
            health.DamageMultiplier = IsBlocking ? blockDamageMultiplier : 1f;

        if (_hasBlockParam)
            animator.SetBool("IsBlocking", IsBlocking);
    }

    private void HandleDash()
    {
        int dir = Mathf.Abs(_moveInput) > 0.01f ? (int)Mathf.Sign(_moveInput) : 0;

        if (dir != 0 && dir != _prevInputDir)
        {
            bool isDoubleTap = dir == _lastTapDir && Time.time - _lastTapTime <= doubleTapWindow;

            if (isDoubleTap && Time.time >= _nextDashTime && !IsDashing)
            {
                StartDash(dir);
                _lastTapDir = 0; 
            }
            else
            {
                _lastTapDir = dir;
                _lastTapTime = Time.time;
            }
        }
        _prevInputDir = dir;

        if (IsDashing && Time.time >= _dashEndTime)
            IsDashing = false;
    }

    private void StartDash(int dir)
    {
        IsDashing = true;
        _dashDir = dir;
        _dashEndTime = Time.time + dashDuration;
        _nextDashTime = Time.time + dashDuration + dashCooldown;

        if (_hasDashParam)
            animator.SetTrigger("Dash");
    }

    void FixedUpdate()
    {
        float x;
        if (IsDashing)
            x = _dashDir * dashSpeed;
        else
            x = _moveInput * (IsBlocking ? blockSpeed : moveSpeed);

        _rb.linearVelocity = new Vector2(x, _rb.linearVelocity.y);
    }

    public void SetFacing(int direction) => FacingDirection = direction >= 0 ? 1 : -1;
}