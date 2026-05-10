using UnityEngine;

/// <summary>
/// Base class cho tất cả skill behaviors.
/// Mỗi skill cụ thể kế thừa class này.
/// Được gắn trên prefab của skill → instantiate khi player chọn skill.
/// Chỉ dùng cho Active và EVO skills (Passive không có behavior).
/// </summary>
public abstract class SkillBehaviorBase : MonoBehaviour
{
    protected SkillInstance skillInstance;
    protected Transform playerTransform;
    protected bool isInitialized;

    /// <summary>
    /// Gọi bởi SkillManager khi skill được kích hoạt.
    /// </summary>
    public virtual void Initialize(SkillInstance instance)
    {
        skillInstance = instance;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        isInitialized = true;
        OnActivate();
    }

    /// <summary>
    /// Gọi khi skill level thay đổi (upgrade).
    /// Override để cập nhật stats runtime (VD: tăng damage, giảm cooldown).
    /// </summary>
    public virtual void OnLevelChanged(int newLevel)
    {
        // Base: chỉ cập nhật reference, subclass override để áp dụng
    }

    /// <summary>
    /// Gọi khi skill lần đầu được kích hoạt.
    /// Override để start coroutines, spawn objects, etc.
    /// </summary>
    protected abstract void OnActivate();

    /// <summary>
    /// Gọi khi skill bị deactivate (VD: evolution thay thế).
    /// Override để cleanup.
    /// </summary>
    protected virtual void OnDeactivate()
    {
        // Base implementation — subclass override nếu cần cleanup
    }

    private void OnDestroy()
    {
        OnDeactivate();
    }

    // ---- Helpers cho subclass ----

    /// <summary>Lấy ATK multiplier hiện tại theo level. Dùng để tính damage = BaseATK * multiplier.</summary>
    protected float GetAtkMultiplier() => skillInstance?.AtkMultiplier ?? 1f;

    /// <summary>Lấy damage tính toán cuối = PlayerStats.BaseATK * AtkMultiplier.</summary>
    protected float GetCalculatedDamage()
    {
        float baseAtk = PlayerStats.Instance != null ? PlayerStats.Instance.FinalATK : 10f;
        return baseAtk * GetAtkMultiplier();
    }

    /// <summary>Lấy cooldown hiện tại theo level (đã áp dụng CDR từ PlayerStats).</summary>
    protected float GetCooldown()
    {
        float baseCooldown = skillInstance?.Cooldown ?? 1f;
        float cdrMultiplier = PlayerStats.Instance != null ? PlayerStats.Instance.CooldownMultiplier : 1f;
        return baseCooldown * cdrMultiplier;
    }

    /// <summary>Lấy duration hiện tại theo level (đã áp dụng duration bonus từ passive).</summary>
    protected float GetDuration()
    {
        float baseDuration = skillInstance?.Duration ?? 1f;
        float durationMult = PlayerStats.Instance != null ? PlayerStats.Instance.SkillDurationMultiplier : 1f;
        return baseDuration * durationMult;
    }

    /// <summary>Lấy radius hiện tại theo level.</summary>
    protected float GetRadius() => skillInstance?.Radius ?? 1f;

    /// <summary>Lấy projectile count hiện tại theo level.</summary>
    protected int GetProjectileCount() => skillInstance?.ProjectileCount ?? 1;
}
