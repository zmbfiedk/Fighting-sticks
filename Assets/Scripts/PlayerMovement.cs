using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D _rb;
    private InputAction _moveAction;
    private float _moveInput;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _moveAction = new InputAction("Move", InputActionType.Value);

        _moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");

    }

    void OnEnable() => _moveAction.Enable();
    void OnDisable() => _moveAction.Disable();

    void Update()
    {
        _moveInput = _moveAction.ReadValue<float>();
    }

    void FixedUpdate()
    {
        _rb.linearVelocity = new Vector2(_moveInput * moveSpeed, _rb.linearVelocity.y);
    }
}