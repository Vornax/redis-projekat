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
        private readonly IConnectionMultiplexer _redis;

        public EventsController(RedisService redisService)
        {
            _redis = redisService.Connection; 
        }

        [HttpGet]
        public async Task GetEvents()
        {
            Response.Headers.Append("Content-Type", "text/event-stream"); // stizaće podaci u naletima
            Response.Headers.Append("Cache-Control", "no-cache"); // nigde da snimamo ove podatke
            Response.Headers.Append("Connection", "keep-alive"); // veza treba da ostane otvorena

            var channel = "nove_poruke"; // isti kanal koji publish-uješ u POST i PUT

            var subscriber = _redis.GetSubscriber();

            var channelMessageQueue = Channel.CreateUnbounded<string>();

            // Subscribe na Redis Pub/Sub
            await subscriber.SubscribeAsync(channel, (ch, msg) =>
            {
                channelMessageQueue.Writer.TryWrite(msg.ToString());
            });

            try
            {
                // Heartbeat svakih 15 sekundi da konekcija ne istekne
                var cts = new CancellationTokenSource();
                var heartbeatTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Response.WriteAsync(": heartbeat\n\n");
                        await Response.Body.FlushAsync();
                        await Task.Delay(15000, cts.Token);
                    }
                }, cts.Token);

                await foreach (var message in channelMessageQueue.Reader.ReadAllAsync(HttpContext.RequestAborted))
                {
                    await Response.WriteAsync($"data: {message}\n\n");
                    await Response.Body.FlushAsync();
                }

                cts.Cancel();
            }
            finally
            {
                await subscriber.UnsubscribeAsync(channel);
            }
        }
    }
}