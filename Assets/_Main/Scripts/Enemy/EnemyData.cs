using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa data cho 1 loại enemy.
/// Tạo asset: Right-click > Create > GoGo > Enemy Data
/// HP và damage scale theo wave number.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "GoGo/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("=== Basic Info ===")]
    public string enemyId;
    public string enemyName;

    [Header("=== Base Stats ===")]
    public float baseHP = 100f;
    public float baseDamage = 10f;
    public float moveSpeed = 2f;

    [Header("=== Rewards ===")]
    public float expReward = 1f;
    public int coinReward = 10;

    [Header("=== Scaling Per Wave ===")]
    [Tooltip("HP tăng thêm bao nhiêu % mỗi wave")]
    public float hpScalePerWave = 0.15f;

    [Tooltip("Damage tăng thêm bao nhiêu % mỗi wave")]
    public float damageScalePerWave = 0.1f;

    [Tooltip("Tốc độ tăng thêm bao nhiêu % mỗi wave (nhỏ hơn)")]
    public float speedScalePerWave = 0.02f;

    // ---- Helpers ----

    /// <summary>Tính HP tại wave cụ thể.</summary>
    public float GetHP(int wave)
    {
        return baseHP * (1f + hpScalePerWave * (wave - 1));
    }

    /// <summary>Tính damage tại wave cụ thể.</summary>
    public float GetDamage(int wave)
    {
        return baseDamage * (1f + damageScalePerWave * (wave - 1));
    }

    /// <summary>Tính move speed tại wave cụ thể.</summary>
    public float GetMoveSpeed(int wave)
    {
        return moveSpeed * (1f + speedScalePerWave * (wave - 1));
    }
}
