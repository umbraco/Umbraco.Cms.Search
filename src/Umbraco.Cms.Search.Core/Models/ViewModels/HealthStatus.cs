using System.Text.Json.Serialization;

namespace Umbraco.Cms.Search.Core.Models.ViewModels;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HealthStatus
{
    Healthy,
    Rebuilding,
    Corrupted,
    Empty,
    Unknown,
}
