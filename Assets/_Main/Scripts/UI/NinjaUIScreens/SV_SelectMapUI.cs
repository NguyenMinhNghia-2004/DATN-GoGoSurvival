using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SV_SelectMapUI : UIBase
{
    [Header("UI Elements")]
    [SerializeField] private Button btnBack;
    [SerializeField] private Button btnSelect;
    [SerializeField] private GameObject btnSelected;
    [SerializeField] private Button btnWatchAds;
    [SerializeField] private Text txtHeaderTitle;
    [SerializeField] private Text txtCommentDescription;

    [Header("Map Icons")]
    [SerializeField] private Transform iconsContainer; // Kéo Container/Viewport/Content vào đây
    private Button[] mapIcons;

    private int currentSelectedIndex = 0;

    // Snapping logic variables
    private ScrollRect scrollRect;
    private float[] snapPositions;
    private bool isSnapping;
    private int targetSnapIndex;
    private float snapSpeed = 10f;

    // TODO: Create a MapConfig or Data array to store real map info
    private string[] mockMapNames = { "1. Wild Streets", "2. Desert Sand", "3. Toxic Plant", "4. Ruined City", "5. Dark Forest", "6. Lava Core" };
    private string[] mockMapDescs = { "The first map. Easy to survive.", "Hot and dry. Enemies are faster.", "Poisonous area. Watch your step.", "Ruins of the old world.", "Dark and scary.", "Extremely hot and dangerous." };
    // Tạm thời mở khoá hết để test
    private bool[] mockMapUnlocked = { true, true, true, true, true, true };

    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        if (btnBack != null) btnBack.onClick.AddListener(OnBack);
        if (btnSelect != null) btnSelect.onClick.AddListener(OnSelectClicked);

        // Auto-find references if not assigned
        if (btnBack == null) btnBack = FindChildButton("Back");
        if (btnSelect == null) btnSelect = FindChildButton("Select");
        if (btnSelected == null)
        {
            Transform t = FindChildTransform("Selected");
            if (t != null) btnSelected = t.gameObject;
        }
        if (btnWatchAds == null) btnWatchAds = FindChildButton("Watch Ads");
        
        if (txtHeaderTitle == null)
        {
            Transform t = FindChildTransform("Header");
            if (t != null) txtHeaderTitle = t.GetComponentInChildren<Text>();
        }

        if (iconsContainer == null)
        {
            // Auto-find "Container/Viewport/Content"
            Transform container = transform.Find("Container");
            if (container != null)
            {
                Transform viewport = container.Find("Viewport");
                if (viewport != null) iconsContainer = viewport.Find("Content");
            }
        }
        
        scrollRect = GetComponentInChildren<ScrollRect>();
        
        if (iconsContainer != null)
        {
            int count = iconsContainer.childCount;
            mapIcons = new Button[count];
            snapPositions = new float[count];
            
            for (int i = 0; i < count; i++)
            {
                int index = i;
                Transform child = iconsContainer.GetChild(i);
                Button btn = child.GetComponent<Button>();
                if (btn == null) btn = child.gameObject.AddComponent<Button>(); // Tự động thêm Button nếu chưa có
                
                mapIcons[i] = btn;
                mapIcons[i].onClick.AddListener(() => OnMapIconClicked(index));
                
                snapPositions[i] = (count > 1) ? ((float)i / (count - 1)) : 0f;
            }
        }

        return UniTask.CompletedTask;
    }

    private Transform FindChildTransform(string name)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    public override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
    {
        // Hiển thị map đã chọn lần trước
        int savedIndex = PlayerPrefs.GetInt("SelectedMapIndex", 0);
        OnMapIconClicked(savedIndex);
        
        // Đặt ngay lập tức vị trí cuộn mà không cần animation
        if (scrollRect != null && snapPositions != null && savedIndex < snapPositions.Length)
        {
            scrollRect.horizontalNormalizedPosition = snapPositions[savedIndex];
        }
        
        return UniTask.CompletedTask;
    }

    private Button FindChildButton(string name)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == name)
            {
                var b = t.GetComponent<Button>();
                if (b != null) return b;
            }
        return null;
    }

    private void OnBack()
    {
        UIManager.Instance.HideAsync(UIId.SV_SelectMap).Forget();
    }

    private void OnMapIconClicked(int index)
    {
        currentSelectedIndex = index;
        targetSnapIndex = index;
        isSnapping = true;
        if (scrollRect != null) scrollRect.velocity = Vector2.zero;

        // Cập nhật text tiêu đề và mô tả
        if (txtHeaderTitle != null && index < mockMapNames.Length) 
            txtHeaderTitle.text = mockMapNames[index];
            
        if (txtCommentDescription != null && index < mockMapDescs.Length) 
            txtCommentDescription.text = mockMapDescs[index];

        bool isUnlocked = index < mockMapUnlocked.Length && mockMapUnlocked[index];

        // Cập nhật trạng thái các nút
        if (btnSelect != null) btnSelect.gameObject.SetActive(isUnlocked);
        if (btnSelected != null) btnSelected.SetActive(false); // Tuỳ logic nếu map đang được chọn làm mặc định
        if (btnWatchAds != null) btnWatchAds.gameObject.SetActive(!isUnlocked);

        // Cập nhật UI của các Icon (VD: Phóng to icon được chọn, hiển thị Lock)
        for (int i = 0; i < mapIcons.Length; i++)
        {
            if (mapIcons[i] == null) continue;

            bool unlocked = i < mockMapUnlocked.Length && mockMapUnlocked[i];
            Transform inactiveObj = mapIcons[i].transform.Find("Inactive");
            if (inactiveObj != null) inactiveObj.gameObject.SetActive(!unlocked);

            // Phóng to icon được chọn
            mapIcons[i].transform.localScale = (i == index) ? new Vector3(1.1f, 1.1f, 1.1f) : Vector3.one;
        }
    }

    private void OnSelectClicked()
    {
        // Lưu index của map được chọn vào GameData hoặc PlayerPrefs để GameController đọc
        PlayerPrefs.SetInt("SelectedMapIndex", currentSelectedIndex);
        PlayerPrefs.Save();

        // Lấy thông tin map để gửi sang MainMenu
        Sprite icon = null;
        if (mapIcons != null && currentSelectedIndex < mapIcons.Length && mapIcons[currentSelectedIndex] != null) 
        {
            var img = mapIcons[currentSelectedIndex].GetComponent<Image>();
            if (img != null) icon = img.sprite;
        }

        string mapName = currentSelectedIndex < mockMapNames.Length ? mockMapNames[currentSelectedIndex] : "Unknown Map";

        Broadcaster.Broadcast(new Data_MapSelected { 
            MapIndex = currentSelectedIndex, 
            MapName = mapName, 
            MapIcon = icon 
        });

        // Đóng UI sau khi chọn
        OnBack();
    }

    public override bool HandleEscape()
    {
        OnBack();
        return true;
    }

    private void Update()
    {
        if (scrollRect == null || snapPositions == null || snapPositions.Length == 0) return;

        bool isInteracting = Input.GetMouseButton(0) || Input.touchCount > 0;
        
        if (isInteracting) 
        {
            isSnapping = false;
            return;
        }

        // Đã thả tay, chờ tốc độ scroll giảm
        if (!isSnapping) 
        {
            if (Mathf.Abs(scrollRect.velocity.x) > 0.01f && Mathf.Abs(scrollRect.velocity.x) < 300f) 
            {
                // Tìm vị trí gần nhất
                float currentPos = scrollRect.horizontalNormalizedPosition;
                float minDistance = float.MaxValue;
                int closestIndex = 0;
                for (int i = 0; i < snapPositions.Length; i++) 
                {
                    float dist = Mathf.Abs(snapPositions[i] - currentPos);
                    if (dist < minDistance) 
                    {
                        minDistance = dist;
                        closestIndex = i;
                    }
                }
                
                // Nếu khoảng cách còn quá lớn thì tự động snap
                if (minDistance > 0.001f)
                {
                    targetSnapIndex = closestIndex;
                    isSnapping = true;
                    scrollRect.velocity = Vector2.zero; // Dừng trớn
                    
                    // Tự động focus (chọn) UI vào map này
                    if (currentSelectedIndex != targetSnapIndex) 
                    {
                        OnMapIconClicked(targetSnapIndex);
                    }
                }
            }
        }

        // Thực hiện nội suy vị trí để snap
        if (isSnapping) 
        {
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(scrollRect.horizontalNormalizedPosition, snapPositions[targetSnapIndex], Time.deltaTime * snapSpeed);
            if (Mathf.Abs(scrollRect.horizontalNormalizedPosition - snapPositions[targetSnapIndex]) < 0.001f) 
            {
                isSnapping = false;
            }
        }
    }
}

public struct Data_MapSelected : Luzart.IBroadcastData
{
    public int MapIndex;
    public string MapName;
    public Sprite MapIcon;
}
