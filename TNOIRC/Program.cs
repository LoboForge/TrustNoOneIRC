using BotScripts;
using ElectronNET.API;
using ElectronNET.API.Entities;
using LoboForge.TNOIRC;
using LoboForge.TNOIRC.Data;
using LoboForge.TNOIRC.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.Connections;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseElectron(args);
builder.WebHost.UseEnvironment("Development");

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<ToxicService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub(options =>
{
    options.Transports = HttpTransportType.WebSockets;
});
app.MapFallbackToPage("/_Host");

ConfigService.Load();

if (HybridSupport.IsElectronActive)
{
    Task.Run(async () =>
    {
        var window = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
        {
            Width = 1280,
            Height = 800,
            Show = true,
            Icon = "/icon.ico"
        });

        window.OnClosed += () =>
        {
            Electron.App.Quit();
        };
    });
}

await app.RunAsync();
