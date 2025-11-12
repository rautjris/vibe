using System;
using System.Threading;
using System.Threading.Tasks;
using MySpeaker.Models;

namespace MySpeaker.Services;

public sealed class PlayerStatusCache
{
    private PlayerStatus? _current;
    private DateTimeOffset _lastUpdated;
    private readonly object _gate = new();

    public PlayerStatus? Current
    {
        get { lock (_gate) { return _current; } }
    }

    public DateTimeOffset LastUpdated
    {
        get { lock (_gate) { return _lastUpdated; } }
    }

    public void Set(PlayerStatus? status)
    {
        lock (_gate)
        {
            _current = status;
            _lastUpdated = DateTimeOffset.UtcNow;
        }
    }
}
