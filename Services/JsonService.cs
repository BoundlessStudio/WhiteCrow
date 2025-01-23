using System.Text.Json;

namespace WhiteCrow.Services;

public class JsonService : IChunkify
{
  public async Task<int> ChunkifyAsync(Stream input, Stream output, CancellationToken ct)
  {
    // Parse the JSON in memory. If your JSON is very large and you need
    // streaming/buffered parsing, you’ll need a different approach (JsonDocument
    // can handle fairly large documents, but it’s still an in-memory model).
    using var doc = await JsonDocument.ParseAsync(input);

    JsonElement arrayElement;

    // 1) If root is already an array, just use it.
    if (doc.RootElement.ValueKind == JsonValueKind.Array)
    {
      arrayElement = doc.RootElement;
    }
    // 2) If root is an object, find the property with the largest array.
    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
    {
      int largestArraySize = -1;
      JsonElement largestArrayElement = default;

      foreach (JsonProperty property in doc.RootElement.EnumerateObject())
      {
        ct.ThrowIfCancellationRequested();

        // Check if this property is an array.
        if (property.Value.ValueKind == JsonValueKind.Array)
        {
          int length = property.Value.GetArrayLength();
          if (length > largestArraySize)
          {
            largestArraySize = length;
            largestArrayElement = property.Value;
          }
        }
      }

      // If we found any array property at all, set arrayElement to it. 
      // Otherwise, there’s nothing to process.
      if (largestArraySize > -1)
      {
        arrayElement = largestArrayElement;
      }
      else
      {
        // No array found in the object, do nothing or handle accordingly.
        return 0;
      }
    }
    else
    {
      // Root is neither an array nor an object => nothing to do.
      return 0;
    }

    // 3) Now that we have our array, convert each element in that array to a string.
    var listOfStrings = new List<string>();
    foreach (JsonElement element in arrayElement.EnumerateArray())
    {
      ct.ThrowIfCancellationRequested();

      // GetRawText() returns the element’s original JSON string (including quotes if it’s a string).
      // If you need a more human-friendly representation, you can parse & re-serialize,
      // or tailor it otherwise.
      listOfStrings.Add(element.GetRawText());
    }

    // 4) Write the entire list of JSON strings back to the output stream as JSON
    await output.WriteAsync(BinaryData.FromObjectAsJson(listOfStrings));
    output.Seek(0, SeekOrigin.Begin);

    return listOfStrings.Count;
  }
}
