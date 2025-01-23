using System.Text.Json.Serialization;

namespace WhiteCrow.Models;

public class EnumeratorModel
{
  [JsonPropertyName("url")]
  public string Url { get; set; } = string.Empty;

}
