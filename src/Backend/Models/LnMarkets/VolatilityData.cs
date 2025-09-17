using System.Text.Json.Serialization;

namespace AutoBot.Models.LnMarkets;

public class VolatilityData
{
    [JsonPropertyName("pair")]
    public required string Pair { get; set; }

    [JsonPropertyName("volatilityIndex")]
    public decimal VolatilityIndex { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }
}