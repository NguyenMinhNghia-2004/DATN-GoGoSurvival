namespace Luzart
{
    using System;
    using System.Text;
    public static class TimeUtils
    {
        public static long GetLongTimeCurrent
        {
            get
            {
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }
        public static long GetLongTimeDayInMonth(int day)
        {
            DateTimeOffset dayOfMonth = GetDateTimeDayInMonth(day);
            return dayOfMonth.ToUnixTimeSeconds();
        }
        public static DateTimeOffset GetDateTimeDayInMonth(int day)
        {
            DateTimeOffset now = DateTimeOffset.FromUnixTimeSeconds(GetLongTimeCurrent);
            return new DateTimeOffset(now.Year, now.Month, day, 0, 0, 0, TimeSpan.Zero);
        }
        public static DateTimeOffset GetDateTimeTimeByDay(long time, int space)
        {
            DateTimeOffset now = DateTimeOffset.FromUnixTimeSeconds(time);
            DateTimeOffset dayIt = now.AddDays(space);
            return new DateTimeOffset(now.Year, now.Month, dayIt.Day, 0, 0, 0, TimeSpan.Zero);
        }
        public static long GetLongTimeByDay(long time, int space)
        {
            DateTimeOffset now = GetDateTimeTimeByDay(time, space);
            return now.ToUnixTimeSeconds();
        }
        public static long GetLongTimeFirstTimeOfCurrentMonth
        {
            get
            {
                return GetLongTimeDayInMonth(1);
            }
        }
        public static DateTimeOffset GetDateTimeLastDayOfCurrentMonth
        {
            get
            {
                // Lấy thời gian hiện tại theo UTC
                DateTimeOffset now = DateTimeOffset.FromUnixTimeSeconds(GetLongTimeCurrent);
                // Lấy ngày đầu tiên của tháng kế tiếp
                DateTimeOffset firstDayOfNextMonth = new DateTimeOffset(now.Year, now.Month, 1, 23, 59, 59, TimeSpan.Zero).AddMonths(1);
                // Trừ đi một ngày để có ngày cuối cùng của tháng hiện tại
                DateTimeOffset lastDayOfMonth = firstDayOfNextMonth.AddDays(-1);
                return lastDayOfMonth;
            }
        }
        public static long GetLongTimeLastDayOfCurrentMonth
        {
            get
            {
                DateTimeOffset dayOfMonth = GetDateTimeLastDayOfCurrentMonth;
                return dayOfMonth.ToUnixTimeSeconds();
            }
        }
        // Lấy ngày đầu tiên của tuần hiện tại (tính từ Thứ Hai)
        public static DateTimeOffset GetDateTimeFirstDayOfCurrentWeek
        {
            get
            {
                DateTimeOffset now = DateTimeOffset.FromUnixTimeSeconds(GetLongTimeCurrent);
                int diff = (int)now.DayOfWeek - 1; // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
                if (diff < 0) diff = 6; // Nếu là Chủ Nhật (Sunday) thì chuyển về 6
                DateTimeOffset firstDayOfWeek = now.AddDays(-diff);
                // Trả về ngày đầu tiên của tuần (giờ đặt về 00:00 UTC)
                var first = new DateTimeOffset(firstDayOfWeek.Date, TimeSpan.Zero); // TimeSpan.Zero để đảm bảo múi giờ UTC
                return first;
            }
        }
        /// <summary>
        /// Lấy thời gian của một ngày cụ thể trong tuần, 1 tuần từ thứ 2 - Chủ nhât
        /// </summary>
        /// <param name="dayOfWeek"></param>
        /// <returns></returns>
        public static DateTimeOffset GetDateTimeDayOfCurrentWeek(DayOfWeek dayOfWeek)
        {
            int diff = (int)dayOfWeek - 1;
            if (diff < 0)
            {
                diff = 6;
            }
            DateTimeOffset firstDayOfWeek = GetDateTimeFirstDayOfCurrentWeek;
            DateTimeOffset lastDayOfWeek = firstDayOfWeek.AddDays(diff); // Thêm 6 ngày vào ngày đầu tuần
            return lastDayOfWeek;
        }
        public static long GetLongTimeDayOfCurrentWeek(DayOfWeek dOW)
        {
            return GetDateTimeDayOfCurrentWeek(dOW).ToUnixTimeSeconds();
        }
        // Lấy thời gian Unix của ngày đầu tiên trong tuần hiện tại
        public static long GetLongTimeFirstDayOfCurrentWeek
        {
            get
            {
                return GetDateTimeFirstDayOfCurrentWeek.ToUnixTimeSeconds();
            }
        }
        // Lấy ngày cuối cùng của tuần hiện tại (tính là Chủ Nhật)
        public static DateTimeOffset GetDateTimeLastDayOfCurrentWeek
        {
            get
            {
                DateTimeOffset lastDayOfWeek = GetDateTimeDayOfCurrentWeek(DayOfWeek.Sunday);
                return lastDayOfWeek;
            }
        }
        // Lấy thời gian Unix của ngày cuối cùng trong tuần hiện tại
        public static long GetLongTimeLastDayOfCurrentWeek
        {
            get
            {
                return GetDateTimeLastDayOfCurrentWeek.ToUnixTimeSeconds();
            }
        }
        public static long GetLongTimeStartToday
        {
            get
            {
                return GetDateTimeStartToday.ToUnixTimeSeconds();
            }
        }
        public static DateTimeOffset GetDateTimeStartToday
        {
            get
            {
                return GetDateTimeStartDay(GetLongTimeCurrent);
            }
        }
        public static long GetLongTimeStartDay(long time)
        {
            return GetDateTimeStartDay(time).ToUnixTimeSeconds();
        }
        public static DateTimeOffset GetDateTimeStartDay(long time)
        {
            DateTimeOffset now = DateTimeOffset.FromUnixTimeSeconds(time);
            return new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        }
        public static long GetLongTimeStartTomorrow
        {
            get
            {
                return GetDateTimeStartTomorrow.ToUnixTimeSeconds();
            }
        }
        public static DateTimeOffset GetDateTimeStartTomorrow
        {
            get
            {
                DateTimeOffset now = DateTimeOffset.FromUnixTimeSeconds(GetLongTimeCurrent);
                now = now.AddDays(1);
                return new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
            }
        }
        /// <summary>
        /// Lấy thời gian của một ngày cụ thể trong tuần, 1 tuần từ thứ 2 - Chủ nhât
        /// </summary>
        /// <param name="targetDay"> thứ mấy</param>
        /// <param name="weeksAgo"> cách bao nhiêu tuần,  -1 là tuần trước, 1 là tuần sau </param>
        /// <returns></returns>
        public static DateTimeOffset GetDateTimeDayOfCustomWeeks(DayOfWeek targetDay, int weeksAgo)
        {
            // Lấy ngày đầu tiên của tuần hiện tại (thứ Hai)
            DateTimeOffset firstDayOfCurrentWeek = GetDateTimeFirstDayOfCurrentWeek;
            // Tính chênh lệch từ ngày đầu tuần đến targetDay
            int dayOffset = (int)targetDay - (int)DayOfWeek.Monday;
            if (dayOffset < 0)
            {
                dayOffset += 7; // Đảm bảo giá trị dương nếu targetDay là Chủ Nhật
            }
            // Lùi hoặc tiến lại số tuần theo yêu cầu
            DateTimeOffset targetDate = firstDayOfCurrentWeek.AddDays(dayOffset + (7 * weeksAgo));
            return targetDate;
        }
        public static int GetCurrentDayOfYear()
        {
            return DateTime.Now.DayOfYear;
        }
        #region Time And Ordinal To String
        // Chuyển đổi thời gian Unix (seconds) thành chuỗi định dạng đơn giản (long)
        public static string ToUnixTimeString(this long unixTimeSeconds, bool isDoubleParam = false, string day = "d", string hour = "h", string minutes = "m", string second = "s")
        {
            TimeSpan dateTime = TimeSpan.FromSeconds(unixTimeSeconds);
            return dateTime.ToFormattedTimeString(isDoubleParam, day, hour, minutes, second);
        }
        // Chuyển đổi thời gian Unix (seconds) thành chuỗi định dạng đơn giản (float)
        public static string ToUnixTimeString(this float unixTimeSeconds, bool isDoubleParam = false, string day = "d", string hour = "h", string minutes = "m", string second = "s")
        {
            TimeSpan dateTime = TimeSpan.FromSeconds(unixTimeSeconds);
            var strValue = dateTime.ToFormattedTimeString(isDoubleParam, day, hour, minutes, second);
            // Chỉ thêm milliseconds nếu có
            int milliseconds = dateTime.Milliseconds;
            return milliseconds > 0 ? $"{strValue}.{milliseconds:D3}" : strValue;
        }
        // Chuyển đổi TimeSpan thành chuỗi theo định dạng đơn giản
        private static string ToFormattedTimeString(this TimeSpan dateTime, bool isDoubleParam = false, string day = "d", string hour = "h", string minutes = "m", string second = "s")
        {
            var sb = new StringBuilder();
            // Tính toán days và hours
            if (dateTime.Days > 0)
            {
                sb.AppendFormat(isDoubleParam || dateTime.Hours > 0 ? $"{dateTime.Days:D2}{day}:{dateTime.Hours:D2}{hour}" : $"{dateTime.Days:D2}{day}");
            }
            else if (dateTime.Hours > 0)
            {
                sb.AppendFormat(isDoubleParam || dateTime.Minutes > 0 ? $"{dateTime.Hours:D2}{hour}:{dateTime.Minutes:D2}{minutes}" : $"{dateTime.Hours:D2}{hour}");
            }
            else
            {
                sb.AppendFormat(isDoubleParam || dateTime.Seconds > 0 ? $"{dateTime.Minutes:D2}{minutes}:{dateTime.Seconds:D2}{second}" : $"{dateTime.Minutes:D2}{minutes}");
            }
            return sb.ToString();
        }
        // Chuyển đổi số nguyên thành dạng thứ tự (ordinal)
        public static string ToOrdinal(this int number)
        {
            return $"{number}{number.GetOrdinalSuffix()}";
        }
        // Lấy hậu tố thứ tự (ordinal suffix)
        private static string GetOrdinalSuffix(this int number)
        {
            if (number <= 0) return "";
            int lastTwoDigits = number % 100;
            int lastDigit = number % 10;
            // Đặc biệt với số 11, 12, 13
            if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
            {
                return "th";
            }
            // Các số khác, chỉ cần kiểm tra chữ số cuối
            return lastDigit switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            };
        }
        #endregion
    }
}
