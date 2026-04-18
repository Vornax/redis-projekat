using StackExchange.Redis;
using TrendingApi.Services;
using TrendingApi.Models;

public class UserService
{
    private readonly IDatabase _db;
    private readonly RedisService _redis;

    public UserService(RedisService redis)
    {
        _db = redis.Database;
        _redis = redis;
    }

   public async Task CreateOrUpdateUserAsync(User user)
    {
        string key = $"user:{user.Username.ToLowerInvariant()}";

        await _db.HashSetAsync(key, new HashEntry[]
        {
            new("username", user.Username),
            new("role", user.Role)
        });
    }

    public async Task<User?> GetUserAsync(string username)
    {
        string key = $"user:{username.ToLowerInvariant()}";
        // Dohvati sve podatke o tom korisniku
        var hash = await _db.HashGetAllAsync(key);

        if (hash.Length == 0) return null;

        // iz tih podataka o korisniku izvuci polje "username" i "role"
        var usernameEntry = hash.FirstOrDefault(h => h.Name == "username");
        var roleEntry    = hash.FirstOrDefault(h => h.Name == "role");

        var user = new User
        {
            Username = hash.FirstOrDefault(h => h.Name == "username").Value.IsNullOrEmpty ? "" : (string)hash.FirstOrDefault(h => h.Name == "username").Value,
            Role = hash.FirstOrDefault(h => h.Name == "role").Value.IsNullOrEmpty    ? "user" : (string)hash.FirstOrDefault(h => h.Name == "role").Value,
        };

        return user;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = new List<User>();
        // da bi dobili spisak svih ključeva, mora da se "popne" na nivo servera i da se koristi metoda Keys
        var server = _redis.Connection.GetServer(_redis.Connection.GetEndPoints().FirstOrDefault());
        
        // Pronađi sve ključeve koji počinju sa "user:"
        var keys = server.Keys(pattern: "user:*");
        
        foreach (var key in keys)
        {
            var hash = await _db.HashGetAllAsync(key);
            if (hash.Length > 0)
            {
                var user = new User
                {
                    Username = hash.FirstOrDefault(h => h.Name == "username").Value.IsNullOrEmpty ? "" : (string)hash.FirstOrDefault(h => h.Name == "username").Value,
                    Role = hash.FirstOrDefault(h => h.Name == "role").Value.IsNullOrEmpty ? "user" : (string)hash.FirstOrDefault(h => h.Name == "role").Value,
                };
                
                if (!string.IsNullOrWhiteSpace(user.Username))
                {
                    users.Add(user);
                }
            }
        }
        
        return users.OrderBy(u => u.Username).ToList();
    }

    public async Task<bool> CanPostAsync(string username)
    {
        var user = await GetUserAsync(username);
        if (user == null) return true; // dozvoli kreiranje ako ne postoji
        return user.Role == "user" || user.Role == "admin";
    }
}