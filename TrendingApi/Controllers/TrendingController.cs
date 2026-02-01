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

        [HttpGet("current")]
        public async Task<ActionResult<List<TrendingItem>>> GetCurrentTrending(
            [FromQuery] int top = 10,
            [FromQuery] int hours = 24)
        {
            // // Ograničavamo koliko rezultata vraćamo (max 50) i koliko sati unazad gledamo (max 24h)
            if (top < 1 || top > 50) top = 10;
            if (hours < 1 || hours > 24) hours = 24; 

            var db = _redis.Database;
            var keys = new List<RedisKey>();

            // hashtagove čuvamo po datumima (npr. trending:2026-01-20), moramo da odredimo koje sve datume obuhvata poslednjih N sati.
            var now = DateTime.UtcNow;
            var uniqueDates = new HashSet<string>(); // HashSet automatski sprečava duplikate
            
            for (int i = 0; i < hours; i++)
            {
                // Idemo unazad sat po sat i dodajemo datum u listu unikatnih datuma
                string date = now.AddHours(-i).ToString("yyyy-MM-dd");
                uniqueDates.Add(date);
            }

            // Proveravamo koji od tih datuma zaista postoje u Redisu kao ključevi.
            foreach (var date in uniqueDates.OrderByDescending(d => d))
            {
                string key = $"trending:{date}";
                if (await db.KeyExistsAsync(key))
                {
                    keys.Add(key);
                }
            }

            if (keys.Count == 0)
            {
                return Ok(new List<TrendingItem>());
            }

            // Pravimo privremeni ključ za rezultat spajanja
            string unionKey = $"trending:union:hours:{Guid.NewGuid():N}";
            // SortedSetCombineAndStoreAsync uzima Sorted Set-ove (za više dana), i spaja ih u jedan
            // Aggregate.Sum : "Ako se isti hashtag pojavi u dva dana, saberi mu rezultate".
            await db.SortedSetCombineAndStoreAsync(
                SetOperation.Union,
                unionKey,
                keys.ToArray(),
                weights: null,
                aggregate: Aggregate.Sum);

            // Iz (spojenog) seta izvlačimo najbolje rangirane
            var results = await db.SortedSetRangeByRankWithScoresAsync(unionKey, 0, top - 1, Order.Descending);

            // Pretvaramo Redis rezultate u našu C# listu TrendingItem
            var trending = new List<TrendingItem>();
            foreach (var entry in results)
            {
                trending.Add(new TrendingItem
                {
                    Hashtag = (string)entry.Element,
                    Score = (long)entry.Score
                });
            }

            // Brišemo privremeni ključ iz Redisa da ne bismo trošili memoriju
            await db.KeyDeleteAsync(unionKey);

            return Ok(trending);
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