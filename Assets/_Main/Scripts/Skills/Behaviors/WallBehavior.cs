using System.Collections;
using UnityEngine;

/// <summary>
/// Skill Support: Tạo tường chặn enemy định kỳ.
/// Thay thế: Brick.cs + brickManager.cs
/// </summary>
public class WallBehavior : SkillBehaviorBase
{
    private Coroutine spawnCoroutine;

    protected override void OnActivate()
    {
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    protected override void OnDeactivate()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(GetCooldown());
            if (playerTransform == null) continue;
            SpawnWall();
        }
    }

    private void SpawnWall()
    {
        if (skillInstance.data.projectilePrefab == null) return;
        var pos = playerTransform.position;
        GameObject wall;
        if (ObjectPool.Instance != null)
            wall = ObjectPool.Instance.Get(skillInstance.data.projectilePrefab, pos, Quaternion.identity);
        else
            wall = Instantiate(skillInstance.data.projectilePrefab, pos, Quaternion.identity);

        Destroy(wall, GetDuration());
    }
}
