using Backend.Extenstion;
using Backend.Middlewares;
using Backend.Persistence;
using Backend.Utiliy;
using DotNetEnv;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 🔐 LOAD .env (ONLY IN DEVELOPMENT)
// .env is located in ROOT (Backend/.env)
// =====================================================

// LOAD ENV FILE
Env.Load(Path.Combine(builder.Environment.ContentRootPath, ".env.local"));
// read port from env
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

if (!string.IsNullOrEmpty(urls))
{
    builder.WebHost.UseUrls(urls);
}

Console.WriteLine("JWT_KEY: " + Environment.GetEnvironmentVariable("JWT_KEY"));

// =====================================================
// 1️⃣ SERVICES (DB, Identity, JWT, App, Infra)
// =====================================================
builder.Services.AddWebApiServices(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });

//// =====================================================
//// 2️⃣ CORS
//// =====================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("KahiyeApp", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost",
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1",
                "https://www.adnankahiye.com",
                "https://adnankahiye.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


// =====================================================
// 3️⃣ BUILD APP
// =====================================================
var app = builder.Build();



app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// =====================================================
// 🔥 AUTO APPLY EF MIGRATIONS (SAFE)
// =====================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database migration failed");
    }
}

// =====================================================
// 4️⃣ GLOBAL EXCEPTION HANDLER
// =====================================================
app.UseGlobalExceptionHandler();

// =====================================================
// 5️⃣ SWAGGER (DEV ONLY)
// =====================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// =====================================================
// 6️⃣ HTTPS (OPTIONAL – DISABLED FOR DOCKER)
// =====================================================
// if (!app.Environment.IsDevelopment())
// {
//     app.UseHttpsRedirection();
// }







// =====================================================
// 7️⃣ ROUTING + CORS
// =====================================================
app.UseRouting();
app.UseCors("KahiyeApp");

// =====================================================
// 8️⃣ ALLOW PREFLIGHT (OPTIONS)
// =====================================================
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    await next();
});

// =====================================================
// 9️⃣ AUTH
// =====================================================
app.UseAuthentication();
app.UseAuthorization();

// =====================================================
// 🔟 STATIC FILES (OPTIONAL)
// =====================================================
app.UseDefaultFiles();
app.UseStaticFiles();

// =====================================================
// 1️⃣1️⃣ ENDPOINTS
// =====================================================
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();
app.MapFallbackToFile("index.html");

// =====================================================
// 1️⃣2️⃣ IDENTITY SEEDER (DEV ONLY)
// =====================================================
if (app.Environment.IsDevelopment()|| app.Environment.IsProduction())
{
    await app.UseIdentitySeederAsync();
}


// =====================================================
// 1️⃣3️⃣ RUN
// =====================================================
app.Run();

