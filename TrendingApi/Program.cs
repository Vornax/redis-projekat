using TrendingApi.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "Frontend")
});

builder.Services.AddSingleton<RedisService>();
builder.Services.AddSingleton<RateLimiterService>();
builder.Services.AddSingleton<UserService>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers(); 
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CORS", policy =>
    {
        policy.WithOrigins( new string[]{
            "http://localhost:5500",
            "https://localhost:5500",
            "http://127.0.0.1:5500",
            "https://127.0.0.1:5500"
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Serviruj static files iz Frontend foldera
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("CORS");
app.UseMiddleware<TrendingApi.Middleware.AuthorizationMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Fallback na index.html za SPA
app.MapFallbackToFile("index.html");

app.Run();


