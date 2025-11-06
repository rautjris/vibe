using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySpeaker.Models;

namespace MySpeaker.Services;

public sealed class StreamStore : IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<StreamStore> _logger;
    private readonly string _storagePath;

    public StreamStore(IWebHostEnvironment environment, IOptions<SpeakerOptions> options, ILogger<StreamStore> logger)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        var configuredPath = options.Value.StreamStorePath;
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = "App_Data/streams.json";
        }

        _storagePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath.Replace('/', Path.DirectorySeparatorChar));

        var directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public async Task<IReadOnlyList<StreamInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var items = await ReadAsync(cancellationToken).ConfigureAwait(false);
            return items.OrderBy(static s => s.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<StreamInfo>> UpsertAsync(StreamInfo stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var items = await ReadAsync(cancellationToken).ConfigureAwait(false);
            var normalized = Normalize(stream);
            var existingIndex = items.FindIndex(s => s.Id == normalized.Id);
            if (existingIndex >= 0)
            {
                items[existingIndex] = normalized;
            }
            else
            {
                items.Add(normalized);
            }

            await WriteAsync(items, cancellationToken).ConfigureAwait(false);
            return items.OrderBy(static s => s.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<StreamInfo>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var items = await ReadAsync(cancellationToken).ConfigureAwait(false);
            var removed = items.RemoveAll(s => s.Id == id);
            if (removed > 0)
            {
                await WriteAsync(items, cancellationToken).ConfigureAwait(false);
            }

            return items.OrderBy(static s => s.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }

    private static StreamInfo Normalize(StreamInfo input)
    {
        var name = (input.Name ?? string.Empty).Trim();
        var url = (input.Url ?? string.Empty).Trim();
        var description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Stream name cannot be empty.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Stream URL must be an absolute URI.");
        }

        return new StreamInfo(input.Id == Guid.Empty ? Guid.NewGuid() : input.Id, name, url, description);
    }

    private async Task<List<StreamInfo>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storagePath))
        {
            _logger.LogInformation("Creating stream storage at {Path}", _storagePath);
            await WriteAsync(new List<StreamInfo>(), cancellationToken).ConfigureAwait(false);
            return new List<StreamInfo>();
        }

        await using var stream = File.Open(_storagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length == 0)
        {
            return new List<StreamInfo>();
        }

        try
        {
            var items = await JsonSerializer.DeserializeAsync<List<StreamInfo>>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
            return items ?? new List<StreamInfo>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to read stream storage. File will be reset.");
            await WriteAsync(new List<StreamInfo>(), cancellationToken).ConfigureAwait(false);
            return new List<StreamInfo>();
        }
    }

    private async Task WriteAsync(List<StreamInfo> items, CancellationToken cancellationToken)
    {
        await using var stream = File.Open(_storagePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, items, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }
}
