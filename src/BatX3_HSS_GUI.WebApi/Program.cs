using BatX3_HSS_GUI.Application;
using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Configuration.Input;
using BatX3_HSS_GUI.Infrastructure;
using BatX3_HSS_GUI.Infrastructure.Configuration;
using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.WebApi.Hubs;
using BatX3_HSS_GUI.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddSignalR();

NetworkSettings defaultNetworkSettings =
    builder.Configuration
        .GetSection(NetworkSettings.SectionName)
        .Get<NetworkSettings>()
    ?? new NetworkSettings();

InputSettings defaultInputSettings = new();

string userSettingsFilePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "BatX3_HSS_GUI", "WebApi", "appsettings.user.json");

JsonSettingsService settingsService = new(
    defaultNetworkSettings,
    defaultInputSettings,
    userSettingsFilePath);

builder.Services.AddSingleton<ISettingsService>(settingsService);

InputSettings effectiveInputSettings = settingsService.GetInputSettings();

GamepadSettingsStore gamepadSettingsStore = new(effectiveInputSettings.Gamepad);

builder.Services.AddSingleton<IGamepadSettingsStore>(gamepadSettingsStore);

builder.Services.AddSingleton<IGamepadAnalogMotionController, GamepadAnalogMotionController>();

builder.Services.AddHostedService<VideoFrameBroadcastService>();
builder.Services.AddHostedService<DetectionFrameBroadcastService>();
builder.Services.AddHostedService<OperationStatusBroadcastService>();
builder.Services.AddHostedService<DiagnosticsBroadcastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapHub<TurretHub>("/turretHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
