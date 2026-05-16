---
title: Scene Boot Flow
category: technical
tags: [boot, scene, lifecycle]
sources: [raw/technical/overnight-summary.md, raw/technical/ninja-ui-migration-guide.md]
created: 2026-05-16
updated: 2026-05-16
---

# Scene Boot Flow

How `GamePlay.unity` initializes itself on Play. Two anchor GameObjects drive the order: `_GameBoot` (entities + DI) and `UIBootstrap` (UI flow).

## Scene hierarchy (post-migration)

```
GamePlay
├── _NinjaUI (Canvas, sortOrder=100)        ← new UI root
│   ├── 0_WorldOverlay
│   ├── 1_Screen
│   ├── 2_Hud
│   ├── 3_Popup
│   ├── 4_System
│   └── 5_Toast
├── _GameBoot                                ← new entity boot
│   ├── SceneRootManager
│   └── EntityManager
├── UIBootstrap                              ← drives Splash → MainMenu
├── UI (legacy)                              ← DATN.Legacy.UIManager (to be removed)
├── GameManager, AudioManager, …             ← DATN unchanged
└── EventSystem
```

## Frame 0 — `_GameBoot.SceneRootManager.Awake()`

1. `new Domain()` — a typed registry the framework uses for cross-system lookup.
2. `FindObjectsOfType<AbstractMonoBehaviorContent>()` — discovers every component implementing `IContent`, currently:
   - `DATNPlayerEntityAdapter` on the Player GameObject ([[technical/entity-adapter-pattern]])
   - `EntityManager` (sibling on `_GameBoot`)
3. For each, call `Inject(domain)` → adapter creates its `DATNPlayerCharacter` and registers it via `domain.Add<PlayerCharacter>(character)`.
4. Then `Initialize()` → character's `TransformBehavior` + `StatsBehavior` are created.

## Frame 0 — `UIBootstrap.Start()`

1. Wait 1 frame.
2. `await UIManager.Instance.ShowAsync(UIId.Splash)`.
3. `SV_SplashUI` instantiates under `_NinjaUI/1_Screen`, runs its fill-bar coroutine.
4. When fill = 100% → splash signals close → UIManager hides it.
5. After `minSplashDuration`, `await ShowAsync(UIId.SV_MainMenu)`.

`UIBootstrap` is in `Assets/_Main/Scripts/UI/NinjaUIScreens/UIBootstrap.cs`.

## Every Update

```
DATNPlayerEntityAdapter.DoUpdate:
  character.Transform.SetPosition(transform.position)   // Unity → framework sync
  character.OnUpdate(dt)                                // tick behaviors

EnemyManager (DATN legacy):
  for each Zombie spawned via Instantiate(...)
    DATNEnemyEntityAdapter.Awake registers w/ EntityManager
    DoUpdate syncs transform, OnDestroy unregisters
```

## Why this layering?

DATN's legacy code already drives gameplay (input, animation, AI, attack timing). The boot flow adds a **shadow registry** alongside, so framework code can look up entities without owning them. The split keeps gameplay working while skill-side framework code becomes usable.

See [[decisions/adapter-bridge-vs-rewrite]] for why the bridge over rewrite.

---
## Backlinks
- [[overview]]
- [[technical/entity-adapter-pattern]]
- [[systems/ui-flow]]
- [[entities/player-character]]
