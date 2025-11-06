using System;

namespace MySpeaker.Models;

public sealed class SpeakerOptions
{
    public string BaseUrl { get; set; } = "http://192.168.1.50";

    public string StreamStorePath { get; set; } = "App_Data/streams.json";

    public int RequestTimeoutSeconds { get; set; } = 10;

    public bool UseMock { get; set; }
}
