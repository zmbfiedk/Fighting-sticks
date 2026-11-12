using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;

    public float DamageMultiplier { get; set; } = 1f;

    public event Action<int> OnDamaged; 
    public event Action OnDeath;

    void Awake() => CurrentHealth = maxHealth;

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(amount * DamageMultiplier));

        CurrentHealth = Mathf.Max(CurrentHealth - finalDamage, 0);
        OnDamaged?.Invoke(finalDamage);
        Debug.Log($"{name} took {finalDamage} damage ({CurrentHealth}/{maxHealth})");

        if (IsDead)
        {
            OnDeath?.Invoke();
            Debug.Log($"{name} died");
        }
    }
}