using System.Text.Json.Serialization;

namespace WhiteCrow.Models;

public class EnumeratorInput
{
  [JsonPropertyName("type")]
  public string Type { get; set; } = String.Empty;

  [JsonPropertyName("id")]
  public string Id { get; set; } = string.Empty;

  [JsonPropertyName("count")]
  public int Count { get; set; }

  [JsonPropertyName("instructions")]
  public string Instructions { get; set; } = string.Empty;

}