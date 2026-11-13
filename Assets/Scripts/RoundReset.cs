using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundReset : MonoBehaviour
{
    [SerializeField] private Health player;
    [SerializeField] private Health enemy;
    [SerializeField] private float resetDelay = 2f; 

    private bool _resetting;

    void Awake()
    {
        if (player == null)
        {
            var p = FindFirstObjectByType<PlayerMovement>();
            if (p != null) player = p.GetComponentInParent<Health>();
        }
        if (enemy == null)
        {
            var e = FindFirstObjectByType<EnemyAI>();
            if (e != null) enemy = e.GetComponentInParent<Health>();
        }
    }

    void OnEnable()
    {
        if (player != null) player.OnDeath += HandleDeath;
        if (enemy != null) enemy.OnDeath += HandleDeath;
    }

    void OnDisable()
    {
        if (player != null) player.OnDeath -= HandleDeath;
        if (enemy != null) enemy.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (_resetting) return; 
        _resetting = true;
        StartCoroutine(ResetRoutine());
    }

    private IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(resetDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}