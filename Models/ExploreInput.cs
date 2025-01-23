using System.Text.Json.Serialization;

namespace WhiteCrow.Models;

public class ExploreInput
{
  [JsonPropertyName("url")]
  public string Url { get; set; } = string.Empty;

  [JsonPropertyName("proxy")]
  public int Proxy { get; set; }

  [JsonPropertyName("javascript")]
  public int Javascript { get; set; }

  [JsonPropertyName("extract")]
  public int Extract { get; set; }

  [JsonPropertyName("template")]
  public int Template { get; set; }

  [JsonPropertyName("query")]
  public string Query { get; set; } = string.Empty;
}
