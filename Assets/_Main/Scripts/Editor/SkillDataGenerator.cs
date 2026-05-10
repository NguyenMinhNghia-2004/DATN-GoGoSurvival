using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor tool tự động tạo tất cả 33 SkillData ScriptableObjects.
/// Chạy từ menu: GoGo > Generate All Skill Data
/// Tạo trong: Assets/_Main/Data/Skills/Active, Passive, EVO
/// Tự động link evolution partners và tạo/cập nhật SkillDatabase.
/// </summary>
public class SkillDataGenerator : EditorWindow
{
    private const string BASE_PATH = "Assets/_Main/Data/Skills";
    private const string ACTIVE_PATH = BASE_PATH + "/Active";
    private const string PASSIVE_PATH = BASE_PATH + "/Passive";
    private const string EVO_PATH = BASE_PATH + "/EVO";
    private const string DATABASE_PATH = BASE_PATH + "/SkillDatabase.asset";

    [MenuItem("GoGo/Generate All Skill Data")]
    public static void GenerateAll()
    {
        // Tạo folders
        EnsureFolder(BASE_PATH);
        EnsureFolder(ACTIVE_PATH);
        EnsureFolder(PASSIVE_PATH);
        EnsureFolder(EVO_PATH);

        // ============================
        // 1. Tạo Passive Skills (tạo trước để link vào Active)
        // ============================
        var hiPowerMagnet = CreatePassive("hi_power_magnet", "Hi-Power Magnet",
            PassiveStatType.ItemLootRange,
            new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f },
            new string[] { "Item loot range +100%", "Item loot range +200%", "Item loot range +300%", "Item loot range +400%", "Item loot range +500%" });

        var fitnessGuide = CreatePassive("fitness_guide", "Fitness Guide",
            PassiveStatType.MaxHP,
            new float[] { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f },
            new string[] { "Max HP +20%", "Max HP +40%", "Max HP +60%", "Max HP +80%", "Max HP +100%" });

        var ammoThruster = CreatePassive("ammo_thruster", "Ammo Thruster",
            PassiveStatType.BulletSpeed,
            new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f },
            new string[] { "Bullet flight speed +10%", "Bullet flight speed +20%", "Bullet flight speed +30%", "Bullet flight speed +40%", "Bullet flight speed +50%" });

        var heFuel = CreatePassive("he_fuel", "HE Fuel",
            PassiveStatType.AttackRange,
            new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f },
            new string[] { "All ammo and weapon range +10%", "All ammo and weapon range +20%", "All ammo and weapon range +30%", "All ammo and weapon range +40%", "All ammo and weapon range +50%" });

        var energyDrink = CreatePassive("energy_drink", "Energy Drink",
            PassiveStatType.HPRegen,
            new float[] { 0.01f, 0.02f, 0.03f, 0.04f, 0.05f },
            new string[] { "Restores 1% HP/5s", "Restores 2% HP/5s", "Restores 3% HP/5s", "Restores 4% HP/5s", "Restores 5% HP/5s" });

        var exoBracer = CreatePassive("exo_bracer", "Exo-Bracer",
            PassiveStatType.SkillDuration,
            new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f },
            new string[] { "Over-time effect duration +10%", "Over-time effect duration +20%", "Over-time effect duration +30%", "Over-time effect duration +40%", "Over-time effect duration +50%" });

        var oilBond = CreatePassive("oil_bond", "Oil Bond",
            PassiveStatType.GoldGain,
            new float[] { 0.08f, 0.16f, 0.24f, 0.32f, 0.4f },
            new string[] { "Gold gain +8%", "Gold gain +16%", "Gold gain +24%", "Gold gain +32%", "Gold gain +40%" });

        var roninOyoroi = CreatePassive("ronin_oyoroi", "Ronin Oyoroi",
            PassiveStatType.DamageReduction,
            new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f },
            new string[] { "Received damage -10%", "Received damage -20%", "Received damage -30%", "Received damage -40%", "Received damage -50%" });

        var sportsShoes = CreatePassive("sports_shoes", "Sports Shoes",
            PassiveStatType.MovementSpeed,
            new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f },
            new string[] { "Movement Speed +10%", "Movement Speed +20%", "Movement Speed +30%", "Movement Speed +40%", "Movement Speed +50%" });

        var kogaNinjaScroll = CreatePassive("koga_ninja_scroll", "Koga Ninja Scroll",
            PassiveStatType.EXPGain,
            new float[] { 0.08f, 0.16f, 0.24f, 0.32f, 0.4f },
            new string[] { "EXP gain +8%", "EXP gain +16%", "EXP gain +24%", "EXP gain +32%", "EXP gain +40%" });

        var energyCube = CreatePassive("energy_cube", "Energy Cube",
            PassiveStatType.CooldownReduction,
            new float[] { 0.08f, 0.16f, 0.24f, 0.32f, 0.4f },
            new string[] { "All attack CD -8%", "All attack CD -16%", "All attack CD -24%", "All attack CD -32%", "All attack CD -40%" });

        // ============================
        // 2. Tạo EVO Skills (tạo trước để link vào Active)
        // ============================
        var magneticRebounder = CreateEVO("magnetic_rebounder", "Magnetic Rebounder", 6.0f,
            "Reverse polarity. 2 magnetic boomerangs orbit around the player.");

        var oneTonIron = CreateEVO("one_ton_iron", "1-Ton Iron", 6.0f,
            "Toughest in the world. 8 dumbells fly out infinitely.");

        var whistlingArrow = CreateEVO("whistling_arrow", "Whistling Arrow", 12.6f,
            "Till death do we rest. Drills fly forever, piercing all.");

        var caltrops = CreateEVO("caltrops", "Caltrops", 10.0f,
            "Fruit shower incoming. Increased size, fires penetrating spikes.");

        var forceBarrier = CreateEVO("force_barrier", "Force Barrier", 2.5f,
            "This is my domain. Slows enemies on contact.");

        var defender = CreateEVO("defender", "Defender", 0.9f,
            "The Wheel of Death. Tops orbit permanently, no time limit.");

        var spiritShuriken = CreateEVO("spirit_shuriken", "Spirit Shuriken", 5.5f,
            "In and out like a ghost. Kunai split after hitting enemy.");

        var fuelBarrel = CreateEVO("fuel_barrel", "Fuel Barrel", 1.2f,
            "Scorched earth policy. Blue fire surrounds player, slows enemies.");

        var moonhaloSlash = CreateEVO("moonhalo_slash", "Moonhalo Slash", 8.0f,
            "Eclipse of the crescent moon. Slashes orbit continuously.");

        var sharkmawGun = CreateEVO("sharkmaw_gun", "Sharkmaw Gun", 20.0f,
            "Death and destruction. Massive rockets with huge explosions.");

        var quantumBall = CreateEVO("quantum_ball", "Quantum Ball", 8.0f,
            "Quantum entanglement. Balls teleport between enemies.");

        // ============================
        // 3. Tạo Active Skills (link Passive partner + EVO)
        // ============================
        CreateActive("boomerang", "Boomerang",
            new float[] { 2.4f, 2.4f, 4.8f, 4.8f, 6.0f, 6.0f },
            new int[] { 1, 2, 2, 2, 2 },
            new float[] { 2.0f, 2.0f, 1.8f, 1.6f, 1.4f },
            new float[] { 3.0f, 3.0f, 3.0f, 3.5f, 4.0f },
            new float[] { 1.5f, 1.5f, 1.5f, 2.0f, 2.5f },
            hiPowerMagnet, magneticRebounder,
            new string[] { "Throws 1 boomerang", "+1 boomerang", "Boomerang damage doubled", "Adds boomerang size", "Adds boomerang size + damage", "Reverse polarity" });

        CreateActive("brick", "Brick",
            new float[] { 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 6.0f },
            new int[] { 1, 2, 3, 4, 5 },
            new float[] { 2.5f, 2.5f, 2.2f, 2.0f, 1.8f },
            new float[] { 2.0f, 2.0f, 2.5f, 3.0f, 3.5f },
            new float[] { 1.0f, 1.0f, 1.2f, 1.5f, 1.8f },
            fitnessGuide, oneTonIron,
            new string[] { "Throws 1 brick", "+1 brick, adds damage", "+1 brick, adds damage", "+1 brick, adds damage", "+1 brick, adds damage", "Toughest in the world" });

        CreateActive("drill_shot", "Drill Shot",
            new float[] { 1.0f, 1.0f, 1.0f, 1.333f, 1.333f, 12.6f },
            new int[] { 1, 2, 2, 2, 3 },
            new float[] { 1.5f, 1.5f, 1.2f, 1.0f, 0.8f },
            new float[] { 4.0f, 4.0f, 4.0f, 5.0f, 5.0f },
            new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f },
            ammoThruster, whistlingArrow,
            new string[] { "Fires a drill", "+1 drill", "Drill flight speed doubled", "Drill speed up, adds damage", "+1 drill", "Till death do we rest" });

        CreateActive("durian", "Durian",
            new float[] { 6.0f, 6.0f, 8.0f, 10.0f, 10.0f, 10.0f },
            new int[] { 1, 1, 1, 1, 1 },
            new float[] { 3.0f, 3.0f, 2.5f, 2.5f, 2.0f },
            new float[] { 5.0f, 5.0f, 5.0f, 6.0f, 7.0f },
            new float[] { 1.0f, 1.0f, 1.5f, 1.5f, 2.0f },
            heFuel, caltrops,
            new string[] { "Throws a durian that sticks around", "Durian damage doubled", "Durian size doubled", "Adds more durian damage", "Durian damage doubled", "Fruit shower incoming" });

        CreateActive("forcefield", "Forcefield",
            new float[] { 1.0f, 1.5f, 2.0f, 2.25f, 2.5f, 2.5f },
            new int[] { 1, 1, 1, 1, 1 },
            new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f },
            new float[] { 0f, 0f, 0f, 0f, 0f },
            new float[] { 1.5f, 2.0f, 2.5f, 3.0f, 3.5f },
            energyDrink, forceBarrier,
            new string[] { "Generates a dissolving forcefield", "Adds area size + damage", "Adds area size + damage", "Adds area size + damage", "Adds area size + damage", "This is my domain" });

        CreateActive("guardian", "Guardian",
            new float[] { 0.5f, 0.6f, 0.7f, 0.9f, 0.9f, 0.9f },
            new int[] { 2, 3, 4, 5, 6 },
            new float[] { 0f, 0f, 0f, 0f, 0f },
            new float[] { 5.0f, 5.0f, 6.0f, 6.0f, 7.0f },
            new float[] { 1.5f, 1.5f, 2.0f, 2.0f, 2.5f },
            exoBracer, defender,
            new string[] { "Summons 2 tops, stops bullets", "+1 top, adds spin speed/damage", "+1 top, adds spin speed/damage", "+1 top, adds spin speed/damage", "+1 top, adds spin speed/damage", "The Wheel of Death" });

        CreateActive("kunai", "Kunai",
            new float[] { 1.5f, 2.0f, 3.0f, 4.0f, 5.5f, 5.5f },
            new int[] { 1, 2, 3, 4, 5 },
            new float[] { 0.8f, 0.7f, 0.6f, 0.5f, 0.4f },
            new float[] { 0f, 0f, 0f, 0f, 0f },
            new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f },
            kogaNinjaScroll, spiritShuriken,
            new string[] { "Throws kunai", "+1 kunai, adds damage", "+1 kunai, adds damage", "+1 kunai, adds damage", "+1 kunai, adds damage", "In and out like a ghost" });

        CreateActive("molotov", "Molotov",
            new float[] { 0.6f, 0.8f, 0.9f, 1.1f, 1.2f, 1.2f },
            new int[] { 2, 3, 4, 5, 6 },
            new float[] { 3.0f, 3.0f, 2.5f, 2.5f, 2.0f },
            new float[] { 3.0f, 3.5f, 4.0f, 4.5f, 5.0f },
            new float[] { 1.5f, 2.0f, 2.5f, 3.0f, 3.5f },
            oilBond, fuelBarrel,
            new string[] { "Throws 2 molotovs", "+1 molotov, bigger burn area", "+1 molotov, bigger burn area, adds damage", "+1 molotov, bigger burn area, adds damage", "+1 molotov, bigger burn area, adds damage", "Scorched earth policy" });

        CreateActive("moonshade_slash", "Moonshade Slash",
            new float[] { 3.0f, 3.0f, 4.5f, 6.0f, 6.0f, 8.0f },
            new int[] { 1, 2, 2, 2, 3 },
            new float[] { 1.8f, 1.8f, 1.5f, 1.2f, 1.0f },
            new float[] { 0f, 0f, 0f, 0f, 0f },
            new float[] { 2.0f, 2.0f, 2.5f, 3.0f, 3.5f },
            roninOyoroi, moonhaloSlash,
            new string[] { "Releases 1 crescent slash", "+1 slash", "Slash size up, adds damage", "Slash speed up, adds damage", "+1 slash", "Eclipse of the crescent moon" });

        CreateActive("rpg", "RPG",
            new float[] { 2.0f, 4.0f, 4.0f, 6.0f, 6.0f, 20.0f },
            new int[] { 1, 1, 2, 2, 3 },
            new float[] { 3.5f, 3.5f, 3.0f, 2.5f, 2.0f },
            new float[] { 0f, 0f, 0f, 0f, 0f },
            new float[] { 2.0f, 2.0f, 2.5f, 3.0f, 3.5f },
            heFuel, sharkmawGun,
            new string[] { "Fires an explosive rocket", "Rocket damage doubled", "+1 rocket", "Adds more rocket damage", "+1 rocket", "Death and destruction" });

        CreateActive("soccer_ball", "Soccer Ball",
            new float[] { 1.0f, 1.0f, 4.0f, 5.0f, 5.0f, 8.0f },
            new int[] { 1, 2, 2, 2, 3 },
            new float[] { 2.5f, 2.5f, 2.0f, 1.8f, 1.5f },
            new float[] { 5.0f, 5.0f, 5.0f, 6.0f, 7.0f },
            new float[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f },
            sportsShoes, quantumBall,
            new string[] { "Throws 1 football that bounces", "+1 football", "Increases flight speed + damage", "Increases flight speed + damage", "+1 football", "Quantum entanglement" });

        // ============================
        // 4. Tạo / cập nhật SkillDatabase
        // ============================
        CreateSkillDatabase();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SkillDataGenerator] ✅ Generated 33 SkillData SOs + SkillDatabase at {BASE_PATH}");
        EditorUtility.DisplayDialog("SkillData Generator",
            "Đã tạo thành công 33 SkillData SOs!\n\n" +
            "• 11 Active Skills (Active/)\n" +
            "• 11 Passive Skills (Passive/)\n" +
            "• 11 EVO Skills (EVO/)\n" +
            "• 1 SkillDatabase\n\n" +
            "Tất cả evolution links đã được auto-assign.",
            "OK");
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static SkillData CreatePassive(string id, string name,
        PassiveStatType statType, float[] values, string[] descriptions)
    {
        string path = $"{PASSIVE_PATH}/{name}.asset";
        var skill = LoadOrCreate<SkillData>(path);

        skill.skillId = id;
        skill.skillName = name;
        skill.category = SkillCategory.Passive;
        skill.maxLevel = 5;

        skill.passiveStatType = statType;
        skill.passiveValues = values;

        // Passive không dùng atkMultiplier
        skill.atkMultiplier = new float[] { 0, 0, 0, 0, 0, 0 };
        skill.cooldown = new float[] { 0, 0, 0, 0, 0 };
        skill.duration = new float[] { 0, 0, 0, 0, 0 };
        skill.radius = new float[] { 0, 0, 0, 0, 0 };
        skill.projectileCount = new int[] { 0, 0, 0, 0, 0 };

        // Descriptions (5 levels, pad nếu thiếu)
        skill.levelDescriptions = PadDescriptions(descriptions, 6);

        EditorUtility.SetDirty(skill);
        return skill;
    }

    private static SkillData CreateEVO(string id, string name, float atkMult, string description)
    {
        string path = $"{EVO_PATH}/{name}.asset";
        var skill = LoadOrCreate<SkillData>(path);

        skill.skillId = id;
        skill.skillName = name;
        skill.category = SkillCategory.EVO;
        skill.maxLevel = 1;

        skill.atkMultiplier = new float[] { atkMult, 0, 0, 0, 0, 0 };
        skill.cooldown = new float[] { 0, 0, 0, 0, 0 };
        skill.duration = new float[] { 0, 0, 0, 0, 0 };
        skill.radius = new float[] { 0, 0, 0, 0, 0 };
        skill.projectileCount = new int[] { 0, 0, 0, 0, 0 };
        skill.passiveStatType = PassiveStatType.None;
        skill.passiveValues = new float[] { 0, 0, 0, 0, 0 };

        skill.levelDescriptions = new string[] { description, "", "", "", "", "" };

        EditorUtility.SetDirty(skill);
        return skill;
    }

    private static SkillData CreateActive(string id, string name,
        float[] atkMults, int[] projCounts,
        float[] cooldowns, float[] durations, float[] radii,
        SkillData passivePartner, SkillData evolvedForm,
        string[] descriptions)
    {
        string path = $"{ACTIVE_PATH}/{name}.asset";
        var skill = LoadOrCreate<SkillData>(path);

        skill.skillId = id;
        skill.skillName = name;
        skill.category = SkillCategory.Active;
        skill.maxLevel = 5;

        skill.atkMultiplier = PadFloats(atkMults, 6);
        skill.projectileCount = PadInts(projCounts, 5);
        skill.cooldown = PadFloats(cooldowns, 5);
        skill.duration = PadFloats(durations, 5);
        skill.radius = PadFloats(radii, 5);

        skill.passiveStatType = PassiveStatType.None;
        skill.passiveValues = new float[] { 0, 0, 0, 0, 0 };

        skill.evolutionPartner = passivePartner;
        skill.evolvedForm = evolvedForm;

        skill.levelDescriptions = PadDescriptions(descriptions, 6);

        EditorUtility.SetDirty(skill);
        return skill;
    }

    private static void CreateSkillDatabase()
    {
        var db = LoadOrCreate<SkillDatabase>(DATABASE_PATH);

        var allSkills = new List<SkillData>();

        // Load tất cả SkillData từ folder
        string[] guids = AssetDatabase.FindAssets("t:SkillData", new[] { BASE_PATH });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(assetPath);
            if (skill != null)
                allSkills.Add(skill);
        }

        db.allSkills = allSkills.ToArray();
        EditorUtility.SetDirty(db);

        Debug.Log($"[SkillDataGenerator] SkillDatabase populated with {allSkills.Count} skills");
    }

    // ---- Utility ----

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folderName = Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static float[] PadFloats(float[] source, int length)
    {
        var result = new float[length];
        for (int i = 0; i < length; i++)
            result[i] = i < source.Length ? source[i] : (source.Length > 0 ? source[source.Length - 1] : 0);
        return result;
    }

    private static int[] PadInts(int[] source, int length)
    {
        var result = new int[length];
        for (int i = 0; i < length; i++)
            result[i] = i < source.Length ? source[i] : (source.Length > 0 ? source[source.Length - 1] : 0);
        return result;
    }

    private static string[] PadDescriptions(string[] source, int length)
    {
        var result = new string[length];
        for (int i = 0; i < length; i++)
            result[i] = i < source.Length ? source[i] : "";
        return result;
    }
}
