using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using TrendingApi.Services;
using StackExchange.Redis;
using System.Threading.Channels;

namespace TrendingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("CORS")]
    public class EventsController : ControllerBase
    {
        private readonly IConnectionMultiplexer _redis; // konekcija ka Redis serveru

        public EventsController(RedisService redisService)
        {
            _redis = redisService.Connection; 
        }

        [HttpGet]
        public async Task GetEvents() // cev koji će primati podatke sa Redis Pub/Sub i slati ih klijentu(browser) u realnom vremenu
        {
            Response.Headers.Append("Content-Type", "text/event-stream"); // stizaće podaci u naletima
            Response.Headers.Append("Cache-Control", "no-cache"); // nigde da snimamo ove podatke
            Response.Headers.Append("Connection", "keep-alive"); // veza treba da ostane otvorena

            var channel = "nove_poruke"; // naziv Redis kanala na koji ćemo se pretplatiti

            var subscriber = _redis.GetSubscriber();

            // "cekaonica"(buffer) cuva poruke koje stizu sa Redis pre nego sto ih posaljemo klijentu(browseru)
            var channelMessageQueue = Channel.CreateUnbounded<string>();

            // Pretplatimo se na Redis kanal i svaki put kad stigne nova poruka, stavimo je u "cekaonicu"
            await subscriber.SubscribeAsync(channel, (ch, msg) =>
            {
                channelMessageQueue.Writer.TryWrite(msg.ToString());
            });

            try
            {
                var cts = new CancellationTokenSource();

                // svakih 15 sekundi salje poruku klijentu da konekcija ne istekne
                var heartbeatTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Response.WriteAsync(": heartbeat\n\n");
                        await Response.Body.FlushAsync();
                        await Task.Delay(15000, cts.Token);
                    }
                }, cts.Token);

                // Čitamo poruke iz "cekaonice" i šaljemo ih klijentu u realnom vremenu
                await foreach (var message in channelMessageQueue.Reader.ReadAllAsync(HttpContext.RequestAborted))
                {
                    await Response.WriteAsync($"data: {message}\n\n");
                    await Response.Body.FlushAsync();
                }

                cts.Cancel();
            }
            finally
            {
                // Kada klijent prekine konekciju, otkačemo se sa Redis kanala
                await subscriber.UnsubscribeAsync(channel);
            }
        }
    }
}