using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CycleScrollView : MonoBehaviour
{
    public enum ScrollDirection
    {
        Vertical,
        Horizontal,
        Grid
    }
    [Header("Settings")]
    [Tooltip("Nếu bật, tự Init() trong Start()")]
    public bool autoInit = true;
    [Tooltip("Kiểu scroll: Vertical / Horizontal / Grid")]
    public ScrollDirection direction = ScrollDirection.Vertical;
    [Tooltip("Tổng số phần tử data")]
    public int dataCount = 100;
    [Tooltip("Số item giữ trong pool (nên > số item nhìn thấy trên màn hình)")]
    public int poolSize = 15;
    [Header("References")]
    public ScrollRect scrollRect;
    public RectTransform content;
    public RectTransform itemPrefab;
    /// <summary>
    /// Callback để update UI item theo index data.
    /// Tham số 1: RectTransform item, Tham số 2: dataIndex
    /// </summary>
    public Action<RectTransform, int> OnUpdateScrollView;
    // Internal
    private readonly List<RectTransform> _items = new List<RectTransform>();
    private int _startIndex = 0; // index của item "đầu tiên" trong data
    private float _itemWidth;
    private float _itemHeight;
    private GridLayoutGroup _grid;
    private VerticalLayoutGroup _vlg;
    private HorizontalLayoutGroup _hlg;
    // Với grid: số item trong 1 dòng (hoặc 1 cột)
    private int _gridPerLine = 1;
    #region Unity
    private void Start()
    {
        if (autoInit)
        {
            Init();
        }
    }
    private void OnDestroy()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScroll);
        }
    }
    #endregion
    #region Public API
    /// <summary>
    /// Gọi Init() nếu bạn muốn tự control (khi autoInit = false)
    /// </summary>
    public void Init()
    {
        if (scrollRect == null || content == null || itemPrefab == null)
        {
            Debug.LogError("[CycleScrollView] Thiếu reference ScrollRect / Content / ItemPrefab");
            return;
        }
        DetectLayout();
        CalculateItemSize();
        BuildPool();
        scrollRect.onValueChanged.RemoveListener(OnScroll);
        scrollRect.onValueChanged.AddListener(OnScroll);
    }
    /// <summary>
    /// Đổi số data và rebuild (ví dụ đổi list khác)
    /// </summary>
    public void SetDataCount(int newCount, bool rebuild = true)
    {
        dataCount = Mathf.Max(0, newCount);
        if (rebuild)
        {
            Init();
        }
    }
    #endregion
    #region Setup
    private void DetectLayout()
    {
        _grid = content.GetComponent<GridLayoutGroup>();
        _vlg = content.GetComponent<VerticalLayoutGroup>();
        _hlg = content.GetComponent<HorizontalLayoutGroup>();
    }
    private void CalculateItemSize()
    {
        // Size mặc định từ prefab
        _itemWidth = itemPrefab.sizeDelta.x;
        _itemHeight = itemPrefab.sizeDelta.y;
        if (direction == ScrollDirection.Grid && _grid != null)
        {
            _itemWidth = _grid.cellSize.x;
            _itemHeight = _grid.cellSize.y;
            // Tính số item trên 1 dòng/cột dựa theo viewport
            var viewport = scrollRect.viewport != null
                ? scrollRect.viewport
                : (RectTransform)scrollRect.transform;
            if (scrollRect.vertical)
            {
                // Grid kéo dọc, mỗi hàng là 1 "line"
                float totalWidth = viewport.rect.width + _grid.spacing.x;
                _gridPerLine = Mathf.FloorToInt(totalWidth / (_itemWidth + _grid.spacing.x));
            }
            else
            {
                // Grid kéo ngang, mỗi cột là 1 "line" (theo chiều dọc)
                float totalHeight = viewport.rect.height + _grid.spacing.y;
                _gridPerLine = Mathf.FloorToInt(totalHeight / (_itemHeight + _grid.spacing.y));
            }
            _gridPerLine = Mathf.Max(1, _gridPerLine);
        }
        else
        {
            _gridPerLine = 1;
        }
    }
    private void BuildPool()
    {
        // Clear cũ
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
        _items.Clear();
        _startIndex = 0;
        if (poolSize <= 0 || dataCount <= 0)
            return;
        int actualPool = Mathf.Min(poolSize, dataCount);
        for (int i = 0; i < actualPool; i++)
        {
            RectTransform rt = Instantiate(itemPrefab, content);
            rt.gameObject.SetActive(true);
            _items.Add(rt);
            int dataIndex = (_startIndex + i) % dataCount;
            UpdateItem(rt, dataIndex);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
    #endregion
    #region Scroll Handler
    private void OnScroll(Vector2 _)
    {
        if (_items.Count == 0 || dataCount == 0)
            return;
        switch (direction)
        {
            case ScrollDirection.Vertical:
                HandleVertical();
                break;
            case ScrollDirection.Horizontal:
                HandleHorizontal();
                break;
            case ScrollDirection.Grid:
                HandleGrid();
                break;
        }
    }
    #endregion
    #region Vertical
    private void HandleVertical()
    {
        float contentY = content.anchoredPosition.y;
        float viewportHeight = scrollRect.viewport.rect.height;
        RectTransform first = _items[0];
        RectTransform last = _items[_items.Count - 1];
        float firstBottom = first.localPosition.y - _itemHeight; // thấp hơn first
        float lastTop = last.localPosition.y;                     // đỉnh của last
        // Khi item đầu trôi quá trên
        if (firstBottom > contentY + _itemHeight)
        {
            MoveFirstToLastVertical();
        }
        // Khi item cuối trôi quá dưới
        else if (lastTop < contentY - viewportHeight - _itemHeight)
        {
            MoveLastToFirstVertical();
        }
    }
    private void MoveFirstToLastVertical()
    {
        RectTransform first = _items[0];
        _items.RemoveAt(0);
        _items.Add(first);
        first.SetSiblingIndex(content.childCount - 1);
        _startIndex = (_startIndex + 1) % dataCount;
        int newDataIndex = (_startIndex + _items.Count - 1) % dataCount;
        UpdateItem(first, newDataIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
    private void MoveLastToFirstVertical()
    {
        RectTransform last = _items[_items.Count - 1];
        _items.RemoveAt(_items.Count - 1);
        _items.Insert(0, last);
        last.SetSiblingIndex(0);
        _startIndex = (_startIndex - 1 + dataCount) % dataCount;
        int newDataIndex = _startIndex;
        UpdateItem(last, newDataIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
    #endregion
    #region Horizontal
    private void HandleHorizontal()
    {
        float contentX = -content.anchoredPosition.x;
        float viewportWidth = scrollRect.viewport.rect.width;
        RectTransform first = _items[0];
        RectTransform last = _items[_items.Count - 1];
        float firstRight = first.localPosition.x + _itemWidth;
        float lastLeft = last.localPosition.x;
        // Item đầu trôi quá bên trái
        if (firstRight < contentX - _itemWidth)
        {
            MoveFirstToLastHorizontal();
        }
        // Item cuối trôi quá bên phải
        else if (lastLeft > contentX + viewportWidth + _itemWidth)
        {
            MoveLastToFirstHorizontal();
        }
    }
    private void MoveFirstToLastHorizontal()
    {
        RectTransform first = _items[0];
        _items.RemoveAt(0);
        _items.Add(first);
        first.SetSiblingIndex(content.childCount - 1);
        _startIndex = (_startIndex + 1) % dataCount;
        int newDataIndex = (_startIndex + _items.Count - 1) % dataCount;
        UpdateItem(first, newDataIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
    private void MoveLastToFirstHorizontal()
    {
        RectTransform last = _items[_items.Count - 1];
        _items.RemoveAt(_items.Count - 1);
        _items.Insert(0, last);
        last.SetSiblingIndex(0);
        _startIndex = (_startIndex - 1 + dataCount) % dataCount;
        int newDataIndex = _startIndex;
        UpdateItem(last, newDataIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
    #endregion
    #region Grid
    private void HandleGrid()
    {
        if (_grid == null)
            return;
        if (scrollRect.vertical)
        {
            HandleGridVertical();
        }
        else
        {
            HandleGridHorizontal();
        }
    }
    // Grid kéo dọc (trên dưới)
    private void HandleGridVertical()
    {
        float contentY = content.anchoredPosition.y;
        float viewportHeight = scrollRect.viewport.rect.height;
        float lineHeight = _itemHeight + _grid.spacing.y;
        RectTransform first = _items[0];
        RectTransform last = _items[_items.Count - 1];
        float firstBottom = first.localPosition.y - lineHeight;
        float lastTop = last.localPosition.y;
        if (firstBottom > contentY + lineHeight)
        {
            MoveGridFirstToLast(+1);
        }
        else if (lastTop < contentY - viewportHeight - lineHeight)
        {
            MoveGridLastToFirst(-1);
        }
    }
    // Grid kéo ngang (trái phải)
    private void HandleGridHorizontal()
    {
        float contentX = -content.anchoredPosition.x;
        float viewportWidth = scrollRect.viewport.rect.width;
        float lineWidth = _itemWidth + _grid.spacing.x;
        RectTransform first = _items[0];
        RectTransform last = _items[_items.Count - 1];
        float firstRight = first.localPosition.x + lineWidth;
        float lastLeft = last.localPosition.x;
        if (firstRight < contentX - lineWidth)
        {
            MoveGridFirstToLast(+1);
        }
        else if (lastLeft > contentX + viewportWidth + lineWidth)
        {
            MoveGridLastToFirst(-1);
        }
    }
    /// <summary>
    /// Di chuyển 1 "line" (1 hàng hoặc 1 cột) từ đầu -> cuối
    /// </summary>
    private void MoveGridFirstToLast(int stepLines)
    {
        int moveCount = Mathf.Min(_gridPerLine, _items.Count);
        for (int i = 0; i < moveCount; i++)
        {
            RectTransform first = _items[0];
            _items.RemoveAt(0);
            _items.Add(first);
            first.SetSiblingIndex(content.childCount - 1);
        }
        // Cập nhật index data
        _startIndex = (_startIndex + moveCount) % dataCount;
        // Update lại các item vừa move (đang nằm cuối)
        for (int i = 0; i < moveCount; i++)
        {
            int idx = _items.Count - moveCount + i;
            int dataIndex = (_startIndex + idx) % dataCount;
            UpdateItem(_items[idx], dataIndex);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
    /// <summary>
    /// Di chuyển 1 "line" từ cuối -> đầu
    /// </summary>
    private void MoveGridLastToFirst(int stepLines)
    {
        int moveCount = Mathf.Min(_gridPerLine, _items.Count);
        for (int i = 0; i < moveCount; i++)
        {
            RectTransform last = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            _items.Insert(0, last);
            last.SetSiblingIndex(0);
        }
        _startIndex = (_startIndex - moveCount + dataCount) % dataCount;
        for (int i = 0; i < moveCount; i++)
        {
            int dataIndex = (_startIndex + i) % dataCount;
            UpdateItem(_items[i], dataIndex);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
    #endregion
    #region UpdateItem
    /// <summary>
    /// Hàm trung gian gọi delegate OnUpdateItem.
    /// Bạn bắt buộc set OnUpdateItem từ ngoài để update UI theo index.
    /// </summary>
    private void UpdateItem(RectTransform rt, int dataIndex)
    {
        if (OnUpdateScrollView != null)
        {
            OnUpdateScrollView(rt, dataIndex);
        }
        else
        {
            // Nếu chưa gán callback, có thể log để biết
            // Debug.LogWarning("[CycleScrollView] OnUpdateItem chưa được gán!");
        }
    }
    #endregion
}
