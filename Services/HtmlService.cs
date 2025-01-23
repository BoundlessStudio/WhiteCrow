using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Markdig;
using ReverseMarkdown;

namespace WhiteCrow.Services;

public class HtmlService : IChunkify
{
  public async Task<int> ChunkifyAsync(Stream input, Stream output, CancellationToken ct)
  {
    // 1) Read the HTML from the input stream
    using var reader = new StreamReader(input);
    var html = await reader.ReadToEndAsync();

    // 2) Convert HTML to Markdown
    var converter = new Converter();
    var markdown = converter.Convert(html);

    // 3) Write the Markdown to the output stream
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