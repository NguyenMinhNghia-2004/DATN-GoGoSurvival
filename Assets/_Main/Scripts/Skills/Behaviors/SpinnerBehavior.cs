using System.Collections;
using UnityEngine;

/// <summary>
/// Skill Attack: Spinner xoay tròn quanh player, gây damage khi chạm enemy.
/// Thay thế: SpinerManager.cs + SpinnerGun.cs
/// Tương tự Survivor.io: Lightning Emitter / Guardian
/// </summary>
public class SpinnerBehavior : SkillBehaviorBase
{
    [Header("Spinner Settings")]
    [SerializeField] private float rotateSpeed = 5f;
    [SerializeField] private float orbitRadius = 1.5f;

    private CircleCollider2D col;
    private Coroutine pulseCoroutine;

    protected override void OnActivate()
    {
        col = GetComponent<CircleCollider2D>();
        pulseCoroutine = StartCoroutine(PulseLoop());
    }

    public override void OnLevelChanged(int newLevel)
    {
        // Level up → tăng radius, tăng damage
        if (col != null)
            col.radius = GetRadius();
    }

    protected override void OnDeactivate()
    {
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
    }

    private void Update()
    {
        if (!isInitialized || playerTransform == null) return;

        // Follow player position
        transform.position = playerTransform.position;

        // Rotate
        transform.Rotate(0, 0, -rotateSpeed);
    }

    /// <summary>
    /// Pulse: bật tắt collider theo chu kỳ (giống SpinerManager cũ).
    /// </summary>
    private IEnumerator PulseLoop()
    {
        while (true)
        {
            // Active phase
            if (col != null) col.enabled = true;
            yield return new WaitForSeconds(GetDuration());

            // Cooldown phase (fade out)
            if (col != null) col.enabled = false;
            yield return new WaitForSeconds(GetCooldown());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized) return;

        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyManager>();
            if (enemy != null)
                enemy.TakeDamage(GetCalculatedDamage());
        }
    }
}
