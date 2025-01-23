namespace WhiteCrow.Services;

public class DefaultService : IChunkify
{
  public async Task<int> ChunkifyAsync(Stream input, Stream output, CancellationToken ct)
  {
    var collection = new List<string>();
    await output.WriteAsync(BinaryData.FromObjectAsJson(collection));
    output.Seek(0, SeekOrigin.Begin);
    return 0;
  }
}