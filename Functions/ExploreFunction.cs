using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using NPOI.SS.Formula.Functions;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using WhiteCrow.Models;

namespace WhiteCrow.Functions;

// TODO: use singleR for realtime updates on the status

public class ExploreFunction
{
  private readonly HttpClient webClient;
  private readonly JsonSerializerOptions jsonSerializerOptions;

  public ExploreFunction(HttpClient client)
  {
    this.webClient = client;
    this.jsonSerializerOptions = new JsonSerializerOptions
    {
      Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
  }

  [Function(nameof(Explore_Run))]
  public async Task<IActionResult> Explore_Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request,
    [BlobInput("explore/{rand-guid}.json")] BlobClient outputBlob,
    CancellationToken ct
  )
  {
    try
    {
      var body = await request.ReadFromJsonAsync<ExploreInput>(ct);
      if (body is null)
        return new BadRequestResult();

      var key = Environment.GetEnvironmentVariable("SCRAPING_BEE_API_KEY");
      var url = HttpUtility.UrlEncode(body.Url);

      var output = string.Empty;
      switch(body.Extract)
      {
        case 1:
          output = "ai_query=" + HttpUtility.UrlEncode(body.Query);
          break;
        case 0:
          switch (body.Template)
          {
            case 5: // Images
              output = "extract_rules=" + HttpUtility.UrlEncode("""{"data":{"selector":"img@src","type": "list"}}""");
              break;
            case 4: // Phones
              output = "extract_rules=" + HttpUtility.UrlEncode("""{"data":{"selector":"a[href^='tel:']@href","type": "list"}}""");
              break;
            case 3: // Emails
              output = "extract_rules=" + HttpUtility.UrlEncode("""{"data":{"selector":"a[href^='mailto']@href","type": "list"}}""");
              break;
            case 2: // Headings
              output = "extract_rules=" + HttpUtility.UrlEncode("""{"data":{"selector":"h1","type": "list"}}""");
              break;
            case 1: // Links
              output = "extract_rules=" + HttpUtility.UrlEncode("""{"data":{"selector":"a@href","type": "list"}}""");
              break;
            case 0: // Tables
            default:
              output = "extract_rules=" + HttpUtility.UrlEncode("""{"data":{"selector":"table","output": "table_json"}}""");
              break;
          }
          break;
        default:
          return new BadRequestResult();
      }

      var render = string.Empty;
      switch (body.Javascript)
      {
        case 2:
          render = "js_scenario=";
          break;
        case 1:
          render = "block_ads=true";
          break;
        case 0:
          render = "render_js=false";
          break;
        default:
          return new BadRequestResult();
      }

      var document = await this.webClient.GetStringAsync($"https://app.scrapingbee.com/api/v1?api_key={key}&url={url}&{render}&{output}", cancellationToken: ct);
      BinaryData data;
      switch (body.Extract)
      {
        case 2:
          // Treat as JSON Document
          data = BinaryData.FromString(document);
          break;
        case 1:
          // Treat as Markdown with JSON array
          var json = ExtractJsonFromMarkdown(document);
          data = BinaryData.FromString(json);
          break;
        case 0:
          // Extract data paramter's array
          var doc = JsonDocument.Parse(document);
          doc.RootElement.TryGetProperty("data", out JsonElement dataElement);
          data = BinaryData.FromObjectAsJson(dataElement, jsonSerializerOptions);
          break;
        default:
          throw new ApplicationException("Invalid Extract Case");
      }

      var outputContainer = outputBlob.GetParentBlobContainerClient();
      await outputContainer.CreateIfNotExistsAsync(cancellationToken: ct);
      await outputBlob.UploadAsync(data, ct);
      var uri = outputBlob.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(12));

      var model = new EnumeratorModel()
      {
        Url = uri.ToString(),
      };
      return new OkObjectResult(model);
    }
    catch (OperationCanceledException ex)
    {
      return new StatusCodeResult(StatusCodes.Status499ClientClosedRequest);
    }
  }

  static string ExtractJsonFromMarkdown(string markdown)
  {
    // Regular expression to match the JSON block within markdown
    string pattern = "```json\\s*(.*?)\\s*```";
    var match = Regex.Match(markdown, pattern, RegexOptions.Singleline);

    if (match.Success)
    {
      // Return the captured group containing JSON content
      return match.Groups[1].Value.Trim();
    }

    return string.Empty; // Return empty if no match is found
  }
}