using StackExchange.Redis;

namespace TrendingApi.Services
{
    public class RateLimiterService
    {
        private readonly IDatabase _db; // Redis baza za čuvanje broja zahteva po korisniku
        private const int MaxRequestsPerMinute = 3;
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

            // da bi se osiguralo da niko drugi ne promeni vrednost dok se čita
            var tran = _db.CreateTransaction();

            var current = tran.StringGetAsync(key);
            tran.Execute(); // sinhrono izvrši da dobijemo vrednost

            // Ako je rezultat null, tretira se kao 0, ako postoji koristi se ta vrednost
            long count = current.Result.IsNull ? 0 : (long)current.Result;

            if (count >= MaxRequestsPerMinute)
            {
                return (false, "Smanjite doživljaj, sačekajte malo");
            }

            // Povećaj i postavi expire samo ako je ovo prva u prozoru
            tran = _db.CreateTransaction();
            tran.StringIncrementAsync(key); // povećava broj zahteva za 1
            if (count == 0)
            {
                tran.KeyExpireAsync(key, TimeSpan.FromSeconds(WindowSeconds)); // postavlja expire samo ako je ovo prvi zahtev u prozoru
            }
            tran.Execute();

            return (true, null);
        }
    }
}