using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings popup — music/sound/vibration toggles, language, etc.
/// Lane: Popup. Dismissable by ESC.
/// </summary>
public class SV_SettingsPopupUI : UIBase
{
    [Header("Toggles")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Toggle vibrationToggle;

    [Header("Buttons")]
    [SerializeField] private Button btnClose;

    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        if (btnClose != null) btnClose.onClick.AddListener(() => OnCloseButtonClicked());
        if (musicToggle != null) musicToggle.onValueChanged.AddListener(OnMusicChanged);
        if (soundToggle != null) soundToggle.onValueChanged.AddListener(OnSoundChanged);
        if (vibrationToggle != null) vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
        return UniTask.CompletedTask;
    }

    public override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
    {
        // TODO: Load current settings values from DataManager / PlayerPrefs and apply to toggles.
        return UniTask.CompletedTask;
    }

    private void OnMusicChanged(bool isOn)
    {
        // TODO: Wire to AudioManager.
        PlayerPrefs.SetInt("sv_music", isOn ? 1 : 0);
    }

    private void OnSoundChanged(bool isOn)
    {
        PlayerPrefs.SetInt("sv_sound", isOn ? 1 : 0);
    }

    private void OnVibrationChanged(bool isOn)
    {
        PlayerPrefs.SetInt("sv_vibration", isOn ? 1 : 0);
    }
}
