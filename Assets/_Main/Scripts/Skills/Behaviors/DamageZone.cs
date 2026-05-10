using System.Collections;
using UnityEngine;

/// <summary>
/// Component cho vùng damage (lửa, gas, explosion...).
/// Gắn trên prefab vùng damage. Gây damage liên tục cho enemy bên trong.
/// Dùng chung cho FireZoneBehavior, FireBombBehavior, v.v.
/// </summary>
public class DamageZone : MonoBehaviour
{
    private float damage;
    private float duration;
    private float tickInterval;
    private float timer;
    private bool isActive;

    /// <summary>
    /// Khởi tạo vùng damage.
    /// </summary>
    public void Setup(float dmg, float dur, float interval)
    {
        damage = dmg;
        duration = dur;
        tickInterval = interval;
        timer = 0f;
        isActive = true;
        StartCoroutine(LifetimeCoroutine());
    }

    private IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(duration);
        isActive = false;

        // Thử return pool
        var tag = GetComponent<PoolTag>();
        if (tag != null && ObjectPool.Instance != null)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive) return;

        timer += Time.deltaTime;
        if (timer < tickInterval) return;
        timer = 0f;

        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyManager>();
            if (enemy != null)
                enemy.TakeDamage(damage);
        }
    }
}
