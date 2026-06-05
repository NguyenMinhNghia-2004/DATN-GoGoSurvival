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

// SV_ShopUI and SV_EquipementUI live in their OWN files (SV_ShopUI.cs / SV_EquipementUI.cs)
// so that the prefab m_Script {fileID:11500000} resolves to the correct class. In a multi-class
// file, fileID 11500000 binds to the class matching the filename (here: none → it fell through
// to SV_LegacyUIBase, which is why subclass logic never ran).
public class SV_ProcessUI : SV_LegacyUIBase { }
public class SV_EvolveUI : SV_LegacyUIBase { }
public class SV_MailsUI : SV_LegacyUIBase { }
public class SV_SelectMapUI : SV_LegacyUIBase { }
