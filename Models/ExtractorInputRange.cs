using System.Text.Json.Serialization;

namespace WhiteCrow.Models;

public class ExtractorInputRange
{
  [JsonPropertyName("max")]
  public int Max { get; set; }
}
