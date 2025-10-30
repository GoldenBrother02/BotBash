using BotBash.Core;
using BotBash.Server;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR
builder.Services.AddSignalR();

builder.Services.AddSingleton<GameHub>();
builder.Services.AddSingleton<EngineManager>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true); //for dev only!
    });
});

var app = builder.Build();


app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapHub<GameHub>("/gamehub"); //Map the hub

app.Run();
