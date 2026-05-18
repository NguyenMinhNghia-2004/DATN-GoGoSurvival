using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;

// Thin NinjaUI wrappers for UIs cloned as-is from the original scene.
// The legacy MonoBehaviour on the GameObject (ShopManager, SelectMapManager, etc.)
// continues to drive logic; this wrapper just lets UIManager track show/hide.
//
// SanitizeOnCreate base: every wrapper disables broken (null-target) onClick
// listeners on its child buttons. The cloned prefabs carry many legacy listeners
// that resolve to null at runtime — without sanitize, clicking those throws NRE.
public class SV_LegacyUIBase : UIBase
{
    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        UIButtonSanitizer.SanitizeChildButtons(transform);
        return UniTask.CompletedTask;
    }
}

public class SV_ShopUI : SV_LegacyUIBase { }
public class SV_EquipementUI : SV_LegacyUIBase { }
public class SV_ProcessUI : SV_LegacyUIBase { }
public class SV_EvolveUI : SV_LegacyUIBase { }
public class SV_MailsUI : SV_LegacyUIBase { }
public class SV_SelectMapUI : SV_LegacyUIBase { }
