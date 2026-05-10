using System.Collections;
using UnityEngine;

/// <summary>
/// Skill Attack: Bóng nảy giữa các cạnh màn hình, gây damage khi chạm enemy.
/// Thay thế: ballManager.cs + AddBallForce.cs
/// Tương tự Survivor.io: Brick / Soccer Ball
/// </summary>
public class BounceBallBehavior : SkillBehaviorBase
{
    [Header("Ball Settings")]
    [SerializeField] private float bounceForce = 2f;

    private Rigidbody2D rb;

    protected override void OnActivate()
    {
        rb = GetComponent<Rigidbody2D>();

        // Tung bóng ra hướng random
        if (rb != null)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            rb.AddForce(dir * bounceForce, ForceMode2D.Impulse);
        }
    }

    public override void OnLevelChanged(int newLevel)
    {
        // Level up → tăng damage, có thể thêm bóng
    }

    private void Update()
    {
        if (!isInitialized) return;
        transform.Rotate(0, 0, 5f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb == null) return;

        // Phản xạ khi chạm tường
        var speed = rb.linearVelocity.magnitude;
        var direction = Vector2.Reflect(
            rb.linearVelocity.normalized, collision.contacts[0].normal);
        rb.linearVelocity = direction * Mathf.Max(speed, bounceForce);
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
