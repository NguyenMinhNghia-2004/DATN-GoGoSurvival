using Cysharp.Threading.Tasks;
using DATN.Legacy;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// One-stop shop to reset gameplay state across all three layers (framework <see cref="GameController"/>,
    /// player <see cref="PlayerCharacter"/> stats, and legacy <see cref="DATN.Legacy.UIManager"/> level/enemies/UI).
    ///
    /// Wired from SV_WinScreenUI / SV_LoseScreenUI button handlers to make Continue / Retry / MainMenu
    /// actually reset the run rather than just hiding the popup.
    /// </summary>
    public static class GameplayResetCoordinator
    {
        public static async UniTask BackToMainMenuAsync()
        {
            ResetAllLayers();
            if (UIManager.Instance != null)
            {
                await UIManager.Instance.HideAllExceptSystemAsync();
                await UIManager.Instance.ShowAsync(UIId.SV_MainMenu);
            }
        }

        public static async UniTask RetryAsync()
        {
            ResetAllLayers();
            if (UIManager.Instance != null)
                await UIManager.Instance.HideAllExceptSystemAsync();

            // Wait a frame for the legacy StartBacking coroutine (0.8s) to settle the scene state.
            // Then re-trigger the same flow MainMenu's Play button uses.
            await UniTask.Delay(900);

            var legacy = Object.FindFirstObjectByType<DATN.Legacy.UIManager>();
            if (legacy != null) legacy.PlayBtn();

            // Restart via the ClassicMode state machine (single entry door); falls back to
            // GameController directly if ClassicMode isn't wired.
            var domain2 = SceneRootManager.Instance?.Domain;
            var classicMode = domain2?.Get<ClassicModeController>();
            if (classicMode != null) classicMode.StartGame();
            else domain2?.Get<GameController>()?.StartGameplay();

            if (UIManager.Instance != null)
                await UIManager.Instance.ShowAsync(UIId.SV_GameplayHud);
        }

        private static void ResetAllLayers()
        {
            // 1) Framework — stop wave timer, reset counters, and return the mode to Idle so the
            //    next StartGame begins fresh.
            var domain = SceneRootManager.Instance?.Domain;
            domain?.Get<GameController>()?.ResetState();
            domain?.Get<ClassicModeController>()?.ResetToIdle();

            // 2) Player stats — restore HP, clear XP/level (Stats.AddXP accumulates).
            var player = domain?.Get<PlayerCharacter>();
            if (player?.Stats != null)
            {
                player.Stats.GetRuntime(StatType.Runtime_HP).Set(100);
                player.Stats.GetRuntime(StatType.Runtime_XP).Set(0);
            }

            // 3) Legacy — destroy spawned level, hide gameplay screens, reset coins/kills/timer.
            var legacy = Object.FindFirstObjectByType<DATN.Legacy.UIManager>();
            if (legacy != null) legacy.BackFinishSafe();
        }
    }
}
