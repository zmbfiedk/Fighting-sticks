using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPunch : MonoBehaviour
{
    [SerializeField] private Animator upperBodyAnimator;

    private InputAction _leftPunch;
    private InputAction _rightPunch;

    void Awake()
    {
        if (upperBodyAnimator == null)
            upperBodyAnimator = GetComponentInChildren<Animator>();

        _leftPunch = new InputAction("PunchLeft", InputActionType.Button, "<Mouse>/leftButton");
        _rightPunch = new InputAction("PunchRight", InputActionType.Button, "<Mouse>/rightButton");

        _leftPunch.performed += _ => upperBodyAnimator.SetTrigger("PunchLeft");
        _rightPunch.performed += _ => upperBodyAnimator.SetTrigger("PunchRight");
    }

    void OnEnable()
    {
        _leftPunch.Enable();
        _rightPunch.Enable();
    }

    void OnDisable()
    {
        _leftPunch.Disable();
        _rightPunch.Disable();
    }

    void OnDestroy()
    {
        _leftPunch.Dispose();
        _rightPunch.Dispose();
    }
}
