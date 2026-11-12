using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum EnemyState
{
    Idle,
    Chase,
    Attack,
    Defend,   
    Recover,  
    Wait,     
    Dash     
}

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Animator animator;
    [SerializeField] private Animator upperBodyAnimator;
    [SerializeField] private Health health;
    [SerializeField] private Hitbox leftFistHitbox;
    [SerializeField] private Hitbox rightFistHitbox;

    [Header("Ranges (gizmos)")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float retreatRange = 0.8f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float blockSpeed = 2f;
    [SerializeField] private float blockDamageMultiplier = 0.5f;
    [SerializeField] private float flipDeadZone = 0.05f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1.5f;
    [SerializeField] private float dashGapMin = 2f;          
    [SerializeField] private float dashRollInterval = 0.5f;  
    [Range(0f, 1f)][SerializeField] private float dashChaseChance = 0.3f;
    [Range(0f, 1f)][SerializeField] private float dashDodgeChance = 0.3f; 
    [Range(0f, 1f)][SerializeField] private float dashBackChance = 0.3f;  

    [Header("Attack")]
    [SerializeField] private int maxComboLength = 3;
    [SerializeField] private float comboGap = 0.5f;
    [SerializeField] private float hitboxDelay = 0.1f;
    [SerializeField] private Vector2 recoverTime = new Vector2(0.4f, 0.9f);

    [Header("Intelligence")]
    [Range(0f, 1f)][SerializeField] private float aggression = 0.6f;
    [Range(0f, 1f)][SerializeField] private float blockChance = 0.5f;
    [Range(0f, 1f)][SerializeField] private float hurtBackoffChance = 0.35f;
    [SerializeField] private Vector2 reactionTime = new Vector2(0.05f, 0.2f);
    [SerializeField] private Vector2 defendTime = new Vector2(0.4f, 0.9f);
    [SerializeField] private Vector2 waitTime = new Vector2(0.2f, 0.6f);

    public EnemyState State { get; private set; } = EnemyState.Idle;
    public bool IsBlocking { get; private set; }
    public bool IsDashing => State == EnemyState.Dash;
    public int FacingDirection { get; private set; } = 1;

    private Rigidbody2D _rb;
    private PlayerMovement _playerMovement;
    private PlayerPunch _playerPunch;

    private float _distance;
    private float _moveX;
    private float _stateEndTime;
    private float _nextDecisionTime;
    private float _nextPunchTime;
    private int _punchesLeft;

    private int _dashDir;
    private float _nextDashTime;
    private float _nextDashRollTime;

    private bool _pendingReact;
    private float _reactAt;

    private readonly HashSet<string> _movementParams = new HashSet<string>();
    private readonly HashSet<string> _upperParams = new HashSet<string>();

    private bool CanDash => Time.time >= _nextDashTime;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (upperBodyAnimator == null) upperBodyAnimator = animator;
        if (health == null) health = GetComponentInParent<Health>();

        if (target == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null) target = player.transform;
        }

        if (target != null)
        {
            _playerMovement = target.GetComponentInParent<PlayerMovement>();
            _playerPunch = target.GetComponentInParent<PlayerPunch>();
            if (_playerPunch == null) _playerPunch = target.GetComponentInChildren<PlayerPunch>();
        }

        CacheParams(animator, _movementParams);
        CacheParams(upperBodyAnimator, _upperParams);
    }

    void OnEnable()
    {
        if (_playerPunch != null) _playerPunch.PunchStarted += OnPlayerPunch;
        if (health != null) health.OnDamaged += OnHurt;
    }

    void OnDisable()
    {
        if (_playerPunch != null) _playerPunch.PunchStarted -= OnPlayerPunch;
        if (health != null)
        {
            health.OnDamaged -= OnHurt;
            health.DamageMultiplier = 1f;
        }
    }

    void Update()
    {
        if (target == null || (health != null && health.IsDead))
        {
            _moveX = 0f;
            IsBlocking = false;
            if (health != null) health.DamageMultiplier = 1f;
            UpdateAnimator();
            return;
        }

        float dx = target.position.x - transform.position.x;
        _distance = Vector2.Distance(transform.position, target.position);
        int dirToTarget = dx >= 0f ? 1 : -1;

        if (Mathf.Abs(dx) > flipDeadZone)
            FacingDirection = dirToTarget;
        transform.eulerAngles = new Vector3(0f, FacingDirection > 0 ? 0f : 180f, 0f);

        if (_pendingReact && Time.time >= _reactAt)
        {
            _pendingReact = false;
            if (State != EnemyState.Recover && State != EnemyState.Dash)
            {
                if (CanDash && Random.value < dashDodgeChance)
                    EnterDash(-FacingDirection);  
                else
                    EnterDefend();
            }
        }

        RunState();

        switch (State)
        {
            case EnemyState.Chase: _moveX = dirToTarget * moveSpeed; break;
            case EnemyState.Defend: _moveX = -dirToTarget * blockSpeed; break;
            case EnemyState.Dash: _moveX = _dashDir * dashSpeed; break;
            default: _moveX = 0f; break;
        }

        IsBlocking = State == EnemyState.Defend;
        if (health != null)
            health.DamageMultiplier = IsBlocking ? blockDamageMultiplier : 1f;

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        _rb.linearVelocity = new Vector2(_moveX, _rb.linearVelocity.y);
    }

    private void RunState()
    {
        switch (State)
        {
            case EnemyState.Idle:
            case EnemyState.Chase:
                Decide();
                break;

            case EnemyState.Defend:
                if (Time.time >= _stateEndTime || _distance > attackRange * 1.5f)
                    State = EnemyState.Idle;
                break;

            case EnemyState.Wait:
            case EnemyState.Recover:
            case EnemyState.Dash:
                if (Time.time >= _stateEndTime)
                    State = EnemyState.Idle;
                break;

            case EnemyState.Attack:
                if (_distance > attackRange * 1.25f)
                {
                    State = EnemyState.Idle;
                    break;
                }

                if (Time.time >= _nextPunchTime)
                {
                    if (_punchesLeft > 0)
                    {
                        Punch();
                        _punchesLeft--;
                        _nextPunchTime = Time.time + comboGap;
                    }
                    else
                    {
                        EnterRecover();
                    }
                }
                break;
        }
    }

    private void Decide()
    {
        if (_distance > detectionRange)
        {
            State = EnemyState.Idle;
            return;
        }

        if (_distance > attackRange)
        {
            if (_distance > attackRange + dashGapMin && CanDash && Time.time >= _nextDashRollTime)
            {
                _nextDashRollTime = Time.time + dashRollInterval;
                if (Random.value < dashChaseChance)
                {
                    EnterDash(FacingDirection);
                    return;
                }
            }

            State = EnemyState.Chase;
            _nextDecisionTime = Time.time + Random.Range(reactionTime.x, reactionTime.y);
            return;
        }

        if (_distance < retreatRange)
        {
            if (CanDash && Random.value < dashBackChance)
                EnterDash(-FacingDirection);
            else
                EnterDefend();
            return;
        }

        if (Time.time < _nextDecisionTime)
        {
            State = EnemyState.Idle;
            return;
        }

        float chance = AttackChance();
        float roll = Random.value;

        if (roll < chance) EnterAttack();
        else if (roll < chance + (1f - chance) * 0.6f) EnterWait();
        else EnterDefend();

        _nextDecisionTime = Time.time + Random.Range(reactionTime.x, reactionTime.y);
    }

    private float HealthPercent()
    {
        if (health == null || health.MaxHealth <= 0) return 1f;
        return health.CurrentHealth / (float)health.MaxHealth;
    }

    private float AttackChance()
    {
        float c = aggression * Mathf.Lerp(0.5f, 1f, HealthPercent());
        if (_playerMovement != null && _playerMovement.IsBlocking) c *= 0.6f;
        return Mathf.Clamp01(c);
    }

    private float BlockChance()
    {
        return Mathf.Clamp01(blockChance * Mathf.Lerp(1.5f, 1f, HealthPercent()));
    }

    private void OnPlayerPunch()
    {
        if (State == EnemyState.Recover || State == EnemyState.Dash || _pendingReact) return;
        if (_distance > attackRange + 0.5f) return;
        if (Random.value > BlockChance()) return;

        _pendingReact = true;
        _reactAt = Time.time + Random.Range(reactionTime.x, reactionTime.y);
    }

    private void OnHurt(int damage)
    {
        if (health != null && health.IsDead) return;
        if (State == EnemyState.Dash) return;
        if (Random.value < hurtBackoffChance)
            EnterDefend();
    }

    private void EnterAttack()
    {
        State = EnemyState.Attack;
        _punchesLeft = Random.Range(1, maxComboLength + 1);
        _nextPunchTime = Time.time + Random.Range(reactionTime.x, reactionTime.y);
    }

    private void EnterDefend()
    {
        State = EnemyState.Defend;
        _stateEndTime = Time.time + Random.Range(defendTime.x, defendTime.y);
        _punchesLeft = 0;
    }

    private void EnterWait()
    {
        State = EnemyState.Wait;
        _stateEndTime = Time.time + Random.Range(waitTime.x, waitTime.y);
    }

    private void EnterRecover()
    {
        State = EnemyState.Recover;
        _stateEndTime = Time.time + Random.Range(recoverTime.x, recoverTime.y);
    }

    private void EnterDash(int dir)
    {
        State = EnemyState.Dash;
        _dashDir = dir;
        _stateEndTime = Time.time + dashDuration;
        _nextDashTime = Time.time + dashDuration + dashCooldown;
        _punchesLeft = 0;
        _pendingReact = false;

        SetTrigger(animator, _movementParams, "Dash");
    }

    private void Punch()
    {
        bool left = Random.value < 0.5f;
        SetTrigger(upperBodyAnimator, _upperParams, left ? "PunchLeft" : "PunchRight");

        Hitbox fist = left ? leftFistHitbox : rightFistHitbox;
        if (fist != null) StartCoroutine(ActivateHitbox(fist));
    }

    private IEnumerator ActivateHitbox(Hitbox fist)
    {
        yield return new WaitForSeconds(hitboxDelay);
        fist.Activate();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        if (_movementParams.Contains("Speed"))
            animator.SetFloat("Speed", Mathf.Abs(_moveX));
        if (_movementParams.Contains("IsBlocking"))
            animator.SetBool("IsBlocking", IsBlocking);
    }

    private void SetTrigger(Animator anim, HashSet<string> parameters, string name)
    {
        if (anim != null && parameters.Contains(name))
            anim.SetTrigger(name);
    }

    private void CacheParams(Animator anim, HashSet<string> set)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters) set.Add(p.name);
    }

    // ---------- Gizmos ----------
    void OnDrawGizmos()
    {
        Vector3 pos = transform.position;

        DrawZone(pos, detectionRange, Color.yellow, EnemyState.Chase);
        DrawZone(pos, attackRange + dashGapMin, Color.green, EnemyState.Dash); 
        DrawZone(pos, attackRange, Color.red, EnemyState.Attack);
        DrawZone(pos, retreatRange, Color.cyan, EnemyState.Defend);

        if (Application.isPlaying && target != null)
        {
            Gizmos.color = StateColor(State);
            Gizmos.DrawLine(pos, target.position);

#if UNITY_EDITOR
            Handles.Label(pos + Vector3.up * 1.5f, State.ToString());
#endif
        }
    }

    private void DrawZone(Vector3 pos, float radius, Color color, EnemyState zoneState)
    {
        bool active = Application.isPlaying && State == zoneState;
        Gizmos.color = active ? color : new Color(color.r, color.g, color.b, 0.4f);
        Gizmos.DrawWireSphere(pos, radius);
    }

    private Color StateColor(EnemyState s)
    {
        switch (s)
        {
            case EnemyState.Chase: return Color.yellow;
            case EnemyState.Attack: return Color.red;
            case EnemyState.Defend: return Color.cyan;
            case EnemyState.Recover: return Color.magenta;
            case EnemyState.Wait: return Color.white;
            case EnemyState.Dash: return Color.green;
            default: return Color.gray;
        }
    }
}