using System.Text;

namespace WhiteCrow.Services;
public class TextService : IChunkify
{
  public async Task<int> ChunkifyAsync(Stream input, Stream output, CancellationToken ct)
  {
    // Use a StreamReader for reading CSV lines from the input stream
    using var reader = new StreamReader(input, Encoding.UTF8);

    var lines = new List<string>();
    while (!reader.EndOfStream)
    {
      ct.ThrowIfCancellationRequested();

      // Read a line from the CSV
      var line = await reader.ReadLineAsync();
      if (line == null) continue;

      lines.Add(line);
    }

    // Ensure everything is flushed out to the output stream
    await output.WriteAsync(BinaryData.FromObjectAsJson(lines));
    output.Seek(0, SeekOrigin.Begin);

    return lines.Count;
  }
}
