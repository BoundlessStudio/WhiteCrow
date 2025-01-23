namespace WhiteCrow.Services;

public interface IChunkify
{
  Task<int> ChunkifyAsync(Stream input, Stream output, CancellationToken cancellationToken = default);
}
