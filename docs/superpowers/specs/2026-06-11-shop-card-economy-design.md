# Shop Card Economy — Win Rewards + Buy Shard with Coin

Date: 2026-06-11
Status: Approved (design), ready for implementation.

## Goal

Change how players acquire equipment cards in the IOPort shop:

1. **Win a Classic run → random reward** (silent grant, no popup): configurable
   `cardChancePercent` (default 15%) to unlock a random still-locked card; otherwise
   grant random shards (random rarity, amount `[shardMin..shardMax]`, default 3–8).
2. **Buy shard with coin (Gold) in shop**: per shard rarity, a card "Buy N shards for G gold".
3. **Keep** the existing card unlock (Gold + Shard) and its require/current cost rows.

Also includes a small bug fix already applied: the equipment detail popup now closes
after Equip/Unequip (`PopupItemEquipView.ClosePopup()` via `GetComponentInParent<Popup>().HideSelf()`).

## Existing infrastructure (reused, not rebuilt)

- Currency: `ResourcePool_Gold` (the shop "coin") + 3 shard pools `ResourcePool_Shard_{Rare,Epic,Legend}`
  (`ERarity { Rare=0, Epic=1, Legend=2 }`). `ResourcePool.Add(double)` / `TryRemove(double)` / `Value`.
- Locked cards: `InventoryItemData.GetAllItemConfigDontBuy()` → items where `Unlockable.IsUnlocked==false`.
  Unlock = `card.Unlockable.IsUnlocked.Set(true)`.
- Win signal: `ClassicModeController.EndGame` broadcasts `Data_ClassicEndGame { IsWin }`.
  Subscribe via static `Broadcaster.Register<Data_ClassicEndGame>` / `Unregister` (mirror `SV_EndGameBridge`).
- Domain registration: SO contents are added to `DomainContentLoader.contents`
  (Inject → Initialize → Start lifecycle). `IOPortDataGenerator` already authors SO assets and
  registers them into that list via SerializedProperty.
- Shop popup `PopupShop`/`PopupShopView` (data `Data_Shop`), card grid `ShopPopupUnlockedView`,
  per-card `ItemShopUnlockView`, built/wired by `IOPortPrefabGenerator`.

## Components

### 1. `Data_WinReward` (new ScriptableObject content)
`Assets/_Main/Scripts/_IOPort/Shop/Data_WinReward.cs` : `AbstractScriptableContent`.

Fields (Inspector-configurable):
- `float cardChancePercent = 15f`
- `int shardMin = 3`, `int shardMax = 8`
- `List<ResourcePool> shardPools` (Rare/Epic/Legend)
- `InventoryItemData inventoryItemData` (ref to resolve locked cards)

Behaviour:
- `DoInitialize`: `Broadcaster.Register<Data_ClassicEndGame>(OnEndGame)`.
- `DoTerminate`: `Broadcaster.Unregister<Data_ClassicEndGame>(OnEndGame)`.
- `OnEndGame(data)`: `if (!data.IsWin) return;`
  - `bool wantCard = Random.Range(0f,100f) < cardChancePercent;`
  - locked = `inventoryItemData.GetAllItemConfigDontBuy()`.
  - if `wantCard && locked.Count>0`: pick random locked card → `IsUnlocked.Set(true)`.
  - else: pick a random pool from `shardPools` → `pool.Add(Random.Range(shardMin, shardMax+1))`.
- Silent (no UI). Card simply appears unlocked in shop on next open. Defensive null guards.

### 2. Buy shard with coin (shop)
- `ShopShardOffer` (`Assets/_Main/Scripts/_IOPort/Shop/ShopShardOffer.cs`, `[Serializable]`):
  `{ ResourcePool shardPool; ResourcePool goldPool; int price; int amount; ERarity rarity; }`.
- `Data_Shop` gains `List<ShopShardOffer> shardOffers` + `IReadOnlyList<ShopShardOffer> ShardOffers`.
- `PopupShopView` exposes `ShardOffers`.
- `ShopBuyShardView` (`Assets/_Main/Scripts/_IOPort/Shop/ShopBuyShardView.cs`, `ViewT<ShopShardOffer>`):
  - Shows rarity frame (via `RaritySpriteResolver`), current shard count (`shardPool.Value`, live via `.Changed`),
    price text, Buy button.
  - `OnClickBuy()`: `if (goldPool.Value >= price) { goldPool.TryRemove(price); shardPool.Add(amount); }`.
  - Updates count text on shard `.Changed`.

### 3. Wiring (generators)
- `IOPortDataGenerator`: new menu action authors a `Data_WinReward.asset`
  (`Data/IOShop/System/`), assigns shardPools + inventoryItemData + defaults; authors 3
  `ShopShardOffer` entries into `Data_Shop` (Rare/Epic/Legend, default price/amount). Registers
  `Data_WinReward` into `DomainContentLoader.contents`. Idempotent.
- `IOPortPrefabGenerator`: build `ShopShardCard.prefab` (rarity frame + count + price + Buy button,
  wire `ShopBuyShardView`); add a "Shards" section to the shop popup hosting a row of 3 shard cards
  (one per offer), spawned/bound by `PopupShopView`.

## Files

New: `Data_WinReward.cs`, `ShopShardOffer.cs`, `ShopBuyShardView.cs`.
Modified: `Data_Shop.cs`, `PopupShopView.cs`, `IOPortDataGenerator.cs`, `IOPortPrefabGenerator.cs`,
scene `DomainContentLoader` (via generator), `PopupItemEquipView.cs` (equip-close, already done).

## Out of scope (YAGNI)
- Daily purchase limit (user dropped it).
- Reward popup / win-screen reward line (user chose silent grant).
- Bridging CurrencyManager gameplay coins → Gold (shop uses Gold pool directly).
- Weighted rarity tables / pity timers (uniform random; tune later via config).

## Verification
- Build clean (0 compile errors / 0 new console exceptions).
- Equip/Unequip closes the detail popup.
- Simulate win → a locked card unlocks OR a shard pool increments (config-driven).
- Shop shard card: Buy spends Gold, increments shard pool, count text updates; blocked when Gold < price.
