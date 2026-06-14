# Cloud Data Sync — Centralized, Backend-Pluggable Player Data

Date: 2026-06-14
Status: Implemented + play-verified (LocalFile provider, full round-trip)

## Goal (user)
Organize player data **centrally** so it can be synced to a backend. Be able to use **my own server OR a third party (Firebase, etc.)** by **only swapping the SDK** — the rest of the game unchanged. Push data to the server; pull it when entering the game.

## Architecture

Three layers, each with one responsibility:

1. **Central data (`PlayerDataSerializer` + `CloudSaveBlob`)** — aggregates EVERY `ISaveable` registered in the Domain (currencies `io_gold` + per-item card pools, item unlocks `AssetUnlockable`, item levels `AssetLevel`, equipped items `AssetEquipmentSlot`) into ONE JSON blob, and applies a blob back by matching `IContent.Id`. Backend-agnostic, `JsonUtility`-friendly (no Json.NET in project). This is "tổ chức data tập trung".

2. **Backend seam (`ICloudSaveProvider`)** — the ONLY thing that changes per backend:
   - `UniTask<bool> SaveAsync(playerId, json)`
   - `UniTask<string> LoadAsync(playerId)` (null = no data)
   - Implementations: `LocalFileCloudProvider` (default, persistentDataPath file — works with no SDK, testable), `RestCloudProvider` (own server: `GET/POST {base}/save/{playerId}`). Firebase/PlayFab/UGS = add one class implementing the interface and select it. Nothing else changes.

3. **Orchestration (`CloudSyncService` : AbstractMonoBehaviorContent)** — auto-discovered scene content (`_CloudSync` GO). On `DoStart` (game enter) PULL+apply; on run end / `OnApplicationPause(true)` / `OnApplicationQuit` / `DoStop` PUSH. Picks the provider via a serialized `ProviderKind` enum (+ REST base URL). Player id = a GUID stored in PlayerPrefs (`cloud_player_id`).

## Data flow
- Enter game → `CloudSyncService.DoStart` → `provider.LoadAsync(playerId)` → `PlayerDataSerializer.ApplyToDomain` (matches by content Id → `ISaveable.Load`).
- Run end → `SV_EndGameBridge` banks coins→io_gold then `CloudSyncService.PushNow()` → `PlayerDataSerializer.SerializeDomain` → `provider.SaveAsync`.
- Also push on app background/quit/stop.

## Ordering (critical)
Apply runs in `DoStart` (Start phase) — AFTER all saveable SO content is registered (DomainContentLoader, exec order -900) and AFTER `ItemConfigsOwned.DoInitialize` random-seeds unlocks (Awake), so synced state authoritatively overrides defaults. Equipment restore resolves `ItemConfig` by id from the Domain (already registered). io_gold seed in `IOPortBootstrap` only fires when ≤0, so a restored balance wins.

## Bug fixed (required for sync correctness)
`AssetUnlockable.DoLoad` used `item.Equals(IS_UNLOCKED)` (SaveItem struct vs string → never matched) → unlock state never restored. Fixed to `item.key == IS_UNLOCKED` and SET the existing IBool (preserve ref so subscribed shop/inventory views refresh).

## Wire format (CloudSaveBlob JSON)
`{ version, contents:[ { id, fields:[ { key, type, val } ] } ] }` where `type` = ValueSaveType (Int0/Float1/String2/Bool3/Double4), `val` = invariant-culture string. Same shape regardless of backend.

## Verification
- Compile clean (0 CS). Boot clean (0 NRE).
- First run: pull → "no cloud data". Stop → push → wrote `cloudsave_<id>.json` (contains io_slot_0..5, item levels, unlocks, currencies).
- Second run: pull → "applied 397 content(s)" — full round-trip confirmed.

## How to switch backend
- Own server: set `CloudSyncService.ProviderKind = Rest` + base URL; implement the GET/POST contract.
- Firebase/PlayFab/UGS: add `FirebaseCloudProvider : ICloudSaveProvider`, add a case in `CloudSyncService.CreateProvider`, select it. No other change.

## Files
`Assets/_Main/Scripts/_LuzartGame/CloudSync/`: ICloudSaveProvider, CloudSaveBlob, PlayerDataSerializer, LocalFileCloudProvider, RestCloudProvider, CloudSyncService. + `AssetUnlockable.cs` (load bug fix), `SV_EndGameBridge.cs` (push hook), `GamePlay.unity` (`_CloudSync` GO).

## Known limitations / follow-ups
- Push on hard quit is best-effort (async may not flush); run-end + background pushes cover the main cases.
- No conflict resolution / merge (last-write-wins) — fine for single-device; add versioning/timestamps for multi-device.
- No auth (player id = device-local GUID); a real backend needs account auth.
- `ItemConfigsOwned` still randomizes unlocks on first run before the (empty) pull; that random set is then pushed and becomes the player's save. Consider disabling random seeding if undesired.
