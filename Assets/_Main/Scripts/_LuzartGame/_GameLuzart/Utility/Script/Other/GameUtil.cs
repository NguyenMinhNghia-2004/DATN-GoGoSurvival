namespace Luzart
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Pool;
    using UnityEngine.UI;
    using static Unity.VisualScripting.Member;
    public class GameUtil : UnityEngine.MonoBehaviour
    {
        public static GameUtil Instance;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private void Start()
        {
            if (isActionPerSecond)
            {
                StartCount();
            }
            if (isOnNewDay)
            {
                WaitAFrame(OnNewDay);
            }
        }
        #region ButtonEffect
        public static void ButtonOnClick(Button bt, UnityAction action, bool isAnim = false, string whereAds = null)
        {
            if (bt == null)
            {
                return;
            }
            UnityAction _action = () =>
            {
                action?.Invoke();
            };
            if (isAnim)
            {
                bt.OnClickAnim(_action);
            }
            else
            {
                bt.onClick.RemoveAllListeners();
                bt.onClick.AddListener(_action);
            }
        }
        #endregion
        #region String Color
        public static string StringColor(string constColor, object value)
        {
            return $"<color={constColor}>{value}</color>";
        }
        #endregion
        #region Count Per Second
        [SerializeField]
        private bool isActionPerSecond = true;
        private void StartCount()
        {
            _ = StartCoroutine(IECountTime());
        }
        private int timePing = 0;
        private int dayCache = int.MinValue;
        private IEnumerator IECountTime()
        {
            dayCache = DateTimeOffset.FromUnixTimeSeconds(TimeUtils.GetLongTimeCurrent).Day;
            WaitForSeconds wait = new WaitForSeconds(1);
            while (true)
            {
                long timeCb = TimeUtils.GetLongTimeCurrent;
                Observer.Instance.Notify(ObserverKey.TimeActionPerSecond, timeCb);
                timePing++;
                if (timePing >= 5)
                {
                    StartDayCount(timeCb);
                    timePing = 0;
                }
                yield return wait;
            }
        }
        private void StartDayCount(long timeCurrent)
        {
            int dayCacheLocal = DateTimeOffset.FromUnixTimeSeconds(TimeUtils.GetLongTimeCurrent).Day;
            if (dayCacheLocal != dayCache)
            {
                Debug.Log("NewDay");
                if (isOnNewDay)
                {
                    WaitAFrame(OnNewDay);
                }
            }
            dayCache = dayCacheLocal;
        }
        #endregion
        #region Coroutine    
        public void WaitAndDo(float time, Action action)
        {
            _ = StartCoroutine(ieWaitAndDo());
            IEnumerator ieWaitAndDo()
            {
                WaitForSeconds wait = new WaitForSeconds(time);
                yield return wait;
                action?.Invoke();
            }
        }
        public void WaitUtilAndDo(Func<bool> predicate, Action action)
        {
            _ = StartCoroutine(ieWaitUtilAndDo());
            IEnumerator ieWaitUtilAndDo()
            {
                WaitUntil wait = new WaitUntil(predicate);
                yield return wait;
                action?.Invoke();
            }
        }
        public void WaitAFrame(Action action)
        {
            _ = StartCoroutine(ieWaitAFrame());
            IEnumerator ieWaitAFrame()
            {
                yield return null;
                action?.Invoke();
            }
        }
        public void WaitFrame(int frame, Action action)
        {
            _ = StartCoroutine(ieWaitFrame(frame));
            IEnumerator ieWaitFrame(int frame)
            {
                for (int i = 0; i < frame; i++)
                {
                    yield return null;
                }
                action?.Invoke();
            }
        }
        private Dictionary<UnityEngine.MonoBehaviour, List<Coroutine>> coroutineDictionary = new Dictionary<UnityEngine.MonoBehaviour, List<Coroutine>>();
        public void WaitAndDo(UnityEngine.MonoBehaviour behaviour, float time, Action action)
        {
            if (!coroutineDictionary.TryGetValue(behaviour, out var coroutines))
            {
                coroutines = new List<Coroutine>();
                coroutineDictionary[behaviour] = coroutines;
            }
            // Dừng tất cả các coroutine hiện tại của MonoBehaviour (nếu cần thiết)
            StopAllCoroutinesForBehaviour(behaviour);
            Coroutine coroutine = behaviour.StartCoroutine(ieWaitAndDo(time, action));
            coroutines.Add(coroutine);
            IEnumerator ieWaitAndDo(float _time, Action callBack)
            {
                WaitForSeconds wait = new WaitForSeconds(_time);
                yield return wait;
                callBack?.Invoke();
            }
        }
        public void StopAllCoroutinesForBehaviour(UnityEngine.MonoBehaviour behaviour)
        {
            if (coroutineDictionary.TryGetValue(behaviour, out var coroutines))
            {
                if (coroutines == null)
                {
                    return;
                }
                _ = coroutines.RemoveAll(item => item == null);
                foreach (var coroutine in coroutines)
                {
                    behaviour.StopCoroutine(coroutine);
                }
                coroutines.Clear();
            }
        }
        #endregion
        #region Coroutine DOVirtual
        public void StartLerpValue(UnityEngine.MonoBehaviour behaviour, float preValue, float value, float timeLerp = 1f, Action<float> action = null, Action onComplete = null)
        {
            if (!coroutineDictionary.TryGetValue(behaviour, out var coroutines))
            {
                coroutines = new List<Coroutine>();
                coroutineDictionary[behaviour] = coroutines;
            }
            else
            {
                // Dừng tất cả các coroutine hiện tại của MonoBehaviour
                StopAllCoroutinesForBehaviour(behaviour);
            }
            Coroutine coroutine = behaviour.StartCoroutine(IEFloatLerp(behaviour, preValue, value, timeLerp, action, onComplete));
            coroutines.Add(coroutine);
        }
        private IEnumerator IEFloatLerp(UnityEngine.MonoBehaviour behaviour, float preValue, float value, float timeLerp = 1f, Action<float> action = null, Action onComplete = null)
        {
            float elapsedTime = 0f;
            // Dùng Time.unscaledDeltaTime để tính toán khi timeScale = 0
            while (elapsedTime < timeLerp)
            {
                elapsedTime += Time.unscaledDeltaTime; // Thay vì Time.deltaTime, dùng unscaledDeltaTime
                float lerpedValue = Mathf.Lerp(preValue, value, elapsedTime / timeLerp);
                action?.Invoke(lerpedValue);
                yield return null;
            }
            // Đảm bảo rằng giá trị cuối cùng là giá trị đích
            action?.Invoke(value);
            onComplete?.Invoke();
            _ = coroutineDictionary.Remove(behaviour);
        }
        #endregion
        #region ONNewDay
        private const string LastCheckedDateKey = "LastCheckedDateSeconds";
        [SerializeField]
        private bool isOnNewDay = true;
        public void OnNewDay()
        {
            // Lấy thời gian hiện tại dưới dạng giây
            long currentSeconds = TimeUtils.GetLongTimeCurrent;
            // Lấy số giây lưu trữ trong PlayerPrefs
            long lastCheckedSeconds = PlayerPrefs.GetInt(LastCheckedDateKey, 0);
            if (lastCheckedSeconds == 0)
            {
                // Nếu không có thời gian lưu trữ trước đó, đây là lần đầu tiên chạy
                PerformNewDayActions(currentSeconds);
            }
            else
            {
                DateTimeOffset lastCheckedDate = DateTimeOffset.FromUnixTimeSeconds(lastCheckedSeconds);
                DateTimeOffset currentDate = DateTimeOffset.FromUnixTimeSeconds(currentSeconds);
                if (currentDate.Date > lastCheckedDate.Date)
                {
                    // Nếu ngày hiện tại khác ngày lưu trữ, thực hiện hành động cho ngày mới
                    PerformNewDayActions(currentSeconds);
                }
            }
        }
        void PerformNewDayActions(long currentSeconds)
        {
            // Cập nhật ngày mới vào PlayerPrefs
            PlayerPrefs.SetInt(LastCheckedDateKey, (int)currentSeconds);
            PlayerPrefs.Save();
            Observer.Instance.Notify(ObserverKey.OnNewDay);
        }
        #endregion
        #region Time And Ordinal To String
        public static string LongTimeSecondToUnixTime(long unixTimeSeconds, bool isDoubleParam = false, string day = "d", string hour = "h", string minutes = "m", string second = "s")
        {
            TimeSpan dateTime = TimeSpan.FromSeconds(unixTimeSeconds);
            return TimeSpanToUnixTime(dateTime, isDoubleParam, day, hour, minutes, second);
        }
        public static string FloatTimeSecondToUnixTime(float unixTimeSeconds, bool isDoubleParam = false, string day = "d", string hour = "h", string minutes = "m", string second = "s")
        {
            TimeSpan dateTime = TimeSpan.FromSeconds(unixTimeSeconds);
            string strValue = TimeSpanToUnixTime(dateTime, isDoubleParam, day, hour, minutes, second);
            int milliseconds = dateTime.Milliseconds;
            if (milliseconds > 0)
            {
                strValue += $".{milliseconds:D3}";
            }
            return strValue;
        }
        public static string TimeSpanToUnixTime(TimeSpan dateTime, bool isDoubleParam = false, string day = "d", string hour = "h", string minutes = "m", string second = "s")
        {
            string strValue;
            if (dateTime.Days > 0)
            {
                if (dateTime.Hours == 0 && !isDoubleParam)
                {
                    strValue = $"{dateTime.Days:D2}{day}";
                }
                else
                {
                    strValue = $"{dateTime.Days:D2}{day}:{dateTime.Hours:D2}{hour}";
                }
            }
            else if (dateTime.Hours > 0)
            {
                if (dateTime.Minutes == 0 && !isDoubleParam)
                {
                    strValue = $"{dateTime.Hours:D2}{hour}";
                }
                else
                {
                    strValue = $"{dateTime.Hours:D2}{hour}:{dateTime.Minutes:D2}{minutes}";
                }
            }
            else
            {
                if (dateTime.Seconds == 0 && !isDoubleParam)
                {
                    strValue = $"{dateTime.Minutes:D2}{minutes}";
                }
                else
                {
                    strValue = $"{dateTime.Minutes:D2}{minutes}:{dateTime.Seconds:D2}{second}";
                }
            }
            return strValue;
        }
        public static string ToOrdinal(int number)
        {
            return $"{number}{GetOrdinalSuffix(number)}";
        }
        public static string GetOrdinalSuffix(int number)
        {
            if (number <= 0) return "";
            int lastTwoDigits = number % 100;
            int lastDigit = number % 10;
            if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
            {
                return "th";
            }
            switch (lastDigit)
            {
                case 1:
                    return "st";
                case 2:
                    return "nd";
                case 3:
                    return "rd";
                default:
                    return "th";
            }
        }
        #endregion
        #region Step To Step
        public static void StepToStep(Action<Action>[] step)
        {
            ExecuteStep(step, 0);
            void ExecuteStep(Action<Action>[] stepOnDone, int currentIndex)
            {
                if (currentIndex < stepOnDone.Length)
                {
                    stepOnDone[currentIndex](() =>
                    {
                        // Sau khi bước hoàn thành, tiếp tục bước tiếp theo
                        ExecuteStep(stepOnDone, currentIndex + 1);
                    });
                }
            }
        }
        #endregion
        #region Log
        public static void Log(object debug)
        {
#if DEBUG_DA
            Debug.Log($"<color={ConstColor.ColorBlueLight}>DEBUG_DA: {debug}</color>");
#endif
        }
        public static void LogError(object debug)
        {
#if DEBUG_DA
            Debug.LogError($"<color={ConstColor.ColorBlueLight}>DEBUG_DA: {debug}</color>");
#endif
        }
        #endregion
        #region LayerMask
        public static bool IsLayerInLayerMask(int layer, LayerMask layerMask)
        {
            return ((layerMask.value & (1 << layer)) != 0);
        }
        #endregion
        #region Extension Check Null
        public static void SetActiveCheckNull(GameObject ob, bool status)
        {
            if (ob == null) return;
            ob.SetActive(status);
        }
        public static void SetSpriteCheckNull(Image im, Sprite sp)
        {
            if (im == null) return;
            im.sprite = sp;
        }
        public static void SetTextCheckNull(TMPro.TMP_Text txt, string str)
        {
            if (txt == null) return;
            txt.text = str;
        }
        #endregion
        #region Claimed Level
        public static int GetIncrementalValueAtLevel(IList<int> listSorted, int level)
        {
            int preLevel = level - 1;
            int amountPreLevel = (preLevel < 0) ? 0 : listSorted[preLevel];
            int amountCurrentLevel = listSorted[level];
            int sub = amountCurrentLevel - amountPreLevel;
            return sub;
        }
        #endregion
        public static List<GameObject> FindGameObjectsByName(string name)
        {
            // Tạo danh sách để lưu các GameObject tìm được
            List<GameObject> objectsWithName = new List<GameObject>();
            // Lấy tất cả các GameObject trong scene
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            // Kiểm tra từng GameObject xem có tên là "collidd" không
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == name)
                {
                    objectsWithName.Add(obj);
                }
            }
            return objectsWithName;
        }
        public static T GetRandomEnumValue<T>() where T : Enum
        {
            Array values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        }
    }
    public static class ConstColor
    {
        public const string ColorRed = "#FF0000";
        public const string ColorGreen = "#00FF06";
        public const string ColorBlack = "#000000";
        public const string ColorWhite = "#FFFFFF";
        public const string ColorBlueLight = "#00FFF0";
        public const string ColorBlueDark = "#000FFF";
        public const string ColorYellow = "#FFFF00";
        public const string ColorPurple = "#7300B3";
    }
    public static class UIExtension
    {
        public static void OnClickAnim(this Button btn, UnityAction action)
        {
            if (btn == null)
            {
                return;
            }
            var effect = btn.GetComponent<EffectButton>();
            if (effect == null)
            {
                effect = btn.gameObject.AddComponent<EffectButton>();
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                action?.Invoke();
            });
        }
        public static Vector2 GetRandomPositionInRect(this RectTransform targetRectTransform)
        {
            if (targetRectTransform == null)
            {
                Debug.LogError("targetRectTransform is null!");
                return Vector2.zero;
            }
            // Lấy kích thước của RectTransform
            Vector2 size = targetRectTransform.rect.size;
            // Lấy tọa độ ngẫu nhiên bên trong kích thước
            float randomX = UnityEngine.Random.Range(-size.x / 2, size.x / 2);
            float randomY = UnityEngine.Random.Range(-size.y / 2, size.y / 2);
            // Trả về vị trí ngẫu nhiên trong RectTransform (trong không gian cục bộ)
            return new Vector2(randomX, randomY);
        }
        public static void CallOnEnable(this GameObject obj)
        {
            if (obj == null)
            {
                return;
            }
            obj.SetActive(false);
            obj.SetActive(true);
        }
        public static void ChangeParentAndKeepSize(this RectTransform childRectTransform, RectTransform newParent)
        {
            // Lưu kích thước trong không gian thế giới trước khi đổi cha
            Vector2 worldSizeBefore = GetWorldSize(childRectTransform);
            // Đổi cha cho đối tượng
            childRectTransform.SetParent(newParent, false); // 'false' để không thay đổi vị trí trong không gian thế giới ngay lập tức
            // Tính tỉ lệ của cha mới trong không gian thế giới
            Vector3 newParentWorldScale = newParent.lossyScale;
            // Cập nhật lại sizeDelta dựa trên kích thước thế giới và tỉ lệ cha mới
            childRectTransform.sizeDelta = new Vector2(worldSizeBefore.x / newParentWorldScale.x, worldSizeBefore.y / newParentWorldScale.y);
            Vector2 GetWorldSize(RectTransform rectTransform)
            {
                Vector2 sizeDelta = rectTransform.sizeDelta;
                Vector3 worldScale = rectTransform.lossyScale;
                // Kích thước trong không gian thế giới
                return new Vector2(sizeDelta.x * worldScale.x, sizeDelta.y * worldScale.y);
            }
        }
        public static void ClaimedRectTransformScrollView(this ScrollRect scrollRect, RectTransform itemRectTransform)
        {
            Canvas.ForceUpdateCanvases();
            Vector3[] itemCorners = new Vector3[4];
            itemRectTransform.GetWorldCorners(itemCorners);
            Vector3[] viewCorners = new Vector3[4];
            scrollRect.viewport.GetWorldCorners(viewCorners);
            float difference = 0;
            if (scrollRect.horizontal)
            {
                if (itemCorners[2].x > viewCorners[2].x)
                {
                    difference = itemCorners[2].x - viewCorners[2].x;
                }
                else if (itemCorners[0].x < viewCorners[0].x)
                {
                    difference = itemCorners[0].x - viewCorners[0].x;
                }
                float width = viewCorners[2].x - viewCorners[0].x;
                float normalizedDifference = difference / width;
                Vector2 posCurrent = scrollRect.content.anchoredPosition;
                Vector2 size = scrollRect.content.sizeDelta;
                float newX = posCurrent.x - normalizedDifference * size.x;
                float minX = 0;
                float maxX = scrollRect.content.sizeDelta.x - scrollRect.viewport.rect.width;
                scrollRect.content.anchoredPosition = new Vector2(Mathf.Clamp(newX, minX, maxX), posCurrent.y);
            }
            else
            {
                if (itemCorners[1].y > viewCorners[1].y)
                {
                    difference = itemCorners[1].y - viewCorners[1].y;
                }
                else if (itemCorners[0].y < viewCorners[0].y)
                {
                    difference = itemCorners[0].y - viewCorners[0].y;
                }
                float height = viewCorners[1].y - viewCorners[0].y;
                float normalizedDifference = difference / height;
                Vector2 posCurrent = scrollRect.content.anchoredPosition;
                Vector2 size = scrollRect.content.sizeDelta;
                float newY = posCurrent.y - normalizedDifference * size.y;
                float minY = 0;
                float maxY = scrollRect.content.sizeDelta.y - scrollRect.viewport.rect.height;
                scrollRect.content.anchoredPosition = new Vector2(posCurrent.x, Mathf.Clamp(newY, minY, maxY));
            }
        }
        public static void ClaimedRectTransformScrollView(this ScrollRect scrollRect, RectTransform itemRectTransform, float elasticity)
        {
            scrollRect.elasticity = 0.5f;
            ClaimedRectTransformScrollView(scrollRect, itemRectTransform);
        }
        public static void FocusOnRectTransform(this ScrollRect scrollRect, RectTransform itemRectTransform, bool isReverse = false, float timeMove = -1)
        {
            // Lấy thông tin của VerticalLayoutGroup nếu có
            VerticalLayoutGroup layoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
            float spacing = 0f;
            float paddingTop = 0f;
            if (layoutGroup != null)
            {
                spacing = layoutGroup.spacing;
                paddingTop = layoutGroup.padding.top;
            }
            // Lấy chiều cao của content và viewport
            float contentHeight = scrollRect.content.rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;
            // Tính vị trí của phần tử (theo PreferredHeight) từ đầu content, cộng thêm paddingTop
            float targetPositionY = paddingTop;
            for (int i = 0; i < scrollRect.content.childCount; i++)
            {
                RectTransform child = scrollRect.content.GetChild(i) as RectTransform;
                // Nếu phần tử có LayoutElement và ignoreLayout = true thì bỏ qua
                LayoutElement layoutElem = child.GetComponent<LayoutElement>();
                if (layoutElem != null && layoutElem.ignoreLayout)
                {
                    continue;
                }
                if (child == itemRectTransform)
                {
                    break; // Dừng khi gặp phần tử cần cuộn tới
                }
                float childHeight = child.rect.height;
                targetPositionY += childHeight + spacing;
            }
            // Tính offset để căn giữa phần tử trong viewport
            float itemHeight = itemRectTransform.rect.height;
            float elementOffset = (viewportHeight - itemHeight) / 2f;
            targetPositionY -= elementOffset;
            // Tính khoảng cuộn được
            float scrollableHeight = contentHeight - viewportHeight;
            if (scrollableHeight <= 0)
            {
                scrollRect.verticalNormalizedPosition = 1f;
                return;
            }
            // Tính normalizedPosition (1 là trên cùng, 0 là dưới cùng)
            float normalizedPosition = Mathf.Clamp01(targetPositionY / scrollableHeight);
            if (!isReverse)
            {
                normalizedPosition = 1 - normalizedPosition;
            }
            if (timeMove == -1)
            {
                scrollRect.verticalNormalizedPosition = normalizedPosition;
            }
            else
            {
                float preValue = scrollRect.verticalNormalizedPosition;
                GameUtil.Instance.StartLerpValue(scrollRect, preValue, normalizedPosition, timeMove, (data) =>
                {
                    scrollRect.verticalNormalizedPosition = data;
                });
            }
        }
        public static void FocusOnRectTransformFromBottom(this ScrollRect scrollRect, RectTransform itemRectTransform)
        {
            float contentHeight = scrollRect.content.rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;
            float targetPositionY = 0f;
            // Tính vị trí phần tử dựa trên offset của VerticalLayoutGroup, nhưng từ dưới lên trên
            for (int i = scrollRect.content.childCount - 1; i >= 0; i--)
            {
                RectTransform child = scrollRect.content.GetChild(i) as RectTransform;
                if (child == itemRectTransform)
                {
                    break; // Dừng lại khi đến phần tử cần cuộn tới
                }
                targetPositionY += child.rect.height;
            }
            // Điều chỉnh vị trí mục tiêu để phần tử hiển thị không sát mép dưới
            float elementOffset = viewportHeight / 2 - itemRectTransform.rect.height / 2;
            targetPositionY -= elementOffset;
            // Tính giá trị cuộn từ dưới lên
            float normalizedPosition = Mathf.Clamp01(targetPositionY / (contentHeight - viewportHeight));
            // Cuộn đến phần tử
            scrollRect.verticalNormalizedPosition = normalizedPosition;
        }
        public static void ScrollToCenter(this ScrollRect scrollRect, RectTransform target, bool isReverse = false)
        {
            if (scrollRect == null || scrollRect.content == null || target == null)
                return;
            int factor = isReverse ? -1 : 1;
            // Lấy world corners của content và target
            Vector3[] contentWorldCorners = new Vector3[4];
            scrollRect.content.GetWorldCorners(contentWorldCorners);
            Vector3[] targetWorldCorners = new Vector3[4];
            target.GetWorldCorners(targetWorldCorners);
            // Chiều cao của content và viewport
            float contentHeight = contentWorldCorners[1].y - contentWorldCorners[0].y;
            float viewportHeight = scrollRect.viewport.rect.height;
            // Tính trung điểm Y của target
            float targetCenterY = (targetWorldCorners[0].y + targetWorldCorners[1].y) * 0.5f;
            float contentTopY = contentWorldCorners[1].y;
            // DỊCH target xuống nửa chiều cao viewport để nó nằm giữa
            float adjustedTargetY = targetCenterY - viewportHeight * 0.5f * factor;
            // Tính normalized position
            float normalizedPosition = (contentTopY - adjustedTargetY) / (contentHeight - viewportHeight);
            if (isReverse)
            {
                scrollRect.verticalNormalizedPosition = 1f - Mathf.Clamp01(normalizedPosition);
            }
            else
            {
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
            }
        }
        public static float CalculatePositionInHorizontalScroll(this ScrollRect scrollRect, RectTransform itemRectTransform)
        {
            // Lấy kích thước và vị trí của content trong tọa độ viewport
            var contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scrollRect.viewport, scrollRect.content);
            var itemBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scrollRect.viewport, itemRectTransform);
            // Tính vị trí mục tiêu của phần tử trong content
            float contentWidth = contentBounds.size.x;
            float viewportWidth = scrollRect.viewport.rect.width;
            // Xác định vị trí mục tiêu
            float targetPositionX = itemBounds.center.x - contentBounds.min.x;
            float elementOffset = viewportWidth / 2 - itemRectTransform.rect.width / 2;
            targetPositionX -= elementOffset;
            // Tính toán giá trị cuộn chuẩn hóa
            float normalizedPosition = Mathf.Clamp01(targetPositionX / (contentWidth - viewportWidth));
            return normalizedPosition;
        }
        public static void SetInteractable(this Button bt, bool interactable)
        {
            var graphics = bt.GetComponentsInChildren<MaskableGraphic>();
            bt.interactable = interactable;
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].color = interactable ? bt.colors.normalColor : bt.colors.disabledColor;
            }
        }
        public static int SumRange(this IList<int> collection, int min, int max)
        {
            int num = 0;
            for (var i = min; i <= max && i < collection.Count; i++)
            {
                int obj = collection[i];
                num += obj;
            }
            return num;
        }
        public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            int num = 0;
            foreach (T obj in collection)
            {
                if (predicate(obj))
                    return num;
                ++num;
            }
            return -1;
        }
        public static void Shuffle<T>(this IList<T> ts)
        {
            var count = ts.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = UnityEngine.Random.Range(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }
    }
    public static class VectorExtensions
    {
        public static Vector2 GetCircularDirection(Vector2 direction, int total, int index)
        {
            if (total <= 0) 
                return direction;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float delta = 360f / total;
            float angle = baseAngle + index * delta;
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        }
        public static bool ApproximateVector3(this Vector3 v1, Vector3 v2)
        {
            if (v1.x - v2.x < 0.01f && v1.y - v2.y < 0.01f && v1.z - v2.z < 0.01f)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static float[] Vector2ToFloatArray(this Vector2 value)
        {
            return new float[]
            {
                value.x,
                value.y,
            };
        }
        public static Vector2 FloatArrayToVector2(float[] value)
        {
            return new Vector2(value[0], value[1]);
        }
    }
    public static class ListExtensions
    {
        /// <summary>
        /// Creates a shallow copy of the list
        /// </summary>
        public static List<T> Clone<T>(this List<T> list)
        {
            if (list == null)
                return null;
            return new List<T>(list);
        }
        public static List<T> ClonePool<T>(this List<T> source)
        {
            if (source == null)
                return null;
            List<T> pooledList = ListPool<T>.Get();
            pooledList.Capacity = source.Count;
            pooledList.AddRange(source);
            return pooledList;
        }
        /// <summary>
        /// Creates a deep copy of the list for ICloneable objects
        /// </summary>
        public static List<T> DeepClone<T>(this List<T> list) where T : ICloneable
        {
            if (list == null)
                return null;
            List<T> clonedList = new List<T>(list.Count);
            foreach (T item in list)
            {
                clonedList.Add(item == null ? default(T) : (T)item.Clone());
            }
            return clonedList;
        }
        public static T DeepClone<T>(T original) where T : class
        {
            if (original == null)
            {
                return null;
            }
            string json = JsonUtility.ToJson(original);
            T clone = JsonUtility.FromJson<T>(json);
            return clone;
        }
        // Kiểm tra 2 list có giống cả từng phần tử
        public static bool AreListsEqual<T>(this List<T> list1, List<T> list2)
        {
            if (list1.Count != list2.Count)
            {
                return false;
            }
            var json1 = JsonUtility.ToJson(list1);
            var json2 = JsonUtility.ToJson(list2);
            return json1.Equals(json2);
        }
        // Lấy phần tử random trong khoảng từ bao nhiêu đến bao nhiêu
        public static int GetRandomValueInCountStack(int total, int contain, int count, int minGive)
        {
            int valueSub = (total - contain);
            if (count <= 1)
            {
                return valueSub;
            }
            int canSub = valueSub - minGive * (count - 1);
            int valueSure = UnityEngine.Random.Range(minGive, canSub);
            return valueSure;
        }
        /// <summary>
        /// Tìm chỉ số của phần tử đầu tiên trong mảng đã sắp xếp có giá trị LỚN HƠN HOẶC BẰNG (target).
        /// </summary>
        /// <param name="sortedArray">Mảng/Danh sách số nguyên đã được sắp xếp tăng dần.</param><param name="target">Giá trị cần tìm vị trí chèn.</param>
        /// <returns>
        /// Chỉ số của phần tử đầu tiên >= target .Trả về sortedArray.Count nếu target lớn hơn phần tử cuối cùng (vị trí chèn).
        /// </returns>
        public static int FindLowerBoundIndex(IList<int> sortedArray, double target)
        {
            int left = 0;
            int right = sortedArray.Count - 1;
            int lowerBoundIndex = sortedArray.Count; // Khởi tạo là vị trí chèn cuối cùng
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (sortedArray[mid] >= target)
                {
                    // Phần tử này (mid) có thể là đáp án, lưu lại và tìm tiếp ở nửa trái
                    // để tìm chỉ số nhỏ nhất thỏa mãn.
                    lowerBoundIndex = mid;
                    right = mid - 1;
                }
                else // sortedArray[mid] < target
                {
                    // Phần tử này quá nhỏ, phải tìm ở nửa bên phải.
                    left = mid + 1;
                }
            }
            // lowerBoundIndex sẽ là chỉ số của phần tử đầu tiên >= target
            // hoặc sortedArray.Count nếu target lớn hơn phần tử cuối cùng.
            return lowerBoundIndex;
        }
        private static readonly System.Random Rng = new System.Random();
        public static List<T> GetShuffle<T>(this List<T> list, int count)
        {
            // 1. Tạo bản sao để tránh thay đổi danh sách gốc
            // O(N)
            var result = new List<T>(list);
            // Đảm bảo count không vượt quá số lượng phần tử hiện có
            int effectiveCount = Math.Min(count, result.Count);
            // 2. Thực hiện Fisher-Yates Shuffle (chỉ đến vị trí effectiveCount)
            // Đây là thuật toán xáo trộn tối ưu O(N)
            for (int i = 0; i < effectiveCount; i++)
            {
                // Chọn một phần tử ngẫu nhiên từ vị trí i đến cuối
                int k = Rng.Next(i, result.Count);
                // Hoán đổi (swap) phần tử tại i và k
                T value = result[k];
                result[k] = result[i];
                result[i] = value;
            }
            // 3. Trả về effectiveCount phần tử đầu tiên đã được xáo trộn
            // O(effectiveCount)
            return result.Take(effectiveCount).ToList();
        }
    }
    public struct ListPoolHelper<T> : IDisposable
    {
        private List<T> _list;
        public List<T> List
        {
            get { return _list; }
        }
        public ListPoolHelper(IEnumerable<T> values = null)
        {
            _list = ListPool<T>.Get();
            _list.Clear();
            if (values != null) _list.AddRange(values);
        }
        public void Dispose()
        {
            if (_list != null)
            {
                _list.Clear();
                ListPool<T>.Release(_list);
                _list = null;
            }
        }
    }
    public static class CollectionExtensions
    {
        // Extension method cho Dictionary để dễ dàng thêm vào mà không cần phải lặp lại thủ công
        public static void AddRange<TK, TV>(this Dictionary<TK, TV> dictionary, IEnumerable<KeyValuePair<TK, TV>> keyValuePairs)
        {
            foreach (var pair in keyValuePairs)
            {
                if (!dictionary.ContainsKey(pair.Key))
                {
                    dictionary.Add(pair.Key, pair.Value);
                }
            }
        }
        // Extension method cho Dictionary để thêm vào dựa trên 1 danh sách đối tượng
        public static void AddListToDictionary<T, TK, TV>(this Dictionary<TK, TV> dictionary, IEnumerable<T> items, System.Func<T, KeyValuePair<TK, TV>> keySelector)
        {
            foreach (var item in items)
            {
                var pair = keySelector(item);
                if (!dictionary.ContainsKey(pair.Key))
                {
                    dictionary.Add(pair.Key, pair.Value);
                }
            }
        }
    }
    // Sinh vat the va tu pool
    public static class MasterHelper
    {
        public static void InitListObj<Tobj, Tdata>(IList<Tdata> data, Tobj objPf, IList<Tobj> objs, Transform holdObj, System.Action<Tobj, int> onSetup) where Tobj : UnityEngine.MonoBehaviour
        {
            if (objs == null)
            {
                objs = new List<Tobj>();
            }
            objPf.gameObject.SetActive(false);
            if (data != null)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    Tobj n;
                    var idx = i;
                    if (i < objs.Count)
                    {
                        n = objs[idx];
                    }
                    else
                    {
                        n = UnityEngine.Object.Instantiate(objPf, holdObj);
                        objs.Add(n);
                    }
                    onSetup?.Invoke(n, idx);
                }
            }
            var c = data == null ? 0 : data.Count;
            if (c < objs.Count)
            {
                for (int i = c; i < objs.Count; i++)
                {
                    objs[i].gameObject.SetActive(false);
                }
            }
        }
        public static void InitListObj<Tobj>(int num, Tobj objPf, IList<Tobj> objs, Transform holdObj, System.Action<Tobj, int> onSetup) where Tobj : UnityEngine.MonoBehaviour
        {
            if (objs == null)
            {
                objs = new List<Tobj>();
            }
            objPf.gameObject.SetActive(false);
            for (int i = 0; i < num; i++)
            {
                Tobj n;
                var idx = i;
                if (i < objs.Count)
                {
                    n = objs[idx];
                }
                else
                {
                    n = UnityEngine.Object.Instantiate(objPf, holdObj);
                    objs.Add(n);
                }
                onSetup?.Invoke(n, idx);
            }
            if (num < objs.Count)
            {
                for (int i = num; i < objs.Count; i++)
                {
                    objs[i].gameObject.SetActive(false);
                }
            }
        }
    }
    public class Singleton<T> : UnityEngine.MonoBehaviour where T : UnityEngine.MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new();
        public static T Instance
        {
            get
            {
                //if (applicationIsQuitting)
                //{
                //    Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                //    "' already destroyed on application quit." +
                //    " Won't create again - returning null.");
                //    return null;
                //}
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));
                        if (FindObjectsOfType(typeof(T)).Length > 1)
                        {
                            Debug.LogError("[Singleton] Something went really wrong " +
                            " - there should never be more than 1 singleton!" +
                            " Reopenning the scene might fix it.");
                            return _instance;
                        }
                        if (_instance == null)
                        {
                            GameObject singleton = new();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = "(singleton)" + typeof(T).ToString();
                            Debug.Log("[Singleton] An instance of " + typeof(T) +
                            " is needed in the scene, so '" + singleton +
                            "' was created with DontDestroyOnLoad.");
                        }
                        else
                        {
                            //Debug.Log("[Singleton] Using instance already created: " +  _instance.gameObject.name);
                        }
                        //DontDestroyOnLoad(_instance.gameObject);
                    }
                }
                return _instance;
            }
        }
        //private static bool applicationIsQuitting = false;
        //public virtual void OnDestroy()
        //{
        //    applicationIsQuitting = true;
        //}
    }
}
