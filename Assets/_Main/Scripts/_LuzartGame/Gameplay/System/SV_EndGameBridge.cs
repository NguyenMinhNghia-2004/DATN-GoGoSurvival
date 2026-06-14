using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Listens for the gameplay end-game broadcast and shows the appropriate
    /// NinjaUI screen (<see cref="SV_LoseScreenUI"/> or <see cref="SV_WinScreenUI"/>).
    ///
    /// Survival time / kills come from <see cref="GameController"/>; coin total
    /// comes from <see cref="CurrencyManager"/> (framework currency owner).
    /// </summary>
    public class SV_EndGameBridge : MonoBehaviour
    {
        private GameController _gameController;
        private bool _registered;

        private void Start()
        {
            if (SceneRootManager.Instance != null && SceneRootManager.Instance.Domain != null)
                _gameController = SceneRootManager.Instance.Domain.Get<GameController>();

            Broadcaster.Register<Data_ClassicEndGame>(OnEndGame);
            _registered = true;
        }

        private void OnDestroy()
        {
            if (_registered) Broadcaster.Unregister<Data_ClassicEndGame>(OnEndGame);
        }

        private void OnEndGame(Data_ClassicEndGame data)
        {
            ShowEndGameUI(data.IsWin).Forget();
        }

        private async UniTaskVoid ShowEndGameUI(bool isWin)
        {
            if (UIManager.Instance == null) return;

            // Hide the gameplay HUD before showing the result screen. The HUD lives on the
            // persistent "Hud" lane (KeepLoaded); NinjaUI does NOT auto-hide one lane when a
            // Screen-lane UI (Win/Lose) is shown, so without this the HUD — and its joystick —
            // kept rendering on top of the end screen. SV_GameplayHudUI.OnHiddenAsync disables
            // the joystick on hide; GameplayResetCoordinator re-shows the HUD on Retry.
            await UIManager.Instance.HideAsync(UIId.SV_GameplayHud);

            int kills = _gameController != null ? (int)_gameController.CountEnemyDead.Value : 0;
            float survival = _gameController != null ? _gameController.CountTime.Value : 0f;
            int coins = CurrencyManager.Instance != null ? (int)CurrencyManager.Instance.Coins : 0;

            // Economy bridge: bank THIS run's coins into the shop gold (io_gold), then zero the
            // in-run counter. Playing thus earns spendable shop currency, and the menu/pause/end
            // screens (which display io_gold) reflect the new total.
            if (coins > 0)
            {
                var pool = SceneRootManager.Instance?.Domain?.Get<ResourcePool>("io_gold");
                if (pool != null)
                {
                    var _ = ((IResourcePool)pool).Value; // ensure inner Number is initialized
                    pool.Add(coins);
                }
                CurrencyManager.Instance.AddCoin(-coins); // zero the in-run counter
            }

            // Push updated meta-progression (gold/unlocks/levels/equipment) to the backend at run end.
            SceneRootManager.Instance?.Domain?.Get<CloudSyncService>()?.PushNow().Forget();

            if (isWin)
            {
                var data = new SV_WinData
                {
                    FinalScore = kills * 10,
                    EnemiesKilled = kills,
                    SurvivalTime = survival,
                    CoinsEarned = coins,
                    XpEarned = 0,
                };
                await UIManager.Instance.ShowAsync(UIId.SV_WinScreen, new UIContext(data));
            }
            else
            {
                var data = new SV_LoseData
                {
                    EnemiesKilled = kills,
                    SurvivalTime = survival,
                    CoinsEarned = coins,
                };
                await UIManager.Instance.ShowAsync(UIId.SV_LoseScreen, new UIContext(data));
            }
        }
    }
}
