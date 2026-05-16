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

    [Header("Anim")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.22f;

    protected override UniTask OnBeforeShowAsync(SV_LevelUpData data, CancellationToken ct)
    {
        if (data == null || data.Options == null || slotContainer == null) return UniTask.CompletedTask;

        if (titleText != null) titleText.text = "LEVEL UP! Choose a skill";

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

        Time.timeScale = 0f;
        return UniTask.CompletedTask;
    }

    private void BindSlot(GameObject slotGO, Data_UpgradeSkill opt)
    {
        var slot = slotGO.GetComponent<SV_LevelUpSlot>();
        if (slot != null)
        {
            slot.Bind(opt, () => HandlePick(opt));
            return;
        }
        // Best-effort: fill Name/Description/Icon/Stars children, attach click handler.
        var nameTr = slotGO.transform.Find("Name");
        if (nameTr != null && opt.SkillConfig != null)
        {
            var tmp = nameTr.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = opt.SkillConfig.name;
            else { var t = nameTr.GetComponent<UnityEngine.UI.Text>(); if (t != null) t.text = opt.SkillConfig.name; }
        }
        var descTr = slotGO.transform.Find("Description");
        if (descTr == null) descTr = slotGO.transform.Find("Description ");
        if (descTr == null) descTr = slotGO.transform.Find("Description  ");
        if (descTr != null)
        {
            var tmp = descTr.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = $"Lv.{opt.LevelIndex + 1}";
            else { var t = descTr.GetComponent<UnityEngine.UI.Text>(); if (t != null) t.text = $"Lv.{opt.LevelIndex + 1}"; }
        }
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
            // No Button → add one to the root so the player can click anywhere on the slot.
            btn = slotGO.AddComponent<Button>();
            btn.onClick.AddListener(() => HandlePick(opt));
        }
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

    private static SV_SkillCatalog Catalog
    {
        get
        {
            if (_catalog == null)
            {
                #if UNITY_EDITOR
                _catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<SV_SkillCatalog>("Assets/_Main/Data/Skills/SV_SkillCatalog.asset");
                #endif
                // Runtime fallback: must be referenced from a scene SO field, or load via Resources.
                // For DATN, catalog is asset-driven; consumers can also pass directly.
            }
            return _catalog;
        }
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
