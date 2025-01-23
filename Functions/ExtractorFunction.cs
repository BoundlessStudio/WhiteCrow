using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WhiteCrow.Models;
using WhiteCrow.Services;

namespace WhiteCrow.Functions;

// TODO: use singleR for realtime updates on the status

public class ExtractorFunction
{
  private readonly ILogger<ExtractorFunction> logger;

  public ExtractorFunction(ILogger<ExtractorFunction> logger)
  {
    this.logger = logger;
  }

  [Function(nameof(Extractor_Range))]
  public async Task<IActionResult> Extractor_Range(
   [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request,
   [BlobInput("extractor/{rand-guid}.json")] BlobClient client,
   CancellationToken ct
  )
  {
    var body = await request.ReadFromJsonAsync<ExtractorInputRange>();

    var count = body?.Max ?? 1;
    var list = Enumerable.Range(1, count).Select(i => $"{i}");

    var output = new MemoryStream();
    await output.WriteAsync(BinaryData.FromObjectAsJson(list));
    output.Seek(0, SeekOrigin.Begin);

    var containter = client.GetParentBlobContainerClient();
    await containter.CreateIfNotExistsAsync(cancellationToken: ct);
    await client.UploadAsync(output, ct);
    var url = client.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(12)).ToString();

    var response = new ExtractorModel()
    {
      Type = "range",
      Id = Path.GetFileNameWithoutExtension(client.Name),
      Name = "range.json",
      Url = url,
      Count = count,
      Size = output.Length,
    };
    return new OkObjectResult(response);
  }

  [Function(nameof(Extractor_List))]
  public async Task<IActionResult> Extractor_List(
   [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request,
   [BlobInput("extractor/{rand-guid}.json")] BlobClient client,
   CancellationToken ct
  )
  {
    var body = await request.ReadFromJsonAsync<ExtractorInputList>();

    var list = body?.Input?.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)?.ToList() ?? new List<string>();

    var output = new MemoryStream();
    await output.WriteAsync(BinaryData.FromObjectAsJson(list));
    output.Seek(0, SeekOrigin.Begin);

    var containter = client.GetParentBlobContainerClient();
    await containter.CreateIfNotExistsAsync(cancellationToken: ct);
    await client.UploadAsync(output, ct);
    var url = client.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(12)).ToString();

    var response = new ExtractorModel()
    {
      Type = "list",
      Id = Path.GetFileNameWithoutExtension(client.Name),
      Name = "list.json",
      Url = url,
      Count = list.Count,
      Size = output.Length
    };
    return new OkObjectResult(response);
  }

  [Function(nameof(Extractor_File))]
  public async Task<IActionResult> Extractor_File(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request,
    [BlobInput("extractor/{rand-guid}.json")] BlobClient client,
    CancellationToken ct
  )
  {
    try
    {
      var file = request.Form.Files.FirstOrDefault();
      if (file is null)
        return new BadRequestResult();

      var service = GetServiceForFile(file);
      if (service is null)
        return new BadRequestResult();

      var input = file.OpenReadStream();
      var output = new MemoryStream();
      var count = await service.ChunkifyAsync(input, output, ct);
      var containter = client.GetParentBlobContainerClient();
      await containter.CreateIfNotExistsAsync(cancellationToken: ct);
      await client.UploadAsync(output, ct);
      var url = client.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(12)).ToString();

      var response = new ExtractorModel()
      {
        Type = "file",
        Id = Path.GetFileNameWithoutExtension(client.Name),
        Name = Path.ChangeExtension(file.FileName, ".json"),
        Url = url,
        Count = count,
        Size = output.Length
      };
      return new OkObjectResult(response);
    }
    catch (OperationCanceledException)
    {
      return new StatusCodeResult(StatusCodes.Status499ClientClosedRequest);
    }
  }

  private IChunkify? GetServiceForFile(IFormFile file)
  {
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    switch (extension)
    {
      case ".csv":
      case ".txt":
      case ".rtf":
        return new TextService();
      case ".md":
        return new MarkdownService();
      case ".json":
        return new JsonService();
      case ".xls":
        return new ExcelxlsService();
      case ".xlsx":
        return new ExcelxlsxService();
      case ".docx":
        return new WorddocxService();
      case ".pdf":
        return new PdfService();
      case ".html":
        return new HtmlService();
      case ".ics":
        return new CalendarService();
      case ".xml":
      case ".zip":
      case ".sqlite3":
      case ".mdb":
      case ".py":
      default:
        this.logger.LogWarning($"The file extension of {extension} is not supported.");
        return null;
    }
  }
}
