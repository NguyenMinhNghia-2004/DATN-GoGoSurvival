using System.Collections;
using UnityEngine;

/// <summary>
/// Skill Attack: Tạo vùng lửa/gas tại vị trí ngẫu nhiên quanh player.
/// Thay thế: RanyRoute.cs + RanshoneManager.cs + ranshone.cs
/// Tương tự Survivor.io: Molotov Cocktail
/// </summary>
public class FireZoneBehavior : SkillBehaviorBase
{
    [Header("Fire Zone Settings")]
    [SerializeField] private float spawnRadius = 3f;
    [SerializeField] private float damageTickInterval = 0.5f;

    private Coroutine spawnCoroutine;

    protected override void OnActivate()
    {
        spawnCoroutine = StartCoroutine(SpawnLoop());
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

            int count = GetProjectileCount();
            for (int i = 0; i < count; i++)
            {
                SpawnFireZone();
                if (count > 1)
                    yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void SpawnFireZone()
    {
        if (skillInstance.data.skillPrefab == null && skillInstance.data.projectilePrefab == null)
            return;

        // Vị trí ngẫu nhiên quanh player
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = playerTransform.position + (Vector3)randomOffset;

        var prefab = skillInstance.data.projectilePrefab ?? skillInstance.data.skillPrefab;
        GameObject fireZone;

        if (ObjectPool.Instance != null)
            fireZone = ObjectPool.Instance.Get(prefab, spawnPos, Quaternion.identity);
        else
            fireZone = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Setup fire zone damage
        var zone = fireZone.GetComponent<DamageZone>();
        if (zone != null)
        {
            zone.Setup(GetCalculatedDamage(), GetDuration(), damageTickInterval);
        }
        else
        {
            // Fallback: tự destroy sau duration
            Destroy(fireZone, GetDuration());
        }
    }
}
