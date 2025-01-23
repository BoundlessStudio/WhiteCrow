using System.Text.Json.Serialization;

namespace WhiteCrow.Models;

public class ExtractorModel
{
  [JsonPropertyName("type")]
  public string Type { get; set; } = String.Empty;

  [JsonPropertyName("id")]
  public string Id { get; set; } = string.Empty;

  [JsonPropertyName("name")]
  public string Name { get; set; } = string.Empty;

  [JsonPropertyName("url")]
  public string Url { get; set; } = string.Empty;

  [JsonPropertyName("count")]
  public int Count { get; set; }

  [JsonPropertyName("size")]
  public long Size { get; set; }

  
}
