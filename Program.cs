using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TNO.mIRC;
using TNO.mIRC.Data;
using TNO.mIRC.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

Common.ircClient = new IrcClientService(
    host: "Envidia.local",       // IRC server hostname (your machine or a LAN DNS name)
    port: 6667,                  // Standard plaintext IRC port
    nick: "LoboForge",           // Your nickname on the server
    user: "mIRCUser",            // Username passed in the USER command
    dispatcher: Common.Dispatcher // Instance of your IrcCommandDispatcher
);
//irc.libera.chat
//palladium.libera.chat
//6697
//// Kick off the connection in the background
//_ = Task.Run(() => Common.ircClient.ConnectAsync(CancellationToken.None));
app.Run();
