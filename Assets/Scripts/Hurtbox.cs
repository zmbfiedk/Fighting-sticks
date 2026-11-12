using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hurtbox : MonoBehaviour
{
    [SerializeField] private Team team = Team.Enemy; 
    [SerializeField] private Health health;
    [SerializeField] private float damageMultiplier = 1f;

    public Team Team => team;
    public Health Owner => health;

    void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
    }

    public void ReceiveHit(int damage)
    {
        if (health == null) return;
        health.TakeDamage(Mathf.RoundToInt(damage * damageMultiplier));
    }
}