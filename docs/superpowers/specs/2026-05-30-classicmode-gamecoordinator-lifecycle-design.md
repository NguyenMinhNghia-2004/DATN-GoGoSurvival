# Design — Classic Mode lifecycle: ClassicModeController + GameCoordinator

**Date:** 2026-05-30 · **Status:** approved (brainstorm) → implementing

## Problem

The end-game flow leaks. On Win/Lose, `GameController` only **broadcasts** `Data_ClassicEndGame` — it never stops gameplay:
- `OnWinGame()`/`OnLoseGame()` (`GameController.cs:135-148`) just broadcast.
- The timer `IECountPerSecond` is `while(true)` and keeps ticking → `OnWinGame()` **re-fires every second** (broadcast spam).
- Nothing pauses/stops the simulation → the spawner keeps spawning and enemies keep chasing **under** the Win/Lose screen.
- Gameplay only actually stops when the player clicks a Win/Lose button (`GameplayResetCoordinator.ResetAllLayers → GameController.ResetState → StopGameplay`).

Result (reported): "win xong vẫn chơi tiếp, quái vẫn đi theo người."

Root issue: there is no game-mode **state machine** and no **single end funnel**. `GameController` conflates state + win/lose detection + timer/wave spawning. `GameplayResetCoordinator` is a static reset helper, not a lifecycle coordinator.

## Target architecture

Two new `AbstractMonoBehaviorContent` on `_GameBoot` (registered in Domain):

### `ClassicModeController` — the state machine (single door in/out)
- `enum ClassicModeState { Idle, Playing, Ended }` (Pause = a flag within `Playing`, not a state).
- `enum EndReason { PlayerDied, WavesCleared, QuitFromPause }`.
- `StartGame()` — Idle→Playing; calls `GameCoordinator.BeginRun()`.
- `Pause()/Resume()` — `Time.timeScale` 0/1; state stays `Playing`.
- **`EndGame(EndReason)` — the ONLY exit.** Guard (`Ended` → no-op, kills the re-fire). Sets `Ended`, calls `GameCoordinator.EndRun()`, then broadcasts `Data_ClassicEndGame{ IsWin = reason==WavesCleared }`.
- `ResetToIdle()` — back to Idle for replay (called by reset flow).

### `GameCoordinator` — lifecycle of gameplay components
- `BeginRun()` → `foreach IRunParticipant.OnRunBegin()`.
- `EndRun()` → `foreach IRunParticipant.OnRunEnd()` (stop spawner, **despawn all enemies**, freeze player, stop timer).
- `ResetRun()` → reset counters/HP/XP/level (folds in `GameplayResetCoordinator.ResetAllLayers`).
- `BuildResult(reason)` → kills/time/coins for the end screen.

### `IRunParticipant { void OnRunBegin(); void OnRunEnd(); }`
Implemented by the spawner, player hooks, wave runner, etc. Coordinator just iterates — new participants (drops, vfx, boss) plug in without touching the coordinator. Matches "enemy/player cũng có lifecycle."

## Responsibility mapping (old → new)

| Current | Moves to |
|---|---|
| `GameController.StartGameplay/StopGameplay` + `_gameplayActive` | ClassicMode (state + start/end) |
| `GameController.OnWinGame/OnLoseGame` (broadcast-only) | call `ClassicMode.EndGame(reason)` |
| `GameController` timer/wave/win-detect (`IECountPerSecond`, `OnTimerTick`, `SpawnNewWave`) | `WaveRunner : IRunParticipant` (S4) — on cleared → `ClassicMode.EndGame(WavesCleared)` |
| `GameController` counters (`IndexWave/CountTime/CurrentLevel/CountEnemyDead`) + XP→level | **stays** in GameController (readers: HUD, UpgradeSkill, EndGameBridge) |
| `GameplayResetCoordinator.ResetAllLayers/Retry/BackToMainMenu` | `GameCoordinator.ResetRun()/EndRun()` |
| `EnemySpawnerManager` | `IRunParticipant`: `OnRunEnd` = stop spawn coroutines + destroy all enemies (tag `Enemy`) |
| Player (`LuzartPlayerEntityRoot`) | `IRunParticipant`: `OnRunEnd` = freeze input/HP |

`SV_EndGameBridge` is unchanged — it still listens to `Data_ClassicEndGame` and shows `SV_WinScreen`/`SV_LoseScreen`.

## Single-door flow

```
SV_MainMenu.Play ─► ClassicMode.StartGame() ─► Coordinator.BeginRun() ─► participants.OnRunBegin
   (Playing)                                     (spawner on, timer, player alive, HUD)

   ┌── HP≤0 ─────────────► ClassicMode.EndGame(PlayerDied)  ─┐
   ├── waves cleared ────► ClassicMode.EndGame(WavesCleared) ─┤ (guard: once)
   └── quit from Pause ──► ClassicMode.EndGame(QuitFromPause)─┘
                                   │ state=Ended
                                   ▼
              Coordinator.EndRun()  (spawner off, DESTROY all enemies, timer off, input off)
                                   ▼
              Broadcast Data_ClassicEndGame{IsWin} ─► SV_EndGameBridge ─► Win/Lose screen

   [Continue]/[Retry] ─► Coordinator.ResetRun() ─► ClassicMode.ResetToIdle()
                          Retry: ResetRun → StartGame again
```

## Slice plan (each = 1 commit, game playable, compile+boot verified; feel hand-tested)

- **S1 — single end funnel + stop on end (fixes the spam + "win xong vẫn chơi tiếp").**
  Add `ClassicModeController` + `EndReason`. Route the 3 end sources into `EndGame(reason)`:
  `GameController.OnWinGame`→`WavesCleared`, HP≤0→`PlayerDied`, Pause "quit"→`QuitFromPause`.
  `EndGame` guards once + calls `GameController.StopGameplay()` then broadcasts. `StartGame` wraps
  `GameController.StartGameplay()`; route `SV_MainMenu.Play` + `Retry` through it.
- **S2 — `GameCoordinator` + `IRunParticipant`.** `StartGame→BeginRun`, `EndGame→EndRun` (before
  broadcast). `EnemySpawnerManager` + player become participants; `EnemySpawner.OnRunEnd` **destroys
  all enemies** → fixes "quái vẫn đuổi sau khi end".
- **S3 — fold reset into Coordinator.** `GameplayResetCoordinator.ResetAllLayers` → `Coordinator.ResetRun`;
  Continue/Retry → ClassicMode + Coordinator; Retry = EndRun→ResetRun→StartGame; ClassicMode.ResetToIdle.
- **S4 — extract `WaveRunner`** (timer+wave+win-detect) out of GameController into a participant; GameController
  keeps only counters/progression. (cleanup, last.)

After S2 the reported bug is fully gone; S3/S4 finish the clean architecture.

## Risks / notes
- Combat is migration-fragile (dual enemy mover: legacy `EnemyManager` + `LuzartEnemyEntityRoot` both enabled). `EnemySpawner.OnRunEnd` must destroy enemies regardless of which mover owns them (destroy by tag `Enemy` / spawn registry).
- Pause "quit" button lives on `SV_PausePopup` — wire it to `ClassicMode.EndGame(QuitFromPause)`.
- Machine can verify compile + boot only; win/lose/quit/retry feel must be hand-tested.
