using System.Net.WebSockets;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebSocketServer.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IWebSocketServerConnectionManager, WebSocketServerConnectionManager>();
builder.Services.AddTransient<WebSocketServerMiddleware>();

var app = builder.Build();

app.UseWebSockets();

app.UseWhen(context => context.WebSockets.IsWebSocketRequest,
    app =>
    {
        app.UseMiddleware<WebSocketServerMiddleware>();
    }
);

app.Use(async (context, next) =>
{
    Console.WriteLine("Hello from the 2nd request delegate.");
    await next(context);
});

//app.UseMiddleware<WebSocketServerMiddleware>();
//app.UseWebSocketServer();//Extension method implementation

app.Run(async context =>
{
    Console.WriteLine("Hello from the 3rd request delegate.");
    await context.Response.WriteAsync("Hello from the 3rd request delegate.");
});

app.MapGet("/", () => "Hello World!");

app.Run();


