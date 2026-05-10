using System.Collections;
using UnityEngine;

/// <summary>
/// Skill Support: Tạo shield xoay quanh player.
/// Thay thế: ProtectedGreen.cs
/// </summary>
public class ShieldBehavior : SkillBehaviorBase
{
    [SerializeField] private float rotateSpeed = 3f;
    private Coroutine orbitCoroutine;
    private Transform shieldParent;

    protected override void OnActivate()
    {
        shieldParent = new GameObject("ShieldOrbit").transform;
        orbitCoroutine = StartCoroutine(OrbitLoop());
    }

    protected override void OnDeactivate()
    {
        if (orbitCoroutine != null) StopCoroutine(orbitCoroutine);
        if (shieldParent != null) Destroy(shieldParent.gameObject);
    }

    private IEnumerator OrbitLoop()
    {
        SpawnOrbiters();
        while (true)
        {
            if (playerTransform != null && shieldParent != null)
            {
                shieldParent.position = playerTransform.position;
                shieldParent.Rotate(0, 0, rotateSpeed * Time.deltaTime * 60f);
            }
            yield return null;
        }
    }

    private void SpawnOrbiters()
    {
        if (skillInstance.data.projectilePrefab == null || shieldParent == null) return;
        int count = GetProjectileCount();
        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * GetRadius();
            var orbiter = Instantiate(skillInstance.data.projectilePrefab, shieldParent);
            orbiter.transform.localPosition = offset;
        }
    }
}
