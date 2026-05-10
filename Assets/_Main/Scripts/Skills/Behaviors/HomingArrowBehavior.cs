using System.Collections;
using UnityEngine;

/// <summary>
/// Skill Attack: Bắn mũi tên/bolt tự homing vào enemy gần nhất.
/// Thay thế: aigule.cs + AiguleManager.cs + BoltSHooter.cs
/// Tương tự Survivor.io: Kunai
/// </summary>
public class HomingArrowBehavior : SkillBehaviorBase
{
    [Header("Settings")]
    [SerializeField] private float spawnInterval = 1f;

    private Coroutine spawnCoroutine;

    protected override void OnActivate()
    {
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public override void OnLevelChanged(int newLevel)
    {
        // Cooldown giảm khi level tăng
    }

    protected override void OnDeactivate()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(GetCooldown());

            if (playerTransform == null) continue;

            // Tìm enemy gần nhất
            var target = FindNearestEnemy();
            if (target == null) continue;

            int count = GetProjectileCount();
            for (int i = 0; i < count; i++)
            {
                SpawnArrow(target);
                if (count > 1)
                    yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void SpawnArrow(Transform target)
    {
        if (skillInstance.data.projectilePrefab == null) return;

        var pos = playerTransform.position;
        GameObject arrow;

        if (ObjectPool.Instance != null)
            arrow = ObjectPool.Instance.Get(skillInstance.data.projectilePrefab, pos, Quaternion.identity);
        else
            arrow = Instantiate(skillInstance.data.projectilePrefab, pos, Quaternion.identity);

        var projectile = arrow.GetComponent<SkillProjectile>();
        if (projectile != null)
        {
            projectile.Launch(target, GetCalculatedDamage(), 10f);
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
            float dist = Vector2.Distance(playerTransform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = e.transform;
            }
        }
        return nearest;
    }
}
