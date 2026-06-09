using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Luzart;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Data passed when opening the Level Up popup.
/// Holds the 3 skill choices rolled by UpgradeSkillManager.
/// </summary>
public class SV_LevelUpData
{
    public IReadOnlyList<Data_UpgradeSkill> Options;
    public System.Action<Data_UpgradeSkill> OnPicked;
}

/// <summary>
/// Level-up popup — Survivor.io core feature: player picks 1 of 3 skill options.
/// Lane: Popup. DismissByEscape = FALSE (player MUST choose).
/// Cache: PoolOnClose.
///
/// Hook for gameplay: when UpgradeSkillManager triggers an upgrade,
/// call UIManager.Instance.ShowAsync<SV_LevelUpPopupUI>(UIId.SV_LevelUpPopup,
///     new UIContext(new SV_LevelUpData { Options = rolled, OnPicked = HandlePicked }));
/// </summary>
public class SV_LevelUpPopupUI : UIBase<SV_LevelUpData>
{
    [Header("Slot prefab + container")]
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private GameObject slotPrefab; // Should have SV_LevelUpSlot or similar

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI playerLevelText;

    [Header("Anim")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.22f;

    [Header("Skill BG Sprites")]
    [SerializeField] private Sprite activeSkillBG;
    [SerializeField] private Sprite passiveSkillBG;

    [Header("Current Owned Skills Display")]
    [SerializeField] private RectTransform shotYollowContainer;
    [SerializeField] private RectTransform packGreenContainer;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (activeSkillBG == null || passiveSkillBG == null)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/_Main/UI/User Interface/7.png");
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset is Sprite s)
                    {
                        if (s.name == "WeaponItemBG_Normal") activeSkillBG = s;
                        else if (s.name == "SupplyItemBG_Normal") passiveSkillBG = s;
                    }
                }
            }
        }

        if (shotYollowContainer == null)
        {
            var panel = transform.Find("CurrentSkill");
            if (panel != null)
            {
                var tr = panel.Find("ShotYollow");
                if (tr != null) shotYollowContainer = tr.GetComponent<RectTransform>();
            }
        }

        if (packGreenContainer == null)
        {
            var panel = transform.Find("CurrentSkill");
            if (panel != null)
            {
                var tr = panel.Find("PackGreen");
                if (tr != null) packGreenContainer = tr.GetComponent<RectTransform>();
            }
        }

        if (playerLevelText == null)
        {
            var header = transform.Find("Header");
            if (header != null)
            {
                var tr = header.Find("LevelText");
                if (tr == null) tr = header.Find("Level");
                if (tr != null) playerLevelText = tr.GetComponent<TextMeshProUGUI>();
            }
            if (playerLevelText == null)
            {
                var tr = transform.Find("LevelText");
                if (tr == null) tr = transform.Find("Level");
                if (tr != null) playerLevelText = tr.GetComponent<TextMeshProUGUI>();
            }
        }
    }
#endif

    protected override UniTask OnBeforeShowAsync(SV_LevelUpData data, CancellationToken ct)
    {
        // Disable broken Inspector-wired onClicks on the cloned slot buttons before we
        // attach our own pick handlers. Without this, click events trigger legacy
        // NULL_TARGET listeners → NullReferenceException.
        UIButtonSanitizer.SanitizeChildButtons(transform);

        if (data == null || data.Options == null || slotContainer == null) return UniTask.CompletedTask;

        if (titleText != null) titleText.text = "LEVEL UP! Choose a skill";

        if (playerLevelText != null)
        {
            var gameController = SceneRootManager.Instance?.Domain?.Get<GameController>();
            if (gameController != null)
            {
                playerLevelText.text = $"{gameController.CurrentLevel.Value}";
            }
        }

        // Bind to the fixed child slots already present in the prefab (Left/Center/Right etc.).
        // If slotPrefab is assigned, fall back to instantiation for legacy flows.
        int slotCount = slotContainer.childCount;
        for (int i = 0; i < slotCount; i++)
        {
            var slotTr = slotContainer.GetChild(i);
            if (i >= data.Options.Count)
            {
                slotTr.gameObject.SetActive(false);
                continue;
            }
            slotTr.gameObject.SetActive(true);
            BindSlot(slotTr.gameObject, data.Options[i]);
        }

        UpdateCurrentSkills();

        Time.timeScale = 0f;
        return UniTask.CompletedTask;
    }

    private void UpdateCurrentSkills()
    {
        if (shotYollowContainer == null || packGreenContainer == null) return;
        var playerRoot = Object.FindObjectOfType<LuzartPlayerEntityRoot>();
        if (playerRoot == null) return;
        var runtimes = playerRoot.SkillRuntimes;
        if (runtimes == null) return;
        var activeSkills = new List<ZSkill>();
        var passiveSkills = new List<ZSkill>();
        foreach (var rt in runtimes)
        {
            if (rt == null || rt.Config == null) continue;
            if (rt.Config.ETypeSkill == ETypeSkill.Active) activeSkills.Add(rt);
            else if (rt.Config.ETypeSkill == ETypeSkill.Stat) passiveSkills.Add(rt);
        }
        var catalog = SV_LevelUpSlot.LookupCatalog();
        PopulateSkillIcons(shotYollowContainer, activeSkills, catalog);
        PopulateSkillIcons(packGreenContainer, passiveSkills, catalog);
    }

    private void PopulateSkillIcons(RectTransform container, List<ZSkill> skills, SV_SkillCatalog catalog)
    {
        int childCount = container.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var slotTr = container.GetChild(i);
            var iconTr = slotTr.Find("ICON");
            if (iconTr == null) continue;
            var img = iconTr.GetComponent<Image>();
            if (img == null) continue;

            var starsContainer = FindChildLoose(slotTr, "Stars");
            if (starsContainer != null)
            {
                starsContainer.gameObject.SetActive(false);
            }

            if (i < skills.Count)
            {
                var skill = skills[i];
                Sprite iconSprite = null;
                if (catalog != null)
                {
                    string id = skill.Config.name;
                    var entry = catalog.FindById(id);
                    if (entry == null && id != null && (id.StartsWith("ZSk_") || id.StartsWith("ZPs_")))
                    {
                        entry = catalog.FindById(id.Substring(1));
                    }
                    if (entry != null)
                    {
                        iconSprite = entry.icon;
                    }
                }
                if (iconSprite != null)
                {
                    img.sprite = iconSprite;
                    img.enabled = true;
                }
                else
                {
                    img.enabled = false;
                }
            }
            else
            {
                img.enabled = false;
            }
        }
    }

    private void BindSlot(GameObject slotGO, Data_UpgradeSkill opt)
    {
        var bgImage = slotGO.GetComponent<Image>();
        if (bgImage != null && opt.SkillConfig != null)
        {
            bool isActive = opt.SkillConfig.ETypeSkill == ETypeSkill.Active;
            bgImage.sprite = isActive ? activeSkillBG : passiveSkillBG;
        }

        var slot = slotGO.GetComponent<SV_LevelUpSlot>();
        if (slot != null)
        {
            slot.Bind(opt, () => HandlePick(opt));
            return;
        }

        // Fallback: slot child has no SV_LevelUpSlot component (legacy prefab layout).
        // Find ICON/Name/Description by name (tolerating trailing whitespace), look up the
        // SV_SkillCatalog for icon + per-star description, and wire the click handler.
        var catalog = SV_LevelUpSlot.LookupCatalog();
        SV_SkillEntry entry = null;
        if (catalog != null && opt.SkillConfig != null)
        {
            string id = opt.SkillConfig.name; // e.g. "ZSk_Molotov" or "ZPs_EnergyDrink"
            entry = catalog.FindById(id);
            // Catalog IDs drop the leading 'Z' framework prefix (Sk_/Ps_ vs ZSk_/ZPs_).
            if (entry == null && id != null && (id.StartsWith("ZSk_") || id.StartsWith("ZPs_")))
                entry = catalog.FindById(id.Substring(1));
        }

        // Icon: try ICON child (with optional trailing space).
        var iconTr = FindChildLoose(slotGO.transform, "ICON");
        if (iconTr != null && entry != null && entry.icon != null)
        {
            var img = iconTr.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = entry.icon;
                img.color = Color.white;
                img.preserveAspect = true;
                img.enabled = true;
            }
        }

        // Name: prefer entry.displayName, fall back to ZSkillConfig.name.
        string displayName = entry != null ? entry.displayName : (opt.SkillConfig != null ? opt.SkillConfig.name : "?");
        var nameTr = FindChildLoose(slotGO.transform, "Name");
        if (nameTr != null) SetText(nameTr, displayName);

        var playerRoot = Object.FindObjectOfType<LuzartPlayerEntityRoot>();
        var owned = playerRoot != null && opt.SkillConfig != null ? playerRoot.GetRuntime(opt.SkillConfig) : null;
        int nextLevelIndex = owned != null ? (int)((IZSkill)owned).LevelIndex.Value + 1 : 0;

        string descText = $"Lv.{nextLevelIndex + 1}";
        if (entry != null && entry.perStarDescription != null && entry.perStarDescription.Length > 0)
        {
            int starIdx = Mathf.Clamp(nextLevelIndex, 0, entry.perStarDescription.Length - 1);
            descText = entry.perStarDescription[starIdx];
        }
        var descTr = FindChildLoose(slotGO.transform, "Description");
        if (descTr != null) SetText(descTr, descText);

        var starsContainer = FindChildLoose(slotGO.transform, "Stars");
        int maxLevel = opt.SkillConfig != null && opt.SkillConfig.UpgradeConfigs != null ? opt.SkillConfig.UpgradeConfigs.Count : 5;
        UpdateStars(starsContainer, nextLevelIndex, maxLevel);

        // Attach click handler — either on the root or on the Background child.
        var btn = slotGO.GetComponent<Button>();
        if (btn == null) btn = slotGO.GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => HandlePick(opt));
        }
        else
        {
            btn = slotGO.AddComponent<Button>();
            btn.onClick.AddListener(() => HandlePick(opt));
        }
    }

    /// <summary>Find a direct child whose name matches `name` (ignoring trailing whitespace).</summary>
    private static Transform FindChildLoose(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name.TrimEnd() == name) return c;
        }
        // Also try one level deeper since the prefab has nested 'ICON' sometimes.
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            var found = FindChildLoose(c, name);
            if (found != null) return found;
        }
        return null;
    }

    private static void UpdateStars(Transform starsContainer, int levelIndex, int maxLevel)
    {
        if (starsContainer == null) return;
        starsContainer.gameObject.SetActive(true);
        int starsLit = Mathf.Clamp(levelIndex + 1, 1, maxLevel);
        for (int s = 1; s <= 5; s++)
        {
            var starTr = FindChildLoose(starsContainer, "Stars" + s);
            if (starTr == null) continue;
            bool isVisible = s <= maxLevel;
            starTr.gameObject.SetActive(isVisible);
            if (isVisible)
            {
                var active = starTr.Find("Active");
                var inactive = starTr.Find("Inactive");
                bool on = s <= starsLit;
                if (active != null) active.gameObject.SetActive(on);
                if (inactive != null) inactive.gameObject.SetActive(!on);
            }
        }
    }

    private static void SetText(Transform tr, string text)
    {
        // Try component on this transform first, fall back to first Text/TMP in subtree
        // (the prefab's "Description" is an Image with a child TMP — must dig deeper).
        var tmp = tr.GetComponent<TextMeshProUGUI>();
        if (tmp != null) { tmp.text = text; return; }
        var t = tr.GetComponent<UnityEngine.UI.Text>();
        if (t != null) { t.text = text; return; }
        var childTmp = tr.GetComponentInChildren<TextMeshProUGUI>(true);
        if (childTmp != null) { childTmp.text = text; return; }
        var childText = tr.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (childText != null) childText.text = text;
    }

    public override async UniTask AnimateShowAsync(bool instant, CancellationToken ct)
    {
        if (canvasGroup == null) return;
        if (instant) { canvasGroup.alpha = 1f; return; }
        canvasGroup.alpha = 0f;
        var tw = canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        await tw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);
    }

    public override async UniTask AnimateHideAsync(bool instant, CancellationToken ct)
    {
        if (canvasGroup == null) return;
        if (instant) { canvasGroup.alpha = 0f; return; }
        var tw = canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        await tw.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);
    }

    protected override UniTask OnHiddenAsync(SV_LevelUpData data, UIHideReason reason, CancellationToken ct)
    {
        Time.timeScale = 1f;
        return UniTask.CompletedTask;
    }

    public override bool HandleEscape()
    {
        // Force pick — don't allow ESC to close.
        return true;
    }

    private void HandlePick(Data_UpgradeSkill picked)
    {
        Data?.OnPicked?.Invoke(picked);
        OnCloseButtonClicked();
    }
}

/// <summary>
/// Component on a single level-up slot prefab (icon + name + button).
///
/// Looks up `SV_SkillCatalog` (loaded from Resources) to populate icon + nice description.
/// Catalog asset path: `Assets/_Main/Data/Skills/SV_SkillCatalog.asset`. If located,
/// uses `displayName` and `icon` from catalog. Falls back to ZSkillConfig.name otherwise.
/// </summary>
public class SV_LevelUpSlot : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI descriptionText;
    public Button pickButton;

    private System.Action onPicked;
    private static SV_SkillCatalog _catalog;

    private static SV_SkillCatalog Catalog => LookupCatalog();

    /// <summary>Internal helper used by SV_LevelUpPopupUI fallback path.
    /// Loads catalog from AssetDatabase in editor, or from Resources at runtime.</summary>
    public static SV_SkillCatalog LookupCatalog()
    {
        if (_catalog != null) return _catalog;
#if UNITY_EDITOR
        _catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<SV_SkillCatalog>("Assets/_Main/Data/Skills/SV_SkillCatalog.asset");
#endif
        if (_catalog == null)
            _catalog = Resources.Load<SV_SkillCatalog>("SV_SkillCatalog");
        return _catalog;
    }

    public void Bind(Data_UpgradeSkill data, System.Action onPickedCallback)
    {
        onPicked = onPickedCallback;

        SV_SkillEntry entry = null;
        if (Catalog != null && data.SkillConfig != null)
        {
            // Convention: ZSkillConfig.name matches SV_SkillEntry.skillId (e.g., "Sk_Kunai").
            entry = Catalog.FindById(data.SkillConfig.name);
        }

        if (entry != null)
        {
            if (nameText != null) nameText.text = entry.displayName;
            if (iconImage != null && entry.icon != null) iconImage.sprite = entry.icon;
            int starIdx = Mathf.Clamp(data.LevelIndex, 0, entry.perStarDescription.Length - 1);
            if (descriptionText != null && entry.perStarDescription.Length > starIdx)
                descriptionText.text = entry.perStarDescription[starIdx];
        }
        else if (data.SkillConfig != null)
        {
            if (nameText != null) nameText.text = data.SkillConfig.name;
        }

        if (levelText != null) levelText.text = $"Lv.{data.LevelIndex + 1}";

        if (pickButton != null)
        {
            pickButton.onClick.RemoveAllListeners();
            pickButton.onClick.AddListener(() => onPicked?.Invoke());
        }
    }
}
