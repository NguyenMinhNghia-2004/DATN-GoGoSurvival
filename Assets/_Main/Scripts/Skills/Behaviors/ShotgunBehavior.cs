using System.Collections;
using UnityEngine;

/// <summary>
/// Skill Attack: Bắn nhiều viên đạn ra nhiều hướng (shotgun pattern).
/// Thay thế: GunManager.cs + GunBullte.cs
/// Tương tự Survivor.io: Shotgun
/// </summary>
public class ShotgunBehavior : SkillBehaviorBase
{
    private Coroutine shootCoroutine;

    protected override void OnActivate()
    {
        shootCoroutine = StartCoroutine(ShootLoop());
    }

    protected override void OnDeactivate()
    {
        if (shootCoroutine != null)
            StopCoroutine(shootCoroutine);
    }

    private IEnumerator ShootLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(GetCooldown());

            if (playerTransform == null) continue;

            int count = GetProjectileCount(); // 6 ở level 1, tăng theo level
            float spreadAngle = 360f; // bắn xung quanh
            float angleStep = spreadAngle / count;
            float startAngle = 0f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + (angleStep * i);
                Vector2 dir = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                SpawnBullet(dir);
            }
        }
    }

    private void SpawnBullet(Vector2 direction)
    {
        if (skillInstance.data.projectilePrefab == null) return;

        var pos = playerTransform.position;
        GameObject bullet;

        if (ObjectPool.Instance != null)
            bullet = ObjectPool.Instance.Get(skillInstance.data.projectilePrefab, pos, Quaternion.identity);
        else
            bullet = Instantiate(skillInstance.data.projectilePrefab, pos, Quaternion.identity);

        var projectile = bullet.GetComponent<SkillProjectile>();
        if (projectile != null)
        {
            projectile.LaunchDirection(direction, GetCalculatedDamage(), 7f);
        }
    }
}
