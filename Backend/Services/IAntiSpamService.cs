using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Backend.Services
{
    public interface IAntiSpamService
    {
        Task<bool> CanUserReview(int userId, int productId);
        Task<bool> IsSpamming(int userId);
        void RecordReviewAttempt(int userId);
        Task<TimeSpan?> GetRemainingCooldown(int userId, int productId);
        bool ContainsProfanity(string content);
        void BanUser(int userId, TimeSpan duration);
        bool IsUserBanned(int userId);
        TimeSpan? GetBanRemainingTime(int userId);
    }

    public class AntiSpamService : IAntiSpamService
    {
        private readonly ILogger<AntiSpamService> _logger;

        // Lưu thời gian review gần nhất của user cho mỗi sản phẩm
        private readonly ConcurrentDictionary<string, DateTime> _lastReviewTime = new();

        // Lưu số lần review trong khoảng thời gian ngắn
        private readonly ConcurrentDictionary<int, List<DateTime>> _reviewAttempts = new();

        // Lưu danh sách user bị ban
        private readonly ConcurrentDictionary<int, DateTime> _bannedUsers = new();

        // ⭐ Cấu hình mới theo yêu cầu
        private const int MIN_REVIEW_INTERVAL_MINUTES = 5; // Tối thiểu 5 phút giữa 2 review cho cùng sản phẩm
        private const int MAX_REVIEWS_PER_15_MINUTES = 30; // Tối đa 30 review/15 phút
        private const int MAX_REVIEWS_PER_HOUR = 100; // Tối đa 100 review/giờ
        private const int MAX_REVIEWS_PER_DAY = 1200; // Tối đa 1200 review/ngày
        private const int BAN_DURATION_MINUTES = 15; // Cấm 15 phút khi vi phạm

        // ⭐ Danh sách từ thô tục (có thể mở rộng)
        private static readonly HashSet<string> ProfanityWords = new(StringComparer.OrdinalIgnoreCase)
        {
            // Tiếng Việt
            "dm", "đm", "d m", "đ m",
            "cl", "c l", "clgt", "c l g t",
            "vl", "v l", "vlol", "v l o l",
            "dcm", "d c m", "đcm", "đ c m",
            "cc", "c c", "cặc", "buồi",
            "lồn", "l o n", "lon",
            "đéo", "deo", "đ e o",
            "địt", "dit", "đ i t",
            "đụ", "du", "đ u",
            "cứt", "cut", "c u t",
            "shit", "s h i t",
            "fuck", "f u c k", "f*ck", "fck",
            "bitch", "b i t c h",
            "ass", "a s s",
            "damn", "d a m n",
            "hell", "h e l l",
            "wtf", "w t f",
            "stfu", "s t f u",
            "motherfucker", "m f",
            "pussy", "dick", "cock",
            "bastard", "asshole",
            "cunt", "whore", "slut",
            
            // Thêm các biến thể
            "đ!t", "đ1t", "đ|t",
            "l0n", "l@n", "l0n",
            "clg", "clmm", "clmn",
            "vcl", "vkl", "vck",
            "ngu", "n g u", "ngu si", "ngụ",
            "chó", "ch o", "dog",
            "loz", "l o z", "lờ",
            "cặn bã", "rác rưởi", "đồ chó",
            "con chó", "thằng chó", "đồ ngu"
        };

        // ⭐ Regex pattern để bắt các biến thể
        private static readonly Regex ProfanityPattern = new(
            @"(d[\s\*\.\-_]*[m|ế]|" +
            @"c[\s\*\.\-_]*l[\s\*\.\-_]*g[\s\*\.\-_]*t|" +
            @"c[\s\*\.\-_]*l(?!\w)|" +
            @"v[\s\*\.\-_]*l[\s\*\.\-_]*o[\s\*\.\-_]*l|" +
            @"v[\s\*\.\-_]*l(?!\w)|" +
            @"đ[\s\*\.\-_]*[c|ế|é|e][\s\*\.\-_]*m|" +
            @"l[\s\*\.\-_]*[o|ồ|ô|0][\s\*\.\-_]*n|" +
            @"đ[\s\*\.\-_]*[i|ị|í][\s\*\.\-_]*t|" +
            @"f[\s\*\.\-_]*[u|ư][\s\*\.\-_]*c[\s\*\.\-_]*k|" +
            @"s[\s\*\.\-_]*h[\s\*\.\-_]*i[\s\*\.\-_]*t|" +
            @"b[\s\*\.\-_]*i[\s\*\.\-_]*t[\s\*\.\-_]*c[\s\*\.\-_]*h)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        public AntiSpamService(ILogger<AntiSpamService> logger)
        {
            _logger = logger;

            // Cleanup task chạy mỗi giờ
            Task.Run(async () => await CleanupOldRecords());
        }

        public Task<bool> CanUserReview(int userId, int productId)
        {
            // Kiểm tra user có bị ban không
            if (IsUserBanned(userId))
            {
                return Task.FromResult(false);
            }

            var key = $"{userId}_{productId}";

            if (_lastReviewTime.TryGetValue(key, out var lastTime))
            {
                var timeSinceLastReview = DateTime.UtcNow - lastTime;
                if (timeSinceLastReview.TotalMinutes < MIN_REVIEW_INTERVAL_MINUTES)
                {
                    _logger.LogWarning("User {UserId} đang spam review sản phẩm {ProductId}", userId, productId);
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }

        public Task<bool> IsSpamming(int userId)
        {
            // Kiểm tra user có bị ban không
            if (IsUserBanned(userId))
            {
                return Task.FromResult(true);
            }

            if (!_reviewAttempts.TryGetValue(userId, out var attempts))
            {
                return Task.FromResult(false);
            }

            var now = DateTime.UtcNow;

            // Xóa các attempt cũ hơn 24 giờ
            attempts.RemoveAll(t => (now - t).TotalHours > 24);

            // ⭐ 1. Kiểm tra số lượng review trong 15 phút
            var reviewsInLast15Minutes = attempts.Count(t => (now - t).TotalMinutes <= 15);
            if (reviewsInLast15Minutes >= MAX_REVIEWS_PER_15_MINUTES)
            {
                _logger.LogWarning("User {UserId} đã review {Count} lần trong 15 phút - BAN!",
                    userId, reviewsInLast15Minutes);
                BanUser(userId, TimeSpan.FromMinutes(BAN_DURATION_MINUTES));
                return Task.FromResult(true);
            }

            // ⭐ 2. Kiểm tra số lượng review trong 1 giờ
            var reviewsInLastHour = attempts.Count(t => (now - t).TotalHours <= 1);
            if (reviewsInLastHour >= MAX_REVIEWS_PER_HOUR)
            {
                _logger.LogWarning("User {UserId} đã review {Count} lần trong 1 giờ - BAN!",
                    userId, reviewsInLastHour);
                BanUser(userId, TimeSpan.FromMinutes(BAN_DURATION_MINUTES));
                return Task.FromResult(true);
            }

            // ⭐ 3. Kiểm tra số lượng review trong 24 giờ
            var reviewsInLastDay = attempts.Count;
            if (reviewsInLastDay >= MAX_REVIEWS_PER_DAY)
            {
                _logger.LogWarning("User {UserId} đã review {Count} lần trong 24 giờ - BAN!",
                    userId, reviewsInLastDay);
                BanUser(userId, TimeSpan.FromMinutes(BAN_DURATION_MINUTES));
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public void RecordReviewAttempt(int userId)
        {
            var now = DateTime.UtcNow;

            _reviewAttempts.AddOrUpdate(
                userId,
                new List<DateTime> { now },
                (key, existing) =>
                {
                    existing.Add(now);
                    return existing;
                });
        }

        public Task<TimeSpan?> GetRemainingCooldown(int userId, int productId)
        {
            var key = $"{userId}_{productId}";

            if (_lastReviewTime.TryGetValue(key, out var lastTime))
            {
                var timeSinceLastReview = DateTime.UtcNow - lastTime;
                var remainingTime = TimeSpan.FromMinutes(MIN_REVIEW_INTERVAL_MINUTES) - timeSinceLastReview;

                if (remainingTime.TotalSeconds > 0)
                {
                    return Task.FromResult<TimeSpan?>(remainingTime);
                }
            }

            return Task.FromResult<TimeSpan?>(null);
        }

        // ⭐ Kiểm tra từ thô tục
        public bool ContainsProfanity(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            var normalizedContent = content.ToLower()
                .Replace(" ", "")
                .Replace(".", "")
                .Replace("*", "")
                .Replace("-", "")
                .Replace("_", "");

            // 1. Kiểm tra từ khóa trực tiếp
            foreach (var word in ProfanityWords)
            {
                var normalizedWord = word.ToLower()
                    .Replace(" ", "")
                    .Replace(".", "")
                    .Replace("*", "")
                    .Replace("-", "")
                    .Replace("_", "");

                if (normalizedContent.Contains(normalizedWord))
                {
                    _logger.LogWarning("Phát hiện từ thô tục: {Word} trong nội dung: {Content}",
                        word, content);
                    return true;
                }
            }

            // 2. Kiểm tra bằng regex pattern
            if (ProfanityPattern.IsMatch(content))
            {
                _logger.LogWarning("Phát hiện từ thô tục (regex) trong nội dung: {Content}", content);
                return true;
            }

            return false;
        }

        // ⭐ Ban user
        public void BanUser(int userId, TimeSpan duration)
        {
            var banUntil = DateTime.UtcNow.Add(duration);
            _bannedUsers.AddOrUpdate(userId, banUntil, (key, existing) => banUntil);

            _logger.LogWarning("User {UserId} đã bị cấm đến {BanUntil}", userId, banUntil);
        }

        // ⭐ Kiểm tra user có bị ban không
        public bool IsUserBanned(int userId)
        {
            if (_bannedUsers.TryGetValue(userId, out var banUntil))
            {
                if (DateTime.UtcNow < banUntil)
                {
                    return true;
                }
                else
                {
                    // Hết thời gian ban, xóa khỏi danh sách
                    _bannedUsers.TryRemove(userId, out _);
                    return false;
                }
            }

            return false;
        }

        // ⭐ Lấy thời gian còn lại của ban
        public TimeSpan? GetBanRemainingTime(int userId)
        {
            if (_bannedUsers.TryGetValue(userId, out var banUntil))
            {
                var remaining = banUntil - DateTime.UtcNow;
                if (remaining.TotalSeconds > 0)
                {
                    return remaining;
                }
                else
                {
                    _bannedUsers.TryRemove(userId, out _);
                }
            }

            return null;
        }

        public void RecordSuccessfulReview(int userId, int productId)
        {
            var key = $"{userId}_{productId}";
            _lastReviewTime[key] = DateTime.UtcNow;
            RecordReviewAttempt(userId);
        }

        private async Task CleanupOldRecords()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30)); // Chạy mỗi 30 phút

                    var now = DateTime.UtcNow;

                    // Cleanup review attempts cũ hơn 24 giờ
                    foreach (var kvp in _reviewAttempts.ToArray())
                    {
                        kvp.Value.RemoveAll(t => (now - t).TotalHours > 24);

                        if (kvp.Value.Count == 0)
                        {
                            _reviewAttempts.TryRemove(kvp.Key, out _);
                        }
                    }

                    // Cleanup last review time cũ hơn 7 ngày
                    foreach (var kvp in _lastReviewTime.ToArray())
                    {
                        if ((now - kvp.Value).TotalDays > 7)
                        {
                            _lastReviewTime.TryRemove(kvp.Key, out _);
                        }
                    }

                    // Cleanup banned users đã hết thời gian ban
                    foreach (var kvp in _bannedUsers.ToArray())
                    {
                        if (now >= kvp.Value)
                        {
                            _bannedUsers.TryRemove(kvp.Key, out _);
                            _logger.LogInformation("User {UserId} đã được gỡ ban", kvp.Key);
                        }
                    }

                    _logger.LogInformation("Đã cleanup anti-spam records");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cleanup anti-spam records");
                }
            }
        }

        // ⭐ Lấy thống kê cho admin
        public object GetStatistics()
        {
            var now = DateTime.UtcNow;

            return new
            {
                TotalTrackedUsers = _reviewAttempts.Count,
                BannedUsers = _bannedUsers.Count,
                ActiveReviewsLast15Min = _reviewAttempts.Values
                    .Sum(attempts => attempts.Count(t => (now - t).TotalMinutes <= 15)),
                ActiveReviewsLastHour = _reviewAttempts.Values
                    .Sum(attempts => attempts.Count(t => (now - t).TotalHours <= 1)),
                ActiveReviewsLast24Hours = _reviewAttempts.Values
                    .Sum(attempts => attempts.Count(t => (now - t).TotalHours <= 24))
            };
        }
    }
}
