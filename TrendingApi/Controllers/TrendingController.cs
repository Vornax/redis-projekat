using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using StackExchange.Redis;
using TrendingApi.Services;  
using TrendingApi.Models;

namespace TrendPulse.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("CORS")]
    public class TrendingController : ControllerBase
    {
        private readonly RedisService _redis;
        private readonly UserService _userService; 

        public TrendingController(RedisService redis, UserService userService)
        {
            _redis = redis;
            _userService = userService;  
        }

        [HttpGet("period")]
        public async Task<ActionResult<List<TrendingItem>>> GetTrendingPeriod(
            [FromQuery] int days = 3,
            [FromQuery] int top = 10,
            [FromQuery] string username = "")  
        {
            // Prvo proveravamo da li je username poslat
            if (string.IsNullOrEmpty(username))
                return Unauthorized("Potreban je username za pristup trending dashboard-u");
            // Koristimo GetUserAsync da proverimo ulogu korisnika u bazi
            var user = await _userService.GetUserAsync(username);
            // Ako korisnik ne postoji ili nema ulogu "admin", pristup se odbija
            if (user == null || user.Role != "admin")
                return Unauthorized("Samo admin može pristupiti trending dashboard-u");

            // Ograničavamo period na maksimalno 30 dana i prikaz na maksimalno 50 rezultata
            if (days < 1 || days > 30) days = 3;
            if (top < 1 || top > 50) top = 10;

            var db = _redis.Database;

            var keys = new List<RedisKey>();
            for (int i = 0; i < days; i++)
            {
                // Generišemo datume unazad
                string date = DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd");
                string key = $"trending:{date}";
                // Dodajemo u listu samo one ključeve koji zaista postoje u Redisu
                if (await db.KeyExistsAsync(key))
                {
                    keys.Add(key);
                }
            }

            if (keys.Count == 0)
                return Ok(new List<TrendingItem>());

            // Pravimo privremeni ključ za čuvanje rezultata spajanja
            string unionKey = $"trending:union:{Guid.NewGuid():N}";

            // Spajamo sve dnevne setove u jedan veliki set
            await db.SortedSetCombineAndStoreAsync(
                SetOperation.Union,
                unionKey,
                keys.ToArray(),
                weights: null,
                aggregate: Aggregate.Sum);

            // Uzimamo najbolje rangirane elemente iz privremenog seta
            var results = await db.SortedSetRangeByRankWithScoresAsync(unionKey, 0, top - 1, Order.Descending);

            var trending = new List<TrendingItem>();
            foreach (var entry in results)
            {
                trending.Add(new TrendingItem
                {
                    Hashtag = (string)entry.Element,
                    Score = (long)entry.Score
                });
            }

            await db.KeyDeleteAsync(unionKey);

            return Ok(trending);
        }
    }
}