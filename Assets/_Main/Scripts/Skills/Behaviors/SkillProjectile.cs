using UnityEngine;

/// <summary>
/// Component cho projectile (đạn, mũi tên, rocket...).
/// Gắn trên prefab projectile. Tự homing tới target, gây damage, tự destroy/return pool.
/// Dùng chung cho nhiều skill attack.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SkillProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float defaultSpeed = 10f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private bool isHoming = true;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private bool piercing = false;
    [SerializeField] private int maxPierceCount = 1;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;

    // Runtime
    private Transform target;
    private float damage;
    private float speed;
    private int pierceCount;
    private Rigidbody2D rb;
    private float timer;
    private bool isLaunched;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Khởi tạo projectile. Gọi bởi skill behavior.
    /// </summary>
    public void Launch(Transform targetTransform, float dmg, float spd = 0f)
    {
        target = targetTransform;
        damage = dmg;
        speed = spd > 0 ? spd : defaultSpeed;
        pierceCount = 0;
        timer = 0f;
        isLaunched = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Launch theo hướng cố định (không homing).
    /// </summary>
    public void LaunchDirection(Vector2 direction, float dmg, float spd = 0f)
    {
        target = null;
        damage = dmg;
        speed = spd > 0 ? spd : defaultSpeed;
        pierceCount = 0;
        timer = 0f;
        isLaunched = true;

        rb.linearVelocity = direction.normalized * speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle;
    }

    private void Update()
    {
        if (!isLaunched) return;

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            ReturnToPool();
            return;
        }

        if (isHoming && target != null)
        {
            // Homing movement
            Vector2 direction = (Vector2)target.position - (Vector2)transform.position;
            transform.position = Vector2.MoveTowards(
                transform.position, target.position, speed * Time.deltaTime);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rb.rotation = angle;
        }
        else if (!isHoming)
        {
            // Đã set velocity trong LaunchDirection, chỉ cần bay thẳng
        }
        else
        {
            // Target bị destroy → tự hủy
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isLaunched) return;

        if (other.CompareTag("Enemy"))
        {
            // Gây damage cho enemy
            var enemy = other.GetComponent<EnemyManager>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // Spawn hit effect
            if (hitEffectPrefab != null)
            {
                if (ObjectPool.Instance != null)
                {
                    var fx = ObjectPool.Instance.Get(hitEffectPrefab,
                        transform.position, Quaternion.identity);
                    ObjectPool.Instance.ReturnDelayed(fx, hitEffectPrefab, 1f);
                }
                else
                {
                    var fx = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                    Destroy(fx, 1f);
                }
            }

            if (piercing)
            {
                pierceCount++;
                if (pierceCount >= maxPierceCount)
                    ReturnToPool();
            }
            else if (destroyOnHit)
            {
                ReturnToPool();
            }
        }
    }

    private void ReturnToPool()
    {
        isLaunched = false;
        rb.linearVelocity = Vector2.zero;

        // Thử return về pool, nếu không có pool thì destroy
        var tag = GetComponent<PoolTag>();
        if (tag != null && ObjectPool.Instance != null)
        {
            // Tìm prefab qua tag
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
