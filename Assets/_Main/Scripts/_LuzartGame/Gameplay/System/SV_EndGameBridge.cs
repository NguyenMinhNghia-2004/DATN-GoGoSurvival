using Cysharp.Threading.Tasks;
using DATN.Legacy;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Listens for the gameplay end-game broadcast and shows the appropriate
    /// NinjaUI screen (<see cref="SV_LoseScreenUI"/> or <see cref="SV_WinScreenUI"/>).
    /// Replaces the legacy <c>DATN.Legacy.UIManager.FinishScreen</c> flow.
    ///
    /// Survival time / kills / coins are pulled from <see cref="GameController"/> +
    /// the legacy <see cref="GameManager"/> (for coins) at the moment of broadcast.
    /// </summary>
    public class SV_EndGameBridge : MonoBehaviour
    {
        private GameController _gameController;
        private GameManager _legacyGameManager;
        private bool _registered;

        private void Start()
        {
            if (SceneRootManager.Instance != null && SceneRootManager.Instance.Domain != null)
                _gameController = SceneRootManager.Instance.Domain.Get<GameController>();
            _legacyGameManager = FindFirstObjectByType<GameManager>();

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

            int kills = _gameController != null ? (int)_gameController.CountEnemyDead.Value : 0;
            float survival = _gameController != null ? _gameController.CountTime.Value : 0f;
            int coins = _legacyGameManager != null ? _legacyGameManager.CurrentCurrency : 0;

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
