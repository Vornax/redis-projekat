using StackExchange.Redis;

namespace TrendingApi.Services
{
    public class RateLimiterService
    {
        private readonly IDatabase _db;
        private const int MaxRequestsPerMinute = 5;
        private const int WindowSeconds = 60;

        public RateLimiterService(RedisService redisService)
        {
            _db = redisService.Database;
        }

        public (bool Allowed, string Message) CheckAndIncrement(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Korisničko ime je obavezno");

            var key = $"limiter:user:{username.ToLowerInvariant()}";

            // Koristimo transakciju da izbegnemo race conditions
            var tran = _db.CreateTransaction();

            var current = tran.StringGetAsync(key);
            tran.Execute(); // sinhrono izvrši da dobijemo vrednost

            long count = current.Result.IsNull ? 0 : (long)current.Result;

            if (count >= MaxRequestsPerMinute)
            {
                return (false, "Smanjite doživljaj, sačekajte malo");
            }

            // Povećaj i postavi expire samo ako je ovo prva u prozoru
            tran = _db.CreateTransaction();
            tran.StringIncrementAsync(key);
            if (count == 0)
            {
                tran.KeyExpireAsync(key, TimeSpan.FromSeconds(WindowSeconds));
            }
            tran.Execute();

            return (true, null);
        }
    }
}