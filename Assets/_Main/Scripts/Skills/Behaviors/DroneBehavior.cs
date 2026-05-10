using System.Collections;
using UnityEngine;

/// <summary>
/// Skill Attack: Drone bay theo player và bắn rocket tự động.
/// Thay thế: DroneManager.cs + RocketManager.cs
/// Tương tự Survivor.io: Satellite Drone
/// </summary>
public class DroneBehavior : SkillBehaviorBase
{
    [Header("Drone Settings")]
    [SerializeField] private float followSpeed = 2.8f;
    [SerializeField] private float rocketBurstCount = 3;
    [SerializeField] private float rocketBurstDelay = 0.15f;

    private SpriteRenderer spriteRenderer;
    private Coroutine shootCoroutine;

    protected override void OnActivate()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        shootCoroutine = StartCoroutine(ShootLoop());
    }

    public override void OnLevelChanged(int newLevel)
    {
        // Level up → bắn nhiều rocket hơn mỗi burst
    }

    protected override void OnDeactivate()
    {
        if (shootCoroutine != null)
            StopCoroutine(shootCoroutine);
    }

    private void Update()
    {
        if (!isInitialized || playerTransform == null) return;

        // Follow player
        transform.position = Vector2.MoveTowards(
            transform.position, playerTransform.position, followSpeed * Time.deltaTime);

        // Flip sprite theo player
        if (spriteRenderer != null)
            spriteRenderer.flipX = playerTransform.position.x < transform.position.x;
    }

    private IEnumerator ShootLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(GetCooldown());

            int burstCount = GetProjectileCount();

            for (int i = 0; i < burstCount; i++)
            {
                ShootRocket();
                yield return new WaitForSeconds(rocketBurstDelay);
            }
        }
    }

    private void ShootRocket()
    {
        if (skillInstance.data.projectilePrefab == null) return;

        // Tìm enemy gần nhất
        var target = FindNearestEnemy();

        GameObject rocket;
        if (ObjectPool.Instance != null)
            rocket = ObjectPool.Instance.Get(skillInstance.data.projectilePrefab,
                transform.position, Quaternion.identity);
        else
            rocket = Instantiate(skillInstance.data.projectilePrefab,
                transform.position, Quaternion.identity);

        var projectile = rocket.GetComponent<SkillProjectile>();
        if (projectile != null)
        {
            if (target != null)
                projectile.Launch(target, GetCalculatedDamage(), 8f);
            else
            {
                // Không có enemy → bắn random
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                projectile.LaunchDirection(randomDir, GetCalculatedDamage(), 8f);
            }
        }
    }

    private Transform FindNearestEnemy()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return null;

        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var e in enemies)
        {
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < minDist) { minDist = dist; nearest = e.transform; }
        }
        return nearest;
    }
}
