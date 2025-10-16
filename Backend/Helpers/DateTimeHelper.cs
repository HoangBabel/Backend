namespace Backend.Helpers
{
    public class DateTimeHelper
    {
        public static TimeZoneInfo GetVietNamTz()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); // Linux, Mac
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Windows
            }
        }

        public static DateTime ToLocal(DateTime utcOrLocal, TimeZoneInfo tz)
        {
            if (utcOrLocal.Kind == DateTimeKind.Local)
                return TimeZoneInfo.ConvertTime(utcOrLocal, tz);

            if (utcOrLocal.Kind == DateTimeKind.Utc)
                return TimeZoneInfo.ConvertTimeFromUtc(utcOrLocal, tz);

            // Unspecified → xem như UTC
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcOrLocal, DateTimeKind.Utc), tz);
        }

        public static DateTime LocalDateOnly(DateTime dt)
        {
            return dt.Date; // Cắt phần giờ
        }
    }

}
