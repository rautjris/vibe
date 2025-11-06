using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MySpeaker.Models;

public sealed class StreamInfo
{
    [JsonConstructor]
    public StreamInfo(Guid id, string name, string url, string? description = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = name;
        Url = url;
        Description = description;
    }

    public StreamInfo()
        : this(Guid.NewGuid(), string.Empty, string.Empty, null)
    {
    }

    public Guid Id { get; init; }

    [Required(ErrorMessage = "Please provide a name.")]
    [StringLength(80, ErrorMessage = "Name is too long (80 characters max).")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide a stream URL.")]
    [Url(ErrorMessage = "Enter a valid absolute URL.")]
    public string Url { get; set; } = string.Empty;

    [StringLength(240, ErrorMessage = "Description is too long (240 characters max).")]
    public string? Description { get; set; }

    public StreamInfo Clone() => new(Id, Name, Url, Description);
}
