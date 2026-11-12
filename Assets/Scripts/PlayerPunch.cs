using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPunch : MonoBehaviour
{
    [SerializeField] private Animator upperBodyAnimator;
    [SerializeField] private Hitbox leftFistHitbox;
    [SerializeField] private Hitbox rightFistHitbox;
    [SerializeField] private float punchCooldown = 0.4f;
    [SerializeField] private float hitboxDelay = 0.1f;

    private InputAction _leftPunch;
    private InputAction _rightPunch;
    private float _nextPunchTime;

    void Awake()
    {
        if (upperBodyAnimator == null)
            upperBodyAnimator = GetComponentInChildren<Animator>();

        _leftPunch = new InputAction("PunchLeft", InputActionType.Button, "<Mouse>/leftButton");
        _rightPunch = new InputAction("PunchRight", InputActionType.Button, "<Mouse>/rightButton");

        _leftPunch.performed += _ => TryPunch("PunchLeft");
        _rightPunch.performed += _ => TryPunch("PunchRight");
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

    private void TryPunch(string trigger)
    {
        if (Time.time < _nextPunchTime) return;

        _nextPunchTime = Time.time + punchCooldown;
        upperBodyAnimator.SetTrigger(trigger);

        Hitbox fist = trigger == "PunchLeft" ? leftFistHitbox : rightFistHitbox;
        if (fist != null)
            StartCoroutine(ActivateHitbox(fist));
    }

    private IEnumerator ActivateHitbox(Hitbox fist)
    {
        yield return new WaitForSeconds(hitboxDelay);
        fist.Activate();
    }
}