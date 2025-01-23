using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace WhiteCrow.Services
{
  public class PdfService : IChunkify
  {
    public async Task<int> ChunkifyAsync(Stream input, Stream output, CancellationToken ct)
    {
      // Copy the PDF from the input stream to a MemoryStream.
      // (iText does not read directly from a non-seekable stream.)
      using var memoryStream = new MemoryStream();
      await input.CopyToAsync(memoryStream);
      memoryStream.Seek(0, SeekOrigin.Begin);

      // Create the PdfReader and PdfDocument.
      using var pdfReader = new PdfReader(memoryStream);
      using var pdfDocument = new PdfDocument(pdfReader);

      // Prepare a list of strings to store the text from each page.
      var pagesText = new List<string>();

      // Iterate through each page in the PDF.
      int numberOfPages = pdfDocument.GetNumberOfPages();
      for (int i = 1; i <= numberOfPages; i++)
      {
        ct.ThrowIfCancellationRequested();

        // Extract text from the page.
        var page = pdfDocument.GetPage(i);
        var strategy = new SimpleTextExtractionStrategy();
        string pageText = PdfTextExtractor.GetTextFromPage(page, strategy);

        // Add the extracted text to the list.
        pagesText.Add(pageText.Trim());
      }

      await output.WriteAsync(BinaryData.FromObjectAsJson(pagesText));
      output.Seek(0, SeekOrigin.Begin);

      return pagesText.Count;
    }
  }
}
