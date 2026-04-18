using TrendingApi.Services;
using Microsoft.OpenApi.Models;


// ------------
// Faza 1: Inicjalizacja i konfiguracja aplikacji
//-------------
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "Frontend")
});


// ------------
// Faza 2: Rejestracja servisa
//-------------
builder.Services.AddSingleton<RedisService>();
builder.Services.AddSingleton<RateLimiterService>();
builder.Services.AddSingleton<UserService>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Enter the API key value (no 'Bearer ' prefix)."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" },
                In = ParameterLocation.Header,
                Name = "Authorization"
            },
            new string[] { }
        }
    });
});
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

// ------------
// Faza 3: prawienie aplikacji
//-------------
var app = builder.Build();

// ------------
// Faza 4: Konfiguracja middleware i pravila puta
//-------------
app.UseDefaultFiles();
app.UseStaticFiles();

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

app.MapFallbackToFile("index.html");

// ------------
// Faza 5: pokretanje aplikacije 
//-------------
app.Run();


