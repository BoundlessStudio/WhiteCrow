using System.Text.Json.Serialization;

namespace WhiteCrow.Models;

public class EnumeratorChunk
{
  [JsonPropertyName("input")]
  public string Input { get; set; } = String.Empty;

  [JsonPropertyName("output")]
  public string Output { get; set; } = String.Empty;


}