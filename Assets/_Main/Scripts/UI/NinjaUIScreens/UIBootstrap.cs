using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using UnityEngine;

/// <summary>
/// Entry point: shows initial UI flow when the scene starts.
/// Attach to a GameObject in the scene (e.g. UIBootstrap empty GameObject).
///
/// Default flow: Splash → MainMenu.
/// Override <see cref="StartFlow"/> for a different boot sequence.
/// </summary>
public class UIBootstrap : MonoBehaviour
{
    [Header("Initial flow")]
    [SerializeField] private bool showSplash = true;
    [SerializeField] private bool showMainMenu = true;
    [SerializeField] private float minSplashDuration = 1.5f;

    [Header("UI IDs (override if your registry uses different ids)")]
    [SerializeField] private UIId splashId = UIId.Splash;
    [SerializeField] private UIId mainMenuId = UIId.SV_MainMenu;

    private async void Start()
    {
        // Wait 1 frame so UIManager.Awake runs first if both in same scene.
        await UniTask.NextFrame();
        try { await StartFlow(this.GetCancellationTokenOnDestroy()); }
        catch (System.Exception e) { Debug.LogError("[UIBootstrap] StartFlow threw: " + e); }
    }

    protected virtual async UniTask StartFlow(CancellationToken ct)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[UIBootstrap] UIManager.Instance is null — make sure UIManager GameObject exists in scene.");
            return;
        }

        if (showSplash)
        {
            var splashHandle = await UIManager.Instance.ShowAsync(splashId, ct: ct);
            // Splash auto-closes when fill bar fills. Wait until it does.
            var splashView = splashHandle.View;
            float t = 0f;
            while (splashView != null && splashView.IsVisible)
            {
                t += Time.deltaTime;
                if (t > 30f) break; // safety timeout
                await UniTask.NextFrame(ct);
            }
            if (t < minSplashDuration)
                await UniTask.Delay((int)((minSplashDuration - t) * 1000f), cancellationToken: ct);
        }

        if (showMainMenu)
        {
            await UIManager.Instance.ShowAsync(mainMenuId, ct: ct);
        }
    }
}
