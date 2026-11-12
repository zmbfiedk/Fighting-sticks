using System.Collections.Generic;
using UnityEngine;
public enum Team
{
    Player,
    Enemy
}

[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour
{
    [SerializeField] private Team target = Team.Enemy; 
    [SerializeField] private int damage = 10;
    [SerializeField] private float activeDuration = 0.2f;

    private Collider2D _collider;
    private readonly HashSet<Health> _hitThisSwing = new HashSet<Health>();

    void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;

        if (!TryGetComponent<Rigidbody2D>(out _))
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        _collider.enabled = false;
    }

    public void Activate()
    {
        CancelInvoke(nameof(Deactivate));
        _hitThisSwing.Clear();
        _collider.enabled = true;
        Invoke(nameof(Deactivate), activeDuration);
    }

    private void Deactivate()
    {
        _collider.enabled = false;
        _hitThisSwing.Clear();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Hurtbox hurtbox = other.GetComponentInParent<Hurtbox>();
        if (hurtbox == null || hurtbox.Owner == null) return;

        if (hurtbox.Team != target) return;                
        if (!_hitThisSwing.Add(hurtbox.Owner)) return;     

        hurtbox.ReceiveHit(damage);
    }
}