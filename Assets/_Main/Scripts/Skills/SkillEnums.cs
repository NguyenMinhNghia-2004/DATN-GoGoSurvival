/// <summary>
/// Enums cho hệ thống Skill.
/// SkillCategory phân loại Active / Passive / EVO.
/// PassiveStatType xác định loại buff cho Passive skills.
/// </summary>
public enum SkillCategory
{
    Active,     // Boomerang, Brick, Drill Shot, Durian, Forcefield, Guardian, Kunai, Molotov, Moonshade Slash, RPG, Soccer Ball
    Passive,    // Hi-Power Magnet, Fitness Guide, Ammo Thruster, HE Fuel, Energy Drink, Exo-Bracer, Oil Bond, Ronin Oyoroi, Sports Shoes, Koga Ninja Scroll, Energy Cube
    EVO         // Magnetic Rebounder, 1-Ton Iron, Whistling Arrow, Caltrops, Force Barrier, Defender, Spirit Shuriken, Fuel Barrel, Moonhalo Slash, Sharkmaw Gun, Quantum Ball
}

/// <summary>
/// Loại stat buff mà Passive skill cung cấp.
/// Mỗi Passive skill chỉ có 1 PassiveStatType.
/// </summary>
public enum PassiveStatType
{
    None,
    MaxHP,              // Fitness Guide: +20% ~ +100%
    MovementSpeed,      // Sports Shoes: +10% ~ +50%
    BulletSpeed,        // Ammo Thruster: +10% ~ +50%
    AttackRange,        // HE Fuel: +10% ~ +50%
    HPRegen,            // Energy Drink: 1% ~ 5% HP/5s
    SkillDuration,      // Exo-Bracer: +10% ~ +50%
    GoldGain,           // Oil Bond: +8% ~ +40%
    DamageReduction,    // Ronin Oyoroi: -10% ~ -50%
    ItemLootRange,      // Hi-Power Magnet: +100% ~ +500%
    EXPGain,            // Koga Ninja Scroll: +8% ~ +40%
    CooldownReduction   // Energy Cube: -8% ~ -40%
}
