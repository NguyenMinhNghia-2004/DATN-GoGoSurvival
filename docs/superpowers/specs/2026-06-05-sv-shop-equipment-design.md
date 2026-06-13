# SV_Shop + SV_Equipment — Logic Design (adapted from IO_Training)

Date: 2026-06-05
Status: Approved (design Q&A) → implementation

## Goal
The `SV_Shop` and `SV_Equipement` prefabs (registered NinjaUI screens, UIRegistry 6001/6002)
currently have only visuals + dead "Missing Script" components. They open from the Main Menu
but have **no visible close button** and no logic. Add:
1. A working **Close button** on each popup.
2. **Shop** logic: buy items with coins.
3. **Equipment** logic: equip/unequip owned items, apply their stats to the player.
4. Clean up the missing-script components.

Logic is modeled on IO_Training but built on GoGoSurvival's real infrastructure.

## Why not port IO_Training wholesale
IO_Training uses `PopupService`/`PopupOpener`/`Popup<T>`/`ViewT<T>` + `ItemConfig.AssetUnlockable`
+ a live modifier pipeline. In GoGoSurvival:
- The prefabs already live inside **NinjaUI** (`Luzart.UIManager`), a different UI framework.
- `ItemConfig.asset` files have `unlockable: {fileID:0}` and `assetLevel: {fileID:0}` (null).
  `ItemConfigsOwned.DoInitialize` dereferences `item.Unlockable.IsUnlocked` → would
  **NullReferenceException**. The heavy item graph is not authored for use here.
- The modifier pipeline (`Stat_Player_*` SOs) is dormant (mode=Constant, not wired to Final numbers).

So we adapt the **patterns**, not the fragile graph.

## Existing primitives we build on (verified)
- `CurrencyManager` (singleton): `long Coins`, `AddCoin(long)` (negative = spend),
  `event Action<long> OnCoinChanged`. (`_FrameworkStubs.cs`)
- `StatsBehavior.ApplyStatBonus(StatType, double factor, StatBonusMode)` with modes
  `Additive | PercentMultiply | PercentSubtract`; plus `RemoveStatBonus`.
- NinjaUI `UIBase`: `OnCreateAsync` (once), `OnBeforeShowAsync` (each show),
  `OnCloseButtonClicked()` → fires `OnCloseRequested` → UIManager hides the popup.
- `ETypeItem { Weapon, Armor, Necklace, Belt, Gloves, Shoes }`, `ERarity { Rare, Epic, Legend }`.
- `Eq_*.asset` (26 equipment definitions) + `ItemConfig_*` (130) exist for names/sprites.

## Components to build

### 1. Data — `SV_ItemCatalog` (ScriptableObject)
`Assets/_Main/Data/Shop/SV_ItemCatalog.asset` holding `List<SV_ItemEntry>`:
```
[Serializable] class SV_ItemEntry {
  string   id;            // stable key (e.g. "Eq_Kunai")
  string   displayName;
  Sprite   icon;
  ETypeItem slot;
  ERarity  rarity;
  int      priceCoins;
  StatType statType;      // ATK / HPMax / Speed / Armor / ...
  double   statAmount;
  StatsBehavior.StatBonusMode mode;  // Additive / PercentMultiply
}
```
Seeded by a one-off editor/generator from `Eq_*` names+sprites; prices/stats derived from rarity.
Authorable in Inspector afterwards. Start with a curated ~8–12 entries spanning slots for testing.

### 2. Runtime state — `SV_PlayerInventory` (persisted, PlayerPrefs JSON)
Plain C# singleton (lazy), no Domain dependency:
```
HashSet<string> owned;
Dictionary<ETypeItem,string> equipped;   // one item per slot
bool IsOwned(id); bool TryBuy(SV_ItemEntry); // coin check + AddCoin(-price) + owned.Add
void Equip(id); void Unequip(slot); string GetEquipped(slot);
event Action OnChanged;
Save()/Load() → PlayerPrefs key "sv_inventory_v1" (JSON).
```

### 3. Stat application — `SV_EquipmentStatApplier`
Equipment menu is on Main Menu; the player/`StatsBehavior` only exists in gameplay. So equip in
the menu is pure data; stats are applied when gameplay starts. Hook: when the player's
`StatsBehavior` initializes (`LuzartPlayerEntityRoot`), read equipped entries and
`ApplyStatBonus(statType, statAmount, mode)` for each. Minimal, contained — no modifier pipeline.

### 4. UI — replace dead wrappers (keep registered guids)
- `SV_ShopUI : UIBase` (guid stays so UIRegistry 6002 still resolves).
  - `OnCreateAsync`: `UIButtonSanitizer.SanitizeChildButtons`; **add Close button** (new GO,
    top-right) wired to `OnCloseButtonClicked`; build a runtime grid of buy-cards from the catalog
    (unowned items): icon + name + price + Buy button. Coin label reflects `CurrencyManager`.
  - Buy → `inventory.TryBuy(entry)` → refresh grid + coin label. Subscribe `OnCoinChanged`.
- `SV_EquipmentUI : UIBase`.
  - `OnCreateAsync`: sanitize; **add Close button** wired to `OnCloseButtonClicked`.
  - **Hybrid**: bind the existing equipment slots (`Firs/Second/Third`, …) to the 6 `ETypeItem`
    slots where cleanly identifiable; list owned items; tapping an owned item equips it into its
    slot (or unequips). Show equipped state. Slot→type mapping resolved by runtime inspection of
    the prefab during implementation.

### Cleanup
- Remove dead `ShopManager` missing-script (guid `bfb038d2d54ad214eb612c5e36853d62`, fields
  `Mecanique,btn0..btn5`) from `SV_Shop` root.
- Fix `SV_Equipement` root's broken `m_Script {guid:0}` → real `SV_EquipmentUI`.
- Old `SV_ShopUI`/`SV_EquipementUI` empty wrappers in `SV_LegacyWrappers.cs` are superseded
  (remove those two lines; keep the other 4 wrappers).

## Decisions (from Q&A)
- Target prefabs: **`Assets/_Main/Perfabes/UI/SV_Shop.prefab` + `SV_Equipement.prefab`** (NinjaUI).
- Depth: **full** (buy + equip + stats + close).
- Persistence: **yes**, PlayerPrefs.
- Currency: **coins** via `CurrencyManager`.
- UI fidelity: **hybrid** (existing slots for Equipment, runtime grid for Shop).

## Test plan (user-facing)
1. Open Shop from Main Menu → see item cards with prices + a Close button → Close works.
2. Buy an affordable item → coins decrease, item leaves shop / marked owned. Unaffordable → Buy disabled.
3. Open Equipment → see owned items + slots + Close button → Close works.
4. Equip an item → slot shows it; unequip clears it. Persists after restart.
5. Start gameplay with equipment on → player stat reflects the bonus (e.g. higher Max HP).
6. Console clean (no new exceptions).

## Out of scope
- Gems/secondary currency, item upgrade/level-up, chests/gacha, ads rewards.
- Reviving the dormant modifier pipeline or `ItemConfig.AssetUnlockable` graph.
