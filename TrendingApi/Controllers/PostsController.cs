using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using StackExchange.Redis;
using System.Text.RegularExpressions;
using TrendingApi.Models;
using TrendingApi.Services;

namespace TrendingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("CORS")]
    public class PostsController : ControllerBase
    {
        private readonly RedisService _redis;
        private readonly RateLimiterService _rateLimiter;

        private readonly UserService _userService;

        public PostsController(RedisService redis, RateLimiterService rateLimiter, UserService userService)
        {
            _redis = redis;
            _rateLimiter = rateLimiter;
            _userService = userService;
        }

        [HttpPost]
        public async Task<ActionResult<Post>> CreatePost([FromBody] Post request)
        {
            // --- KORAK 1: VALIDACIJA I RATE LIMIT ---
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { Success = false, Message = "Username i text su obavezni" });

            // Provera da li korisnik šalje previše zahteva (Rate Limiter)
            var (allowed, errorMsg) = _rateLimiter.CheckAndIncrement(request.Username);
            if (!allowed)
                return StatusCode(429, new { Success = false, Message = errorMsg });

            // --- KORAK 2: PRIPREMA PODATAKA ---
            var db = _redis.Database;
            
            // Generišemo novi jedinstveni ID povećavanjem globalnog brojača
            long postIdNumber = await db.StringIncrementAsync("global:next_post_id");
            string postKey = $"post:{postIdNumber}";

            // Izvlačimo sve hashtagove iz teksta (npr. #programiranje)
            // Regex gleda za # praćeno sa jednom ili više word karaktera (slova, brojeva, _)
            var hashtags = Regex.Matches(request.Text, @"#(\w+)", RegexOptions.IgnoreCase)
                            .Cast<Match>()
                            .Select(m => "#" + m.Groups[1].Value.ToLowerInvariant())
                            .Distinct()
                            .ToList();

            // Definišemo ključ za današnji trending (npr. trending:2026-01-20)
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string trendingKey = $"trending:{today}";

            // --- KORAK 3: TRANSAKCIONI UPIS U REDIS ---
             // Otvaramo transakciju da bi sve operacije prošle kao jedna
            var tran = db.CreateTransaction();

            // 1. Čuvamo ceo post kao HASH
            tran.HashSetAsync(postKey, new HashEntry[]
            {
                new("username", request.Username),
                new("text", request.Text),
                new("time", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            });

            // 2. Dodajemo ID posta u globalnu listu (Timeline) na prvo mesto
            tran.ListLeftPushAsync("global_timeline", postKey);

            // 3. Ažuriramo trending listu za svaki pronađeni hashtag
            foreach (var tag in hashtags)
            {
                tran.SortedSetIncrementAsync(trendingKey, tag, 1);
            }

            // 4. Pamtimo koji je ovo bio zadnji post za tog korisnika
            string userLastKey = $"last_post:user:{request.Username.ToLowerInvariant()}";
            tran.StringSetAsync(userLastKey, postKey);

            // 5. "Vičemo" na razglas (Pub/Sub) da je stigao novi post
            tran.PublishAsync("nove_poruke", $"Novi post: {postKey}");

            await tran.ExecuteAsync();

            // --- KORAK 4: ODGOVOR KLIJENTU ---
            var createdPost = new Post
            {
                Id = postKey,
                Username = request.Username,
                Text = request.Text,
                Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            return Ok(createdPost);
        }

        [HttpGet]
        public async Task<ActionResult<List<Post>>> GetPosts([FromQuery] int count = 20)
        {
            // Ograničavamo broj postova koje korisnik može da traži (između 1 i 100)
            if (count < 1 || count > 100) count = 20;

            var db = _redis.Database;

            // Iz Redis liste "global_timeline" uzimamo samo ID-jeve, 0 je početak, a count-1 je kraj opsega
            var postIds = await db.ListRangeAsync("global_timeline", 0, count - 1);

            if (postIds.Length == 0)
            {
                return Ok(new List<Post>());
            }
            
            // Lista u koju ćemo pakovati prave objekte postova
            var posts = new List<Post>();

            // Pošto u listi imamo samo ključeve (ID-jeve), moramo za svaki ID "skoknuti" u Redis po detalje
            foreach (var id in postIds)
            {
                var postKey = id.ToString(); 
                var hash = await db.HashGetAllAsync(postKey); // // Uzimamo ceo heš (username, text, time...)

                if (hash.Length == 0) continue;

                // Proveri da li je poruka obrisana (soft delete)
                var deletedEntry = hash.FirstOrDefault(h => h.Name == "deleted");
                bool isDeleted = !deletedEntry.Value.IsNullOrEmpty && (string)deletedEntry.Value == "true";

                // Sigurno dohvatanje Username
                var usernameEntry = hash.FirstOrDefault(h => h.Name == "username");
                string username = usernameEntry.Value.IsNullOrEmpty ? "" : (string)usernameEntry.Value;

                // Sigurno dohvatanje Time
                var timeEntry = hash.FirstOrDefault(h => h.Name == "time");
                long time = 0;
                if (!timeEntry.Value.IsNullOrEmpty && long.TryParse((string)timeEntry.Value, out long parsedTime))
                {
                    time = parsedTime;
                }

                // Logika za tekst: Ako je isDeleted true, sakrivamo originalni tekst
                string text;
                if (isDeleted)
                {
                    text = "[Poruka je obrisana]";
                }
                else
                {
                    var textEntry = hash.FirstOrDefault(h => h.Name == "text");
                    text = textEntry.Value.IsNullOrEmpty ? "" : (string)textEntry.Value;
                }

                // kreiranje objekta i dodavanje u listu
                var post = new Post
                {
                    Id = postKey,
                    Username = username,
                    Text = text,
                    Time = time
                };

                posts.Add(post);
            }

            return Ok(posts);
        }

        [HttpPut("last")]
        public async Task<ActionResult> EditLastPost([FromBody] Post request)
        {
            // Proveravamo da li smo dobili ko šalje i šta je novi tekst
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { Success = false, Message = "Username i novi text su obavezni" });

            var db = _redis.Database;

            // Tražimo u Redisu poseban ključ koji smo napravili pri kreiranju posta (kljuc poslejdnje poruke)
            string userLastKey = $"last_post:user:{request.Username.ToLowerInvariant()}";
            var lastPostId = await db.StringGetAsync(userLastKey);

            // Ako ključ ne postoji, znači da korisnik još ništa nije objavio
            if (lastPostId.IsNullOrEmpty)
                return NotFound(new { Success = false, Message = "Nemate nijednu poruku za izmenu" });

            string postKey = lastPostId.ToString();

            // Čak i ako imamo ID, proveravamo da li je taj post u međuvremenu obrisan iz baze.
            if (!await db.KeyExistsAsync(postKey))
                return NotFound(new { Success = false, Message = "Poslednja poruka više ne postoji" });

            // Menjamo samo polje "text"
            await db.HashSetAsync(postKey, "text", request.Text);

            // Ponovo koristimo Pub/Sub sistem da javimoo da je post izmenjen
            await db.PublishAsync("nove_poruke", $"Update post: {postKey}");

            return Ok(new 
            { 
                Success = true, 
                Message = "Poslednja poruka uspešno izmenjena",
                PostId = postKey,
                NewText = request.Text
            });
        }

        [HttpDelete("{postId}")]
        public async Task<ActionResult> DeletePost(string postId, [FromQuery] string username)
        {
            // Proveravamo da li su prosleđeni ID posta i korisničko ime.
            if (string.IsNullOrWhiteSpace(postId) || string.IsNullOrWhiteSpace(username))
                return BadRequest(new { Success = false, Message = "PostId i username su obavezni" });

            var db = _redis.Database;
            // Osiguravamo da ključ počinje sa "post:", čak i ako klijent pošalje samo broj.
            string postKey = postId.StartsWith("post:") ? postId : $"post:{postId}";

            // // Uzimamo sve podatke o postu iz Redisa
            var postHash = await db.HashGetAllAsync(postKey);
            if (postHash.Length == 0)
                return NotFound(new { Success = false, Message = "Poruka ne postoji" });

            // Proveravamo ko je vlasnik posta (polje "username" u hešu)
            var usernameEntry = postHash.FirstOrDefault(h => h.Name == "username");
            string postOwner = usernameEntry.Value.IsNullOrEmpty ? "" : (string)usernameEntry.Value;

            // Ako korisnik koji pokušava da obriše nije onaj koji je post napisao, vraćamo 403 Forbidden
            if (postOwner != username)
                return Forbid();

            // Soft delete - označi poruku kao obrisanu
            await db.HashSetAsync(postKey, "deleted", "true");

            // poruka više "ne postoji", moramo da poništimo njen uticaj na trending hashtagove
            var textEntry = postHash.FirstOrDefault(h => h.Name == "text");
            string text = textEntry.Value.IsNullOrEmpty ? "" : (string)textEntry.Value;

            // Pronađi i smanjim skore hashtagova
            var hashtags = Regex.Matches(text, @"#(\w+)", RegexOptions.IgnoreCase)
                            .Select(m => "#" + m.Groups[1].Value.ToLowerInvariant())
                            .Distinct()
                            .ToList();

            if (hashtags.Count > 0)
            {
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                string trendingKey = $"trending:{today}";

                foreach (var tag in hashtags)
                {
                    await db.SortedSetIncrementAsync(trendingKey, tag, -1);
                }
            }

            // Javljamo SSE klijentima da je poruka obrisana kako bi je uklonili sa ekrana
            await db.PublishAsync("nove_poruke", $"Poruka obrisana: {postKey}");

            return Ok(new
            {
                Success = true,
                Message = "Poruka uspešno obrisana"
            });
        }
    }
}