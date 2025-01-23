using System.Text.Json.Serialization;

namespace WhiteCrow.Models;

public class ExtractorInputList
{
  [JsonPropertyName("input")]
  public string Input { get; set; } = string.Empty;
}
