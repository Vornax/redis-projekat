using StackExchange.Redis;
using System;

namespace TrendingApi.Services
{
    public class RedisService : IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        public IConnectionMultiplexer Connection => _redis;
        public IDatabase Database { get; }

        public RedisService(IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis") ?? "redis:6379";
            var redisPassword = configuration["REDIS_PASSWORD"];

            var options = ConfigurationOptions.Parse(redisConnectionString);
            if (!string.IsNullOrEmpty(redisPassword))
            {
                options.Password = redisPassword;
            }
            options.AbortOnConnectFail = false;

            const int maxAttempts = 8;
            var delay = TimeSpan.FromSeconds(1);
            Exception lastEx = null!;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _redis = ConnectionMultiplexer.Connect(options);
                    Database = _redis.GetDatabase();
                    lastEx = null!;
                    break;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    System.Threading.Thread.Sleep(delay);
                    delay = TimeSpan.FromSeconds(Math.Min(10, delay.TotalSeconds * 2));
                }
            }

            if (lastEx != null)
            {
                throw new InvalidOperationException($"Unable to connect to Redis at '{redisConnectionString}' after retries.", lastEx);
            }
        }

        public void Dispose()
        {
            _redis?.Dispose();
        }

    }
}