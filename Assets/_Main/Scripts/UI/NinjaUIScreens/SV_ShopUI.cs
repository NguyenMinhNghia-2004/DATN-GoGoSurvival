using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;

/// <summary>
/// Shop screen. Sanitizes the legacy mockup buttons, then overlays a functional runtime
/// buy-grid + Close button (SV_ShopView). Standalone file so the prefab m_Script
/// {fileID:11500000} resolves to THIS class (filename == class name).
/// </summary>
public class SV_ShopUI : SV_LegacyUIBase
{
    private SV_ShopView _view;

    public override async UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        await base.OnCreateAsync(ctx, ct); // disable broken legacy onClick listeners
        _view = new SV_ShopView();
        _view.Build(transform, OnCloseButtonClicked);
    }

    public override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
    {
        _view?.OnShow();
        return UniTask.CompletedTask;
    }

    public override void OnRelease() => _view?.Dispose();
}
