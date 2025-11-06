using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using MySpeaker.Components;
using MySpeaker.Models;
using MySpeaker.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

builder.Services.Configure<SpeakerOptions>(builder.Configuration.GetSection("Speaker"));

builder.Services.AddHttpClient<SpeakerApiClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<SpeakerOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException("Speaker BaseUrl configuration is missing.");
    }

    client.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/", UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.RequestTimeoutSeconds));
});

builder.Services.AddSingleton<MockSpeakerApi>();

builder.Services.AddSingleton<ISpeakerApi>(sp =>
{
    var options = sp.GetRequiredService<IOptions<SpeakerOptions>>().Value;
    if (options.UseMock)
    {
        return sp.GetRequiredService<MockSpeakerApi>();
    }

    return sp.GetRequiredService<SpeakerApiClient>();
});

builder.Services.AddSingleton<StreamStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
