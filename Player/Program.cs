using Microsoft.Extensions.Options;
using MudBlazor.Services;
using MySpeaker.Components;
using MySpeaker.Models;
using MySpeaker.Services;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMudServices();

// Strongly typed + validated options
builder.Services.AddOptions<SpeakerOptions>()
    .Bind(builder.Configuration.GetSection("Speaker"))
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "Speaker BaseUrl must be a valid absolute URI")
    .Validate(o => o.RequestTimeoutSeconds > 0 && o.RequestTimeoutSeconds <= 120, "RequestTimeoutSeconds must be between 1 and 120")
    .ValidateOnStart();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HTTP resilience policies
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .Or<TaskCanceledException>()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));

var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));

builder.Services.AddHttpClient<SpeakerApiClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<SpeakerOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/", UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.RequestTimeoutSeconds));
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(timeoutPolicy);

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

// Background status cache
builder.Services.AddSingleton<PlayerStatusCache>();
builder.Services.AddHostedService<PlayerStatusPollingService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<SpeakerHealthCheck>("speaker")
    .AddCheck<StreamStoreHealthCheck>("streamstore");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapHealthChecks("/healthz");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
