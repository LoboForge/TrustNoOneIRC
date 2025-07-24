using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.Connections;
using TNO.mIRC;
using TNO.mIRC.Data;
using TNO.mIRC.Services;

var builder = WebApplication.CreateBuilder(args);

// Electron.NET bootstrapping
builder.WebHost.UseElectron(args);
builder.WebHost.UseEnvironment("Development");

// Blazor services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// Error handling
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

// Set up IRC client
Common.ircClient = new IrcClientService(
    host: "Envidia.local",
    port: 6667,
    nick: "LoboForge",
    user: "mIRCUser",
    dispatcher: Common.Dispatcher
);
ConfigService.Load();
// Start Electron app and create window
if (HybridSupport.IsElectronActive)
{
    Task.Run(async () =>
    {
        var window = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
        {
            Width = 1280,
            Height = 800,
            Show = true
        });

        window.OnClosed += () =>
        {
            Electron.App.Quit();
        };
    });
}

await app.RunAsync();
