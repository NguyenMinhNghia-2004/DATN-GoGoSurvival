using Luzart;

// Thin NinjaUI wrappers for UIs cloned as-is from the original scene.
// The legacy MonoBehaviour on the GameObject (ShopManager, SelectMapManager, etc.)
// continues to drive logic; this wrapper just lets UIManager track show/hide.
public class SV_ShopUI : UIBase { }
public class SV_EquipementUI : UIBase { }
public class SV_ProcessUI : UIBase { }
public class SV_EvolveUI : UIBase { }
public class SV_MailsUI : UIBase { }
public class SV_SelectMapUI : UIBase { }
