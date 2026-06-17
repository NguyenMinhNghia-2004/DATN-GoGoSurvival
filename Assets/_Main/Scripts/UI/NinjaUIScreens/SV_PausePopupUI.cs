using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause popup. Pauses gameplay while shown.
/// Lane: Popup. Cache: PoolOnClose.
/// </summary>
public class SV_PausePopupUI : UIBase
{
    [Header("Refs")]
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnMainMenu;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Anim")]
    [SerializeField] private float showDuration = 0.18f;
    [SerializeField] private Ease showEase = Ease.OutCubic;

    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        UIButtonSanitizer.SanitizeChildButtons(transform);
        // Auto-find by common legacy names if Inspector refs aren't wired.
        if (btnResume == null) btnResume = FindByName("Resume") ?? FindByName("BtnResume");
        if (btnSettings == null) btnSettings = FindByName("Setting") ?? FindByName("Settings") ?? FindByName("BtnSetting");
        if (btnMainMenu == null) btnMainMenu = FindByName("Home") ?? FindByName("MainMenu") ?? FindByName("BtnHome");
        if (btnResume != null) btnResume.onClick.AddListener(OnResume);
        if (btnSettings != null) btnSettings.onClick.AddListener(OnSettings);
        if (btnMainMenu != null) btnMainMenu.onClick.AddListener(OnMainMenu);
        // Show the shop gold balance on the pause screen.
        if (GetComponent<SV_GoldDisplay>() == null) gameObject.AddComponent<SV_GoldDisplay>();
        return UniTask.CompletedTask;
    }

    private Button FindByName(string name)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name.TrimEnd() == name)
            {
                var b = t.GetComponent<Button>();
                if (b != null) return b;
            }
        return null;
    }

    public override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
    {
        Time.timeScale = 0f;
        UpdateCurrentSkills();
        return UniTask.CompletedTask;
    }

    public override async UniTask AnimateShowAsync(bool instant, CancellationToken ct)
    {
        if (canvasGroup == null) return;
        if (instant) { canvasGroup.alpha = 1f; return; }
        canvasGroup.alpha = 0f;
        var tween = canvasGroup.DOFade(1f, showDuration).SetEase(showEase).SetUpdate(true);
        await tween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);
    }

    public override async UniTask AnimateHideAsync(bool instant, CancellationToken ct)
    {
        if (canvasGroup == null) return;
        if (instant) { canvasGroup.alpha = 0f; return; }
        var tween = canvasGroup.DOFade(0f, showDuration).SetEase(showEase).SetUpdate(true);
        await tween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);
    }

    public override UniTask OnHiddenAsync(UIHideReason reason, CancellationToken ct)
    {
        Time.timeScale = 1f;
        return UniTask.CompletedTask;
    }

    private void OnResume() => OnCloseButtonClicked();

    private async void OnSettings()
    {
        await UIManager.Instance.ShowAsync(UIId.SV_SettingsPopup, ct: this.GetCancellationTokenOnDestroy());
    }

    private async void OnMainMenu()
    {
        // Quit from pause = abandon the run → funnel through the single end door. EndGame stops
        // the sim + despawns enemies/player/map, then the defeat screen shows (its Retry/Home
        // buttons own the next navigation). Same end-game screen as a real death.
        Time.timeScale = 1f;
        OnCloseButtonClicked(); // close the pause popup first
        var classicMode = Luzart.SceneRootManager.Instance?.Domain?.Get<Luzart.ClassicModeController>();
        if (classicMode != null && classicMode.IsPlaying)
        {
            classicMode.EndGame(Luzart.EndReason.QuitFromPause);
            return;
        }
        // Fallback (ClassicMode not wired / not playing): direct-to-menu.
        await UIManager.Instance.HideAllExceptSystemAsync();
        await UIManager.Instance.ShowAsync(UIId.SV_MainMenu, ct: this.GetCancellationTokenOnDestroy());
    }

    [Header("Current Owned Skills Display")]
    [SerializeField] private RectTransform weaponContainer;
    [SerializeField] private RectTransform supplyContainer;
    [SerializeField] private RectTransform damageMeterContainer;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (weaponContainer == null)
        {
            var panel = transform.Find("Container");
            if (panel != null)
            {
                var ws = panel.Find("Weapon skil");
                if (ws != null)
                {
                    var c = ws.Find("Container");
                    if (c != null) weaponContainer = c.GetComponent<RectTransform>();
                }
            }
        }
        if (supplyContainer == null)
        {
            var panel = transform.Find("Container");
            if (panel != null)
            {
                var sup = panel.Find("Supplies");
                if (sup != null)
                {
                    var c = sup.Find("Container");
                    if (c != null) supplyContainer = c.GetComponent<RectTransform>();
                }
            }
        }
        if (damageMeterContainer == null)
        {
            var viewFile = transform.Find("Static/Background/Container/ViewFile");
            if (viewFile != null) damageMeterContainer = viewFile.GetComponent<RectTransform>();
        }
    }
#endif

    private void UpdateCurrentSkills()
    {
        if (weaponContainer == null || supplyContainer == null) return;
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
        PopulateSkillIcons(weaponContainer, activeSkills, catalog);
        PopulateSkillIcons(supplyContainer, passiveSkills, catalog);
        PopulateDamageMeter(damageMeterContainer, activeSkills, catalog);
    }

    private void PopulateDamageMeter(RectTransform container, List<ZSkill> skills, SV_SkillCatalog catalog)
    {
        if (container == null) return;
        int childCount = container.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var slotTr = container.GetChild(i);
            
            if (i < skills.Count)
            {
                slotTr.gameObject.SetActive(true);
                var skill = skills[i];
                string id = skill.Config != null ? skill.Config.name : "Unknown";

                var iconTr = slotTr.Find("Icon");
                if (iconTr != null)
                {
                    var img = iconTr.GetComponent<Image>();
                    if (img != null)
                    {
                        Sprite iconSprite = null;
                        if (catalog != null)
                        {
                            var entry = catalog.FindById(id);
                            if (entry == null && id != null && (id.StartsWith("ZSk_") || id.StartsWith("ZPs_")))
                            {
                                entry = catalog.FindById(id.Substring(1));
                            }
                            if (entry != null) iconSprite = entry.icon;
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
                }

                var nameTr = slotTr.Find("Name");
                if (nameTr != null)
                {
                    string displayName = id.Replace("ZSk_", "").Replace("Sk_", "");
                    var tmp = nameTr.GetComponent<TMPro.TextMeshProUGUI>();
                    if (tmp != null) tmp.text = displayName;
                    else
                    {
                        var txt = nameTr.GetComponent<UnityEngine.UI.Text>();
                        if (txt != null) txt.text = displayName;
                    }
                }

                float totalDamageAll = 0f;
                foreach (var s in skills) 
                {
                    if (s is IZSkill zs) totalDamageAll += zs.TotalDamageDealt;
                }
                
                float percent = 0f;
                if (totalDamageAll > 0 && skill is IZSkill zs2) 
                {
                    percent = zs2.TotalDamageDealt / totalDamageAll;
                }

                var valueTr = slotTr.Find("Value");
                if (valueTr != null)
                {
                    string percentStr = $"{(percent * 100f):0.#}%";
                    var tmp = valueTr.GetComponent<TMPro.TextMeshProUGUI>();
                    if (tmp != null) tmp.text = percentStr;
                    else
                    {
                        var txt = valueTr.GetComponent<UnityEngine.UI.Text>();
                        if (txt != null) txt.text = percentStr;
                    }
                }

                var fillingTr = slotTr.Find("Filling");
                if (fillingTr != null)
                {
                    var fillImg = fillingTr.GetComponent<Image>();
                    if (fillImg != null) fillImg.fillAmount = percent;
                }
            }
            else
            {
                slotTr.gameObject.SetActive(false);
            }
        }
    }

    private void PopulateSkillIcons(RectTransform container, List<ZSkill> skills, SV_SkillCatalog catalog)
    {
        if (container == null) return;
        int childCount = container.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var slotTr = container.GetChild(i);
            var contentTr = slotTr.Find("Content");
            if (contentTr == null) continue;

            if (i < skills.Count)
            {
                contentTr.gameObject.SetActive(true);
                var skill = skills[i];
                var iconTr = contentTr.Find("Icon");
                if (iconTr != null)
                {
                    var img = iconTr.GetComponent<Image>();
                    if (img != null)
                    {
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
                }

                var starsManagerTr = contentTr.Find("StarsManager");
                if (starsManagerTr != null)
                {
                    int maxLevel = skill.Config != null && skill.Config.UpgradeConfigs != null ? skill.Config.UpgradeConfigs.Count : 5;
                    int currentLevelIndex = (int)((IZSkill)skill).LevelIndex.Value;
                    UpdateStars(starsManagerTr, currentLevelIndex, maxLevel);
                }
            }
            else
            {
                contentTr.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateStars(Transform starsManagerTr, int levelIndex, int maxLevel)
    {
        if (starsManagerTr == null) return;
        int starsLit = Mathf.Clamp(levelIndex + 1, 1, maxLevel);
        for (int s = 1; s <= 5; s++)
        {
            var starTr = starsManagerTr.Find("Star" + s);
            if (starTr == null) continue;
            bool isVisible = s <= maxLevel;
            starTr.gameObject.SetActive(isVisible);
            if (isVisible)
            {
                var active = starTr.Find("Active");
                if (active != null) active.gameObject.SetActive(s <= starsLit);
                var inactive = starTr.Find("Inactive");
                if (inactive != null) inactive.gameObject.SetActive(s > starsLit);
            }
        }
    }
}
