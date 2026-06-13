# Per-Item Card Economy — Replace Rarity Shards with Per-Item Cards

Date: 2026-06-11
Status: Approved (design), ready for implementation plan.
Supersedes: `2026-06-11-shop-card-economy-design.md` (rarity-shard model).

## Goal

Remove the shared "shard-per-rarity" abstraction. The unit of progression becomes a
**per-item card**: each equipment item has its OWN card. Players collect copies of an
item's card, then spend them to **unlock** and **upgrade** that specific item
(survivor.io / Archero model). "Shard = the item's own card."

User decisions (locked):
- D1. Each item card unlocks/upgrades **that same item** (collect N copies of item X's card).
- D2. Win a match → receive cards of a **random item, weighted by rarity**.
- D3. Shop sells cards of **specific items** from a **fixed, hand-configured list**, paid in **Gold**.
- D4. Unlock and upgrade both consume the item's own cards (amounts author-configured). Gold cost kept (configurable; can be zeroed).
- D5. Architecture: one `ResourcePool` asset per item (Approach 1) — reuse existing cost/save/UI rails.
- D6. **Shop layout must NOT change.** Only displayed content may change: rarity gem → real item icon, and add a Gold icon next to the price. The Item/Inventory screen visuals ARE updated to show per-item card progress.

## Why Approach 1 fits

Every progression path (unlock cost, level-up cost, shop purchase, win reward) already
speaks `ResourcePool`. The cost system is a generic list of `{ resourcePool, amount }`.
`ResourcePool : AbstractScriptableContentSaveable` auto-persists its amount by `_id`.
So giving each item its own card pool and re-pointing its costs at that pool makes all
existing UI/save/purchase logic work with near-zero runtime rewrites. The bulk work is a
one-time editor conversion tool (mirrors existing `FindItemEditor` / `IOPortDataGenerator`
authoring patterns).

## Existing infrastructure (reused, not rebuilt)

- `ItemConfig` (130 assets under `Assets/_Main/Data/Items/{Good,Better,Best,...}`): has
  `_sprite`, `_eRarity` (`ERarity { Rare=0, Epic=1, Legend=2 }`), embedded `AssetUnlockable unlockable`
  (`unlockCosts`) and `AssetLevel assetLevel` (`levelUpCosts`, 20 tiers).
- Cost entry = `SerializableCostCreator_CommonlyUsed { mode, resourcePool, resourceAmount, assetCost }`.
- `ResourcePool`: `Add(double)`, `TryRemove(double)`, `Value` (INumber, `.Changed`), `Definition.GetMainImage()`.
  Save/load by `_id` via `DoSave/DoLoad`.
- `ResourceDefinition`: `_mainImage`, `_displayName`, `_eType` (1=Shard), `GetMainImage()`.
- Gold pool: `ResourcePool_Gold` (guid `0c7143df3f325a34d91ccca22b5687de`).
- Current generic shard pools to retire: `ResourcePool_Shard_{Rare,Epic,Legend}`
  (guids `bb103…`, `1cb2e…`, `9652a…`) + their definitions (`_mainImage` currently null).
- Shop: `Data_Shop` (`shardOffers`), `PopupShopView`, `ShopBuyShardView`, prefab
  `ShopShardCard.prefab` (children: `Frame`, `Count`, `BuyButton/Price`). Built/wired by
  `IOPortPrefabGenerator`; data authored by `IOPortDataGenerator`.
- Win: `Data_WinReward` listens `Broadcaster.Register<Data_ClassicEndGame>`; currently rolls
  instant-unlock or rarity-shard grant.
- `RaritySpriteResolver` (rarity frame sprites), `DomainContentLoader.contents` (SO lifecycle list).

## Components

### 1. Per-item card assets (new, generated)
For each `ItemConfig`, generate (idempotent) under `Assets/_Main/Data/IOShop/Resources/Cards/`:
- `ResourceDefinition_Card_<ItemId>`: `_mainImage = item._sprite`, `_displayName = item._name`,
  `_eType = Shard`, `_id = card_<itemId>_def`.
- `ResourcePool_Card_<ItemId>`: `_resourceDefinition` → the above, `_id = card_<itemId>`.

The item's rarity is read from the `ItemConfig` (not stored on the card) for frame color + drop weight.

**Runtime item→pool link.** `ItemConfig` gains a serialized field `[SerializeField] ResourcePool _cardPool;`
with `public ResourcePool CardPool => _cardPool;`, populated by the converter. This is the single
runtime source of truth used by the shop offer, win reward, and (indirectly) the cost rows. It keeps
shop/reward decoupled from asset-name lookups (which only exist in editor). The card pool is also
registered into `DomainContentLoader.contents` so it participates in the save lifecycle.

### 2. Editor conversion tool (new) — `Tools/IOShop/Convert To Per-Item Cards`
Implemented in the `_IOPort` editor generators (extend `IOPortDataGenerator` or a sibling
`PerItemCardConverter`). For every `ItemConfig`:
1. Ensure its card Definition+Pool exist (create if missing).
2. **Unlock cost** (`unlockable.unlockCosts`): replace the entry whose `resourcePool` is a
   retired shard-rarity pool with one pointing at the item's card pool (amount preserved or
   author default by rarity). Leave the Gold entry intact. If no shard entry exists, add a card entry.
3. **Level-up cost** (`assetLevel.levelUpCosts`, all tiers): same replacement per tier; keep Gold.
4. Mark assets dirty, save. Idempotent (re-running detects already-card pools and skips).
5. Log per item; summary count at end.

A second action `Tools/IOShop/Verify No Shard Refs` greps remaining references to the 3 shard
pool guids; when zero, the shard pools/definitions may be deleted in a final cleanup step.
`ERarity` enum is retained.

### 3. Shop — per-item card offers (layout unchanged)
- `ShopShardOffer` → repurposed to reference a specific item:
  `{ ItemConfig item; ResourcePool goldPool; int price; int amount; }`
  (`amount` = cards granted per purchase). The item's card pool is resolved at runtime via
  `item.CardPool`, rarity from `item.Rarity`.
- `Data_Shop.shardOffers` → a fixed, author-configured `List<ShopShardOffer>` (any items, any count).
- `PopupShopView` continues to expose the offers list.
- `ShopBuyShardView` (binds to existing `ShopShardCard` prefab — NO layout change):
  - `Frame`: keep as rarity-colored backing (`RaritySpriteResolver.GetSpriteByRarity(item.Rarity)`).
  - **Add child `Icon` (Image)** centered on the card showing `item.Sprite` → the "card image".
  - **Add child `GoldIcon` (Image)** next to `Price` showing the Gold sprite → price reads as Gold.
  - `Count` = `x{cards owned}` of the item's card pool (live via `.Changed`).
  - `Price` = `{price}`.
  - `OnClickBuy()`: `if (gold >= price) { goldPool.TryRemove(price); itemCardPool.Add(amount); }`.
- Prefab edits (adding `Icon` + `GoldIcon`) are additive content elements applied via
  `IOPortPrefabGenerator` (idempotent), not a layout redesign.

### 4. Win reward — random item card by rarity weight
`Data_WinReward`:
- Replace `shardPools` + `shardMin/Max` + instant-unlock with:
  `int cardMin, cardMax;` plus rarity weights `float weightRare, weightEpic, weightLegend;`.
- On win: pick a random `ItemConfig` (from inventory pool) using rarity weight; add
  `Random(cardMin..cardMax)` cards to that item's card pool. Silent grant. Defensive null guards.
- Drop the old `TryGrantRandomCard` (instant unlock) and `GrantRandomShards` paths.

### 5. Item / Inventory screen (visual update)
- Each item view shows the item icon + `cards owned / required` sourced from the item's own card
  pool (correct automatically once costs are rewired to that pool).
- Unlock button enabled when enough cards; after unlock, the existing Upgrade flow consumes cards per
  level tier. Verify `ItemShopUnlockView` and the inventory item view reflect the per-item pool;
  adjust the item view prefab/bindings as needed (detailed in the plan).

## Files

New:
- `PerItemCardConverter` (editor) — or new menu methods on `IOPortDataGenerator`.
- Generated assets: `ResourceDefinition_Card_<Item>` + `ResourcePool_Card_<Item>` (×130) in `Data/IOShop/Resources/Cards/`.

Modified:
- `ShopShardOffer.cs` (item reference instead of shardPool+rarity).
- `ShopBuyShardView.cs` (item icon + gold icon + buy into item card pool).
- `Data_Shop.cs` / `PopupShopView.cs` (offer list typing unchanged structurally).
- `Data_WinReward.cs` (weighted random item card grant).
- `IOPortDataGenerator.cs` (author per-item offers + Data_WinReward fields).
- `IOPortPrefabGenerator.cs` (add `Icon` + `GoldIcon` to ShopShardCard).
- `ItemConfig.cs` (add `_cardPool` serialized field + `CardPool` accessor).
- 130 `ItemConfig` assets (unlock + levelUp cost pool refs swapped, `_cardPool` set) — via converter tool.
- `DomainContentLoader.contents` (register the 130 card pools for save lifecycle) — via converter.
- Item/Inventory view prefab + binding as needed.

Cleanup (final): delete `ResourcePool/Definition_Shard_{Rare,Epic,Legend}` after verify-zero-refs.

## Migration / save

In-dev: existing saved shard balances become orphaned (acceptable, user approved). New per-item
card pools start at 0. No migration script.

## Out of scope (YAGNI)

- Daily shop refresh / rotation (fixed list chosen).
- Reward popup / win-screen reward line (silent grant kept).
- New currency (Gold reused).
- Crafting (unlock with a different item's cards) — rejected in favor of same-item.
- Pity timers.

## Verification

- Build clean (0 compile errors / 0 new console exceptions).
- Converter: after run, a sampled item's unlock + every levelUp tier references its own card pool
  (not a shard pool); Gold entries intact; re-run is a no-op.
- `Verify No Shard Refs` reports zero references before shard-pool deletion.
- Shop card shows item icon + Gold icon; Buy spends Gold and increments the item's card pool;
  blocked when Gold < price; count text updates live.
- Win simulation: a random item's card pool increments by `cardMin..cardMax`, distribution skews by
  rarity weight (seeded test).
- Item screen: cards owned/required reflect the item's own pool; Unlock enables at threshold; Upgrade
  consumes cards per tier.
- EditMode pure-C# tests: pool Add/TryRemove, shop buy (enough/insufficient gold), weighted roll
  (seeded `IRandomSource`), cost-rewire correctness (mock item → pool ref).
