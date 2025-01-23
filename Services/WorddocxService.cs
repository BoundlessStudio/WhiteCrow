using NPOI.XWPF.UserModel;
using System.Text.Json;

namespace WhiteCrow.Services;

public class WorddocxService : IChunkify
{
  public async Task<int> ChunkifyAsync(Stream input, Stream output, CancellationToken ct)
  {
    // Open the .docx file with NPOI
    using var docx = new XWPFDocument(input);

    // We'll store each "block" of text in a list of strings.
    // In this example, each paragraph = one block. 
    // If you want to handle tables too, see the "Optional: Reading Tables" section below.
    var blocks = new List<string>();

    // XWPFDocument.BodyElements: A list of paragraphs, tables, etc.
    foreach (var element in docx.BodyElements)
    {
      ct.ThrowIfCancellationRequested();

      if (element is XWPFParagraph paragraph)
      {
        string paragraphText = paragraph.ParagraphText?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(paragraphText))
        {
          blocks.Add(paragraphText);
        }
      }
      else if (element is XWPFTable table)
      {
        string tableText = ExtractTableText(table);
        if (!string.IsNullOrWhiteSpace(tableText))
          blocks.Add(tableText);
      }
    }

    await output.WriteAsync(BinaryData.FromObjectAsJson(blocks));
    output.Seek(0, SeekOrigin.Begin);

    return blocks.Count;
  }

  private string ExtractTableText(XWPFTable table)
  {
    // Combine all cells in the table into one big string
    var lines = new List<string>();
    foreach (var row in table.Rows)
    {
      var cellTexts = new List<string>();
      foreach (var cell in row.GetTableCells())
      {
        // Each cell may contain paragraphs
        var cellParagraphs = cell.Paragraphs;
        var cellContent = new List<string>();
        foreach (var p in cellParagraphs)
        {
          if (!string.IsNullOrWhiteSpace(p.ParagraphText))
            cellContent.Add(p.ParagraphText.Trim());
        }
        cellTexts.Add(string.Join(" ", cellContent));
      }
      lines.Add(string.Join(" | ", cellTexts));
    }
    return string.Join(Environment.NewLine, lines);
  }
}
