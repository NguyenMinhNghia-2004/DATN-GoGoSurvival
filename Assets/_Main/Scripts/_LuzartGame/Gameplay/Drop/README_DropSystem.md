# Drop System - H? Th?ng Drop Linh Ho?t Không Gi?i H?n

## ?? T?ng Quan

Drop System ?ã ???c refactor ?? **lo?i b? hoàn toàn enum limitations**. Thay vào ?ó, system s? d?ng **string-based drop IDs** và **flexible effect system**, cho phép t?o b?t k? lo?i drop item nào v?i custom effects mà không b? gi?i h?n.

## ??? Ki?n Trúc M?i

```
DropItemConfig (Flexible Config)
    ? Uses String ID instead of Enum
    ? Custom Effects Support
DropBehavior (Attached to Entity)
    ? Detects Death & Triggers Drop
DropManager (Global Manager)
    ? Effect-based Processing
    ? Custom Effect Handlers
    ? Unlimited Drop Types
```

## ?? Files ?ã Refactor

1. **`DropItemData.cs`** - Lo?i b? enum, thêm flexible effects
2. **`DropBehavior.cs`** - Unchanged (compatible)
3. **`DropManager.cs`** - String-based lookup, effect handlers
4. **`DropSystemExample.cs`** - Updated examples
5. **`CustomDropEffects.cs`** - **NEW** - Custom effect examples

## ?? Drop Types - Không Còn Gi?i H?n!

### Built-in Effect Types
```csharp
public enum DropEffectType
{
    XP,           // T?ng XP
    Gold,         // T?ng Gold  
    Health,       // H?i máu
    Mana,         // H?i mana
    AttackBoost,  // T?ng damage t?m th?i
    SpeedBoost,   // T?ng t?c ?? t?m th?i
    Shield,       // T?o shield
    Custom        // Effect tùy ch?nh - UNLIMITED!
}
```

### Default Drop IDs Available
- **XP**: `"xp_small"`, `"xp_medium"`, `"xp_large"`, `"xp_rare"`
- **Gold**: `"gold_small"`, `"gold_medium"`, `"gold_large"`, `"gold_rare"`
- **Health**: `"health_small"`, `"health_large"`
- **Boosts**: `"speed_boost"`, `"attack_boost"`

## ?? Cách Setup - Updated

### B??c 1: Register DropManager (Unchanged)
```csharp
// Trong GameCoordinator.cs
[SerializeField] private DropManager dropManager;
domain.Add<DropManager>(dropManager);
```

### B??c 2: Add DropBehavior (Unchanged)
```csharp
// Trong EnemyCharacter.cs
AddBehavior<DropBehavior>();
```

### B??c 3: Configure Drops - NEW API
```csharp
// OLD: dropBehavior.AddDropEntry(dropManager.GetDropConfig(DropType.XP_Small), ...);
// NEW: Use string IDs instead!

var dropBehavior = enemy.GetBehavior<DropBehavior>();
var dropManager = domain.Resolve<DropManager>();

// S? d?ng drop IDs
var xpConfig = dropManager.GetDropConfig("xp_small");
dropBehavior.AddDropEntry(xpConfig, 0.8f, 1, 3);

var goldConfig = dropManager.GetDropConfig("gold_medium");
dropBehavior.AddDropEntry(goldConfig, 0.5f, 1, 2);
```

## ?? T?o Custom Drops - Unlimited Power!

### T?o Drop Config Runtime
```csharp
var customConfig = new DropItemConfig
{
    dropId = "my_custom_item",           // Unique string ID
    displayName = "Magic Scroll",        // Display name
    description = "Does amazing things", // Description
    effectType = DropEffectType.XP,      // Main effect
    effectValue = 100,                   // Effect value
    effectMultiplier = 1.5f,             // Multiplier
    
    // Visual settings
    texture = myCustomTexture,
    tintColor = Color.purple,
    scale = 0.6f,
    
    // Behavior settings
    lifetime = 60f,
    attractSpeed = 12f,
    attractRange = 5f,
    collectRange = 1.2f,
    
    // Audio/Visual feedback
    collectSoundId = "magic_pickup",
    collectParticleId = "sparkle_effect"
};

// Spawn it
dropManager.SpawnDropItem(customConfig, position);
```

### Multi-Effect Drops
```csharp
var treasureChest = new DropItemConfig
{
    dropId = "treasure_chest",
    displayName = "Treasure Chest",
    effectType = DropEffectType.XP,      // Main effect
    effectValue = 50,                    // Main value
    
    // Additional effects - UNLIMITED!
    additionalEffects = new DropEffect[]
    {
        new DropEffect
        {
            effectType = DropEffectType.Gold,
            value = 25,
            effectColor = Color.yellow,
            effectText = "+25 Gold"
        },
        new DropEffect
        {
            effectType = DropEffectType.Health,
            value = 50,
            effectColor = Color.red,
            effectText = "+50 Health"
        },
        new DropEffect
        {
            effectType = DropEffectType.Custom,
            customEffectId = "teleport",
            value = 10,
            effectText = "Teleported!"
        }
    }
};
```

## ?? Custom Effect Handlers - Extend Anything!

### Create Custom Effect Handler
```csharp
public class MyCustomEffect : IDropEffectHandler
{
    public string EffectId => "my_effect";

    public void ApplyEffect(DropEffect effect, IEntity target)
    {
        // Do anything you want!
        Debug.Log($"Applied custom effect with value {effect.value}");
        
        // Access game systems
        var gameManager = target.myDomain.Resolve<GameManager>();
        // Modify player stats
        // Trigger special abilities
        // Change game state
        // etc.
    }

    public bool CanApplyTo(IEntity target)
    {
        return target is PlayerCharacter; // Or any condition
    }
}

// Register it
dropManager.RegisterEffectHandler(new MyCustomEffect());

// Use it in drop configs
var customDrop = new DropItemConfig
{
    // ... other settings ...
    additionalEffects = new DropEffect[]
    {
        new DropEffect
        {
            effectType = DropEffectType.Custom,
            customEffectId = "my_effect",  // Links to handler
            value = 42
        }
    }
};
```

### Built-in Custom Examples
H? th?ng ?ã include s?n examples:
- **Teleport Effect**: `"teleport"` - Teleport player
- **Invincibility Effect**: `"invincibility"` - Temporary invincibility  
- **Explosion Effect**: `"explosion"` - Area damage
- **Time Slow Effect**: `"time_slow"` - Slow time for enemies

## ?? API Changes

### OLD vs NEW

#### OLD (Enum-based)
```csharp
// Limited by enum
dropManager.SpawnDropItem(DropType.XP_Small, position);
dropManager.GetDropConfig(DropType.XP_Large);
```

#### NEW (String-based)
```csharp
// Unlimited possibilities!
dropManager.SpawnDropItem("xp_small", position);
dropManager.SpawnDropItem("my_custom_super_item", position);
dropManager.GetDropConfig("anything_you_want");
```

### NEW APIs
```csharp
// Get configs by effect type
List<DropItemConfig> xpDrops = dropManager.GetDropConfigsByEffect(DropEffectType.XP);

// Register custom handlers
dropManager.RegisterEffectHandler(new MyCustomHandler());

// Get all configs
List<DropItemConfig> allConfigs = dropManager.GetAllConfigs();
```

## ?? Use Cases - Unlimited Creativity

### RPG Style Drops
```csharp
// Weapons
CreateDrop("sword_basic", DropEffectType.Custom, "equip_weapon");
CreateDrop("staff_fire", DropEffectType.Custom, "equip_staff");

// Potions  
CreateDrop("mana_potion", DropEffectType.Mana, 50);
CreateDrop("poison_antidote", DropEffectType.Custom, "cure_poison");

// Scrolls
CreateDrop("fireball_scroll", DropEffectType.Custom, "cast_fireball");
CreateDrop("teleport_scroll", DropEffectType.Custom, "teleport");
```

### Survival Game Drops
```csharp
// Resources
CreateDrop("wood", DropEffectType.Custom, "add_wood");
CreateDrop("iron_ore", DropEffectType.Custom, "add_iron");

// Food
CreateDrop("apple", DropEffectType.Health, 10);
CreateDrop("bread", DropEffectType.Custom, "add_hunger");
```

### Power-up Drops
```csharp
// Temporary abilities
CreateDrop("double_jump", DropEffectType.Custom, "enable_double_jump");
CreateDrop("wall_walk", DropEffectType.Custom, "enable_wall_walk");
CreateDrop("time_freeze", DropEffectType.Custom, "freeze_time");
```

## ?? Advanced Features

### Conditional Effects
```csharp
public class ConditionalEffectHandler : IDropEffectHandler
{
    public string EffectId => "conditional";
    
    public void ApplyEffect(DropEffect effect, IEntity target)
    {
        if (target is PlayerCharacter player)
        {
            // Different effects based on player state
            if (player.Stats.HP.Value < 30)
            {
                // Emergency heal
                player.Stats.HP.Value = player.Stats.MaxHP.Value;
            }
            else
            {
                // Normal effect
                player.Stats.XP.Value += effect.value;
            }
        }
    }
}
```

### Combo Effects
```csharp
public class ComboEffectHandler : IDropEffectHandler
{
    private int comboCount = 0;
    
    public void ApplyEffect(DropEffect effect, IEntity target)
    {
        comboCount++;
        int bonusValue = effect.value * comboCount;
        
        // Apply bonus based on combo
        ApplyBonusEffect(bonusValue, target);
        
        // Reset combo after 5 seconds
        StartCoroutine(ResetComboAfterDelay());
    }
}
```

## ? Migration Guide

### From Old System
1. Replace `DropType.XP_Small` ? `"xp_small"`
2. Replace `GetDropConfig(DropType.X)` ? `GetDropConfig("x")`  
3. Replace `SpawnDropItem(DropType.X, pos)` ? `SpawnDropItem("x", pos)`
4. Add custom effects if needed

### Benefits of New System
- ? **Unlimited drop types** - không còn b? gi?i h?n enum
- ? **Runtime creation** - t?o drops b?t k? lúc nào
- ? **Custom effects** - extend functionality không gi?i h?n
- ? **Multi-effects** - m?t drop có th? có nhi?u effects
- ? **Modding support** - d? dàng add content t? bên ngoài
- ? **Backward compatible** - APIs c? v?n work v?i string mapping

---

**Now You Can Drop Anything! ???**