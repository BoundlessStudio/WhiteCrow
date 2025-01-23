using System.Text;
using Markdig;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;

namespace WhiteCrow.Services;

public class MarkdownService : IChunkify
{
  public async Task<int> ChunkifyAsync(Stream input, Stream output, CancellationToken ct)
  {
    // Read the entire input stream into a string
    string markdown;
    using (var reader = new StreamReader(input, Encoding.UTF8, leaveOpen: true))
    {
      markdown = await reader.ReadToEndAsync();
    }

    // Parse the markdown
    var pipeline = new MarkdownPipelineBuilder().Build();
    MarkdownDocument document = Markdown.Parse(markdown, pipeline);

    var renderedBlocks = new List<string>();

    // Loop through each block in the parsed document
    foreach (var block in document)
    {
      ct.ThrowIfCancellationRequested();

      using var writer = new StringWriter();
      var renderer = new NormalizeRenderer(writer);
      pipeline.Setup(renderer);
      renderer.Render(block);
      renderedBlocks.Add(writer.ToString());
    }

    // Write the list of rendered blocks as JSON to the output
    await output.WriteAsync(BinaryData.FromObjectAsJson(renderedBlocks));
    output.Seek(0, SeekOrigin.Begin);

    return renderedBlocks.Count;
  }
}
