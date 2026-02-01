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
            var redisConnectionString = configuration.GetConnectionString("Redis")
                ?? "localhost:6379";  

            _redis = ConnectionMultiplexer.Connect(redisConnectionString);
            Database = _redis.GetDatabase();
        }

        public void Dispose()
        {
            _redis?.Dispose();
        }

        // Kasnije ćemo dodavati metode ovde: Publish, GetLastPost itd.
    }
}