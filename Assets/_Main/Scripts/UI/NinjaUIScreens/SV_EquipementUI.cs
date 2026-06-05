using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;

/// <summary>
/// Equipment screen. Sanitizes the legacy mockup, then overlays a functional runtime
/// equipment panel (6 slot widgets + owned-item list + Close button) via SV_EquipmentView.
/// Standalone file so the prefab m_Script {fileID:11500000} resolves to THIS class.
/// </summary>
public class SV_EquipementUI : SV_LegacyUIBase
{
    private SV_EquipmentView _view;

    public override async UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        await base.OnCreateAsync(ctx, ct);
        _view = new SV_EquipmentView();
        _view.Build(transform, OnCloseButtonClicked);
    }

    public override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
    {
        _view?.OnShow();
        return UniTask.CompletedTask;
    }

    public override void OnRelease() => _view?.Dispose();
}
