using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NinjaUI wrapper cho màn Splash của DATN.
/// Lane: Screen. Cache: ReleaseOnClose (chỉ show 1 lần lúc boot).
/// Sau khi loading bar đầy → tự fire OnClosedCallback, gameplay code load màn kế tiếp.
///
/// Migration note:
///   - Thay vì gắn `SplashManager` (legacy MonoBehaviour) vào prefab,
///     gắn class này. Class kế thừa toàn bộ logic (filling bar, narrator text)
///     từ SplashManager nhưng đi qua NinjaUI lifecycle.
///   - Sau migrate xong scene, xóa `SplashManager.cs` legacy.
/// </summary>
public class SV_SplashUI : UIBase
{
    [Header("Refs (kéo từ prefab)")]
    [SerializeField] private Image fillingBar;
    [SerializeField] private Text valueText;
    [SerializeField] private Text narratorText;
    [SerializeField] private Text versionText;

    [Header("Behavior")]
    [SerializeField] private float fillSpeed = 100f; // %/sec
    [SerializeField] private string loadingPrefix = "Loading";

    private float currentFill = 0f;
    private bool isComplete = false;
    private Coroutine narratorCo;

    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        if (versionText != null)
            versionText.text = $"v{Application.version}";
        return UniTask.CompletedTask;
    }

    public override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
    {
        currentFill = 0f;
        isComplete = false;
        if (fillingBar != null) fillingBar.fillAmount = 0f;
        if (valueText != null) valueText.text = "0%";
        narratorCo = StartCoroutine(NarratorAnim());
        return UniTask.CompletedTask;
    }

    private void Update()
    {
        if (isComplete) return;
        if (fillingBar == null) return;

        currentFill = Mathf.Min(100f, currentFill + fillSpeed * Time.deltaTime);
        fillingBar.fillAmount = currentFill / 100f;
        if (valueText != null) valueText.text = ((int)currentFill) + "%";

        if (currentFill >= 100f)
        {
            isComplete = true;
            // Splash xong → request close. UIManager sẽ gọi OnClosedCallback (nếu có).
            OnCloseButtonClicked();
        }
    }

    private IEnumerator NarratorAnim()
    {
        int dots = 0;
        while (!isComplete)
        {
            if (narratorText != null)
                narratorText.text = loadingPrefix + new string('.', dots);
            dots = (dots + 1) % 4;
            yield return new WaitForSeconds(0.3f);
        }
    }

    public override UniTask OnHiddenAsync(UIHideReason reason, CancellationToken ct)
    {
        if (narratorCo != null)
        {
            StopCoroutine(narratorCo);
            narratorCo = null;
        }
        return UniTask.CompletedTask;
    }
}
